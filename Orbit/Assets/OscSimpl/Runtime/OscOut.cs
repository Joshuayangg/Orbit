/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using OscSimpl;

/// <summary>
/// MonoBehaviour for sending OscPackets. The "OSC Client".
/// </summary>
public class OscOut : OscMonoBase
{
	[SerializeField] int _port = 8000;
	[SerializeField] OscSendMode _mode = OscSendMode.UnicastToSelf;
	[SerializeField,FormerlySerializedAs( "_ipAddress" )] string _remoteIpAddress = IPAddress.Loopback.ToString(); // 127.0.0.1;
	[SerializeField] OscRemoteStatus _remoteStatus = OscRemoteStatus.Unknown;
	[SerializeField] bool _bundleMessagesAutomatically = true;

	UdpSender _sender;

	byte[] _sendBuffer;

	bool _wasClosedOnDisable; // Flag to indicate if we should open UDPTransmitter OnEnable.
	
	int _messageCountThisFrame;
	int _byteCountThisFrame;

	IEnumerator _pingCoroutine;
	WaitForSeconds _pingYield = new WaitForSeconds( _pingInterval );

	OscTimeTag _bundleTimeTag = new OscTimeTag();
	SerializedOscMessageBuffer _autoBundleMessageBuffer;
	Queue<OscMessage> _userBundleTempMessages = new Queue<OscMessage>();

	DateTime _dateTime;

	// For the inspector.
	#if UNITY_EDITOR
	[SerializeField] bool _settingsFoldout;
	[SerializeField] bool _messagesFoldout;
	#endif
	
	static readonly string logPrepend = "<b>[" + nameof( OscOut ) + "]</b> ";
	const float _pingInterval = 1f; // Seconds

	/// <summary>
	/// Gets the port to be send to on the target remote device (read only).
	/// To set, call the Open method.
	/// </summary>
	public int port { get { return _port; } }

	/// <summary>
	/// Gets the transmission mode (read only). Can either be UnicastToSelf, Unicast, Broadcast or Multicast.
	/// The mode is automatically derived from the IP address passed to the Open method.
	/// </summary>
	public OscSendMode mode { get { return _mode; } }

	/// <summary>
	/// Gets the IP address of the target remote device (read only). To set, call the 'Open' method.
	/// </summary>
	public string remoteIpAddress { get { return _remoteIpAddress; } }

	/// <summary>
	/// Indicates whether the Open method has been called and the object is ready to send.
	/// </summary>
	public bool isOpen { get { return _sender != null; } }

	/// <summary>
	/// Gets the remote connection status (read only). Can either be Connected, Disconnected or Unknown.
	/// </summary>
	public OscRemoteStatus remoteStatus { get { return _remoteStatus; } }

	/// <summary>
	/// Gets or sets the size of the UDP buffer.
	/// </summary>
	public int udpBufferSize
	{
		get { return _udpBufferSize; }
		set {
			int newBufferSize = Mathf.Clamp( value, OscConst.udpBufferSizeMin, OscConst.udpBufferSizeMax );
			if( newBufferSize != _udpBufferSize ) {
				_udpBufferSize = newBufferSize;
				if( _sender != null ) _sender.bufferSize = _udpBufferSize;
			}
		}
	}

	/// <summary>
	/// Set to false ONLY if the receiving end does not support OSC bundles. Without bundling, messages that are send successively are prone to be lost.
	/// By default, messages are wrapped in a bundle (or multiple bundles if the buffer size is exceeded) that is send by the end of the Unity frame.
	/// </summary>
	public bool bundleMessagesAutomatically
	{
		get { return _bundleMessagesAutomatically; }
		set { _bundleMessagesAutomatically = value; }
	}

	/// <summary>
	/// Gets the number of messages send last update.
	/// </summary>
	public int messageCountSendLastFrame { get { return _messageCountLastFrame; } }

	/// <summary>
	/// Gets the number of bytes send last update.
	/// </summary>
	public int byteCountSendLastFrame { get { return _byteCountLastFrame; } }


	void Awake()
	{
		_autoBundleMessageBuffer = new SerializedOscMessageBuffer( _udpBufferSize );
		if( enabled && Application.isPlaying && _openOnAwake ) Open( _port, _remoteIpAddress );
	}


