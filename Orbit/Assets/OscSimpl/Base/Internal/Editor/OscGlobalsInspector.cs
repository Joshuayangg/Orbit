/*
	Created by Carl Emil Carlsen.
	Copyright 2020 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEditor;

namespace OscSimpl
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(OscGlobals))]
	public class OscGlobalsInspector : Editor
	{
		OscGlobals _oscGlobals;
		
		SerializedProperty _logStatuses;
		SerializedProperty _logWarnings;

		//readonly GUIContent _stackCountLabel = new GUIContent( "Stack Count", "One stack per unique OSC address is expected" );


		void OnEnable()
		{
			_oscGlobals = target as OscGlobals;

			_logStatuses = serializedObject.FindProperty( "_logStatuses" );
			_logWarnings = serializedObject.FindProperty( "_logWarnings" );
		}
		
		
		void OnDisable()
		{
			
		}


		public override void OnInspectorGUI()
		{
			// Load serialized object
			serializedObject.Update();

			// Logs.
			EditorGUILayout.LabelField( "Log settings", EditorStyles.boldLabel );
			EditorGUILayout.PropertyField( _logStatuses );
			EditorGUILayout.PropertyField( _logWarnings );

			// Pool stats.
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Pool stats", EditorStyles.boldLabel );
			//EditorGUILayout.LabelField( _stackCountLabel, OscPool.internalStackCount.ToString() );
			EditorGUILayout.LabelField( "Message count", OscPool.internalStackCount.ToString() );

			// Apply
			serializedObject.ApplyModifiedProperties();
			
			// Request OnInspectorGUI to be called every frame as long as inspector is active
			EditorUtility.SetDirty( target );
		}
	}
}