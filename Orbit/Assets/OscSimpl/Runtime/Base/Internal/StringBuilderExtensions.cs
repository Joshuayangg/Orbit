/*
	StringBuilderExtensions

	Original by Gavin Pugh 9th March 2010.
		Copyright (c) Gavin Pugh 2010 - Released under the zlib license: http://www.opensource.org/licenses/zlib-license.php
		https://www.gavpugh.com/2010/04/01/xnac-avoiding-garbage-when-working-with-stringbuilder/

	Modified and further extended by Carl Emil Carlsen
		Copyright (c) Carl Emil Carlsen 2020
 */

using System.Text;
using UnityEngine;

namespace OscSimpl
{
	public static class StringBuilderExtensions
	{
		static readonly char[] digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		const int base10 = 10;


		public static StringBuilder AppendGarbageFree( this StringBuilder sb, uint value, bool includeLiteralPostFix = true )
		{
			const char postFix = 'u';

			// Zero case.
			if( value == 0 ) {
				sb.Append( '0' );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb; 
			}

			// Count length and adapt string builder.
			int count = 0;
			uint tmp = value;
			while( tmp > 0 ) {
				tmp /= base10;
				count++;
			}
			sb.Append( '0', count );

			// Add digits in reverse order.
			int pos = sb.Length;
			while( count > 0 ) {
				pos--;
				sb[ pos ] = digits[ value % base10 ];
				value /= base10;
				count--;
			}

			return includeLiteralPostFix ? sb.Append( postFix ) : sb;
		}


		public static StringBuilder AppendGarbageFree( this StringBuilder sb, int value )
		{
			// Zero case.
			if( value == 0 ) return sb.Append( '0' );

			// Handle negative values.
			uint uValue;
			if( value < 0 ) {
				sb.Append( '-' );
				uValue = uint.MaxValue - ( (uint) value ) + 1;
			} else {
				uValue = (uint) value;
			}

			// Count length and adapt string builder.
			int count = 0;
			uint tmp = uValue;
			while( tmp > 0 ) {
				tmp /= base10;
				count++;
			}
			sb.Append( '0', count );

			// Add digits in reverse order.
			int pos = sb.Length;
			while( count > 0 ) {
				pos--;
				sb[ pos ] = digits[ uValue % base10 ];
				uValue /= base10;
				count--;
			}

			return sb;
		}


		public static StringBuilder AppendGarbageFree( this StringBuilder sb, ulong value, bool includeLiteralPostFix = true )
		{
			const string postFix = "ul";

			// Zero case.
			if( value == 0 ){
				sb.Append( '0' );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}

			// Count length and adapt string builder.
			int count = 0;
			ulong tmp = value;
			while( tmp > 0 ) {
				tmp /= base10;
				count++;
			}
			sb.Append( '0', count );

			// Add digits in reverse order.
			int pos = sb.Length;
			while( count > 0 ) {
				pos--;
				sb[ pos ] = digits[ value % base10 ];
				value /= base10;
				count--;
			}

			return includeLiteralPostFix ? sb.Append( postFix ) : sb;
		}


		public static StringBuilder AppendGarbageFree( this StringBuilder sb, long value, bool includeLiteralPostFix = true )
		{
			const char postFix = 'l';

			// Zero case.
			if( value == 0 ){
				sb.Append( '0' );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}

			// Handle negative values.
			ulong uValue;
			if( value < 0 ) {
				sb.Append( '-' );
				uValue = ulong.MaxValue - ( (ulong) value ) + 1;
			} else {
				uValue = (ulong) value;
			}

			// Count length and adapt string builder.
			int count = 0;
			ulong tmp = uValue;
			while( tmp > 0 ) {
				tmp /= base10;
				count++;
			}
			sb.Append( '0', count );

			// Add digits in reverse order.
			int pos = sb.Length;
			while( count > 0 ) {
				pos--;
				sb[ pos ] = digits[ uValue % base10 ];
				uValue /= base10;
				count--;
			}

			return includeLiteralPostFix ? sb.Append( postFix ) : sb;
		}


