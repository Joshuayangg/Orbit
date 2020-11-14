/*
	Copyright © Carl Emil Carlsen 2019-2020
	http://cec.dk
*/

using System;
using System.Collections.Generic;

namespace OscSimpl
{
	public class SerializedOscMessageBuffer
	{
		byte[] _data;
		List<int> _sizes;
		int _occupiedSize;

		readonly int capacityStep;

		public int count { get { return _sizes.Count; } }

		public byte[] data { get { return _data; } }


		public SerializedOscMessageBuffer( int capacity )
		{
			capacityStep = capacity;
			_data = new byte[capacity];
			_sizes = new List<int>();
		}


		public void Add( OscMessage message )
		{
			int messageSize = message.Size();

			// Adapt size.
			int requiredCapacity = _occupiedSize + messageSize;
			if( requiredCapacity > _data.Length ) {
				byte[] newData = new byte[ _data.Length + capacityStep ];
				Buffer.BlockCopy( _data, 0, newData, 0, _occupiedSize );
				_data = newData;
			}

			// Write.
			int index = _occupiedSize;
			message.TryWriteTo( _data, ref index );

			// Update counts.
			_occupiedSize = index;
			_sizes.Add( messageSize );

			//UnityEngine.Debug.Log( "Buffered message in Frame num " + UnityEngine.Time.frameCount + "\n" + message );
		}


		public void Clear()
		{
			_sizes.Clear();
			_occupiedSize = 0;
		}


		public int GetSize( int messageIndex )
		{
			if( messageIndex < 0 || messageIndex > _sizes.Count-1 ) return 0;
			return _sizes[messageIndex];
		}
	}
}