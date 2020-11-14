/*
	Copyright © Carl Emil Carlsen 2019
	http://cec.dk
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OscSimpl
{
	[CustomPropertyDrawer( typeof( OscMappingEntry ) )]
	public class OscMappingEntryDrawer : PropertyDrawer
	{
        const float verticalPadding = 2;

        static Dictionary<Type,Dictionary<Type,CandidateLists>> sharedMethodsLookups;
        static char[] anonymousChars = new char[] { '<', '>' };


    public static float GetPropertyHeight()
        {
            return EditorGUIUtility.singleLineHeight + verticalPadding * 2;
        }


		public override float GetPropertyHeight( SerializedProperty entryProp, GUIContent label )
		{
            return GetPropertyHeight();
		}
		
		
		public override void OnGUI( Rect areaRect, SerializedProperty entryProp, GUIContent label )
		{
            // Begin.
			EditorGUI.BeginProperty( areaRect, label, entryProp );

            // Find property path to OscEvent in which this entry lives.
            String mappingPropPath = entryProp.propertyPath.Substring( 0, entryProp.propertyPath.IndexOf("_entries") -1 );

            // Get properties.
            SerializedProperty mappingProp = entryProp.serializedObject.FindProperty( mappingPropPath );
            SerializedProperty mappingTypeProp = mappingProp.FindPropertyRelative( "_type" );
            SerializedProperty targetGameObjectProp = entryProp.FindPropertyRelative( "targetGameObject" );
            SerializedProperty serializedTargetObjectProp = entryProp.FindPropertyRelative( "serializedTargetObject" );
            SerializedProperty nonSerializedTargetObjectNameProp = entryProp.FindPropertyRelative( "nonSerializedTargetObjectName" );
            SerializedProperty targetMethodNameProp = entryProp.FindPropertyRelative( "targetMethodName" );
            SerializedProperty targetParamAssemblyNameProp = entryProp.FindPropertyRelative( "targetParamAssemblyQualifiedName" );

            //Debug.Log("oscEventParamAssemblyNameProp: " + oscEventParamAssemblyNameProp.name + " " + oscEventParamAssemblyNameProp.propertyPath + "  " + oscEventParamAssemblyNameProp.stringValue );

            // Get the parameter type.
            Type mappingParamType = OscMapping.GetParamType( (OscMessageType) mappingTypeProp.enumValueIndex );

            // Prepare positioning.
            Rect rect = areaRect;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width = (areaRect.width+16+26) / 3f;
            rect.y += verticalPadding;
            rect.x -= 16;

            // Target GameObject.
            bool displayGameObjectField = ( Application.isPlaying && targetGameObjectProp.objectReferenceValue != null ) || !Application.isPlaying;
            if( displayGameObjectField ) {
                targetGameObjectProp.objectReferenceValue = EditorGUI.ObjectField( rect, targetGameObjectProp.objectReferenceValue, typeof( GameObject ), true );
                if( targetGameObjectProp.objectReferenceValue is GameObject && !( serializedTargetObjectProp.objectReferenceValue is Component ) ) serializedTargetObjectProp.objectReferenceValue = null;
            } else {
                EditorGUI.LabelField( rect, string.Empty );
            }
            rect.x += rect.width - 13;

            // Target Object.
            rect.y += 1; // The default object selection GUI is taller than drop down.
            bool isAnonymous = targetMethodNameProp.stringValue.IndexOfAny( anonymousChars ) > -1;
            if( isAnonymous ) {
                EditorGUI.LabelField( rect, string.Empty );
            } else {
                bool displayObjectDropdown = targetGameObjectProp.objectReferenceValue != null;
                if( displayObjectDropdown ) {
                    if( targetGameObjectProp.objectReferenceValue != null ) {
                        GameObject go = targetGameObjectProp.objectReferenceValue as GameObject;
                        Component[] components = go.GetComponents<Component>();
                        int selectedComponentIndex = Array.IndexOf( components, serializedTargetObjectProp.objectReferenceValue as Component );
                        GUIContent[] componentOptions = new GUIContent[components.Length];
                        for( int i = 0; i < components.Length; i++ ) componentOptions[i] = new GUIContent( components[i].GetType().Name );
                        int newSelectedComponentIndex = EditorGUI.Popup( rect, selectedComponentIndex, componentOptions );
                        if( newSelectedComponentIndex != selectedComponentIndex ){
                            serializedTargetObjectProp.objectReferenceValue = components[newSelectedComponentIndex];
                            targetMethodNameProp.stringValue = string.Empty;
                        }
                    } else if( serializedTargetObjectProp.objectReferenceValue != null ) {
                        EditorGUI.BeginDisabledGroup( true );
                        serializedTargetObjectProp.objectReferenceValue = EditorGUI.ObjectField( rect, serializedTargetObjectProp.objectReferenceValue, typeof( Object ), true );
                        EditorGUI.EndDisabledGroup();
                    }
                } else if( !string.IsNullOrEmpty( nonSerializedTargetObjectNameProp.stringValue ) ) {
                    // Non serialized object.
                    EditorGUI.LabelField( rect, nonSerializedTargetObjectNameProp.stringValue );
                } else {
                    EditorGUI.LabelField( rect, string.Empty );
                }
            }
            rect.x += rect.width - 13;

            // Method.
            if( isAnonymous ) {
                EditorGUI.LabelField( rect, "Anonymous" );
            } else {
                bool displayMethodDropdown = serializedTargetObjectProp.objectReferenceValue != null;
                if( displayMethodDropdown ) {
                    bool isMappingParamNull = mappingParamType == null;
                    CandidateLists candidateLists;
                    GetMethodOptions( serializedTargetObjectProp.objectReferenceValue.GetType(), mappingParamType, out candidateLists );
                    int selectedMethodNameIndex = Array.IndexOf( candidateLists.methodNames, targetMethodNameProp.stringValue );
                    bool isPrivateMethod = selectedMethodNameIndex == -1 && !string.IsNullOrEmpty( targetMethodNameProp.stringValue ) && Application.isPlaying;
                    if( isPrivateMethod ) {
                        EditorGUI.LabelField( rect, targetMethodNameProp.stringValue );
                    } else {
                        int newSelectedMethodNameIndex = EditorGUI.Popup( rect, selectedMethodNameIndex, candidateLists.methodOptions );
                        if( newSelectedMethodNameIndex != selectedMethodNameIndex ){
                            selectedMethodNameIndex = newSelectedMethodNameIndex;
                            targetMethodNameProp.stringValue = candidateLists.methodNames[selectedMethodNameIndex];
                            if( !isMappingParamNull ) targetParamAssemblyNameProp.stringValue = candidateLists.paramTypes[selectedMethodNameIndex].AssemblyQualifiedName;
                        }
                    }
                } else {
                    EditorGUI.LabelField( rect, targetMethodNameProp.stringValue );
                }
            }

            // End.
			EditorGUI.EndProperty();
		}

        
        // All this stuff is just to make sure we only to the heavy reflection operation once per objecttype->paramtype.
        static void GetMethodOptions( Type objectType, Type mappingParamType, out CandidateLists candidateLists )
        {
            // Create object key dictionary.
            if( sharedMethodsLookups == null ) sharedMethodsLookups = new Dictionary<Type,Dictionary<Type,CandidateLists>>();

            // Get or create param key dictionary.
            Dictionary<Type,CandidateLists> paramLookup;
            if( !sharedMethodsLookups.TryGetValue( objectType, out paramLookup ) ) {
                paramLookup = new Dictionary<Type,CandidateLists>();
                sharedMethodsLookups.Add( objectType, paramLookup );
            }

            // Get or create method data.
            bool isParamNull = mappingParamType == null;
            if( isParamNull ) mappingParamType = typeof( NullType );
            if( paramLookup.TryGetValue( mappingParamType, out candidateLists ) ) return;

            // Create new candidate list data.
            candidateLists = new CandidateLists();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            MethodInfo[] methodInfos = objectType.GetMethods( flags );
            List<string> methodNameList = new List<string>();
            List<GUIContent> optionList = new List<GUIContent>();
            List<Type> paramTypeList = new List<Type>();

            bool isPotentiallyWrappable = !isParamNull && !mappingParamType.IsPrimitive && !mappingParamType.IsValueType; // Only wrap when outlet param is object.
            foreach( MethodInfo methodInfo in methodInfos )
            {
                // Disregard methods that return somthing.
                if( methodInfo.ReturnType != typeof( void ) ) continue;

                ParameterInfo[] paramInfos = methodInfo.GetParameters();
                if( isParamNull && paramInfos.Length == 0 ) {
                    // Methods without arguments.
                    optionList.Add( new GUIContent( methodInfo.Name ) );
                    methodNameList.Add( methodInfo.Name );
                } else if( paramInfos.Length == 1 ) {
                    // Methods with one arguments.
                    Type candidateParamType = paramInfos[0].ParameterType;
                    if(
                        candidateParamType == mappingParamType || 
                        ( isPotentiallyWrappable && candidateParamType.IsAssignableFrom( mappingParamType ) )
                    ){
                        optionList.Add( new GUIContent( methodInfo.Name ) );
                        methodNameList.Add( methodInfo.Name );
                        paramTypeList.Add( candidateParamType );
                    }
                }
            }

            // Store.
            candidateLists.methodNames = methodNameList.ToArray();
            candidateLists.methodOptions = optionList.ToArray();
            candidateLists.paramTypes = paramTypeList.ToArray();
            paramLookup.Add( mappingParamType, candidateLists );
        }


        class CandidateLists
        {
            // We need seperate arrays so that we can pass methodOptions to EditorGUI.Popup.
            public string[] methodNames;
            public GUIContent[] methodOptions;
            public Type[] paramTypes;
        }


        class NullType { }
    }
}