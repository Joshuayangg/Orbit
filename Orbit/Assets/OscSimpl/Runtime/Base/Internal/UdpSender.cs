/*
	Copyright © Carl Emil Carlsen 2020
	http://cec.dk

	Alternative to UdpClient but only for sending. 
	https://referencesource.microsoft.com/#system/net/System/Net/Sockets/UDPClient.cs
*/

using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Text.RegularExpressions;

namespace OscSimpl
{
	public class UdpSender
	{
		Socket _socket;
		CachedIpEndPoint _endPoint;

		public int remotePort {
			get { return _endPoint == null ? 0 : _endPoint.Port; }
		}

		public IPAddress remoteIPAddress {
			get { return _endPoint == null ? IPAddress.Loopback : _endPoint.Address; }
		} 

		public bool multicastLoopback {
			get { return _socket.MulticastLoopback; }
			set { _socket.MulticastLoopback = value; }
		}

		public int bufferSize {
			get { return _socket.SendBufferSize; }
			set { _socket.SendBufferSize = value; }
		}

		//static readonly string logPrepend = "<b>[" + nameof( UdpSender ) + "]</b> ";


		public UdpSender( int bufferSize )
		{
			_socket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

			// Set time to live to max. I haven't observed any difference, but we better be safe.
			_socket.Ttl = OscConst.timeToLive;

			// If an outgoing packet happen to exceeds the MTU (Maximum Transfer Unit) then throw an error instead of fragmenting.
			_socket.DontFragment = true;

			// Set default buffer size.
			_socket.SendBufferSize = bufferSize;

			// Allow this socket to broadcast.
			_socket.EnableBroadcast = true;

			// Don't send multicast to self.
			// This only affects this socket (in the case we would also receive on it, which we don't).
			// Other sockets in the same or other applications will still receive the messages.
			// https://stackoverflow.com/questions/8802786/cant-turn-off-multicastloopback-on-udpclient#comment10984393_8802786
			_socket.MulticastLoopback = false;

			// Multicast senders do not need to join a multicast group, but we need to set a few options.
			// Set a time to live, indicating how many routers the messages is allowed to be forwarded by..
			_socket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, OscConst.timeToLive );
		}


		public void SetRemoteTarget( int port, IPAddress remoteIpAddress )
		{
			_endPoint = new CachedIpEndPoint( remoteIpAddress, port );

			if( remoteIpAddress.Equals( IPAddress.Broadcast ) ) {
				_socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1 );
			} else if( Regex.IsMatch( remoteIpAddress.ToString(), OscConst.multicastAddressPattern ) ) {
				_socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0 );
			} else {
				_socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0 );
			}
		}


		public bool TrySendBuffer( byte[] buffer, int byteCount )
		{
			if( _endPoint == null ) return false;

			try {

				//Debug.Log( $"Actually sending { string.Join( ",", _cache ) }" );
				//Debug.Log( "Sending bytes: " + byteCount + ". Frame: " + Time.frameCount + "\n");
				//Debug.Log( "Sending to " + _endPoint );
				
				// Send!!
				_socket.SendTo( buffer, byteCount, SocketFlags.None, _endPoint );

				// Socket error reference: https://msdn.microsoft.com/en-us/library/windows/desktop/ms740668(v=vs.85).aspx
			} catch( SocketException ex ) {
				if( ex.ErrorCode == 10051 ) { // "Network is unreachable"
											  // Ignore. We get this when broadcasting while having no access to a network.

				} else if( ex.ErrorCode == 10065 ) { // "No route to host"
													 // Ignore. We get this sometimes when unicasting.

				} else if( ex.ErrorCode == 10049 ) { // "The requested address is not valid in this context"
													 // Ignore. We get this when we broadcast and have no access to the local network. For example if we are using a VPN.

				} else if( ex.ErrorCode == 10061 ) { // "Connection refused"
													 // Ignore.

				} else if( ex.ErrorCode == 10064 ) { // "Host is down"
													 // Ignore. We get this when the remote target is not found.
				} else if( ex.ErrorCode == 10040 ) { // "Message too long"
					if( OscGlobals.logWarnings ) {
						Debug.LogWarning(
							OscDebug.BuildText( this ).Append( "Failed to send message. Packet size at " )
							.AppendGarbageFree( byteCount ).Append( " bytes exceeds udp buffer size at " )
							.AppendGarbageFree( _socket.SendBufferSize )
							.Append( " Try increasing the buffer size.\n" ).Append( ex )
						);
					}
				} else {
					if( OscGlobals.logWarnings ) {
						Debug.LogWarning(
							OscDebug.BuildText( this ).Append( "Failed to send message to " )
							.Append( _endPoint.Address ).Append( " on port " ).AppendGarbageFree( _endPoint.Port )
							.Append( ".\n" ).Append( ex )
						);
					}
				}
				return false;
			} catch( Exception ex ) {
				if( OscGlobals.logWarnings ) {
					Debug.LogWarning(
						OscDebug.BuildText( this ).Append( "Failed to send message to " )
						.Append( _endPoint.Address ).Append( " on port " ).AppendGarbageFree( _endPoint.Port )
						.Append( ".\n" ).Append( ex )
					);
				}
				return false;
			}

			return true;
		}


		~UdpSender()
		{
			_socket.Close();
			_socket.Dispose();
		}
	}
}