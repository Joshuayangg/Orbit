/*
	Copyright © Carl Emil Carlsen 2019
	http://cec.dk
*/

﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OscSimpl
{
    /// <summary>
    /// An Entry holds a reference to one method on one targetObject. If the targetObject is a Component,
    /// then targetGameObject will also be set.
    /// </summary>
    [Serializable]
    public class OscMappingEntry
    {
        /// <summary>
        /// If target object is a Component, then this should hold the GameObject it is attatched to. Optional.
        /// </summary>
        public GameObject targetGameObject;

        /// <summary>
        /// The object of which the method is a member.
        /// </summary>
        public Object serializedTargetObject;

        /// <summary>
        /// Alternatively to the above; a non-serialiazed runtime object of which the method is a member.
        /// </summary>
        public object nonSerializedTargetObject;

        /// <summary>
        /// Alternatively to the above; a non-serialiazed runtime object of which the method is a member.
        /// </summary>
        public string nonSerializedTargetObjectName;

        /// <summary>
        /// Name of target method. If the target method is a property, it is the set method.
        /// </summary>
        public string targetMethodName;

        /// <summary>
        /// The parameter type of target method. For Impulse, Null and Empty OSCMessages this string is empty.
        /// </summary>
        public string targetParamAssemblyQualifiedName;


        public object targetObject {
            get {
                if( serializedTargetObject != null ) return serializedTargetObject;
                return nonSerializedTargetObject;
            }
        }


        public OscMappingEntry() { }
        public OscMappingEntry( object targetObject, string targetMethodName, string targetParamAssemblyQualifiedName )
        {
            if( targetObject is Object ){
                serializedTargetObject = targetObject as Object;
            } else {
                nonSerializedTargetObject = targetObject;
                nonSerializedTargetObjectName = targetObject != null ? targetObject.GetType().Name : string.Empty;
            } 
            this.targetMethodName = targetMethodName;
            this.targetParamAssemblyQualifiedName = targetParamAssemblyQualifiedName;
        }
    }
}