	// OnEnable is only called when Application.isPlaying.
	void OnEnable()
	{
		if( !isOpen && _wasClosedOnDisable ) Open( _port, _remoteIpAddress );
	}


	// OnEnable is only called when Application.isPlaying.
	void OnDisable()
	{
		if( isOpen ) {
			Close();
			_wasClosedOnDisable = true;
		}
		_remoteStatus = OscRemoteStatus.Unknown;
	}


	void Update()
	{

		// Since DateTime.Now is slow, we just create it once and update it with unity time.
		if( _dateTime.Ticks == 0 ) _dateTime = DateTime.Now;
		else _dateTime = _dateTime.AddSeconds( Time.deltaTime );
	}


	void LateUpdate()
	{
		SendAutoBundle();

		// Update counters.
		// OscOut is set to 5000 in ScriptExecutionOrder, 
		// so we can assume that no more messages will be send this frame.
		_messageCountLastFrame = _messageCountThisFrame;
		_byteCountLastFrame = _byteCountThisFrame;
		_messageCountThisFrame = 0;
		_byteCountThisFrame = 0;
	}


	void OnDestroy()
	{
		if( isOpen ) Close();
	}


	/// <summary>
	/// Open to send messages to specified port and (optional) IP address.
	/// If no IP address is given, messages will be send locally on this device.
	/// Returns success status.
	/// </summary>
	public bool Open( int port, string remoteIpAddress = "" )
	{
		// Validate IP.
		IPAddress ip;
		if( string.IsNullOrEmpty( remoteIpAddress ) ) remoteIpAddress = IPAddress.Loopback.ToString();
		if( remoteIpAddress == IPAddress.Any.ToString() || !IPAddress.TryParse( remoteIpAddress, out ip ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0}Open failed. Invalid IP address {1}\n", logPrepend, remoteIpAddress ) );
			return false;
		}
		if( ip.AddressFamily != AddressFamily.InterNetwork ) {
			if( OscGlobals.logWarnings ) {
				Debug.LogWarning( string.Format( "{0}Open failed. Only IPv4 addresses are supported. {1} is {2}\n", logPrepend, remoteIpAddress, ip.AddressFamily ) );
			}
			return false;
		}
		_remoteIpAddress = remoteIpAddress;

		// Detect and set transmission mode.
		if( _remoteIpAddress == IPAddress.Loopback.ToString() ){
			_mode = OscSendMode.UnicastToSelf;
		} else if( _remoteIpAddress == IPAddress.Broadcast.ToString() ){
			_mode = OscSendMode.Broadcast;
		} else if( Regex.IsMatch( _remoteIpAddress, OscConst.multicastAddressPattern ) ){
			_mode = OscSendMode.Multicast;
		} else {
			_mode = OscSendMode.Unicast;
		}

		// Validate port number range
		if( port < OscConst.portMin || port > OscConst.portMax ){
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.Append( "Open failed. Port " ); sb.Append( port ); sb.Append( " is out of range.\n" );
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}
		_port = port;

		// Create new client and end point.
		if( _sender == null ) _sender = new UdpSender( _udpBufferSize );
		_sender.SetRemoteTarget( _port, ip );

		// Handle pinging
		if( Application.isPlaying )
		{
			_remoteStatus = _mode == OscSendMode.UnicastToSelf ? OscRemoteStatus.Connected : OscRemoteStatus.Unknown;
			if( _mode == OscSendMode.Unicast ){
				_pingCoroutine = PingCoroutine();
				StartCoroutine( _pingCoroutine );
			}
		}

		// Log
		if( Application.isPlaying ){
			string addressTypeString = string.Empty;
			switch( _mode ){
			case OscSendMode.Broadcast: addressTypeString = "broadcast"; break;
			case OscSendMode.Multicast: addressTypeString = "multicast"; break;
			case OscSendMode.Unicast: addressTypeString = "IP"; break;
			case OscSendMode.UnicastToSelf: addressTypeString = "local"; break;
			}

			if( OscGlobals.logStatuses ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.Append( "Ready to send to " ); sb.Append( addressTypeString );
				sb.Append( " address " ); sb.Append( _remoteIpAddress );
				sb.Append( " on port " ); sb.Append( port ); sb.Append( ".\n" );
				Debug.Log( sb.ToString() );
			}
		}

		return true;
	}


