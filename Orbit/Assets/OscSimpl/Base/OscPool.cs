/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System;
using System.Collections.Generic;
using OscSimpl;

/// <summary>
/// The Osc pool holds packets for reuse to reduce garbage generation.
/// </summary>
public static class OscPool
{
	static Dictionary<int,Stack<OscMessage>> _messageStacks;
	static Stack<OscBundle> _bundles;
	static int _messageCount;

	const int safetyCapacity = 1000;


	static OscPool()
	{
		_messageStacks = new Dictionary<int,Stack<OscMessage>>();
		_bundles = new Stack<OscBundle>();
	}


	public static int internalStackCount { get { return _messageStacks.Count; } }
	public static int internalMessageCount { get { return _messageCount; } }


	/// <summary>
	/// Recycle the specified message.
	/// </summary>
	public static void Recycle( OscMessage message )
	{
		//Debug.Log( Time.frameCount +  ": OscPool.Recycle: " + message.address + " " + message.GetAddressHash() + "\n" );
		if( message == null ) return;
		int hash = message.GetAddressHash();
		Stack<OscMessage> stack;
		if( !_messageStacks.TryGetValue( hash, out stack ) ){
			if( _messageStacks.Count >= safetyCapacity ) return;
			stack = new Stack<OscMessage>();
			_messageStacks.Add( hash, stack );
		}
		if( stack.Count >= safetyCapacity ) return;
		stack.Push( message );
		_messageCount++;
	}


	/// <summary>
	/// Recycle the specified bundle.
	/// </summary>
	public static void Recycle( OscBundle bundle )
	{
		if( bundle == null ) return;
		if( _bundles.Count > safetyCapacity ) return;
		_bundles.Push( bundle );
	}


	/// <summary>
	/// Recycle the specified packet.
	/// </summary>
	public static void Recycle( OscPacket packet )
	{
		if( packet == null ) return;
		if( packet  is OscMessage ) Recycle( packet as OscMessage );
		Recycle( packet as OscBundle );
	}


	public static OscMessage GetMessage( int hash )
	{
		//Debug.Log( "Pool lossy hash count: " + _messageStacks.Count );

		Stack<OscMessage> stack;
		if( _messageStacks.TryGetValue( hash, out stack ) && stack.Count > 0 ) {
			OscMessage message = stack.Pop();
			_messageCount--;
			message.Clear();
			return message;
		}

		//Debug.Log( Time.frameCount +  ": OscPool.GetMessage CREATED MESSAGE! for hash " + hash + " when there was " + _messageStacks.Count + " stacks available.\n" );
		//foreach( KeyValuePair<int, Stack<OscMessage>> pair in _messageStacks ) {
		//	Debug.Log( "\tHash: " + pair.Key + ", message count: " + pair.Value.Count + "\n" );
		//}
		return new OscMessage();
	}


	public static OscMessage GetMessage( string address )
	{
		//Debug.Log( "Pool lossy hash count: " + _messageStacks.Count );

		OscMessage message;

		// Compute lossy hash and try to get message from pool.
		int hash = OscStringHash.Pack( address );
		Stack<OscMessage> stack;
		if( _messageStacks.TryGetValue( hash, out stack ) && stack.Count > 0 ){
			message = stack.Pop();
			_messageCount--;
			message.Clear();
			// Only set address if it differs. Ordinal (raw byte) comparison.
			if( string.Compare( message.address, address, StringComparison.Ordinal ) != 0 ) message.address = address;
		} else {
			//Debug.Log( Time.frameCount +  ": OscPool.GetMessage CREATED MESSAGE! for " + address + " with hash " + hash + " when there was " + _messageStacks.Count + " stacks available.\n" );
			//foreach( KeyValuePair<int, Stack<OscMessage>> pair in _messageStacks ) {
			//	Debug.Log( "\tHash: " + pair.Key + ", message count: " + pair.Value.Count + "\n" );
			//}
			message = new OscMessage( address );
		}
		return message;
	}


	public static OscBundle GetBundle()
	{
		if( _bundles.Count > 0 ) {
			OscBundle bundle = _bundles.Pop();
			bundle.Clear();
			return bundle;
		}
		return new OscBundle();
	}


}
