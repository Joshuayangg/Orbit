/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System;

namespace OscSimpl
{
	[Serializable]
	public abstract class OscPacket
	{
		public abstract int Size();

		public abstract bool TryWriteTo( byte[] data, ref int index );

		public static bool TryReadFrom( byte[] data, ref int index, int byteCount, out OscPacket packet )
		{
			if( index >= byteCount ) {
				packet = null;
				UnityEngine.Debug.LogError( "FAIL");
				return false;
			}

			packet = null;

			byte prefix = data[index];
			if( prefix == OscConst.bundlePrefixByte ){
				OscBundle bundle;
				//UnityEngine.Debug.Log( "READ BUNDLE\n" );
				if( OscBundle.TryReadFrom( data, ref index, byteCount, out bundle ) ) packet = bundle;
				//else UnityEngine.Debug.LogError( "Bundle read failed.\n");
			} else if( prefix == OscConst.addressPrefixByte ){
				OscMessage message = null;
				//UnityEngine.Debug.Log( "READ MESSAGE\n" );
				if( OscMessage.TryReadFrom( data, ref index, ref message ) ) packet = message;
				//else UnityEngine.Debug.LogError( "Message read failed.\n" );
			} else {
				UnityEngine.Debug.Log( "INVALID PREFIX\n" );
				index++;
			}

			return packet != null;
		}
	}
}