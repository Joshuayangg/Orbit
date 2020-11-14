/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEngine.Events;
using System;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using OscSimpl;


/// <summary>
/// MonoBehaviour for receiving OscMessage objects. The "OSC Server".
/// </summary>
[ExecuteInEditMode]
public class OscIn : OscMonoBase
{
	[SerializeField] int _port = 7000;
	[SerializeField] OscReceiveMode _mode = OscReceiveMode.UnicastBroadcast;
	[SerializeField] string _multicastAddress = string.Empty;
	[SerializeField] bool _filterDuplicates = false;
	[SerializeField] bool _addTimeTagsToBundledMessages = false;
	[SerializeField] List<OscMapping> _mappings = new List<OscMapping>();

	UdpReceiver _receiver = null;
	object _lock = new object();
	int _lockedReceiveBufferByteCount;
	int _lockedReceiveBufferCount;
	int _safeReceiveBufferByteCount;
	byte[] _lockedReceiveBuffer;
	byte[] _safeReceiveBuffer;
	bool _isOpen;
	Queue<OscPacket> _packetQueue = new Queue<OscPacket>();
	HashSet<string> _uniqueAddresses = new HashSet<string>(); // For filtering duplicates. HashSet is faster than List for more than 6 items https://stackoverflow.com/questions/150750/hashset-vs-list-performance
	HashSet<string> _mappedAddresses = new HashSet<string>();
	Dictionary<string,OscMapping> _regularMappings = new Dictionary<string,OscMapping>();
	List<OscMapping> _specialPatternMappings = new List<OscMapping>();
	bool _wasClosedOnDisable;
	bool _dirtyMappings = true;

	// For the inspector
#if UNITY_EDITOR
	[SerializeField] bool _settingsFoldout;
	[SerializeField] bool _mappingsFoldout;
	[SerializeField] bool _messagesFoldout;
#endif


	/// <summary>
	/// Gets the local port that this application is set to listen to. (read only).
	/// To set, call the Open method.
	/// </summary>
	public int port { get { return _port; } }

	/// <summary>
	/// Gets the transmission mode (read only). Can either be UnicastBroadcast or Multicast.
	/// The mode is automatically derived from arguments passed to the Open method.
	/// </summary>
	public OscReceiveMode mode { get { return _mode; } }

	/// <summary>
	/// Gets the remote address to the multicast group that this application is set to listen to (read only).
	/// To set, call the Open method and provide a valid multicast address.
	/// </summary>
	public string multicastAddress { get { return _multicastAddress; } }

	/// <summary>
	/// Gets the primary local network IP address for this device (read only).
	/// If the the loopback address "127.0.0.1" is returned ensure that your device is connected to a network. 
	/// Using a VPN may block you from getting the local IP.
	/// </summary>
	public static string localIpAddress { get { return OscHelper.GetLocalIpAddress(); } }

	/// <summary>
	/// Gets the alternative local network IP addresses for this device (read only).
	/// Your device may be connected through multiple network adapters, for example through wifi and ethernet.
	/// </summary>
	public static ReadOnlyCollection<string> localIpAddressAlternatives { get { return OscHelper.GetLocalIpAddressAlternatives(); } }

	/// <summary>
	/// Indicates whether the Open method has been called and the object is ready to receive.
	/// </summary>
	public bool isOpen { get { return _receiver != null && _receiver.isOpen; } }

	/// <summary>
	/// When enabled, only one message per OSC address will be forwarded every Update call.
	/// The last (newest) message received will be used. Default is true.
	/// </summary>
	public bool filterDuplicates {
		get { return _filterDuplicates; }
		set { _filterDuplicates = value; }
	}

