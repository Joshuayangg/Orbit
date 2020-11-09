/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2019 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System;
using UnityEngine;

namespace OscSimpl
{
	public class OscMonoBase : MonoBehaviour
	{
		[SerializeField] protected bool _openOnAwake = false;
		[SerializeField] protected int _udpBufferSize = OscConst.udpBufferSizeDefault;

		protected Action<OscMessage> _onAnyMessage;
		protected int _messageCount = 0;
	}
}

