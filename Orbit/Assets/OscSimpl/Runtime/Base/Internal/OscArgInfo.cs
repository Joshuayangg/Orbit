/*
	Created by Carl Emil Carlsen.
	Copyright 2018-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System;

namespace OscSimpl
{
	[Serializable]
	struct OscArgInfo
	{
		public byte tagByte;		// Note that tagByte and argType MUST be in sync at all times.
		public OscArgType argType;	// We store it because we usually know the type when we set it, and so we don't have to derrive it later.
		public short byteCount;
		public short byteIndex;

		public static readonly OscArgInfo nullInfo = new OscArgInfo( OscConst.tagNullByte, OscArgType.Null, 0 );
		public static readonly OscArgInfo impulseInfo = new OscArgInfo( OscConst.tagImpulseByte, OscArgType.Impulse, 0 );
		public static readonly OscArgInfo boolTrueInfo = new OscArgInfo( OscConst.tagTrueByte, OscArgType.Bool,  0 );
		public static readonly OscArgInfo boolFalseInfo = new OscArgInfo( OscConst.tagFalseByte, OscArgType.Bool, 0 );
		public static readonly OscArgInfo floatInfo = new OscArgInfo( OscConst.tagFloatByte, OscArgType.Float, 4 );
		public static readonly OscArgInfo intInfo = new OscArgInfo( OscConst.tagIntByte, OscArgType.Int, 4 );
		public static readonly OscArgInfo charInfo = new OscArgInfo( OscConst.tagCharByte, OscArgType.Char, 4 );
		public static readonly OscArgInfo colorInfo = new OscArgInfo( OscConst.tagColorByte, OscArgType.Color, 4 );
		public static readonly OscArgInfo midiInfo = new OscArgInfo( OscConst.tagMidiByte, OscArgType.Midi, 4 );
		public static readonly OscArgInfo doubleInfo = new OscArgInfo( OscConst.tagDoubleByte, OscArgType.Double, 8 );
		public static readonly OscArgInfo longInfo = new OscArgInfo( OscConst.tagLongByte, OscArgType.Long, 8 );
		public static readonly OscArgInfo timeTagInfo = new OscArgInfo( OscConst.tagTimetagByte, OscArgType.TimeTag, 8 );
		public static readonly OscArgInfo eightByteBlobInfo = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, 4 + 8 ); // Blobs have a 4 byte size prefix.
		public static readonly OscArgInfo twelveByteBlobInfo = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, 4 + 12 );
		public static readonly OscArgInfo sixteenByteBlobInfo = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, 4 + 16 );
		public static readonly OscArgInfo sixtyfourByteBlobInfo = new OscArgInfo( OscConst.tagBlobByte, OscArgType.Blob, 4 + 64 );
		public static readonly OscArgInfo undefinedInfo = new OscArgInfo( OscConst.tagUnsupportedByte, OscArgType.Unsupported, 0 );


		public OscArgInfo( byte tagByte, OscArgType argType, short byteCount, short byteIndex = 0 )
		{
			this.tagByte = tagByte;
			this.argType = argType;
			this.byteCount = byteCount;
			this.byteIndex = byteIndex;
		}


		public override string ToString()
		{
			return "(" + ((char) tagByte) + ", " + byteCount + "," + OscConverter.ToArgType( tagByte ) + ")";
		}
	}
}