	/// <summary>
	/// When enabled, timetags from bundles are added to contained messages as last argument.
	/// Incoming bundles are never exposed, so if you want to access a time tag from a incoming bundle then enable this.
	/// Default is false.
	/// </summary>
	public bool addTimeTagsToBundledMessages {
		get { lock( _lock ) return _addTimeTagsToBundledMessages; }
		set { lock( _lock ) _addTimeTagsToBundledMessages = value; }
	}

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
				if( isOpen ) Open( _port, _multicastAddress );
			}
		}
	}

	/// <summary>
	/// Gets the number of messages received last update.
	/// </summary>
	/// 
	public int messageCountReceivedLastFrame { get { lock( _lock ) return _messageCountLastFrame; } }

	/// <summary>
	/// Gets the number of bytes received last update.
	/// </summary>
	public int byteCountReceivedLastFrame { get { return _byteCountLastFrame; } }


	void Awake()
	{
		if( !Application.isPlaying ) return;

		if( _openOnAwake && enabled && !isOpen ) {
			Open( _port, _multicastAddress );
		}

		_dirtyMappings = true;
	}


	void OnEnable()
	{
		if( !Application.isPlaying ) return;

		if( _wasClosedOnDisable && !isOpen ) {
			Open( _port, _multicastAddress );
		}
	}


	void OnDisable()
	{
		if( !Application.isPlaying ) return;

		if( isOpen ) {
			Close();
			_wasClosedOnDisable = true;
		}
	}


	void Update()
	{
		_messageCountLastFrame = 0;
		_byteCountLastFrame = 0;

		if( _mappings == null || !isOpen ) return;

		// As little work as possible while we touch the locked buffers.
		lock( _lock )
		{
			if( _lockedReceiveBuffer != null ) {
				if( _safeReceiveBuffer == null || _safeReceiveBuffer.Length < _lockedReceiveBuffer.Length ) _safeReceiveBuffer = new byte[ _lockedReceiveBuffer.Length ];
				if( _lockedReceiveBufferCount > 0 ) {
					Array.Copy( _lockedReceiveBuffer, 0, _safeReceiveBuffer, 0, _lockedReceiveBufferByteCount );
				}
			}
			// Update and reset counters.
			_safeReceiveBufferByteCount = _lockedReceiveBufferByteCount; // Including zero padding.
			_byteCountLastFrame = _lockedReceiveBufferCount == 0 ? 0 : _safeReceiveBufferByteCount - (4*(_lockedReceiveBufferCount-1) ); // Excluding zero padding.
			_lockedReceiveBufferByteCount = 0;
			_lockedReceiveBufferCount = 0;
		}

		// If no data, job done.
		if( _safeReceiveBufferByteCount == 0 ) return; 

		// Parse packets and put them in a queue.
		int index = 0;
		while( index < _safeReceiveBufferByteCount ) {
			OscPacket packet;
			if( OscPacket.TryReadFrom( _safeReceiveBuffer, ref index, _safeReceiveBufferByteCount, out packet ) ) _packetQueue.Enqueue( packet );
		}

		// If no messages, job done.
		if( _packetQueue.Count == 0 ) return;

		// Update mappings.
		if( _dirtyMappings ) UpdateMappings();

		// Dispatch messages.
		while( _packetQueue.Count > 0 ) UnpackRecursivelyAndDispatch( _packetQueue.Dequeue() );
		if( _filterDuplicates ) _uniqueAddresses.Clear();
	}


	void OnDestroy()
	{
		if( !Application.isPlaying ) return;

		if( isOpen ) Close();

		// Forget all mappings.
		_onAnyMessage = null;
	}


	void UnpackRecursivelyAndDispatch( OscPacket packet )
	{
		if( packet is OscBundle ) {
			OscBundle bundle = packet as OscBundle;
			foreach( OscPacket subPacket in bundle.packets ) {
				if( _addTimeTagsToBundledMessages && subPacket is OscMessage ) {
					OscMessage message = subPacket as OscMessage;
					message.Add( bundle.timeTag );
				}
				UnpackRecursivelyAndDispatch( subPacket );
			}
			OscPool.Recycle( bundle );
		} else {
			OscMessage message = packet as OscMessage;
			if( _filterDuplicates ) {
				if( _uniqueAddresses.Contains( message.address ) ) {
					OscPool.Recycle( message );
					return;
				}
				_uniqueAddresses.Add( message.address );
			}
			Dispatch( packet as OscMessage );
			_messageCountLastFrame++; // Influenced by _filterDuplicates.
		}
	}



	void Dispatch( OscMessage message )
	{
		bool anyMessageActivated = _onAnyMessage != null;
		bool messageExposed = anyMessageActivated;
		string address = message.address;

		// Regular mappings.
		if( _mappedAddresses.Contains( address ) ) {
			OscMapping mapping;
			if( _regularMappings.TryGetValue( address, out mapping ) ) {
				InvokeMapping( mapping, message );
				if( !messageExposed ) messageExposed = mapping.type == OscMessageType.OscMessage;
			}
		}

		// Special pattern mappings.
		foreach( OscMapping specialMapping in _specialPatternMappings ) {
			if( specialMapping.IsMatching( message.address ) ) {
				InvokeMapping( specialMapping, message );
				if( !messageExposed ) messageExposed = specialMapping.type == OscMessageType.OscMessage;
			}
		}

		// Any message handler.
		if( anyMessageActivated ) _onAnyMessage.Invoke( message );

		// Recycle when possible.
		if( !anyMessageActivated && !messageExposed ) OscPool.Recycle( message );
	}


	void InvokeMapping( OscMapping mapping, OscMessage message )
	{
		switch( mapping.type ) {
			case OscMessageType.OscMessage:
				mapping.Invoke( message );
				break;
			case OscMessageType.Float:
				float floatValue;
				if( message.TryGet( 0, out floatValue ) ){
					mapping.Invoke( floatValue );
					//Debug.Log( floatValue );
				}
				break;
			case OscMessageType.Double:
				double doubleValue;
				if( message.TryGet( 0, out doubleValue ) ) mapping.Invoke( doubleValue );
				break;
			case OscMessageType.Int:
				int intValue;
				if( message.TryGet( 0, out intValue ) ) mapping.Invoke( intValue );
				break;
			case OscMessageType.Long:
				long longValue;
				if( message.TryGet( 0, out longValue ) ) mapping.Invoke( longValue );
				break;
			case OscMessageType.String:
				string stringValue = string.Empty;
				if( message.TryGet( 0, ref stringValue ) ) mapping.Invoke( stringValue );
				break;
			case OscMessageType.Char:
				char charValue;
				if( message.TryGet( 0, out charValue ) ) mapping.Invoke( charValue );
				break;
			case OscMessageType.Bool:
				bool boolValue;
				if( message.TryGet( 0, out boolValue ) ) mapping.Invoke( boolValue );
				break;
			case OscMessageType.Color:
				Color32 colorValue;
				if( message.TryGet( 0, out colorValue ) ) mapping.Invoke( colorValue );
				break;
			case OscMessageType.Midi:
				OscMidiMessage midiValue;
				if( message.TryGet( 0, out midiValue ) ) mapping.Invoke( midiValue );
				break;
			case OscMessageType.Blob:
				byte[] blobValue = null;
				if( message.TryGet( 0, ref blobValue ) ) mapping.Invoke( blobValue );
				break;
			case OscMessageType.TimeTag:
				OscTimeTag timeTagValue;
				if( message.TryGet( 0, out timeTagValue ) ) mapping.Invoke( timeTagValue );
				break;
			case OscMessageType.ImpulseNullEmpty:
				mapping.Invoke();
				break;
		}
	}


	/// <summary>
	/// Open to receive messages on specified port and (optionally) from specified multicast IP address.
	/// Returns success status.
	/// </summary>
	public bool Open( int port, string multicastAddress = "" )
	{
		// Ensure that we have a receiver, even when not in Play mode.
		if( _receiver == null ) _receiver = new UdpReceiver( OnDataReceivedAsync );

		// Close and existing receiver.
		if( isOpen ) Close();

		// Validate port number range.
		if( port < OscConst.portMin || port > OscConst.portMax ) {
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.Append( "Open failed. Port " ); sb.Append( port ); sb.Append( " is out of range.\n" );
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}
		_port = port;

		// Derive mode from multicastAddress.
		IPAddress multicastIP;
		if( !string.IsNullOrEmpty( multicastAddress ) && IPAddress.TryParse( multicastAddress, out multicastIP ) ) {
			if( Regex.IsMatch( multicastAddress, OscConst.multicastAddressPattern ) ) {
				_mode = OscReceiveMode.UnicastBroadcastMulticast;
				_multicastAddress = multicastAddress;
			} else {
				if( OscGlobals.logWarnings ) {
					StringBuilder sb = OscDebug.BuildText( this );
					sb.Append( "Open failed. Multicast IP address " ); sb.Append( multicastAddress );
					sb.Append( " is out not valid. It must be in range 224.0.0.0 to 239.255.255.255.\n" );
					Debug.LogWarning( sb.ToString() );
				}
				return false;
			}
		} else {
			_multicastAddress = string.Empty;
			_mode = OscReceiveMode.UnicastBroadcast;
		}

		// Set buffer size.
		_receiver.bufferSize = _udpBufferSize;

		// Try open.
		if( !_receiver.Open( _port, _multicastAddress ) ) {
			if( OscGlobals.logStatuses ) Debug.Log( "Failed to open" );
			return false;
		}

		// Deal with the success
		if( Application.isPlaying ) {
			StringBuilder sb = OscDebug.BuildText( this );
			if( _mode == OscReceiveMode.UnicastBroadcast ) {
				sb.Append( "Ready to receive unicast and broadcast messages on port " );
			} else {
				sb.Append( "Ready to receive multicast messages on address " ); sb.Append( _multicastAddress ); sb.Append( ", unicast and broadcast messages on port " );
			}
			sb.Append( _port ); sb.AppendLine();
			if( OscGlobals.logStatuses ) Debug.Log( sb.ToString() );
		}

		return true;
	}


	// This is called by UdpReceiver independant of the unity update loop. Can happen at any time AND multiple times within one frame.
	void OnDataReceivedAsync( byte[] data, int byteCount )
	{
		// We want to do as little work as possible here so that UdpReceiver can continue it's work.
		lock( _lock )
		{
			// Ensure we have a buffer.
			if( _lockedReceiveBuffer == null ) _lockedReceiveBuffer = new byte[ _udpBufferSize ];

			// If this is not the first buffer to be received within this update, then pad with zeros.
			if( _lockedReceiveBufferCount != 0 ) for( int i = 0; i < 4; i++ ) _lockedReceiveBuffer[ _lockedReceiveBufferByteCount++ ] = 0;

			// Adapt buffer size.
			int requiredByteCount = _lockedReceiveBufferByteCount + byteCount + 4; // Space for 4 trailing zeros.
			if( requiredByteCount > _lockedReceiveBuffer.Length ) {
				byte[] newBuffer = new byte[ requiredByteCount ];
				Array.Copy( _lockedReceiveBuffer, 0, newBuffer, 0, _lockedReceiveBufferByteCount );
				_lockedReceiveBuffer = newBuffer;
			}

			// Copy.
			Array.Copy( data, 0, _lockedReceiveBuffer, _lockedReceiveBufferByteCount, byteCount );

			// Update count.
			_lockedReceiveBufferByteCount += byteCount;

			// Count.
			_lockedReceiveBufferCount++;
		}
	}


	/// <summary>
	/// Close and stop receiving messages.
	/// </summary>
	public void Close()
	{
		if( _receiver != null ) _receiver.Close();

		lock( _lock ) {
			//_lockedBufferList.Clear();
			//_lockedBufferCount = 0;
			_lockedReceiveBufferByteCount = 0;
		}

		_wasClosedOnDisable = false;
	}


	// NOTE
	// This is the syntex I would prefer, but it gives the following error:
	//  CS0121: The call is ambiguous between the following methods or properties: `OscIn.Map(string,Action<float>)' and `OscIn.Map(string,Action<int>)'
	// There is no elegant solution to this, because both Action<float> and Action<int> are passed as method groups as cast inpliccitely.
	// https://stackoverflow.com/questions/24011887/ambiguous-method-call-with-actiont-parameter-overload
	/*
	public void Map( string address, Action<float> method ) { Map( address, method, OscMessageType.Float ); }
	public void Map( string address, Action<int> method ) { Map( address, method, OscMessageType.Double ); }
	void TestOnFloat( float value ) { }
	void TestMap(){
		Map( "/", TestOnFloat ); // Error here.
	}
	*/
	

	/// <summary>
	/// Request that incoming messages with 'address' are forwarded to 'method'.
	/// </summary>
	public void Map( string address, Action<OscMessage> method ) { Map( address, method, OscMessageType.OscMessage ); }

	/// <summary>
	/// Request that a float type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapFloat( string address, Action<float> method ) { Map( address, method, OscMessageType.Float ); }

	/// <summary>
	/// Request that a double type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapDouble( string address, Action<double> method ) { Map( address, method, OscMessageType.Double ); }

	/// <summary>
	/// Request that a int type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapInt( string address, Action<int> method ) { Map( address, method, OscMessageType.Int ); }

	/// <summary>
	/// Request that a long type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapLong( string address, Action<long> method ) { Map( address, method, OscMessageType.Long ); }

	/// <summary>
	/// Request that a string type argument is extracted from incoming messages with matching 'address' and		forwarded to 'method'.
	/// This method produces heap garbage. Use Map( string, UnityAction<OscMessage> ) instead and then use TryGet( int, ref string ) 
	/// to read into a cached string. See how in the Optimisations example.
	/// </summary>
	[Obsolete( "This method produces heap garbage. Use Map( string, UnityAction<OscMessage> ) instead and then use TryGet( int, ref string ) to read into a cached string. See how in the Optimisations example." )]
	public void MapString( string address, Action<string> method ) { Map( address, method, OscMessageType.String ); }

	/// <summary>
	/// Request that a char type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapChar( string address, Action<char> method ) { Map( address, method, OscMessageType.Char ); }

	/// <summary>
	/// Request that a bool type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapBool( string address, Action<bool> method ) { Map( address, method, OscMessageType.Bool ); }

	/// <summary>
	/// Request that a Color32 type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapColor( string address, Action<Color32> method ) { Map( address, method, OscMessageType.Color ); }

	/// <summary>
	/// Request that a byte blob argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// This method produces heap garbage. Use Map( string, UnityAction<OscMessage> ) instead and then use TryGet( int, ref byte[] ) 
	/// to read into a cached array. See how in the Optimisations example.
	/// </summary>
	[Obsolete( "This method produces heap garbage. Use Map( string, UnityAction<OscMessage> ) instead and use TryGet( int, ref byte[] ) to read into a cached array. See how in the Optimisations example." )]
	public void MapBlob( string address, Action<byte[]> method ) { Map( address, method, OscMessageType.Blob ); }

	/// <summary>
	/// Request that a time tag argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapTimeTag( string address, Action<OscTimeTag> method ) { Map( address, method, OscMessageType.TimeTag ); }

	/// <summary>
	/// Request that a OscMidiMessage type argument is extracted from incoming messages with matching 'address' and forwarded to 'method'.
	/// </summary>
	public void MapMidi( string address, Action<OscMidiMessage> method ) { Map( address, method, OscMessageType.Midi ); }


	/// <summary>
	/// Request that 'method' is invoked when a message with matching 'address' is received with type tag Impulse (i), Null (N) or simply without arguments.
	/// </summary>
	public void MapImpulseNullOrEmpty( string address, UnityAction method )
	{
		// Validate.
		if( !ValidateAddressForMapping( address ) || method == null || !ValidateMethodTarget( method.Target, address ) ) return;

		// Get or create mapping.
		OscMapping mapping = null;
		GetOrCreateMapping( address, OscMessageType.ImpulseNullEmpty, out mapping );

		// Add listener.
		mapping.Map( method.Target, method.Method );

		// Set dirty flag.
		_dirtyMappings = true;
	}


	void Map<T>( string address, Action<T> method, OscMessageType type )
	{
		// Validate.
		if( !ValidateAddressForMapping( address ) || method == null || !ValidateMethodTarget( method.Target, address ) ) return;

		// Get or create mapping.
		OscMapping mapping = null;
		GetOrCreateMapping( address, type, out mapping );

		// Add listener.
		mapping.Map( method.Target, method.Method );

		// Set dirty flag.
		_dirtyMappings = true;
	}



	string GetMethodLabel( object source, System.Reflection.MethodInfo methodInfo )
	{
		string simpleType = source.GetType().ToString();
		int dotIndex = simpleType.LastIndexOf('.')+1;
		simpleType = simpleType.Substring( dotIndex, simpleType.Length - dotIndex );
		return simpleType + "." + methodInfo.Name;
	}


	bool ValidateAddressForMapping( string address )
	{
		// Check for address prefix.
		if( address.Length < 2 || address[0] != OscConst.addressPrefix ){
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.AppendLine( "Ignored attempt to create mapping. OSC addresses must begin with slash '/'." );
				sb.Append( "\"" + address + "\"" );
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}

		// Check for whitespace.
		if( address.Contains(" ") ){
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.AppendLine( "Ignored attempt to create mapping. OSC addresses cannot contain whitespaces." );
				sb.Append( "\"" + address + "\"");
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}

		return true;
	}


	bool ValidateMethodTarget( object target, string address )
	{
		if( target == null ) {
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.AppendLine( "Ignored attempt to create mapping. Method cannot be null." );
				sb.Append( address );
				Debug.LogWarning( sb );
			}
			return false;
		}

		return true;
	}


	bool GetOrCreateMapping( string address, OscMessageType type, out OscMapping mapping )
	{
		mapping = _mappings.Find( m => m.address == address );
		if( mapping == null ){
			mapping = new OscMapping( address, type );
			//mapping = ScriptableObject.CreateInstance<OscMapping>();
			//mapping.Init( address, type );
			_mappings.Add( mapping );
		} else if( mapping.type != type ){
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.Append( "Failed to map address \"" ); sb.Append( address );
				sb.Append( "\" to method with argument type " ); sb.Append( type );
				sb.Append( ". \nAddress is already mapped to a method with argument type " ); sb.Append( mapping.type );
				sb.Append( ", either in the editor, or by a script. Only one type per address is allowed.\n" );
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}
		return true;
	}


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void Unmap( Action<OscMessage> method )	{ Unmap( method, OscMessageType.OscMessage ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapFloat( Action<float> method ){ Unmap( method, OscMessageType.Float ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapInt( Action<int> method ){ Unmap( method, OscMessageType.Int ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapString( Action<string> method ){ Unmap( method, OscMessageType.String ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapBool( Action<bool> method ){ Unmap( method, OscMessageType.Bool ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapColor( Action<Color32> method ){ Unmap( method, OscMessageType.Color ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapChar( Action<char> method ){ Unmap( method, OscMessageType.Char ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapDouble( Action<double> method ){ Unmap( method, OscMessageType.Double ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapLong( Action<long> method ){ Unmap( method, OscMessageType.Long ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapTimeTag( Action<OscTimeTag> method ){ Unmap( method, OscMessageType.TimeTag ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapMidi( Action<OscMidiMessage> method ) { Unmap( method, OscMessageType.Midi ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapBlob( Action<byte[]> method ){ Unmap( method, OscMessageType.Blob ); }


	/// <summary>
	/// Request that 'method' is no longer invoked by OscIn.
	/// </summary>
	public void UnmapImpulseNullOrEmpty( UnityAction method )
	{
		// UnityEvent is secret about whether we removed a runtime handler, so we have to iterate the whole array og mappings.
		for( int m=_mappings.Count-1; m>=0; m-- )
		{
			OscMapping mapping = _mappings[m];

			// If there are no methods mapped to the hanlder left, then remove mapping.
			mapping.Unmap( method.Target, method.Method );
			//if( mapping.ImpulseNullEmptyHandler.GetPersistentEventCount() == 0 ) _mappings.RemoveAt( m );

		}
		_dirtyMappings = true;
	}

	/// <summary>
	/// Request that all methods that are mapped to OSC 'address' will no longer be invoked.
	/// </summary>
	public void UnmapAll( string address )
	{
		OscMapping mapping = _mappings.Find( m => m.address == address );
		if( mapping != null ){
			//mapping.Clear();
			_mappings.Remove( mapping );
		}
	}


	/// <summary>
	/// Subscribe to all outgoing messages.
	/// The state of 'filterDuplicates' does apply.
	/// </summary>
	public void MapAnyMessage( Action<OscMessage> method )
	{
		_onAnyMessage += method;
	}


	/// <summary>
	/// Unsubscribe to all outgoing messages.
	/// </summary>
	public void UnmapAnyMessage( Action<OscMessage> method )
	{
		_onAnyMessage -= method;
	}


	void Unmap<T>( Action<T> method, OscMessageType type )
	{
		for( int m=_mappings.Count-1; m>=0; m-- )
		{
			OscMapping mapping = _mappings[m];

			mapping.Unmap( method.Target, method.Method );

			// If there are no methods mapped to the hanlder left, then remove mapping.
			if( mapping.entryCount == 0 ) _mappings.RemoveAt( m );
		}
		_dirtyMappings = true;
	}


	void UpdateMappings()
	{
		// Clear collections.
		_mappedAddresses.Clear();
		_regularMappings.Clear();
		_specialPatternMappings.Clear();

		// Add mappings.
		foreach( OscMapping mapping in _mappings )
		{
			mapping.SetDirty();
			if( mapping.hasSpecialPattern ){
				_specialPatternMappings.Add( mapping );
			} else {
				if( _mappedAddresses.Contains( mapping.address ) ) continue;
				_mappedAddresses.Add( mapping.address );
				_regularMappings.Add( mapping.address, mapping );
			}
		}

		// Update flag.
		_dirtyMappings = false;
	}

	[Obsolete( "Use messageCountReceivedLastFrame instead." )]
	public int messageCount { get { lock( _lock ) return _messageCountLastFrame; } }

	[Obsolete( "Use localIpAddress instead." )]
	public static string ipAddress { get { return localIpAddress; } }
}