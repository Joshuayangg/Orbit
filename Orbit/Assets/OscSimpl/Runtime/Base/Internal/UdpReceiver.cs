/*
	Created by Carl Emil Carlsen.
	Copyright 2019 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk


	This is a class to replace UdpClient for receiving.
	The problem lies in UdpClient.EndReceive which creates
	a new array every time it's called, creating a ton of garbage:


	public byte[] EndReceive(IAsyncResult asyncResult, ref IPEndPoint remoteEP){ 
		if (m_CleanedUp){
			throw new ObjectDisposedException(this.GetType().FullName);
		}

		EndPoint tempRemoteEP;

		if ( m_Family == AddressFamily.InterNetwork ) { 
			tempRemoteEP = IPEndPoint.Any;
		} 
		else {
			tempRemoteEP = IPEndPoint.IPv6Any;
		}

		int received = Client.EndReceiveFrom(asyncResult,ref tempRemoteEP);
		remoteEP = (IPEndPoint)tempRemoteEP; 

		// because we don't return the actual length, we need to ensure the returned buffer
		// has the appropriate length. 

		if (received < MaxUDPSize) {
			byte[] newBuffer = new byte[received]; // OUCH!
			Buffer.BlockCopy(m_Buffer,0,newBuffer,0,received); 
			return newBuffer;
		} 
		return m_Buffer; 
	}

	http://reflector.webtropy.com/default.aspx/4@0/4@0/untmp/DEVDIV_TFS/Dev10/Releases/RTMRel/ndp/fx/src/Net/System/Net/Sockets/UDPClient@cs/1305376/UDPClient@cs
*/

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace OscSimpl
{
	public class UdpReceiver
	{
		int _port = 7000;
		bool _isOpen;
		string _multicastAddress;

		Socket _socket;
		byte[] _buffer;
		AsyncCallback _receiveCompleteCallback;
		EndPoint _tempRemoteEndPoint;
		Action<byte[], int> _onDatagramReceivedAsyncCallback;

		const int bufferSizeMin = 512;
		const int bufferSizeMax = 65507;

		static readonly string logPrepend = "<b>[" + nameof( UdpReceiver ) + "]</b> ";


		public int bufferSize
		{
			get { return _buffer.Length; }
			set {
				bool wasOpen = _isOpen;
				if( _isOpen ) Close();
				if( value < bufferSizeMin ) value = bufferSizeMin;
				else if( value > bufferSizeMax ) value = bufferSizeMax;
				_buffer = new byte[value];
				if( wasOpen ) Open( _port );
			}
		}

		public bool isOpen { get { return _isOpen; } }


		public UdpReceiver( Action<byte[], int> onDatagramReceivedAsyncCallback )
		{
			_onDatagramReceivedAsyncCallback = onDatagramReceivedAsyncCallback;

			_buffer = new byte[OscConst.udpBufferSizeDefault];
			_receiveCompleteCallback = new AsyncCallback( ReceiveComplete );
			_tempRemoteEndPoint = new IPEndPoint( IPAddress.Any, 0 );
		}


		public bool Open( int port, string multicastAddress = "" )
		{
			if( _isOpen ) Close();

			// "Do not attempt to reuse the Socket after closing".
			// https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.shutdown?view=netframework-4.7.2
			_socket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

			// Ensure that we can have multiple OscIn objects listening to the same port. Must be set before bind.
			// Note that only one OscIn object will receive the packet anyway: http://stackoverflow.com/questions/22810511/bind-multiple-listener-to-the-same-port
			_socket.ExclusiveAddressUse = false;
			_socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );

			_port = port;
			_multicastAddress = multicastAddress;

			try {
				// "Before calling BeginReceiveFrom, you must explicitly bind the Socket to a local endpoint using the Bind method,"
				// https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.beginreceivefrom?view=netframework-4.7.2
				IPEndPoint localEndPoint = new IPEndPoint( IPAddress.Any, _port );
				_socket.Bind( localEndPoint );

				// Join multicast group if in multicast mode.
				if( !string.IsNullOrEmpty( _multicastAddress ) ) {
					IPAddress multicastIp = IPAddress.Parse( _multicastAddress );
					if( multicastIp.AddressFamily == AddressFamily.InterNetwork ) {
						MulticastOption mcOpt = new MulticastOption( multicastIp );
						_socket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.AddMembership, mcOpt );
					} else {
						if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0}IvP6 not supported\n", logPrepend ) );
						return false;
					}
					_socket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, OscConst.timeToLive );
				}

				// Let's begin!
				BeginRecieve();

			} catch( Exception e ) {

				// Socket error reference: https://msdn.microsoft.com/en-us/library/windows/desktop/ms740668(v=vs.85).aspx

				if( e is SocketException && (e as SocketException).ErrorCode == 10048 ) { // "Address already in use"
					if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0}Could not open port {1} because another application is listening on it.\n", logPrepend, _port ) );

				} else if( e is SocketException && !string.IsNullOrEmpty( multicastAddress ) ) {
					if( OscGlobals.logWarnings ){
						Debug.LogWarning( string.Format(
							"{0}Could not subscribe to multicast group. Perhaps you are offline, or your router does not support multicast. Error Code {1}\n{2}", 
							logPrepend, ( e as SocketException ).ErrorCode, e
						) );
					}

				} else if( e.Data is ArgumentOutOfRangeException ) {
					if( OscGlobals.logWarnings ) {
						Debug.LogWarning( string.Format( "{0}Could not open port {1}. Invalid Port Number.\n", logPrepend, _port ) );
					}

				} else {
					//Debug.Log( e );
				}

				Close();
				return false;
			}

			_isOpen = true;
			return true;
		}


		public void Close()
		{
			//Debug.Log( "CLOSE" );
			if( _socket != null ) {
				// Drop multicast group.
				if( !string.IsNullOrEmpty( _multicastAddress ) ) {
					IPAddress multicastIp = IPAddress.Parse( _multicastAddress );
					try {
						if( multicastIp.AddressFamily == AddressFamily.InterNetwork ) {
							MulticastOption mcOpt = new MulticastOption( multicastIp );
							_socket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.DropMembership, mcOpt );
						} else {
							if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0}IvP6 not supported\n", logPrepend ) );
						}
					} catch {
						// Ignore.
					}
				}

				// Close.
				_socket.Close();
				_socket.Dispose();
			}

			_socket = null;
			_isOpen = false;
		}


		void BeginRecieve()
		{
			_socket.BeginReceiveFrom( _buffer, 0, _buffer.Length, SocketFlags.None, ref _tempRemoteEndPoint, _receiveCompleteCallback, _socket );
		}


		void ReceiveComplete( IAsyncResult asyncResult )
		{
			if( _socket == null ) return;


			try {
				// Get the data and copy it to another buffer for exposure.
				int byteCount = _socket.EndReceiveFrom( asyncResult, ref _tempRemoteEndPoint );

				// Invoke callback.
				if( byteCount > 0 ) _onDatagramReceivedAsyncCallback( _buffer, byteCount );

				// Immediately start receving again.
				if( _isOpen ) BeginRecieve();

			} catch( Exception e ) {
				if( e is ObjectDisposedException ) {
					// Ignore.
				} else if( e is SocketException && ( e as SocketException ).ErrorCode == 10040 ){
					// A message sent on a datagram socket was larger than the internal message buffer or some other network limit, or the buffer used to receive a datagram into was smaller than the datagram itself.
					if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0}Your buffer size is too small to receive incoming packet. Try increasing it in the <b>OscIn</b> inspector settings section.\n{1}", logPrepend, e ) );
				} else {
					if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0}Error occurred while receiving message.\n{1}", logPrepend, e ) );
				}
			}
		}
	}
}