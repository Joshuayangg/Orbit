/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2018 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using OscSimpl;

/// <summary>
/// Class representing an OSC bundle. Bundles have a OscTimeTag and can contain OscMessage
/// and OscBundle objects.
/// </summary>
public sealed class OscBundle : OscPacket
{
	OscTimeTag _timeTag;
	List<OscPacket> _packets;

	const string logPrepend = "<b>[" + nameof( OscBundle ) + "]</b> ";


	/// <summary>
	/// Gets or sets the timetag for this bundle.
	/// </summary>
	public OscTimeTag timeTag
	{
		get { return _timeTag; }
		set { _timeTag = value; }
	}

	/// <summary>
	/// Gets the list of OscMessage and OscBundle objects.
	/// </summary>
	public List<OscPacket> packets { get { return _packets; } }

	/// <summary>
	/// Constructor for creating a bundle with a timetag containing the current time.
	/// </summary>
	public OscBundle() : this( new OscTimeTag( DateTime.Now ) ) { }

	/// <summary>
	/// Constructor for creating a bundle with specified timetag.
	/// </summary>
	public OscBundle( OscTimeTag timeTag )
	{
		_timeTag = timeTag;
		_packets = new List<OscPacket>();
	}

	/// <summary>
	/// Add a OscMessage or OscBundle to this bundle. Shorthand for bundle.packets.Add.
	/// </summary>
	public void Add( OscPacket packet )
	{
		_packets.Add( packet );
	}

	/// <summary>
	/// Remove all OscMessage and OscBundle object in this bundle, and
	/// do so recursively for all contained bundles.
	/// </summary>
	public void Clear()
	{
		foreach( OscPacket packet in _packets ) {
			if( packet is OscBundle ) (packet as OscBundle).Clear();
		}
		_timeTag.Reset();
		_packets.Clear();
	}


	// Undocumented on purpose.
	public override bool TryWriteTo( byte[] data, ref int index )
	{
		// Null check.
		if( data == null ) {
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.Append( "Write failed. Buffer cannot be null.\n" );
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}

		// Capacity check.
		int size = Size();
		if( index + size > data.Length ) {
			if( OscGlobals.logWarnings ) {
				StringBuilder sb = OscDebug.BuildText( this );
				sb.Append( "Write failed. Buffer capacity insufficient.\n" );
				Debug.LogWarning( sb.ToString() );
			}
			return false;
		}

		// Write header.
		TryWriteHeader( _timeTag, data, ref index );

		// Write packets.
		foreach( OscPacket packet in _packets )
		{
			if( packet == null ) continue;

			// Silently ensure that nested bundle's timetag is >= than parent bundle's timetag.
			if( packet is OscBundle && (packet as OscBundle).timeTag < _timeTag ) {
				(packet as OscBundle).timeTag = _timeTag;
			}

			// Reserve space for writing package length.
			int packetSizeIndex = index;
			index += 4;

			// Write packet to bytes.
			int packetContentIndex = index;
			if( !packet.TryWriteTo( data, ref index ) ) return false;
			int packetByteCount = index - packetContentIndex;

			// Write packet byte size (before packet data).
			if( !new FourByteOscData( packetByteCount ).TryWriteTo( data, ref packetSizeIndex ) ) {
				if( OscGlobals.logWarnings ) Debug.Log( OscDebug.FailedWritingBytesWarning( this ) );
				return false;
			}
		}

		return true;
	}


	public static bool TryWriteHeader( OscTimeTag timeTag, byte[] data, ref int index )
	{
		// Check capacity.
		if( index + OscConst.bundleHeaderSize > data.Length ) return false;

		// Add prefix.
		Array.Copy( OscConst.bundlePrefixBytes, 0, data, index, OscConst.bundlePrefixBytes.Length );
		index += OscConst.bundlePrefixBytes.Length;

		// Add timetag.
		if( !new EightByteOscData( timeTag ).TryWriteTo( data, ref index ) ) return false;

		return true;
	}


	public static bool TryReadFrom( byte[] data, ref int index, int byteCount, out OscBundle bundle )
	{
		//Debug.Log( "OscBundle.TryReadFrom. index: " + index + ", expected byteCount: " + byteCount );

		// Check header size (prefix + timetag).
		if( data.Length - index < OscConst.bundleHeaderSize ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( logPrepend + "OscBundle with invalid header was ignored." + Environment.NewLine );
			bundle = null;
			return false;
		}

		// Check prefix.
		if( !ReadAndValidatePrefix( data, ref index ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( logPrepend + "OscBundle with invalid header was ignored." + Environment.NewLine );
			bundle = null;
			return false;
		}

		// Try get recycled bundle from the pool, otherwise create new.
		bundle = OscPool.GetBundle();

		// Get (optional) timetag osc ntp.
		EightByteOscData timeTagDataValue;
		if( EightByteOscData.TryReadFrom( data, ref index, out timeTagDataValue ) ) {
			bundle.timeTag = timeTagDataValue.timeTagValue;
			//Debug.Log( "Time tag: " + bundle.timeTag );
		}

		// Extract packets from buffer data.
		while( index < byteCount )
		{
			//Debug.Log( "Read index: " + index + " out of " + byteCount );

			// Read packet size.
			FourByteOscData packetSizeDataValue;
			if( !FourByteOscData.TryReadFrom( data, ref index, out packetSizeDataValue ) || packetSizeDataValue.intValue == 0 ) {
				//Debug.LogError( "No message size provided!" );
				break;
			}
			//Debug.Log( "packetSizeData: " + packetSizeDataValue.intValue );
			int endDataIndex = index + packetSizeDataValue.intValue;
			if( endDataIndex > data.Length ) {
				if( OscGlobals.logWarnings ){
					//Debug.LogError( "packetSizeDataValue.intValue: " + packetSizeDataValue.intValue );
					Debug.LogWarning( string.Format(
							"{0}Failed to read OscBundle at index {1} because a OscPacket is too large to fit in buffer (byte size {2}.\n" +
							"Your buffer may be too small to read the entire bundle. Try increasing the buffer size in OscIn.",
							logPrepend, index, packetSizeDataValue.intValue
					));
				}
				bundle = null;
				return false;
			}

			//if( index % 4 != 0 ) Debug.LogError( "NOT MULTIPLE OF 4" );

			OscPacket subPacket;
			//Debug.Log( ( (char) data[ index ] ) + " " + data[ index ] );
			if( TryReadFrom( data, ref index, endDataIndex, out subPacket ) ){
				//Debug.Log( "Sub packet read: " + index + " == " + endDataIndex );
				bundle.Add( subPacket );
			} else {
				if( OscGlobals.logWarnings ) Debug.LogWarning( logPrepend + "Failed to read packet.\nIndex: " + index + ", Packet size: " + packetSizeDataValue.intValue + ", Byte count: " + byteCount + ", Buffer size: " + data.Length );
				return false;
			}
		}

		// Done.
		return true;
	}


	/// <summary>
	/// Returns the byte size of the bundle.
	/// </summary>
	public override int Size()
	{
		int size = OscConst.bundleHeaderSize;
		foreach( OscPacket packet in _packets ) {
			if( packet  == null ) continue;
			size += 4; // size header.
			size += packet.Size();
		}
		return size;
	}


	static bool ReadAndValidatePrefix( byte[] data, ref int index )
	{
		for( int i = 0; i < OscConst.bundlePrefixBytes.Length; i++ ) if( data[index++] != OscConst.bundlePrefixBytes[i] ) return false;
		return true;
	}
}