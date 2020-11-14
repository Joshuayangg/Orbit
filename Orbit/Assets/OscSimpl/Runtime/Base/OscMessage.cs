/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using OscSimpl;

/// <summary>
/// Class representing an OSC message. Messages have an OSC address an a number of OSC arguments.
/// </summary>
[Serializable]
public class OscMessage : OscPacket
{
	string _address;
	byte[] _argsData;
	OscArgInfo[] _argsInfo;

	int _argsCount;
	int _argsByteCount;

	bool _dirtyByteSize = true;
	int _cachedByteSize;

	bool _dirtyAddressHash = true;
	uint _addressHash;


	// We need two string builders because we may want to build for logging WHILE building for ToString.
	static StringBuilder _toStringSB; // Used for ToString

	static readonly string logPrepend = "<b>[" + nameof( OscMessage ) + "]</b>";


	/// <summary>
	/// Gets or sets the OSC Address Pattern of the message. Must start with '/'.
	/// </summary>
	public string address {
		get { return _address; }
		set {
			OscAddress.Sanitize( ref value );
			_address = value;
			_dirtyByteSize = true;
		}
	}


	/// <summary>
	/// Creates a new OSC Message.
	/// </summary>
	public OscMessage()
	{
		
	}


	/// <summary>
	/// Creates a new OSC Message with address.
	/// </summary>
	public OscMessage( string address ) : this()
	{
		_address = address;
	}


	/// <summary>
	/// Clears all arguments.
	/// </summary>
	public void Clear()
	{
		_argsByteCount = 0;
		_argsCount = 0;
		_dirtyByteSize = true;
		_dirtyAddressHash = true;
	}


	/// <summary>
	/// Returns the number of arguments.
	/// </summary>
	public int Count()
	{
		return _argsCount;
	}


	/// <summary>
	/// Removes argument at index, shifting the subsequent arguments one index down.
	/// </summary>
	public void RemoveAt( int index )
	{
		// Bounds check.
		if( index < 0 || index >= _argsCount ) return;

		// Get args to remove.
		OscArgInfo argToRemoveInfo = _argsInfo[ index ];

		// Remove from args by shifting subsequent args one index down.
		for( int i = index; i < _argsCount-1; i++ ) _argsInfo[ i ] = _argsInfo[ i+1 ];

		// Decrement counter.
		_argsCount--;

		// Check if arg contains data.
		if( argToRemoveInfo.byteCount > 0 )
		{
			// Remove data.
			int shiftStartIndex = argToRemoveInfo.byteIndex + argToRemoveInfo.byteCount;
			FastCopy( _argsData, shiftStartIndex, _argsData, argToRemoveInfo.byteIndex, _argsByteCount - shiftStartIndex );

			// Update byte indices.
			short byteIndex = 0;
			if( index > 0 ) {
				OscArgInfo argInfo = _argsInfo[ index-1 ];
				byteIndex = (short) ( argInfo.byteIndex + argInfo.byteCount );
			}
			for( int i = index; i < _argsCount; i++ ) {
				OscArgInfo info = _argsInfo[i];
				info.byteIndex = byteIndex;
				byteIndex += info.byteCount;
				_argsInfo[ i ] = info;
			}

			// Update arg byte count.
			_argsByteCount = byteIndex;
		}

		// Request update total message byte count.
		_dirtyByteSize = true;
	}


	/// <summary>
	/// Returns the argument type at index.
	/// </summary>
	public bool TryGetArgType( int index, out OscArgType type )
	{
		if( index < 0 || index >= _argsCount ) {
			type = OscArgType.Unsupported;
			return false;
		}
		type = _argsInfo[ index ].argType;
		return true;
	}


	/// <summary>
	/// Tries to get argument tag at index. Returns success status.
	/// </summary>
	public bool TryGetArgTag( int index, out char tag )
	{
		// Arg bounds.
		if( index < 0 || index >= _argsCount ) {
			tag = (char) OscConst.tagUnsupportedByte;
			return false;
		}

		// Get.
		tag = (char) _argsInfo[index].tagByte;
		return true;
	}


