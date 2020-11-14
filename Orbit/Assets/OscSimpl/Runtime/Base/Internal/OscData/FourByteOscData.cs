/*
	Created by Carl Emil Carlsen.
	Copyright 2018-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk

	In this class, we use StructLayout Explicit to convert from and to bytes,
	without generating garbage.
*/


using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OscSimpl
{
	[Serializable, StructLayout( LayoutKind.Explicit )]
	public struct FourByteOscData
	{
		[FieldOffset( 0 )] float _floatValue;
		[FieldOffset( 0 )] int _intValue;
		[FieldOffset( 0 )] Color32 _colorValue;
		[FieldOffset( 0 )] OscMidiMessage _midiMessage;
		[FieldOffset( 0 )] byte _b0;
		[FieldOffset( 1 )] byte _b1;
		[FieldOffset( 2 )] byte _b2;
		[FieldOffset( 3 )] byte _b3;

		public const int byteCount = 4;

		public float floatValue { get { return _floatValue; } }
		public int intValue { get { return _intValue; } }
		public char charValue { get { return (char) _b0; } }
		public Color32 colorValue { get { return _colorValue; } }
		public OscMidiMessage midiMessage { get { return _midiMessage; } }


		public FourByteOscData( byte b0, byte b1, byte b2, byte b3 ) : this()
		{
			if( BitConverter.IsLittleEndian ){
				_b0 = b3;
				_b1 = b2;
				_b2 = b1;
				_b3 = b0;
			} else {
				_b0 = b0;
				_b1 = b1;
				_b2 = b2;
				_b3 = b3;
			}
		}


		public FourByteOscData( float value ) : this(){ _floatValue = value;}
		public FourByteOscData( int value ) : this(){ _intValue = value; }
		public FourByteOscData( Color32 value ) : this(){ _colorValue = value; }
		public FourByteOscData( OscMidiMessage value ) : this(){ _midiMessage = value; }


		public FourByteOscData( char value ) : this()
		{
			_b0 = (byte) value;
			if( _b0 > OscConst.asciiMaxByte ) _b0 = OscConst.asciiUnknownByte; // Mimic Encoding.ASCII.GetString behaviour, but without garbage.
			_b1 = 0;
			_b2 = 0;
			_b3 = 0;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out FourByteOscData value )
		{
			if( index + byteCount > data.Length ){
				value = new FourByteOscData();
				return false;
			}
			value = new FourByteOscData( data[index++], data[index++], data[index++], data[index++] );
			return true;
		}


		public bool TryWriteTo( byte[] data, ref int index )
		{
			if( index + byteCount > data.Length ) return false;

			if( BitConverter.IsLittleEndian ){
				data[index++] = _b3;
				data[index++] = _b2;
				data[index++] = _b1;
				data[index++] = _b0;
			} else {
				data[index++] = _b0;
				data[index++] = _b1;
				data[index++] = _b2;
				data[index++] = _b3;
			}

			return true;
		}


		public override string ToString()
		{
			return string.Format( "[{0},{1},{2},{3}] or [{4},{5},{6},{7}]", _b0, _b1, _b2, _b3, (char) _b0, (char) _b1, (char) _b2, (char) _b3 );
		}
	}
}