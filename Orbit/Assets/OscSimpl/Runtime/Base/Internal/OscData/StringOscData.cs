/*
	Created by Carl Emil Carlsen.
	Copyright 2018-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk

	A sequence of non-null ASCII characters, followed by a null, followed by 0-3 additional null characters to make the total number of bytes a multiple of 4.
*/

using System;
using System.Text;

namespace OscSimpl
{
	public static class StringOscData
	{
		static byte[] _byteCache;
		static char[] _charCache;

		const int byteCacheChunkSize = 256;


		public static int EvaluateByteCount( string text )
		{
			return text.Length + 4 - (text.Length % 4);   // Single byte ASCII, multiple of four bytes with at least one trailing null.
		}


		public static int EvaluateByteCount( byte[] data, int index )
		{
			int count = 0;
			for( int i = index; i < data.Length && data[i] != 0; i++ ) {
				count++;
			}
			count += 4 - (count % 4);
			return count;
		}


		public static bool TryReadFrom( byte[] data, ref int index, ref string originalText )
		{
			if( _charCache == null ) _charCache = new char[ byteCacheChunkSize ]; // A resonable size.

			// Count.
			int existingTextLength = originalText == null ? 0 : originalText.Length;
			bool change = existingTextLength == 0; // If there is no text to compare with, we assume the new text will differ from the original text.
			int count = 0;

			// Parse characters until we hit a null.
			for( int i = index; i < data.Length; i++ )
			{
				byte b = data[ index ];
				if( b == 0 ) break; // End of text.
				index++;

				// Enlarge buffer as needed.
				if( _charCache.Length < count+1 ){
					char[] largerCharCache = new char[ _charCache.Length + byteCacheChunkSize ];
					Array.Copy( _charCache, 0, largerCharCache, 0, _charCache.Length );
					_charCache = largerCharCache;
				}

				// Mimic Encoding.ASCII.GetString behaviour, but without garbage.
				if( b > OscConst.asciiMaxByte ) b = OscConst.asciiUnknownByte;

				char c = (char) b;
				_charCache[ count++ ] = c;


				// Compare with existing string.
				if( !change && ( count > existingTextLength || c != originalText[ count-1 ] ) ) change = true;
			}

			// Handle trailing nulls.
			int trailingNullCount = 4 - ( count % 4 ); // Including a minimum of one trailing null.
			if( index + trailingNullCount > data.Length ) { // Don't accept texts without correct trailing nulls.
				originalText = string.Empty;
				return false;
			}
			index += trailingNullCount;

			// Only create string if it changed from the original.
			if( change ){
				originalText = new string( _charCache, 0, count );
			}

			return true;
		}


		public static void ReadFromAndAppendTo( byte[] data, ref int index, StringBuilder sb, bool addQuotes = false )
		{
			if( addQuotes ) sb.Append( "\"" );

			// Parse characters until we hit a null.
			int count = 0;
			for( int i = index; i < data.Length; i++ ) {
				byte b = data[ index ];
				if( b == 0 ) break; // End of text.
				index++;

				// Mimic Encoding.ASCII.GetString behaviour, but without garbage.
				if( b > OscConst.asciiMaxByte ) b = OscConst.asciiUnknownByte;

				sb.Append( (char) b );
				count++;
			}

			if( addQuotes ) sb.Append( "\"" );

			// Handle trailing nulls.
			int trailingNullCount = 4 - ( count % 4 ); // Including a minimum of one trailing null.
			index += trailingNullCount;
		}


		public static bool TryWriteTo( string text, byte[] data, ref int index )
		{
			// Count and validate.
			int textByteCount = text.Length; // ASCII takes one byte per charater.
			int trailingNullCount = 4 - (textByteCount % 4); // Including a minimum of one trailing null.
			if( index + textByteCount + trailingNullCount > data.Length ) return false;

			// Write.
			Encoding.ASCII.GetBytes( text, 0, text.Length, data, index );
			index += textByteCount;
			for( int i = 0; i < trailingNullCount; i++ ) data[index++] = 0;

			return true;
		}
	}
}