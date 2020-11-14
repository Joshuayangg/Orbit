/*
	Created by Carl Emil Carlsen.
	Copyright 2016-2019 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace OscSimpl
{
	[CustomPropertyDrawer( typeof( OscMapping ) )]
	public class OscMappingDrawer : PropertyDrawer
	{
        Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();

        public const int removeButtonWidth = 20;
		public const int removeButtonHeight = 18;
		const int messageTypeDropdownWidth = 125;
		public const int fieldHeight = 18;

		const int verticalBoxPadding = 3;

        const float entriesMarginTop = 5;
        const float entriesMarginBottom = 5;

        const int horizontalFieldPadding = 8;


		public override float GetPropertyHeight( SerializedProperty mappingProp, GUIContent label )
		{
			SerializedProperty typeProp = mappingProp.FindPropertyRelative( "_type" );
            SerializedProperty entriesProp = mappingProp.FindPropertyRelative( "_entries" );
            
            float elementHeight = OscMappingEntryDrawer.GetPropertyHeight();
            return EditorGUIUtility.singleLineHeight * 2 + elementHeight * Mathf.Max( entriesProp.arraySize, 1 ) + entriesMarginTop + entriesMarginBottom + verticalBoxPadding * 2;
        }
		
		
		public override void OnGUI( Rect rect, SerializedProperty mappingProp, GUIContent label )
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty( rect, label, mappingProp );
			
			// Get properties.
			SerializedProperty typeProp = mappingProp.FindPropertyRelative( "_type" );
			SerializedProperty addressProp = mappingProp.FindPropertyRelative( "_address" );
            SerializedProperty entriesProp = mappingProp.FindPropertyRelative( "_entries" );

            // Apply indentation.
            rect = EditorGUI.IndentedRect( rect );

			// Draw background.
			//EditorGUI.DrawRect( rect, OscEditorUI.boxColor );

			// Adjust rect and store.
			rect.yMin += verticalBoxPadding;
			rect.yMax -= verticalBoxPadding;
			Rect area = rect;
            //float handlerHeight = EditorGUI.GetPropertyHeight( handlerProp );
            float handlerHeight = EditorGUI.GetPropertyHeight( entriesProp );

            // Check area.
            //EditorGUI.DrawRect( area, Color.red );

            // Display entries.
            rect.y += 2;
			rect.xMin -= 1;
			rect.xMax += 2;
			rect.height = handlerHeight;
            ReorderableList entryListUI;
            if( !_reorderableLists.TryGetValue( entriesProp.propertyPath, out entryListUI ) ) {
                entryListUI = BuildEntryListUI( entriesProp );
                _reorderableLists.Add( entriesProp.propertyPath, entryListUI );
            }
            entryListUI.DoList( rect );

            // Draw a rect covering the header of the event handler.
            rect.xMin += 0;
			rect.xMax -= 0;
			rect.y -= 2;
			rect.height = 24;
			EditorGUI.DrawRect( rect, OscEditorUI.eventHandlerHeaderColor );

			// Draw address field.
			rect = area;
			rect.xMin -= 12;
			rect.y += verticalBoxPadding;
			rect.height = fieldHeight;
			rect.xMax -= messageTypeDropdownWidth + removeButtonWidth;
			EditorGUI.BeginChangeCheck();
			string newString = EditorGUI.TextField( rect, addressProp.stringValue );
			if( EditorGUI.EndChangeCheck() ) addressProp.stringValue = newString;

			// Draw OscMessageType dropdown.
			rect = area;
			rect.y += verticalBoxPadding;
			rect.height = fieldHeight;
			rect.xMax -= removeButtonWidth + horizontalFieldPadding + 1;
			rect.xMin = rect.xMax - messageTypeDropdownWidth;
            rect.x -= 2;
			EditorGUI.BeginChangeCheck();
			int newEnumIndex = (int) (OscMessageType) EditorGUI.EnumPopup( rect, (OscMessageType) typeProp.enumValueIndex );
			if( EditorGUI.EndChangeCheck() ){
                typeProp.enumValueIndex = newEnumIndex;
            }

			EditorGUI.EndProperty();
		}


        static ReorderableList BuildEntryListUI( SerializedProperty entriesProp )
        {
            ReorderableList list = new ReorderableList( entriesProp.serializedObject, entriesProp, true, true, true, true );

            list.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) => {
                EditorGUI.PropertyField( rect, entriesProp.GetArrayElementAtIndex( index ) );
            };
            list.draggable = true;
            list.elementHeight = OscMappingEntryDrawer.GetPropertyHeight();
            list.drawNoneElementCallback = ( rect ) => { /* nothing please */ };
            // Remove duplicates methods on add.
            list.onAddCallback = ( ReorderableList l ) => {
                entriesProp.arraySize++;
                SerializedProperty newEntryProp = entriesProp.GetArrayElementAtIndex( entriesProp.arraySize - 1 );
                SerializedProperty targetMethodNameProp = newEntryProp.FindPropertyRelative( "targetMethodName" );
                SerializedProperty targetParamAssemblyQualifiedNameProp = newEntryProp.FindPropertyRelative( "targetParamAssemblyQualifiedName" );
                targetMethodNameProp.stringValue = "";
                targetParamAssemblyQualifiedNameProp.stringValue = "";

            };
            // Set the color of the selected list item
            list.drawElementBackgroundCallback = ( rect, index, active, focused ) => {
                if( active ) EditorGUI.DrawRect( rect, new Color32( 61, 96, 145, 255 ) );
            };
            return list;
        }
    }
}