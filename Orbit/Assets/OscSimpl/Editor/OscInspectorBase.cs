/*
	Copyright © Carl Emil Carlsen 2020
	http://cec.dk
*/

﻿using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OscSimpl
{
	public abstract class OscInspectorBase : Editor
	{
		protected StringBuilder _sb = new StringBuilder();
		protected StringBuilder _messageMonitorText = new StringBuilder( messageBufferCapacity * 500 );
		protected Queue<int> _messgeMonitorCounts = new Queue<int>( messageBufferCapacity );

		protected string _prevControlName;

		protected const int messageBufferCapacity = 10;


		protected void OnOSCMessage( OscMessage message )
		{
			if( _messgeMonitorCounts.Count > messageBufferCapacity ) {
				int count = _messgeMonitorCounts.Dequeue();
				_messageMonitorText.Remove( 0, count );
			}
			int characterCountAdded = message.ToString( _messageMonitorText, true );
			_messgeMonitorCounts.Enqueue( characterCountAdded );
		}


		protected static void BoolSettingsField( SerializedProperty prop, GUIContent label )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField( label, GUILayout.Width( 230 ) );
			GUILayout.FlexibleSpace();
			prop.boolValue = EditorGUILayout.Toggle( prop.boolValue, GUILayout.Width( 30 ) );
			EditorGUILayout.EndHorizontal();
		}


		protected void UnfocusAndUpdateUI()
		{
			GUI.FocusControl( "" );
			EditorUtility.SetDirty( target );
		}
	}
}