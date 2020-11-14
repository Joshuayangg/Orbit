/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2019 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEditor;


namespace OscSimpl
{
	public static class OscEditorUI
	{
		public static Color boxColor = EditorGUIUtility.isProSkin ? new Color( 0.26f, 0.26f, 0.26f, 1 ) : new Color( 0.65f, 0.65f, 0.65f, 1 );
		public static Color32 eventHandlerHeaderColor = EditorGUIUtility.isProSkin ? new Color32( 93, 93, 93, 255 ) : new Color32( 230, 230, 230, 255 );
	}
}