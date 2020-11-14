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
	public struct TwelveByteOscData
	{
		[FieldOffset( 0 )] Vector3 _vector3Value;
		[FieldOffset( 0 )] Vector3Int _vector3IntValue;
		[FieldOffset( 0 )] byte _b0;
		[FieldOffset( 1 )] byte _b1;
		[FieldOffset( 2 )] byte _b2;
		[FieldOffset( 3 )] byte _b3;
		[FieldOffset( 4 )] byte _b4;
		[FieldOffset( 5 )] byte _b5;
		[FieldOffset( 6 )] byte _b6;
		[FieldOffset( 7 )] byte _b7;
		[FieldOffset( 8 )] byte _b8;
		[FieldOffset( 9 )] byte _b9;
		[FieldOffset( 10 )] byte _b10;
		[FieldOffset( 11 )] byte _b11;

		public const int byteCount = 12;

		public Vector3 vector3Value { get { return _vector3Value; } }
		public Vector3Int vector3IntValue { get { return _vector3IntValue; } }


		public TwelveByteOscData
		(
			byte b0, byte b1, byte b2, byte b3, 
			byte b4, byte b5, byte b6, byte b7, 
			byte b8, byte b9, byte b10, byte b11
		) : this() {
			if( BitConverter.IsLittleEndian ) {
				_b0 = b11;
				_b1 = b10;
				_b2 = b9;
				_b3 = b8;
				_b4 = b7;
				_b5 = b6;
				_b6 = b5;
				_b7 = b4;
				_b8 = b3;
				_b9 = b2;
				_b10 = b1;
				_b11 = b0;
			} else {
				_b0 = b0;
				_b1 = b1;
				_b2 = b2;
				_b3 = b3;
				_b4 = b4;
				_b5 = b5;
				_b6 = b6;
				_b7 = b7;
				_b8 = b8;
				_b9 = b9;
				_b10 = b10;
				_b11 = b11;
			}
		}


		public TwelveByteOscData( Vector3 value ) : this() { _vector3Value = value; }
		public TwelveByteOscData( Vector3Int value ) : this() { _vector3IntValue = value; }


		// TODO When .Net 4.5 becomes available in Unity: Replace IList<T> with IReadOnlyList<T> since we want to pass Array where number and order of list elements is read-only.
		public static bool TryReadFrom( byte[] data, ref int index, out TwelveByteOscData value )
		{
			if( index + byteCount > data.Length ) {
				value =new TwelveByteOscData();
				return false;
			}
			value = new TwelveByteOscData(
				data[index++], data[index++], data[index++], data[index++], 
				data[index++], data[index++], data[index++], data[index++],
				data[index++], data[index++], data[index++], data[index++]
			);
			return true;
		}


		// TODO When .Net 4.5 becomes available in Unity: Replace IList<T> with IReadOnlyList<T> since we want to pass Array where number and order of list elements is read-only.
		public bool TryWriteTo( byte[] data, ref int index )
		{
			if( index + byteCount > data.Length ) return false;

			if( BitConverter.IsLittleEndian ) {
				data[index++] = _b11;
				data[index++] = _b10;
				data[index++] = _b9;
				data[index++] = _b8;
				data[index++] = _b7;
				data[index++] = _b6;
				data[index++] = _b5;
				data[index++] = _b4;
				data[index++] = _b3;
				data[index++] = _b2;
				data[index++] = _b1;
				data[index++] = _b0;
			} else {
				data[index++] = _b0;
				data[index++] = _b1;
				data[index++] = _b2;
				data[index++] = _b3;
				data[index++] = _b4;
				data[index++] = _b5;
				data[index++] = _b6;
				data[index++] = _b7;
				data[index++] = _b8;
				data[index++] = _b9;
				data[index++] = _b10;
				data[index++] = _b11;
			}

			return true;
		}
	}
}