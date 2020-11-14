﻿/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace OscSimpl
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(OscIn))]
	public class OscInInspector : OscInspectorBase
	{
		OscIn _oscIn;
		
		SerializedProperty _openOnAwakeProp;
		SerializedProperty _portProp;
		SerializedProperty _modeProp;
		SerializedProperty _multicastAddressProp;
		SerializedProperty _filterDuplicatesProp;
		SerializedProperty _addTimeTagsToBundledMessagesProp;
		SerializedProperty _mappingsProp;
		SerializedProperty _udpBufferSizeProp;
		SerializedProperty _settingsFoldoutProp;
		SerializedProperty _mappingsFoldoutProp;
		SerializedProperty _messagesFoldoutProp;
		
		int _tempPort;
		string _tempMulticastAddress;
		
		// We use reflection to access _inspectorMessageEventObject, to ceil it from users.
		object _inspectorMessageEventObject;

		readonly GUIContent _portLabel = new GUIContent( "Port", "Receiving Port for this computer." );
		readonly GUIContent _modeLabel = new GUIContent( "Receive Mode", "Transmission mode." );
		readonly GUIContent _localIpAddressLabel = new GUIContent( "Local IP Address", "The primary IP address of this device." );
		readonly GUIContent _localIpAddressAlternativesLabel = new GUIContent( "Local IP Alternatives", "Alternative IP addresses of this device (when you have multiple network adapters)." );
		readonly GUIContent _multicastIpAddressLabel = new GUIContent( "Multicast Address", "Multicast group address. Valid range 224.0.0.0 to 239.255.255.255." );
		readonly GUIContent _isOpenLabel = new GUIContent( "Is Open", "Indicates whether this OscIn object is open and ready to receive. In Edit Mode OSC objects are opened and closed automatically by their inspectors" );
		readonly GUIContent _openOnAwakeLabel = new GUIContent( "Open On Awake", "Open Oscin automatically when Awake is invoked by Unity (at runtime). The setting is only accessible using the inspector in Edit Mode." );
		readonly GUIContent _filterDuplicatesLabel = new GUIContent( "Filter Duplicates", "When multiple messages with the same OSC address is recieved within one Update, forward only the most recent one." );
		readonly GUIContent _addTimeTagsToBundledMessagesLabel = new GUIContent( "Add Time Tags To Bundled Messages", "When enabled, timetags from bundles are added to contained messages as last argument." );
		readonly GUIContent _addMappingButtonLabel = new GUIContent( "Add" );
		readonly GUIContent _removeMappingButtonLabel = new GUIContent( "X" );
		readonly GUIContent _udpBufferSizeLabel = new GUIContent( "Udp Buffer Size", "Limits the size of a single OscMessage." );
		readonly GUIContent _settingsFoldLabel = new GUIContent( "Settings" );
		
		const string portControlName = "OscIn Port";
		const string multicastAddressControlName = "OscIn Multicast Ip Address";
		const string bufferSizeControlName = "OscIn Buffer Size";

		const string mappingAddressFieldName = "_address";

		const int executionOrderNum = -5000;


		void OnEnable()
		{
			_oscIn = target as OscIn;

			_openOnAwakeProp = serializedObject.FindProperty("_openOnAwake");
			_portProp = serializedObject.FindProperty("_port");
			_modeProp = serializedObject.FindProperty("_mode");
			_multicastAddressProp = serializedObject.FindProperty("_multicastAddress");
			_filterDuplicatesProp = serializedObject.FindProperty("_filterDuplicates");
			_addTimeTagsToBundledMessagesProp = serializedObject.FindProperty("_addTimeTagsToBundledMessages");
			_mappingsProp = serializedObject.FindProperty("_mappings");
			_udpBufferSizeProp = serializedObject.FindProperty( "_udpBufferSize" );
			_settingsFoldoutProp = serializedObject.FindProperty("_settingsFoldout");
			_mappingsFoldoutProp = serializedObject.FindProperty("_mappingsFoldout");
			_messagesFoldoutProp = serializedObject.FindProperty("_messagesFoldout");
			
			// Store socket info for change check workaround.
			_tempPort = _portProp.intValue;
			_tempMulticastAddress = _multicastAddressProp.stringValue;
			
			// Ensure that OscIn scripts will be executed early, so it can deliver messages before we compute anything.
			MonoScript script = MonoScript.FromMonoBehaviour( target as MonoBehaviour );
			if( MonoImporter.GetExecutionOrder( script ) != executionOrderNum ) MonoImporter.SetExecutionOrder( script, executionOrderNum );

			// When object is selected in Edit Mode then we start listening.
			if( _oscIn.enabled && !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && !_oscIn.isOpen ){
				if( _oscIn.mode == OscReceiveMode.UnicastBroadcast ) _oscIn.Open( _oscIn.port );
				else _oscIn.Open( _oscIn.port, _oscIn.multicastAddress );
			}

			// Subscribe to messages.
			_oscIn.MapAnyMessage( OnOSCMessage );
		}
		
		
		void OnDisable()
		{
			// When object is deselected in Edit Mode then we stop listening.
			if( !Application.isPlaying && _oscIn.isOpen ) _oscIn.Close();

			// Unsubscribe from messsages.
			_oscIn.UnmapAnyMessage( OnOSCMessage );
		}





		public override void OnInspectorGUI()
		{
			Rect rect;
			string currentControlName;

			// Check for key down before drawing any fields because they might consume the event.
			bool enterKeyDown = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
			
			// Load serialized object
			serializedObject.Update();

			// Port field
			EditorGUI.BeginChangeCheck();
			GUI.SetNextControlName( portControlName );
			int newPort = EditorGUILayout.IntField( _portLabel, _oscIn.port );
			if( EditorGUI.EndChangeCheck() ){
				_portProp.intValue = newPort;
				if( _oscIn.isOpen ) _oscIn.Close(); // Close UDPReceiver while editing
			}
			currentControlName = GUI.GetNameOfFocusedControl();
			bool enterKeyDownPort = enterKeyDown && currentControlName == portControlName;
			if( enterKeyDownPort ) UnfocusAndUpdateUI();
			bool deselect = _prevControlName == portControlName && currentControlName != portControlName;
			if( ( deselect || enterKeyDownPort ) && !_oscIn.isOpen ){
				if( _oscIn.Open( _portProp.intValue ) ){
					_tempPort = _portProp.intValue;
				} else {
					_portProp.intValue = _tempPort; // undo
					_oscIn.Open( _portProp.intValue );
				}
			}

			// Mode field
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( _modeProp, _modeLabel );
			if( EditorGUI.EndChangeCheck() ){
				switch( (OscReceiveMode) _modeProp.enumValueIndex ){
					case OscReceiveMode.UnicastBroadcast:
						_oscIn.Open( _oscIn.port, string.Empty );
						_multicastAddressProp.stringValue = string.Empty;
						break;
					case OscReceiveMode.UnicastBroadcastMulticast:
						_oscIn.Open( _oscIn.port, OscConst.multicastAddressDefault );
						_multicastAddressProp.stringValue = OscConst.multicastAddressDefault;
						break;
				}
			}

			// Multicast field
			if( _oscIn.mode == OscReceiveMode.UnicastBroadcastMulticast )
			{
				EditorGUI.BeginChangeCheck();
				GUI.SetNextControlName( multicastAddressControlName );
				EditorGUILayout.PropertyField(_multicastAddressProp, _multicastIpAddressLabel );
				if( EditorGUI.EndChangeCheck() ){
					if( _oscIn.isOpen ) _oscIn.Close(); // Close socket while editing
				}
				currentControlName = GUI.GetNameOfFocusedControl();
				bool enterKeyDownMulticastIpAddress = enterKeyDown && currentControlName == multicastAddressControlName;
				if( enterKeyDownMulticastIpAddress ) UnfocusAndUpdateUI();
				deselect = _prevControlName == multicastAddressControlName && currentControlName != multicastAddressControlName;
				if( ( deselect || enterKeyDownMulticastIpAddress ) && !_oscIn.isOpen ){
					if( _oscIn.Open( _portProp.intValue, _multicastAddressProp.stringValue ) ){
						_tempMulticastAddress = _multicastAddressProp.stringValue;
					} else {
						_multicastAddressProp.stringValue = _tempMulticastAddress; // undo
						_oscIn.Open( _portProp.intValue, _multicastAddressProp.stringValue );
					}
				}
			}

			// IP Address field.
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel( _localIpAddressLabel );
			EditorGUILayout.LabelField( " " );
			rect = GUILayoutUtility.GetLastRect(); // UI voodoo to position the selectable label perfectly
			EditorGUI.SelectableLabel( rect, OscIn.localIpAddress );
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			// Alternative IP Addresses field.
			if( OscIn.localIpAddressAlternatives.Count > 0 ) {
				int i = 0;
				foreach( string ip in OscIn.localIpAddressAlternatives ) {
					EditorGUILayout.BeginVertical();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel( new GUIContent( _localIpAddressAlternativesLabel.text + "[" + i + "]", _localIpAddressAlternativesLabel.tooltip ) );
					EditorGUILayout.LabelField( " " );
					rect = GUILayoutUtility.GetLastRect(); // UI voodoo to position the selectable label perfectly
					EditorGUI.SelectableLabel( rect, ip );
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
					i++;
				}
			}

			// Is Open field
			EditorGUI.BeginDisabledGroup( true );
			EditorGUILayout.Toggle( _isOpenLabel, _oscIn.isOpen );
			EditorGUI.EndDisabledGroup();

			// Open On Awake field
			EditorGUI.BeginDisabledGroup( Application.isPlaying );
			EditorGUILayout.PropertyField( _openOnAwakeProp, _openOnAwakeLabel );
			EditorGUI.EndDisabledGroup();


			// Settings ...
			_settingsFoldoutProp.boolValue = EditorGUILayout.Foldout(_settingsFoldoutProp.boolValue, _settingsFoldLabel, true );

			if( _settingsFoldoutProp.boolValue )
			{
				EditorGUI.indentLevel++;

				// Udp Buffer Size field.
				EditorGUI.BeginChangeCheck();
				int newBufferSize = EditorGUILayout.IntField( _udpBufferSizeLabel, _udpBufferSizeProp.intValue );
				if( EditorGUI.EndChangeCheck() ) {
					if( newBufferSize >= OscConst.udpBufferSizeMin ) {
						_udpBufferSizeProp.intValue = newBufferSize;
						_oscIn.udpBufferSize = newBufferSize; // This will reopen OscIn
					}
				}

				// Filter Duplicates field
				BoolSettingsField( _filterDuplicatesProp, _filterDuplicatesLabel );

				// Add Time Tags To Bundled Messages field.
				BoolSettingsField(_addTimeTagsToBundledMessagesProp, _addTimeTagsToBundledMessagesLabel );

				EditorGUI.indentLevel--;
			}

			// Mappings ...
			string mappingsFoldLabel = "Mappings (" + _mappingsProp.arraySize + ")";
			_mappingsFoldoutProp.boolValue = EditorGUILayout.Foldout(_mappingsFoldoutProp.boolValue, mappingsFoldLabel, true );
			if( _mappingsFoldoutProp.boolValue )
			{
				EditorGUI.indentLevel++;
				EditorGUI.BeginDisabledGroup( Application.isPlaying );

				// Mapping elements ..
				int removeIndexRequsted = -1;

				for( int m=0; m<_mappingsProp.arraySize; m++ )
				{
					SerializedProperty mappingProp = _mappingsProp.GetArrayElementAtIndex( m );

					// Mapping field (using custom property drawer)
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( mappingProp );
					if( EditorGUI.EndChangeCheck() ){
						SerializedProperty addressProp = mappingProp.FindPropertyRelative( mappingAddressFieldName );
						addressProp.stringValue = GetSanitizedAndUniqueAddress( _mappingsProp, m, addressProp.stringValue );
					}
					
					// Remove mapping button
					rect = GUILayoutUtility.GetLastRect();
					rect = EditorGUI.IndentedRect( GUILayoutUtility.GetLastRect() );
					rect.x = rect.x + rect.width - OscMappingDrawer.removeButtonWidth - 7;
					rect.y += 6;
					rect.width = OscMappingDrawer.removeButtonWidth;
					rect.height = OscMappingDrawer.removeButtonHeight;
					if( GUI.Button( rect, _removeMappingButtonLabel ) ) removeIndexRequsted = m;
				}
				
				// Handle mapping removal ..
				if( removeIndexRequsted != -1 ){
					_mappingsProp.DeleteArrayElementAtIndex( removeIndexRequsted );
 
				}
				
				// Add mapping button
				rect = EditorGUI.IndentedRect( GUILayoutUtility.GetRect( 20, 30 ) );
				if( GUI.Button( rect, _addMappingButtonLabel ) )
				{
					string newAddress = GetSanitizedAndUniqueAddress( _mappingsProp, -1, "/" );
					FieldInfo meppingsInfo =  serializedObject.targetObject.GetType().GetField( _mappingsProp.propertyPath, BindingFlags.Instance | BindingFlags.NonPublic );
					List<OscMapping> mappings = (List<OscMapping>) meppingsInfo.GetValue( serializedObject.targetObject );
					OscMapping mapping = new OscMapping( newAddress, OscMessageType.OscMessage );
					
					mapping.AddEmptyEntry();
					mappings.Add( mapping );
				}
				EditorGUILayout.Space();

				EditorGUI.EndDisabledGroup();
				EditorGUI.indentLevel--;
			}

			// Messages ...
			_sb.Clear();
			_sb.Append( "Messages (" ).Append( _oscIn.messageCountReceivedLastFrame ).Append(",").Append( OscDebug.GetPrettyByteSize( _oscIn.byteCountReceivedLastFrame ) ).Append( ")" );
			GUIContent messagesFoldContent = new GUIContent( _sb.ToString(), "Messages received last update" );
			_messagesFoldoutProp.boolValue = EditorGUILayout.Foldout(_messagesFoldoutProp.boolValue, messagesFoldContent, true );
			if( _messagesFoldoutProp.boolValue )
			{
				EditorGUI.indentLevel++;
				if( _messageMonitorText.Length > 0 ) _messageMonitorText.Remove( _messageMonitorText.Length-1, 1 ); // Trim last new line temporarily.
				EditorGUILayout.HelpBox( _messageMonitorText.ToString(), MessageType.None );
				if( _messageMonitorText.Length > 0 ) _messageMonitorText.Append( '\n' );
				EditorGUI.indentLevel--;
			}

			// Apply
			serializedObject.ApplyModifiedProperties();
			
			// Request OnInspectorGUI to be called every frame as long as inspector is active
			EditorUtility.SetDirty( target );

			// Store name of focused control to detect unfocus events
			_prevControlName = GUI.GetNameOfFocusedControl();
		}


		static string GetSanitizedAndUniqueAddress( SerializedProperty mappingsProp, int mappingIndex, string address )
		{
			// Sanitize
			OscAddress.Sanitize( ref address );

			// Gather all addresses, excluding the one from the mapping we are messing with (if any).
			List<string> addresses = new List<string>();
			for( int m = 0; m < mappingsProp.arraySize; m++ ){
				if( m != mappingIndex ){
					addresses.Add( mappingsProp.GetArrayElementAtIndex( m ).FindPropertyRelative( mappingAddressFieldName ).stringValue );
				}
			}
				
			// Make unique
			if( !addresses.Contains( address ) ) return address;
			string addressWithoutNumber = address.TrimEnd( new char[]{ '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' } );
			for( int i=1; ; i++ )
			{
				string candidateAddress = addressWithoutNumber + i;
				if( !addresses.Contains( candidateAddress ) ) return candidateAddress;
			}
		}
	}
}