	/// <summary>
	/// Tries to get argument byte size at index. Returns success status.
	/// </summary>
	bool TryGetArgSize( int index, out int size )
	{
		// Arg bounds.
		if( index < 0 || index >= _argsCount ) {
			size = 0;
			return false;
		}

		// Get.
		size = _argsInfo[index].byteCount;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type float. Returns success status.
	/// </summary>
	public bool TryGet( int index, out float value )
	{
		if( !ValidateTryGet( index, OscArgType.Float ) ) {
			value = 0;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		FourByteOscData dataValue;
		if( !FourByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = 0;
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.floatValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type double. Returns success status.
	/// </summary>
	public bool TryGet( int index, out double value )
	{
		if( !ValidateTryGet( index, OscArgType.Double ) ) {
			value = 0;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		EightByteOscData dataValue;
		if( !EightByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = 0;
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.doubleValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type int. Returns success status.
	/// </summary>
	public bool TryGet( int index, out int value )
	{
		if( !ValidateTryGet( index, OscArgType.Int ) ) {
			value = 0;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		FourByteOscData dataValue;
		if( !FourByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = 0;
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.intValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type long. Returns success status.
	/// </summary>
	public bool TryGet( int index, out long value )
	{
		if( !ValidateTryGet( index, OscArgType.Long ) ) {
			value = 0;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		EightByteOscData dataValue;
		if( !EightByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = 0;
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.longValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type string. Returns success status.
	/// </summary>
	public bool TryGet( int index, ref string value )
	{
		if( !ValidateTryGet( index, OscArgType.String ) ) {
			value = string.Empty;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !StringOscData.TryReadFrom( _argsData, ref dataStartIndex, ref value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}



	/// <summary>
	/// Tries to get argument at index of type char. Returns success status.
	/// </summary>
	public bool TryGet( int index, out char value )
	{
		if( !ValidateTryGet( index, OscArgType.Char ) ) {
			value = (char) OscConst.tagUnsupportedByte;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		FourByteOscData dataValue;
		if( !FourByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = ' ';
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.charValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type bool. Returns success status.
	/// </summary>
	public bool TryGet( int index, out bool value )
	{
		if( !ValidateTryGet( index, OscArgType.Bool ) ) {
			value = false;
			return false;
		}

		// Get.
		value = _argsInfo[index].tagByte == OscConst.tagTrueByte;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type color. Returns success status.
	/// </summary>
	public bool TryGet( int index, out Color32 value )
	{
		if( !ValidateTryGet( index, OscArgType.Color ) ) {
			value = new Color32();
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		FourByteOscData dataValue;
		if( !FourByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = new Color32();
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.colorValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type blob (byte[]). Returns success status.
	/// </summary>
	public bool TryGet( int index, ref byte[] value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = null;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, ref value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type time tag. Returns success status.
	/// </summary>
	public bool TryGet( int index, out OscTimeTag value )
	{
		if( !ValidateTryGet( index, OscArgType.TimeTag ) ) {
			value = new OscTimeTag();
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		EightByteOscData dataValue;
		if( !EightByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = new OscTimeTag();
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.timeTagValue;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of midi message. Returns success status.
	/// </summary>
	public bool TryGet( int index, out OscMidiMessage value )
	{
		if( !ValidateTryGet( index, OscArgType.Midi ) ) {
			value = new OscMidiMessage();
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		FourByteOscData dataValue;
		if( !FourByteOscData.TryReadFrom( _argsData, ref dataStartIndex, out dataValue ) ) {
			value = new OscMidiMessage();
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		value = dataValue.midiMessage;
		return true;
	}


	/// <summary>
	/// Tries to get argument at index of type null. Returns success status.
	/// </summary>
	public bool TryGet( int index, OscNull nothing )
	{
		return ValidateTryGet( index, OscArgType.Null );
	}


	/// <summary>
	/// Tries to get argument at index of type impulse. Returns success status.
	/// </summary>
	public bool TryGet( int index, OscImpulse impulse )
	{
		return ValidateTryGet( index, OscArgType.Impulse );
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, float value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.floatInfo );
		if( !new FourByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, double value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.doubleInfo );
		if( !new EightByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, int value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.intInfo );
		if( !new FourByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, long value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.longInfo );
		if( !new EightByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, string value )
	{
		OscArgInfo info = new OscArgInfo( OscConst.tagStringByte, OscArgType.String, (short) StringOscData.EvaluateByteCount( value ) );
		int dataStartIndex = AdaptiveSet( index, info );
		if( !StringOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			Debug.Log( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, char value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.charInfo );
		if( !new FourByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, bool value )
	{
		AdaptiveSet( index, value ? OscArgInfo.boolTrueInfo : OscArgInfo.boolFalseInfo );
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, Color32 value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.colorInfo );
		if( !new FourByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, byte[] value )
	{
		OscArgInfo info = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, (short) BlobOscData.EvaluateByteCount( value ) );
		int dataStartIndex = AdaptiveSet( index, info );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}

	
	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, OscTimeTag value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.timeTagInfo );
		if( !new EightByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, OscMidiMessage value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.midiInfo );
		if( !new FourByteOscData( value ).TryWriteTo( _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, OscNull nothing )
	{
		AdaptiveSet( index, OscArgInfo.nullInfo );
		return this;
	}


	/// <summary>
	/// Set argument at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage Set( int index, OscImpulse impulse )
	{
		AdaptiveSet( index, OscArgInfo.impulseInfo );
		return this;
	}


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( float value ){ Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( double value ) { Set( _argsCount, value ); return this; }
	

	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( int value ){ Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( long value ) { Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( string value ) { Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( char value ) { Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( bool value ) { Set( _argsCount, value ); return this; }
	
	
	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( Color32 value ){ Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( byte[] value ) { Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( OscTimeTag value ){ Set( _argsCount, value ); return this; }
	
	
	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( OscMidiMessage value ){ Set( _argsCount, value ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( OscNull nothing ) { Set( _argsCount, nothing ); return this; }


	/// <summary>
	/// Adds argument.
	/// </summary>
	public OscMessage Add( OscImpulse impulse ) { Set( _argsCount, impulse ); return this; }


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Vector2 value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Vector2.zero;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Vector2Int value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Vector2Int.zero;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Vector3 value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Vector3.zero;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Vector3Int value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Vector3Int.zero;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Vector4 value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Vector4.zero;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Quaternion value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Quaternion.identity;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Rect value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Rect.zero;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to get value at index of from a byte blob. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, out Matrix4x4 value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = Matrix4x4.identity;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, out value ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to read a list of values from a byte blob at argument index. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, ref List<float> values )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) return false;

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, ref values ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to read a list of values from a byte blob at argument index. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, ref List<int> values )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) return false;

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, ref values ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Tries to read string with a given encoding from a byte blob at argument index. Returns success status.
	/// </summary>
	public bool TryGetBlob( int index, Encoding encoding, out string value )
	{
		if( !ValidateTryGet( index, OscArgType.Blob ) ) {
			value = string.Empty;
			return false;
		}

		// Get.
		int dataStartIndex = _argsInfo[ index ].byteIndex;
		if( !BlobOscData.TryReadFrom( _argsData, ref dataStartIndex, encoding, out value ) ) {
			if( OscGlobals.logWarnings )Debug.LogWarning( OscDebug.FailedReadingBytesWarning( this ) );
			return false;
		}
		return true;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Vector2 value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.eightByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Vector2Int value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.eightByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Vector3 value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.twelveByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings )Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Vector3Int value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.twelveByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Vector4 value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.sixteenByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Quaternion value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.sixteenByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			Debug.Log( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Rect value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.sixteenByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			Debug.Log( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Set a value as a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Matrix4x4 value )
	{
		int dataStartIndex = AdaptiveSet( index, OscArgInfo.sixtyfourByteBlobInfo );
		if( !BlobOscData.TryWriteTo( value, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Writes a list of values to a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, IList<float> values )
	{
		OscArgInfo info = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, (short) ( ( 1 +values.Count) * FourByteOscData.byteCount ) );
		int dataStartIndex = AdaptiveSet( index, info );
		if( !BlobOscData.TryWriteTo( values, _argsData, ref dataStartIndex ) ) {
			Debug.Log( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Writes a list of values to a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, IList<int> values )
	{
		OscArgInfo info = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, (short) ( ( 1+values.Count) * FourByteOscData.byteCount ) );
		int dataStartIndex = AdaptiveSet( index, info );
		if( !BlobOscData.TryWriteTo( values, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Writes a string with a given encoding to a byte blob at specified index, expanding message capacity if necessary.
	/// </summary>
	public OscMessage SetBlob( int index, Encoding encoding, string value )
	{
		OscArgInfo info = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, (short) BlobOscData.EvaluateByteCount( value, encoding ) );
		int dataStartIndex = AdaptiveSet( index, info );
		if( !BlobOscData.TryWriteTo( value, encoding, _argsData, ref dataStartIndex ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedWritingBytesWarning( this ) );
		}
		return this;
	}


	/// <summary>
	/// Returns the byte size of the message.
	/// </summary>
	public override int Size()
	{
		if( !_dirtyByteSize ) return _cachedByteSize;

		_cachedByteSize = 0;

		// Address.
		_cachedByteSize += StringOscData.EvaluateByteCount( _address );

		// Tags.
		_cachedByteSize++;								// Prefix;
		_cachedByteSize += _argsCount;					// ASCII char tags.
		_cachedByteSize += 4 - (_cachedByteSize % 4);	// Followed by at least one trailing zero, multiple of four bytes.

		// Data (already multiple of four bytes).
		_cachedByteSize += _argsByteCount;

		// Clean!
		_dirtyByteSize = false;

		return _cachedByteSize;
	}


	// Undocumented on purpose.
	public override bool TryWriteTo( byte[] data, ref int index )
	{
		// Null check.
		if( data == null ){
			if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0} Write failed. Buffer cannot be null.\n", logPrepend ) );
			return false;
		}

		// Capacity check.
		int size = Size();
		if( index + size > data.Length ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0} Write failed. Buffer capacity insufficient.\n", logPrepend ) );
			return false;
		}

		// Address.
		if( !StringOscData.TryWriteTo( _address, data, ref index ) ){
			if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0} Failed to write bytes.\n", logPrepend ) );
			return false;
		}

		// Tag prefix.
		data[index++] = OscConst.tagPrefixByte;

		// Argument tags.
		for( int i = 0; i < _argsCount; i++ ) data[index++] = _argsInfo[i].tagByte;

		// Followed by at least one trailing zero, multiple of four bytes.
		int trailingNullCount = 4 - (index % 4);
		for( int i = 0; i < trailingNullCount; i++ ) data[index++] = 0;

		// Argument data.
		FastCopy( _argsData, 0, data, index, _argsByteCount );
		index += _argsByteCount;

		//Debug.Log( $"Sending { string.Join( ",", data ) }" );

		return true;
	}


	// Undocumented on purpose.
	public static bool TryReadFrom( byte[] sourceData, ref int sourceIndex, ref OscMessage message )
	{
		//Debug.Log( "Message.TryReadFrom " + sourceIndex );

		int sourceStartIndex = sourceIndex;

		//if( sourceData.Length < 100 ) Debug.Log( $"Receiving { string.Join( ",", sourceData ) }" );

		// If we are not provided with a message, then read the lossy hash and try to reuse a message from the pool.
		if( message == null ){
			uint hash = OscStringHash.Pack( sourceData, sourceIndex );
			message = OscPool.GetMessage( hash );
			//Debug.Log( "hash: " + hash );
		} else {
			message._argsCount = 0; // Ensure that arguments are cleared.
		}

		// Address.
		string address = message._address;
		if( !StringOscData.TryReadFrom( sourceData, ref sourceIndex, ref address ) ) {
			if( OscGlobals.logWarnings ) Debug.LogWarning( OscDebug.FailedReadingBytesWarning( message ) );
			return false;
		}
		message._address = address;

		// Tag prefix.
		if( sourceData[sourceIndex] != OscConst.tagPrefixByte ){
			if( OscGlobals.logWarnings ) {
				Debug.LogWarning( OscDebug.BuildText().Append( "Read failed. Tag prefix '/' is missing.\n" ).Append( address ) );
			}
			return false;
		}
		sourceIndex++;

		// Count argument tags.
		int argTagsStartIndex = sourceIndex;
		while( sourceIndex < sourceData.Length && sourceData[ sourceIndex ] != 0 ) sourceIndex++;
		int argCount = sourceIndex - argTagsStartIndex;
		sourceIndex += 4 - ( sourceIndex % 4 ); // ... followed by at least one trailing zero, multiple of four bytes.

		// Adapt info array.
		if( message._argsInfo == null || message._argsInfo.Length < argCount ) message._argsInfo = new OscArgInfo[ argCount ];

		// Create arg info while evaluating arg data size.
		int newArgsByteCount = 0;
		for( int i = 0; i < argCount; i++ )
		{
			int argByteCount = 0;
			byte tagByte = sourceData[ argTagsStartIndex + i ];
			OscArgType argType;

			switch( tagByte )
			{
				case OscConst.tagNullByte:		argType = OscArgType.Null; break;
				case OscConst.tagImpulseByte:	argType = OscArgType.Impulse; break;
				case OscConst.tagTrueByte:		argType = OscArgType.Bool; break;
				case OscConst.tagFalseByte:		argType = OscArgType.Bool; break;
				case OscConst.tagFloatByte:		argType = OscArgType.Float;	argByteCount = 4; break;
				case OscConst.tagIntByte:		argType = OscArgType.Int; argByteCount = 4; break;
				case OscConst.tagCharByte:		argType = OscArgType.Char; argByteCount = 4; break;
				case OscConst.tagColorByte:		argType = OscArgType.Color; argByteCount = 4; break;
				case OscConst.tagMidiByte:		argType = OscArgType.Midi; argByteCount = 4; break;
				case OscConst.tagDoubleByte:	argType = OscArgType.Double; argByteCount = 8; break;
				case OscConst.tagLongByte:		argType = OscArgType.Long; argByteCount = 8; break;
				case OscConst.tagTimetagByte:	argType = OscArgType.TimeTag; argByteCount = 8; break;
				case OscConst.tagStringByte:
				case OscConst.tagSymbolByte:
					argType = OscArgType.String;
					argByteCount = StringOscData.EvaluateByteCount( sourceData, sourceIndex + newArgsByteCount );
					break;
				case OscConst.tagBlobByte:
					argType = OscArgType.Blob;
					BlobOscData.TryEvaluateByteCount( sourceData, sourceIndex + newArgsByteCount, out argByteCount );
					break;
				default:
					StringBuilder sb = OscDebug.BuildText( message );
					sb.Append( "Read failed. Tag '" ); sb.Append( (char) tagByte ); sb.Append( "' is not supported\n" );
					if( OscGlobals.logWarnings ) Debug.LogWarning( sb.ToString() );
					return false;
			}
			message._argsInfo[ i ] = new OscArgInfo( tagByte, argType, ( short) argByteCount, (short) newArgsByteCount );
			newArgsByteCount += argByteCount;
		}
		message._argsCount = argCount;

		// Adapt data array.
		if( message._argsData == null || message._argsData.Length < newArgsByteCount ) message._argsData = new byte[ newArgsByteCount ];

		// Copy data into message. 
		FastCopy( sourceData, sourceIndex, message._argsData, 0, newArgsByteCount );
		sourceIndex += newArgsByteCount;
		message._argsByteCount = newArgsByteCount;

		//Debug.Log( "newArgsByteCount: " + newArgsByteCount );

		// Cache byte count.
		message._cachedByteSize = sourceIndex - sourceStartIndex;
		message._dirtyByteSize = false;

		//Debug.Log( "Message was read. Arg bytes: " + newArgsByteCount + ". Total bytes: " + message._cachedByteSize + "\n" + message.ToString() );

		return true;
	}


	/// <summary>
	/// Alternative ToString() method that appends to a StringBuilder to avoid creating a new string.
	/// Set optional argument appendNewLineAtEnd append a new line at the end.
	/// Set optional argument insertIndex to Insert instead of Append.
	/// Returns the number of characters added.
	/// </summary>
	public int ToString( StringBuilder sb, bool appendNewLineAtEnd = false, int insertIndex = -1 )
	{
		int beginCount = sb.Length;

		StringBuilder tsb; // Temp String Builder
		if( insertIndex >= 0 ){
			if( _toStringSB == null ) _toStringSB = new StringBuilder();
			else _toStringSB.Clear();
			tsb = _toStringSB;
		}  else {
			tsb = sb;
		}

		tsb.Append( _address );
		for( int i = 0; i < _argsCount; i++ )
		{
			tsb.Append( ' ' );

			OscArgType type;
			if( !TryGetArgType( i, out type ) ) continue;

			switch( type )
			{
				case OscArgType.Null:
					tsb.Append( "Null" ); break;
				case OscArgType.Impulse:
					tsb.Append( "Impulse" ); break;
				case OscArgType.Bool:
					bool boolValue;
					if( TryGet( i, out boolValue ) ) tsb.Append( boolValue );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.Float:
					float floatValue;
					if( TryGet( i, out floatValue ) ) tsb.AppendGarbageFree( floatValue );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.Int:
					int intValue;
					if( TryGet( i, out intValue ) ) tsb.AppendGarbageFree( intValue );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.Char:
					char charValue;
					if( TryGet( i, out charValue ) ) { tsb.Append( '{' ).Append( charValue ).Append( '}' ); } else { tsb.Append( "ERROR" ); }
					break;
				case OscArgType.Color:
					Color32 colorValue;
					if( TryGet( i, out colorValue ) ) tsb.AppendGarbageFree( colorValue );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.Double:
					double doubleValue;
					if( TryGet( i, out doubleValue ) ) tsb.AppendGarbageFree( doubleValue );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.Long:
					long longValue;
					if( TryGet( i, out longValue ) ) tsb.AppendGarbageFree( longValue );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.TimeTag:
					OscTimeTag timeTagValue;
					if( TryGet( i, out timeTagValue ) ) timeTagValue.AppendToString( tsb );
					else tsb.Append( "ERROR" );
					break;
				case OscArgType.String:
					int stringStartIndex = _argsInfo[ i ].byteIndex;
					StringOscData.ReadFromAndAppendTo( _argsData, ref stringStartIndex, tsb, true );
					break;
				case OscArgType.Blob:
					int blobStartIndex = _argsInfo[ i ].byteIndex;
					int blobByteCount;
					if( BlobOscData.TryEvaluateByteCount( _argsData, blobStartIndex, out blobByteCount ) ) { tsb.Append( "Blob[" ).AppendGarbageFree( blobByteCount ).Append( ']' ); } else { tsb.Append( "ERROR" ); }
					break;
				case OscArgType.Midi:
					OscMidiMessage midiValue;
					if( TryGet( i, out midiValue ) ) midiValue.AppendToString( tsb );
					break;
			}
		}

		if( appendNewLineAtEnd ) tsb.AppendLine();

		if( insertIndex >= 0 ){
			sb.EnsureCapacity( sb.Length + tsb.Length );
			// It seems to produce a bit less garbage if we insert each char instead of the string.
			for( int i = 0; i < tsb.Length; i++ ) sb.Insert( insertIndex++, tsb[i] );
		}

		return sb.Length - beginCount;
	}


	// Undocumented on purpose.
	public override string ToString()
	{
		if( _toStringSB == null ) _toStringSB = new StringBuilder();
		else _toStringSB.Clear();
		ToString( _toStringSB );
		return _toStringSB.ToString();	
	}


	// Undocumented on purpose.
	public uint GetAddressHash()
	{
		// Return cached when possible.
		if( !_dirtyAddressHash ) return _addressHash;

		// Compute and return.
		_addressHash = OscStringHash.Pack( _address );
		_dirtyAddressHash = false;
		return _addressHash;
	}


	int AdaptiveSet( int index, OscArgInfo newArginfo )
	{
		// Check for change.
		bool expandArgs = index >= _argsCount;
		OscArgInfo oldArgInfo = expandArgs ? OscArgInfo.undefinedInfo : _argsInfo[ index ];
		bool byteSizeChanged = newArginfo.byteCount != oldArgInfo.byteCount;
		bool typeChanged = newArginfo.tagByte != oldArgInfo.tagByte;
		if( !typeChanged && !byteSizeChanged ) return oldArgInfo.byteIndex; // No adaptation needed.

		// Find the old byte size.
		int oldArgsByteCount = _argsByteCount;

		// Set the new byte index.
		if( expandArgs ) newArginfo.byteIndex = (short) oldArgsByteCount;
		else newArginfo.byteIndex = oldArgInfo.byteIndex;

		// Adapt arg info list.
		if( expandArgs )
		{
			// Add.
			int requiredArgCount = index + 1;
			if( _argsInfo == null ) {
				_argsInfo = new OscArgInfo[ requiredArgCount ];
			} else if( requiredArgCount > _argsInfo.Length ) {
				OscArgInfo[] tempArgsInfo = new OscArgInfo[ requiredArgCount ];
				for( int i = 0; i < _argsInfo.Length; i++ ) tempArgsInfo[ i ] = _argsInfo[ i ];
				_argsInfo = tempArgsInfo;
			}

			// Insert nulls if needed.
			for( int i = _argsCount; i < requiredArgCount-1; i++ ) _argsInfo[ i ] = OscArgInfo.nullInfo; // Insert nulls if needed.

			// Place new arg and update count.
			_argsInfo[ requiredArgCount - 1 ] =  newArginfo;
			_argsCount = requiredArgCount;

		} else {
			// Overwite.
			_argsInfo[ index ] = newArginfo;
		}

		// Adapt data list.
		if( byteSizeChanged )
		{
			int byteDelta = newArginfo.byteCount - oldArgInfo.byteCount;
			int newArgsByteCount = oldArgsByteCount + byteDelta;
			if( _argsData == null || _argsData.Length < newArgsByteCount ) {
				byte[] tempData = new byte[ newArgsByteCount ];
				FastCopy( _argsData, 0, tempData, 0, _argsByteCount );
				_argsData = tempData;
			}

			if( index != _argsCount - 1 ) {
				// Move data beynd byteIndex, prepare for insert.
				FastCopy( _argsData, newArginfo.byteIndex, _argsData, newArginfo.byteIndex + newArginfo.byteCount, _argsByteCount - newArginfo.byteIndex );
			}

			_argsByteCount = newArgsByteCount;

			// Request byte count update.
			_dirtyByteSize = true;
		}

		//Debug.Log( "AdaptiveSet for '" + _address + "'. argIndex: " + index + ", type: " + OscConverter.ToArgType( newArginfo.tagByte ) + ", byteIndex: " + newArginfo.byteIndex  + ", byteCount: " + newArginfo.byteCount + "\n" );

		return newArginfo.byteIndex;
	}


	bool ValidateTryGet( int index, OscArgType requestedArgType )
	{
		const string failedPrependText = "TryGet failed. ";

		// Arg bounds.
		if( index < 0 || index >= _argsCount ) {
			if( OscGlobals.logWarnings ) {
				Debug.LogWarning(
					OscDebug.BuildText( this ).Append( failedPrependText )
					.Append( "Argument index " ).AppendGarbageFree( index )
					.Append( " is out of bounds. Message has " ).AppendGarbageFree( _argsCount ).Append( " arguments.\n" )
				);
			}
			return false;
		} 

		// Arg type.
		OscArgInfo info = _argsInfo[ index ];
		if( requestedArgType != info.argType ){
			if( OscGlobals.logWarnings ) {
				Debug.LogWarning(
					OscDebug.BuildText( this ).Append( failedPrependText )
					.Append( "Argument at index " ).AppendGarbageFree( index )
					.Append( " is not type " ).Append( requestedArgType )
					.Append( " '" ).Append( (char) OscConverter.ToTagByte( requestedArgType ) ).Append( "'" )
					.Append( ", it is type " ).Append( info.argType )
					.Append( " '" ).Append( (char) info.tagByte ).Append( "'.\n" )
				);
			}
			return false;
		}

		// Data capacity.
		if( index + info.byteCount > _argsByteCount ) {
			if( OscGlobals.logWarnings ) {
				Debug.LogWarning(
					OscDebug.BuildText( this ).Append( failedPrependText )
					.Append( "Argument at index " ).AppendGarbageFree( index ).Append( " has incomplete data.\n" )
				);
			}
			return false;
		}

		return true;
	}


	/// <summary>
	/// Fast byte array copy.
	/// </summary>
	static void FastCopy( byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int count )
	{
		// About performance: https://stackoverflow.com/a/33865267/2265840
		if( count < 100 ) {
			if( source == destination && sourceIndex < destinationIndex ){ // Handle overlapping arrays.
				destinationIndex += count;
				sourceIndex += count;
				for( int i = 0; i < count; i++ ) destination[ --destinationIndex ] = source[ --sourceIndex ];
			} else {
				for( int i = 0; i < count; i++ ) destination[ destinationIndex++ ] = source[ sourceIndex++ ];
			}
		} else {
			// Handles overlapping arrays no problem. https://docs.microsoft.com/en-us/dotnet/api/system.array.constrainedcopy?view=netcore-3.1
			// "If sourceArray and destinationArray overlap, this method behaves as if the original values 
			// of sourceArray were preserved in a temporary location before destinationArray is overwritten."
			Array.Copy( source, sourceIndex, destination, destinationIndex, count );
		}
	}
}