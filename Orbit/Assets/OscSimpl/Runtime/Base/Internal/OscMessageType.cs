﻿/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2019 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System;

namespace OscSimpl
{
	/// <summary>
	/// Enum representing the expected argument content of a message.
	/// </summary>
    ///
    [Serializable]
	public enum OscMessageType
	{
		// Full message
		OscMessage,

		// Expecting a single argument of type.
		Float,
		Double,
		Int,
		Long,
		String,
		Char,
		Bool,
		Color,
		Blob,
		TimeTag,
		Midi,

		// Expecting a single argument of type impulse or null, or no arguments.
		ImpulseNullEmpty,
	}
}