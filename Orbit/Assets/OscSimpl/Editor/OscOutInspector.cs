/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Net;

namespace OscSimpl
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(OscOut))]
	public class OscOutInspector : OscInspectorBase
	{
		OscOut _oscOut;
		
		SerializedProperty _openOnAwakeProp;
		SerializedProperty _remoteIpAddressProp;
		SerializedProperty _portProp;
		SerializedProperty _udpBufferSizeProp;
		SerializedProperty _settingsFoldoutProp;
		SerializedProperty _messagesFoldoutProp;
		SerializedProperty _bundleMessagesAutomaticallyProp;

		readonly GUIContent _portLabel = new GUIContent( "Port", "Port for target (remote) device." );
		readonly GUIContent _modeLabel = new GUIContent( "Send Mode", "Transmission mode." );
		readonly GUIContent _remoteIpAddressLabel = new GUIContent( "Remote IP Address", "IP Address for target (remote) device. LED shows ping status; gray for pingning, yellow for fail, green for success." );
		readonly GUIContent _isOpenLabel = new GUIContent( "Is Open", "Indicates whether this OscOut object is open and ready to send. In Edit Mode OSC objects are opened and closed automatically by their inspectors." );
		readonly GUIContent _openOnAwakeLabel = new GUIContent( "Open On Awake", "Open this OscOut object automatically when Awake is invoked by Unity (at runtime). The setting is only accessible using the inspector in Edit Mode." );
		readonly GUIContent _udpBufferSizeLabel = new GUIContent( "Udp Buffer Size", "Limits the size of a single OscMessage." );
		readonly GUIContent _settingsFoldLabel = new GUIContent( "Settings" );
		readonly GUIContent _bundleMessagesAutomaticallyLabel = new GUIContent( "Bundle Messages Automatically", "Only disable if the receving end does not support OSC bundles!" );
		
		string _tempIPAddress;
		int _tempPort;
		
		OscRemoteStatus _statusInEditMode = OscRemoteStatus.Unknown;

		IEnumerator _pingEnumerator;
		DateTime _lastPingTime;
		
		// We use reflection to access _inspectorMessageEventObject, to hide it from users.
		object _inspectorMessageEventObject;

		const string ipAddressControlName = "OscOut IP Address";
		const string portControlName = "OscOut Port";
		const string bufferSizeControlName = "OscOut Buffer Size";

		const float pingInterval = 1.0f; // Seconds
		const int executionOrderNum = 5000;


		void OnEnable()
		{
			_oscOut = target as OscOut;
			
			// Get serialized properties.
			_openOnAwakeProp = serializedObject.FindProperty("_openOnAwake");
			_remoteIpAddressProp = serializedObject.FindProperty( "_remoteIpAddress" );
			_portProp = serializedObject.FindProperty("_port");
			_udpBufferSizeProp = serializedObject.FindProperty( "_udpBufferSize" );
			_settingsFoldoutProp = serializedObject.FindProperty( "_settingsFoldout" );
			_messagesFoldoutProp = serializedObject.FindProperty( "_messagesFoldout" );
			_bundleMessagesAutomaticallyProp = serializedObject.FindProperty( "_bundleMessagesAutomatically" );

			// Store socket info for change check workaround.
			_tempIPAddress = _remoteIpAddressProp.stringValue;
			_tempPort = _portProp.intValue;
			
			// Ensure that OscOut scripts will be executed late, so that if Open On Awake is enabled the socket will open before other scripts are called.
			MonoScript script = MonoScript.FromMonoBehaviour( target as MonoBehaviour );
			if( MonoImporter.GetExecutionOrder( script ) != executionOrderNum ) MonoImporter.SetExecutionOrder( script, executionOrderNum );
			
			// When object is selected in Edit Mode then we start listening.
			if( _oscOut.enabled && !Application.isPlaying && !_oscOut.isOpen ){
				_oscOut.Open( _oscOut.port, _oscOut.remoteIpAddress );
				_statusInEditMode = _oscOut.mode == OscSendMode.UnicastToSelf ? OscRemoteStatus.Connected : OscRemoteStatus.Unknown;
			}

			// Subscribe to OSC messages
			_oscOut.MapAnyMessage( OnOSCMessage );

			// If in Edit Mode, then start a coroutine that will update the connection status. Unity can't start coroutines in Runtime.
			if( !Application.isPlaying && _oscOut.mode == OscSendMode.Unicast ){
				_pingEnumerator = OscHelper.StartCoroutineInEditMode( PingCoroutine(), ref _lastPingTime );
			}
		}
		
		
		void OnDisable()
		{
			// When object is deselected in Edit Mode then we stop listening.
			if( !Application.isPlaying && _oscOut.isOpen ) _oscOut.Close();

			// Unsubscribe from messsages.
			_oscOut.UnmapAnyMessage( OnOSCMessage );
		}
		
		
		public override void OnInspectorGUI()
		{
			string currentControlName;
			bool deselect;

			// Check for key down before drawing any fields because they might consume the event.
			bool enterKeyDown = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
			
			// Load serialized object.
			serializedObject.Update();

			// Port field.
			EditorGUI.BeginChangeCheck();
			GUI.SetNextControlName( portControlName );
			int newPort = EditorGUILayout.IntField( _portLabel, _oscOut.port );
			if( EditorGUI.EndChangeCheck() ){
				_portProp.intValue = newPort;
				if( _oscOut.isOpen ) _oscOut.Close(); // Close socket while editing
			}
			currentControlName = GUI.GetNameOfFocusedControl();
			bool enterKeyDownPort = enterKeyDown && currentControlName == portControlName;
			if( enterKeyDownPort ) UnfocusAndUpdateUI();
			deselect = _prevControlName == portControlName && currentControlName != portControlName;
			if( ( deselect || enterKeyDownPort ) && !_oscOut.isOpen ){
				if( _oscOut.Open( _portProp.intValue, _remoteIpAddressProp.stringValue ) ){
					_tempPort = _portProp.intValue;
				} else {
					_portProp.intValue = _tempPort; // Undo
					_oscOut.Open( _portProp.intValue, _remoteIpAddressProp.stringValue );
				}
			}

			// Mode field.
			EditorGUI.BeginChangeCheck();
			OscSendMode newMode = (OscSendMode) EditorGUILayout.EnumPopup( _modeLabel, _oscOut.mode );
			if( EditorGUI.EndChangeCheck() && newMode != _oscOut.mode ){
				switch( newMode ){
				case OscSendMode.UnicastToSelf: 	_oscOut.Open( _oscOut.port ); break;
				case OscSendMode.Unicast: 			_oscOut.Open( _oscOut.port, OscConst.unicastAddressDefault ); break;
				case OscSendMode.Multicast:			_oscOut.Open( _oscOut.port, OscConst.multicastAddressDefault ); break;
				case OscSendMode.Broadcast:			_oscOut.Open( _oscOut.port, IPAddress.Broadcast.ToString() ); break;
				}
				UpdateStatusInEditMode();
			}

			// IP Address field.
			EditorGUI.BeginChangeCheck();
			GUI.SetNextControlName( ipAddressControlName );
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel( _remoteIpAddressLabel );
			string newIp = EditorGUILayout.TextField( _oscOut.remoteIpAddress ); // Field
			if( EditorGUI.EndChangeCheck() ){
				IPAddress ip;
				if( IPAddress.TryParse( newIp, out ip ) ) _remoteIpAddressProp.stringValue = newIp; // Accept only valid ip addresses
				if( _oscOut.isOpen ) _oscOut.Close(); // Close socket while editing
				if( !Application.isPlaying ){
					if( _pingEnumerator != null ) _pingEnumerator = null; // Don't update ping coroutine while editing
					if( _statusInEditMode != OscRemoteStatus.Unknown ) _statusInEditMode = OscRemoteStatus.Unknown;
				}
			}
			GUILayout.FlexibleSpace();
			Rect rect = GUILayoutUtility.GetRect( 16, 5 );
			rect.width = 5;
			rect.x += 3;
			rect.y += 7;
			OscRemoteStatus status = Application.isPlaying ? _oscOut.remoteStatus : _statusInEditMode;
			EditorGUI.DrawRect( rect, StatusToColor( status ) );
			EditorGUILayout.EndHorizontal();
			GUILayoutUtility.GetRect( 1, 2 ); // vertical spacing
			currentControlName = GUI.GetNameOfFocusedControl();
			bool enterKeyDownIp = enterKeyDown && currentControlName == ipAddressControlName;
			if( enterKeyDownIp ) UnfocusAndUpdateUI();
			deselect = _prevControlName == ipAddressControlName && currentControlName != ipAddressControlName;
			if( ( deselect || enterKeyDownIp ) && !_oscOut.isOpen ){ // All this mess to check for end edit, OMG!!! Not cool.
				if( _oscOut.Open( _portProp.intValue, _remoteIpAddressProp.stringValue ) ){
					_tempIPAddress = _remoteIpAddressProp.stringValue;
					UpdateStatusInEditMode();
				} else {
					_remoteIpAddressProp.stringValue = _tempIPAddress; // Undo
				}
			}
			
			// Is Open field.
			EditorGUI.BeginDisabledGroup( true );
			EditorGUILayout.Toggle( _isOpenLabel, _oscOut.isOpen );
			EditorGUI.EndDisabledGroup();

			// Open On Awake field.
			EditorGUI.BeginDisabledGroup( Application.isPlaying );
			EditorGUILayout.PropertyField( _openOnAwakeProp, _openOnAwakeLabel );
			EditorGUI.EndDisabledGroup();

			// Settings ...
			_settingsFoldoutProp.boolValue = EditorGUILayout.Foldout( _settingsFoldoutProp.boolValue, _settingsFoldLabel, true );
			if( _settingsFoldoutProp.boolValue )
			{
				EditorGUI.indentLevel++;

				// Udp Buffer Size field.
				EditorGUI.BeginChangeCheck();
				int newBufferSize = EditorGUILayout.IntField( _udpBufferSizeLabel, _udpBufferSizeProp.intValue );
				if( EditorGUI.EndChangeCheck() ) {
					if( newBufferSize >= OscConst.udpBufferSizeMin ) {
						_udpBufferSizeProp.intValue = newBufferSize;
						_oscOut.udpBufferSize = newBufferSize; // This will reopen OscOut
					}
				}

				// Bundle messages automatically.
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField( _bundleMessagesAutomaticallyLabel, GUILayout.Width( 220 ) );
				GUILayout.FlexibleSpace();
				_bundleMessagesAutomaticallyProp.boolValue = EditorGUILayout.Toggle( _bundleMessagesAutomaticallyProp.boolValue, GUILayout.Width( 30 ) );
				EditorGUILayout.EndHorizontal();
				if( !_bundleMessagesAutomaticallyProp.boolValue ) {
					EditorGUILayout.HelpBox( "Unbundled messages that are send successively are prone to be lost. Only disable this setting if your receiving end does not support OSC bundles.", MessageType.Warning );
				}

				EditorGUI.indentLevel--;
			}

			// Monitor ...
			EditorGUI.BeginDisabledGroup( !_oscOut.isOpen );
			_sb.Clear();
			_sb.Append( "Messages (" ).Append( _oscOut.messageCountSendLastFrame ).Append( ',' ).Append( OscDebug.GetPrettyByteSize( _oscOut.byteCountSendLastFrame ) ).Append( ')' );
			GUIContent messagesFoldContent = new GUIContent( _sb.ToString(), "Messages received last update" );
			_messagesFoldoutProp.boolValue = EditorGUILayout.Foldout( _messagesFoldoutProp.boolValue, messagesFoldContent, true );
			if( _messagesFoldoutProp.boolValue )
			{
				EditorGUI.indentLevel++;
				if( _messageMonitorText.Length > 0 ) _messageMonitorText.Remove( _messageMonitorText.Length-1, 1 ); // Trim last new line temporarily.
				EditorGUILayout.HelpBox( _messageMonitorText.ToString(), MessageType.None );
				if( _messageMonitorText.Length > 0 ) _messageMonitorText.Append( '\n' );
				EditorGUI.indentLevel--;
			}
			EditorGUI.EndDisabledGroup();


			// Apply
			serializedObject.ApplyModifiedProperties();
			
			// Request OnInspectorGUI to be called every frame as long as inspector is active
			EditorUtility.SetDirty( target );
			
			// Update ping coroutine manually in Edit Mode. (Unity does not run coroutines in Edit Mode)
			if( !Application.isPlaying && _pingEnumerator != null ) OscHelper.UpdateCoroutineInEditMode( _pingEnumerator, ref _lastPingTime );

			// Store name of focused control to detect unfocus events
			_prevControlName = GUI.GetNameOfFocusedControl();
		}


		void UpdateStatusInEditMode()
		{
			switch( _oscOut.mode ){
			case OscSendMode.UnicastToSelf:
				_statusInEditMode = OscRemoteStatus.Connected;
				_pingEnumerator = null;
				break;
			case OscSendMode.Unicast:
				_statusInEditMode = OscRemoteStatus.Unknown;
				if( !Application.isPlaying ) _pingEnumerator = OscHelper.StartCoroutineInEditMode( PingCoroutine(), ref _lastPingTime );
				break;
			case OscSendMode.Multicast:
				_statusInEditMode = OscRemoteStatus.Unknown;
				_pingEnumerator = null;
				break;
			case OscSendMode.Broadcast:
				_statusInEditMode = OscRemoteStatus.Unknown;
				_pingEnumerator = null;
				break;
			}
		}


		static Color StatusToColor( OscRemoteStatus status )
		{
			switch( status )
			{
			case OscRemoteStatus.Unknown: return Color.yellow;
			case OscRemoteStatus.Connected: return Color.green;
			case OscRemoteStatus.Disconnected: return Color.red;
			default: return Color.gray;
			}
		}
		

		// This coroutine is only run in Edit Mode.
		IEnumerator PingCoroutine()
		{
			while( true )
			{
				Ping ping = new Ping( _oscOut.remoteIpAddress );
				yield return new WaitForSeconds( pingInterval );
				//Debug.Log( "Ping time " + ping.time );
				_statusInEditMode = ( ping.isDone && ping.time >= 0 ) ? OscRemoteStatus.Connected : OscRemoteStatus.Disconnected;
			}
		}
	}
}