	/// <summary>
	/// Close and stop sending messages.
	/// </summary>
	public void Close()
	{
		if( _pingCoroutine != null ){
			StopCoroutine( _pingCoroutine );
			_pingCoroutine = null;
		}
		_remoteStatus = OscRemoteStatus.Unknown;
		_sender = null;
		_wasClosedOnDisable = false;
	}


	/// <summary>
	/// Send an OscMessage or OscBundle. Data is serialized and no reference is stored, so you can safely 
	/// change values and send packet immediately again.
	/// Returns success status. 
	/// </summary>
	public bool Send( OscPacket packet )
	{
		if( !isOpen ) return false;
		int index = 0;

		// On any message.
		if( _onAnyMessage != null ) InvokeAnyMessageEventRecursively( packet );

		// Adapt buffer size.
		if( _sendBuffer == null || _sendBuffer.Length != _udpBufferSize ) _sendBuffer = new byte[ _udpBufferSize ];

		// Handle user messages.
		if( packet is OscMessage ){
			if( _bundleMessagesAutomatically ) {
				// Collect to be bundled and send by end of the Unity frame.
				_autoBundleMessageBuffer.Add( packet as OscMessage );
				return true; // Assume success.
			} else {
				// Add to cache and send immediately.
				OscMessage message = packet as OscMessage;
				message.TryWriteTo( _sendBuffer, ref index );
				bool success = TrySendBuffer( index );
				if( success ) _messageCountThisFrame++;
				return success;
			}
		}

		// Handle user bundles. Bundles provided by the user are send immediately. If too big, they are split into more bundles.
		OscBundle bundle = packet as OscBundle;
		if( bundle.Size() > _udpBufferSize ){
			ExtractMessages( packet, _userBundleTempMessages );
			int bundleByteCount = OscConst.bundleHeaderSize;
			OscBundle splitBundle = OscPool.GetBundle();
			while( _userBundleTempMessages.Count > 0 )
			{
				OscMessage message = _userBundleTempMessages.Dequeue();
				// Check if message is too big.
				int messageSize = message.Size() + FourByteOscData.byteCount; // Bundle stores size of each message in a 4 byte integer.
				if( messageSize > _udpBufferSize ){
					if( OscGlobals.logWarnings ) {
						StringBuilder sb = OscDebug.BuildText( this );
						sb.Append( "Failed to send message. Message size at " ); sb.Append( messageSize );
						sb.Append( " bytes exceeds udp buffer size at " ); sb.Append( _udpBufferSize );
						sb.Append( " bytes. Try increasing the buffer size.'\n" );
						Debug.LogWarning( sb.ToString() );
					}
					return false;
				}
				// If bundle is full, send it and prepare for new bundle.
				if( bundleByteCount + messageSize > _udpBufferSize ) { 
					if( !Send( splitBundle ) ) return false;
					bundleByteCount = OscConst.bundleHeaderSize;
					splitBundle.Clear();
				}
				splitBundle.packets.Add( message );
				bundleByteCount += messageSize;
			}
			if( splitBundle.packets.Count > 0 && !Send( splitBundle ) ) return false;
			OscPool.Recycle( splitBundle );
			return true;
		}

		// Try to pack the message.
		if( !bundle.TryWriteTo( _sendBuffer, ref index ) ) return false;
		_messageCountThisFrame += bundle.packets.Count;

		// Send data!
		return TrySendBuffer( index );
	}


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, float value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, double value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, int value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }
	
	
	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, long value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, string value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }

	
	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, char value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, bool value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, Color32 value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }
	

	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, byte[] value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, OscTimeTag value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, OscMidiMessage value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, OscNull value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with a single argument.
	/// </summary>
	public void Send( string address, OscImpulse value ){ SendPooled( OscPool.GetMessage( address ).Add( value ) ); }


	/// <summary>
	/// Send an OscMessage with no arguments.
	/// </summary>
	public void Send( string address ){ SendPooled( OscPool.GetMessage( address ) ); }


	/// <summary>
	/// Subscribe to all outgoing messages.
	/// </summary>
	public void MapAnyMessage( Action<OscMessage> method )
	{
		_onAnyMessage += method;
		//Debug.Log( "MapAnyMessage " + method + " " + _onAnyMessage.GetPersistentEventCount() );
	}


	/// <summary>
	/// Unsubscribe to all outgoing messages.
	/// </summary>
	public void UnmapAnyMessage( Action<OscMessage> method )
	{
		if( _onAnyMessage != null ) _onAnyMessage -= method;
		//Debug.Log( "UnmapAnyMessage " + method + " " + _onAnyMessage.GetPersistentEventCount() );
	}


	void SendPooled( OscMessage message )
	{
		if( isOpen ) Send( message );

		if( _onAnyMessage == null ) OscPool.Recycle( message );
	}


	static void ExtractMessages( OscPacket packet, Queue<OscMessage> list )
	{
		if( packet is OscMessage ) {
			list.Enqueue( packet as OscMessage );
		} else {
			OscBundle bundle = packet as OscBundle;
			foreach( OscPacket subPacket in bundle.packets ) ExtractMessages( subPacket, list );
		}
	}


	void InvokeAnyMessageEventRecursively( OscPacket packet )
	{
		if( packet is OscBundle ){
			OscBundle bundle = packet as OscBundle;
			foreach( OscPacket subPacket in bundle.packets ) InvokeAnyMessageEventRecursively( subPacket );
		} else {
			OscMessage message = packet as OscMessage;
			_onAnyMessage.Invoke( message );

			_messageCountLastFrame++;
		}
	}


	bool TrySendBuffer( int byteCount )
	{
		if( _sender == null ) return false;

		bool success = _sender.TrySendBuffer( _sendBuffer, byteCount );

		if( success ) _byteCountThisFrame += byteCount;

		return success;
	}


	void SendAutoBundle()
	{
		if( _autoBundleMessageBuffer.count == 0 ) return;

		// Prepare for composing bundle.
		_bundleTimeTag.time = _dateTime;
		int sendBufferByteIndex = 0;
		OscBundle.TryWriteHeader( _bundleTimeTag, _sendBuffer, ref sendBufferByteIndex );

		// Loop through serialized messages in the end-of-frame-buffer.
		int messageBufferByteIndex = 0;
		for( int m = 0; m < _autoBundleMessageBuffer.count; m++ )
		{
			// Get message size.
			int messageSize = _autoBundleMessageBuffer.GetSize( m ); // Bundle stores size of each message in a 4 byte integer.

			// Check limit.
			if( sendBufferByteIndex + FourByteOscData.byteCount + messageSize >= _sendBuffer.Length )
			{
				// We have reached the safety limit, now send the bundle.
				TrySendBuffer( sendBufferByteIndex );
				
				// Get ready for composing next bundle.
				sendBufferByteIndex = OscConst.bundleHeaderSize;
			}

			// Write message size and data.
			new FourByteOscData( messageSize ).TryWriteTo( _sendBuffer, ref sendBufferByteIndex );
			Array.Copy( _autoBundleMessageBuffer.data, messageBufferByteIndex, _sendBuffer, sendBufferByteIndex, messageSize );
			messageBufferByteIndex += messageSize;
			sendBufferByteIndex += messageSize;
		}

		// Send bundle if there is anything in it and clean.
		if( sendBufferByteIndex > OscConst.bundleHeaderSize ) TrySendBuffer( sendBufferByteIndex );

		_messageCountThisFrame += _autoBundleMessageBuffer.count;
		_autoBundleMessageBuffer.Clear();
	}


	IEnumerator PingCoroutine()
	{
		while( true )
		{
			Ping ping = new Ping( _remoteIpAddress );
			yield return _pingYield;
			_remoteStatus = ( ping.isDone && ping.time >= 0 ) ? OscRemoteStatus.Connected : OscRemoteStatus.Disconnected;
		}
	}


	[Obsolete( "Use messageCountSendLastFrame instead." )]
	public int messageCount { get { return _messageCountLastFrame; } }

	[Obsolete( "Loopback mode no longer supported." )]
	public bool multicastLoopback {
		get { return false; }
		set { }
	}
}