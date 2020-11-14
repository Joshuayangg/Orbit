/*
	Created by Carl Emil Carlsen.
	Copyright 2018-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk

	A sequence of non-null ASCII characters, followed by a null, followed by 0-3 additional null characters to make the total number of bytes a multiple of 4.
*/

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace OscSimpl
{
	public static class BlobOscData
	{
		static byte[] _cache;

		static readonly string logPrepend = "<b>[" + nameof( BlobOscData ) + "]</b>";

		/// <summary>
		/// Tries to evaluate byte count of a blob, including it's byte count prefix.
		/// </summary>
		public static int EvaluateByteCount( byte[] blob )
		{
			int byteCount = 4 + blob.Length; // Prefix holding the byte count of the blob, then the blob.
			return ( (byteCount+3) / 4 ) * 4; // Multiple of four bytes.
		}


		/// <summary>
		/// Tries to evaluate byte count of a blob containing a string with a given encoding, including it's byte count prefix.
		/// </summary>
		public static int EvaluateByteCount( string text, Encoding encoding )
		{
			int byteCount = 4 + encoding.GetByteCount( text ); // Prefix holding the byte count of the blob, then the blob.
			return ((byteCount+3) / 4) * 4; // Multiple of four bytes.
		}


		/// <summary>
		/// Tries to evaluate byte count prefix of a blob and evaluate it.
		/// </summary>
		public static bool TryEvaluateByteCount( byte[] data, int index, out int byteCountPrefixValue )
		{
			FourByteOscData byteCountPrefix;
			if( !FourByteOscData.TryReadFrom( data, ref index, out byteCountPrefix ) ) {
				// Not enough space for byte count prefix in data array.
				byteCountPrefixValue = 0;
				return false;
			}
			int multipleOfFourByteCount = ((byteCountPrefix.intValue+3) / 4) * 4; // Multiple of four bytes.
			if( index + multipleOfFourByteCount > data.Length ) {
				// Not enough space for blob in data array.
				byteCountPrefixValue = 0;
				return false;
			}

			byteCountPrefixValue = 4 + multipleOfFourByteCount;
			return true;
		}


		/// <summary>
		/// Tries to evaluate byte count prefix of a blob and evaluate it.
		/// </summary>
		static bool TryReadAndEvaluateByteCountPrefix( byte[] data, int index, out int byteCountPrefixValue )
		{
			FourByteOscData byteCountPrefix;
			if( !FourByteOscData.TryReadFrom( data, ref index, out byteCountPrefix ) ) {
				// Not enough space for byte count prefix in data array.
				byteCountPrefixValue = 0;
				if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0} Blob is missing byte count prefix.\n", logPrepend ) );
				return false;
			}

			int multipleOfFourByteCount = ((byteCountPrefix.intValue+3)/4)*4; // No trailing null, rounded to 4 bytes.
			if( index + multipleOfFourByteCount > data.Length ) {
				// Not enough space for blob in data array.
				byteCountPrefixValue = 0;
				if( OscGlobals.logWarnings ) Debug.LogWarning( string.Format( "{0} Blob data is incomplete.\n", logPrepend ) );
				return false;
			}

			byteCountPrefixValue = byteCountPrefix.intValue;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, ref byte[] blob )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ){
				blob = null;
				return false;
			}
			index += FourByteOscData.byteCount;

			if( blob == null || blob.Length != byteCountPrefixValue ) blob = new byte[byteCountPrefixValue];

			// Read blob.
			Array.Copy( data, index, blob, 0, byteCountPrefixValue );
			index += byteCountPrefixValue;
			index = ((index+3)/4)*4; // No trailing null, rounded to 4 bytes.
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Vector2 value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Vector2.zero;
				return false;
			}
			index += FourByteOscData.byteCount;

			EightByteOscData valueData;
			if( !EightByteOscData.TryReadFrom( data , ref index, out valueData ) ) {
				value = Vector2.zero;
				return false;
			}
			value = valueData.vector2Value;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Vector2Int value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Vector2Int.zero;
				return false;
			}
			index += FourByteOscData.byteCount;

			EightByteOscData valueData;
			if( !EightByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Vector2Int.zero;
				return false;
			}
			value = valueData.vector2IntValue;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Vector3 value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Vector3.zero;
				return false;
			}
			index += FourByteOscData.byteCount;

			TwelveByteOscData valueData;
			if( !TwelveByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Vector3.zero;
				return false;
			}
			value = valueData.vector3Value;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Vector3Int value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Vector3Int.zero;
				return false;
			}
			index += FourByteOscData.byteCount;

			TwelveByteOscData valueData;
			if( !TwelveByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Vector3Int.zero;
				return false;
			}
			value = valueData.vector3IntValue;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Vector4 value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Vector4.zero;
				return false;
			}
			index += FourByteOscData.byteCount;

			SixteenByteOscData valueData;
			if( !SixteenByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Vector4.zero;
				return false;
			}
			value = valueData.vector4Value;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Quaternion value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Quaternion.identity;
				return false;
			}
			index += FourByteOscData.byteCount;

			SixteenByteOscData valueData;
			if( !SixteenByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Quaternion.identity;
				return false;
			}
			value = valueData.quaternionValue;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Rect value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Rect.zero;
				return false;
			}
			index += FourByteOscData.byteCount;

			SixteenByteOscData valueData;
			if( !SixteenByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Rect.zero;
				return false;
			}
			value = valueData.rectValue;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, out Matrix4x4 value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = Matrix4x4.identity;
				return false;
			}
			index += FourByteOscData.byteCount;

			SixtyfourByteOscData valueData;
			if( !SixtyfourByteOscData.TryReadFrom( data, ref index, out valueData ) ) {
				value = Matrix4x4.identity;
				return false;
			}
			value = valueData.matrix4x4Value;
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, ref List<float> values )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) return false;
			index += FourByteOscData.byteCount;

			// Adapt list.
			int valueCount = byteCountPrefixValue / FourByteOscData.byteCount;
			if( values == null ){
				values = new List<float>( valueCount );
			} else {
				values.Clear();
				if( values.Capacity < valueCount ) values.Capacity = valueCount;
			}

			// Fill list.
			FourByteOscData dataValue;
			for( int i = 0; i < valueCount; i++ ){
				if( !FourByteOscData.TryReadFrom( data, ref index, out dataValue ) ) return false;
				values.Add( dataValue.floatValue );
			}
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, ref List<int> values )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) return false;
			index += 4;

			// Adapt list.
			int valueCount = byteCountPrefixValue / FourByteOscData.byteCount;
			if( values == null ) {
				values = new List<int>( valueCount );
			} else {
				values.Clear();
				if( values.Capacity < valueCount ) values.Capacity = valueCount;
			}

			// Fill list.
			FourByteOscData dataValue;
			for( int i = 0; i < valueCount; i++ ) {
				if( !FourByteOscData.TryReadFrom( data, ref index, out dataValue ) ) return false;
				values.Add( dataValue.intValue );
			}
			return true;
		}


		public static bool TryReadFrom( byte[] data, ref int index, Encoding encoding, out string value )
		{
			// Try to get blob byte count.
			int byteCountPrefixValue;
			if( !TryReadAndEvaluateByteCountPrefix( data, index, out byteCountPrefixValue ) ) {
				value = string.Empty;
				return false;
			}
			index += 4;

			// Encoding only takes arrays, so we must copy the list first.
			if( _cache == null || _cache.Length < byteCountPrefixValue ) _cache = new byte[byteCountPrefixValue];
			for( int i = 0; i < byteCountPrefixValue; i++ ) _cache[i] = data[index++];

			// Read.
			value = encoding.GetString( _cache, 0, byteCountPrefixValue );
			index = ((index+3)/4)*4; // No following null, rounded to 4 bytes.
			return true;
		}


		public static bool TryWriteTo( byte[] blob, byte[] data, ref int index )
		{
			// Write byte count prefix, blob content, and make muliple of four.
			int trailingZeroCount = ((blob.Length+3) / 4) * 4 - blob.Length;
			int blobByteCount = FourByteOscData.byteCount + blob.Length + trailingZeroCount;
			if( index + blobByteCount < data.Length ) return false;
			if( !new FourByteOscData( blob.Length ).TryWriteTo( data, ref index ) ) return false;
			Array.Copy( blob, 0, data, index, blob.Length );
			index += blob.Length;
			for( int i = 0; i < trailingZeroCount; i++ ) data[index++] = 0;
			return true;
		}


		public static bool TryWriteTo( Vector2 value, byte[] data, ref int index  )
		{
			if( !new FourByteOscData( EightByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new EightByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Vector2Int value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( EightByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new EightByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Vector3 value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( TwelveByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new TwelveByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Vector3Int value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( TwelveByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new TwelveByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Vector4 value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( SixteenByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new SixteenByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Quaternion value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( SixteenByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new SixteenByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Rect value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( SixteenByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new SixteenByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( Matrix4x4 value, byte[] data, ref int index )
		{
			if( !new FourByteOscData( SixtyfourByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			if( !new SixtyfourByteOscData( value ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( IList<float> values, byte[] data, ref int index )
		{
			int count = values.Count;
			if( !new FourByteOscData( count * FourByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			for( int i = 0; i < count; i++ ) if( !new FourByteOscData( values[i] ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( IList<int> values, byte[] data, ref int index )
		{
			int count = values.Count;
			if( !new FourByteOscData( count * FourByteOscData.byteCount ).TryWriteTo( data, ref index ) ) return false;
			for( int i = 0; i < count; i++ ) if( !new FourByteOscData( values[i] ).TryWriteTo( data, ref index ) ) return false;
			return true;
		}


		public static bool TryWriteTo( string value, Encoding encoding, byte[] data, ref int index )
		{
			int byteCount = encoding.GetByteCount( value );
			if( !new FourByteOscData( byteCount ).TryWriteTo( data, ref index ) ) return false;

			// Since encoding only deals arrays, we need to write to cache and then copy to list.
			if( _cache == null || _cache.Length < byteCount ) _cache = new byte[byteCount];

			encoding.GetBytes( value, 0, value.Length, _cache, 0 );
			for( int i = 0; i < byteCount; i++ ) data[index++] = _cache[i];
			return true;
		}
	}
}