		public static StringBuilder AppendGarbageFree( this StringBuilder sb, float value, uint decimalCount = 2, bool includeLiteralPostFix = true )
		{
			const char postFix = 'f';

			// Zero case.
			if( value == 0 ){
				sb.Append( '0' );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}

			// We give up if the we exceed 7 digits, the 32 bit floating point limit.
			float absValue = value > 0f ? value : -value;
			if( absValue > 10000000f || absValue < 0.0000001f ){
				sb.Append( value.ToString( "F" + decimalCount ) );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}

			// No decimals case.
			if( decimalCount == 0 ) {
				if( value < 0f ) sb.Append( '-' );
				ulong ulongValue;
				if( value >= 0f ) ulongValue = (ulong) ( absValue + 0.5f ); // Round up
				else ulongValue = (ulong) ( absValue - 0.5f ); // Round down for negative numbers
				sb.AppendGarbageFree( ulongValue, false );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}
			if( decimalCount > 7 ) decimalCount = 7; // 32 bit floating point limit.

			// First part is easy, just cast to an integer
			long longPart = (long) value;
			sb.AppendGarbageFree( longPart, false );
			
			// Work out remainder we need to print after the d.p.
			double remainder = value - longPart;
			if( remainder < 0 ) remainder *= -1;

			// Multiply up to become an int that we can print
			while( decimalCount > 0 ){
				remainder *= 10;
				decimalCount--;
			}

			// Round up. It's guaranteed to be a positive number, so no extra work required here.
			remainder += 0.5f;

			// All done, print that as an int!
			uint remainedUint = (uint) remainder;
			if( remainedUint == 0 ) return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			sb.Append( '.' ).AppendGarbageFree( remainedUint, false );
			return includeLiteralPostFix ? sb.Append( postFix ) : sb;
		}



		public static StringBuilder AppendGarbageFree( this StringBuilder sb, double value, uint decimalCount = 2, bool includeLiteralPostFix = true )
		{
			const char postFix = 'd';

			// Zero case.
			if( value == 0 ){
				sb.Append( '0' );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}

			// We give up if the we exceed 7 digits, the 32 bit floating point limit.
			double absValue = value > 0f ? value : -value;
			if( absValue > 10000000f || absValue < 0.0000001f ){
				sb.Append( value.ToString( "F" + decimalCount ) );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}

			// No decimals case.
			if( decimalCount == 0 ) {
				if( value < 0f ) sb.Append( '-' );
				ulong ulongValue;
				if( value >= 0f ) ulongValue = (ulong) ( absValue + 0.5f ); // Round up
				else ulongValue = (ulong) ( absValue - 0.5f ); // Round down for negative numbers
				sb.AppendGarbageFree( ulongValue, false );
				return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			}
			if( decimalCount > 7 ) decimalCount = 7; // 32 bit floating point limit.

			// First part is easy, just cast to an integer
			long longPart = (long) value;
			sb.AppendGarbageFree( longPart, false );

			// Work out remainder we need to print after the d.p.
			double remainder = value - longPart;
			if( remainder < 0 ) remainder *= -1;

			// Multiply up to become an int that we can print
			while( decimalCount > 0 ) {
				remainder *= 10;
				decimalCount--;
			}

			// Round up. It's guaranteed to be a positive number, so no extra work required here.
			remainder += 0.5f;

			// All done, print that as an int!
			uint remainedUint = (uint) remainder;
			if( remainedUint == 0 ) return includeLiteralPostFix ? sb.Append( postFix ) : sb;
			sb.Append( '.' ).AppendGarbageFree( remainedUint );
			return includeLiteralPostFix ? sb.Append( postFix ) : sb;
		}


		public static StringBuilder AppendGarbageFree( this StringBuilder sb, Color32 value )
		{
			return sb.Append( "RGBA(" )
				.AppendGarbageFree( (uint) value.r, false ).Append( ',' )
				.AppendGarbageFree( (uint) value.g, false ).Append( ',' )
				.AppendGarbageFree( (uint) value.b, false ).Append( ',' )
				.AppendGarbageFree( (uint) value.a, false ).Append( ')' );
		}
	}
}