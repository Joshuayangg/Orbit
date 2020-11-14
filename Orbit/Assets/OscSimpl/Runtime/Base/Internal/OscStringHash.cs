/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

namespace OscSimpl
{
	/// <summary>
	/// OscStringHash is used to hash OSC addresses from strings and from byte streams,
	/// avoiding garbage and allowing fast lookups.
	///
	/// We are using the hash method posted on StackOverflow by @user3386109 here:
	/// https://stackoverflow.com/a/33816249/2265840
	/// </summary>
	public static class OscStringHash
	{

		public static uint Pack( string text )
		{
			// Validate.
			if( text.Length < 1 || (byte) text[ 0 ] != OscConst.addressPrefixByte ) return 0;

			uint hash = 0;
			for( int i = 1; i < text.Length; i++ ) hash += ( hash + text[ i ] ) * 0xdeece66d + 0xb;
			return hash;
		}


		public static uint Pack( byte[] data, int index )
		{
			// Validate.
			if( data.Length < 1 || data[ index++ ] != OscConst.addressPrefixByte ) return 0;

			uint hash = 0;
			while( index < data.Length ) {
				byte b = data[ index++ ];
				if( b == 0 ) break; // End of string
				hash += ( hash + b ) * 0xdeece66d + 0xb; // https://stackoverflow.com/a/33816249/2265840
			}
			
			return hash;
		}
	}
}