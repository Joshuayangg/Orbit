/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2018 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;

namespace OscSimpl.Examples
{
	public class GettingStartedSending : MonoBehaviour
	{
		[SerializeField] OscOut _oscOut;

		OscMessage _message2; // Cached message.

		const string address1 = "/test1";
		const string address2 = "/test2";


		void Start()
		{
			// Ensure that we have a OscOut component.
			if( !_oscOut ) _oscOut = gameObject.AddComponent<OscOut>();

			// Prepare for sending messages locally on this device on port 7000.
			_oscOut.Open( 7000 );

			// ... or, alternatively target remote devices with a IP Address.
			//oscOut.Open( 7000, "192.168.1.101" );

			// If you want to send a single value then you can use this one-liner.
			_oscOut.Send( address1, Random.value );

			// If you want to send a message with multiple values, then you
			// need to create a message, add your values and send it.
			// Always cache the messages you create, so that you can reuse them.
			_message2 = new OscMessage( address2 );
			_message2.Add( Time.frameCount ).Add( Time.time ).Add( Random.value );
			_oscOut.Send( _message2 );
		}


		void Update()
		{
			// We update the content of message2 and send it again.
			_message2.Set( 0, Time.frameCount );
			_message2.Set( 1, Time.time );
			_message2.Set( 2, Random.value );
			_oscOut.Send( _message2 );
		}
	}
}