/*
	Copyright © Carl Emil Carlsen 2020
	http://cec.dk
*/

using UnityEngine;

/// <summary>
/// Singleton Monobehaviour containing global settings and stats for OSC simpl.
/// Add this script to the scene and manipualte using the inspector or 
/// access statically fromt other scripts. Use is optional.
/// </summary>
//[ExecuteInEditMode]
public class OscGlobals : MonoBehaviour
{
	[SerializeField] bool _logStatuses = true;
	[SerializeField] bool _logWarnings = true;

	static OscGlobals _self; 

	/// <summary>
	/// When true all status messages will be logged in the console.
	/// </summary>
	public static bool logStatuses {
		get {
			if( !_self ) return true;
			return _self._logStatuses;
		}
		set {
			if( !_self ) Init();
			_self._logStatuses = value;
		}
	}

	/// <summary>
	/// When true all warning messages will be logged in the console.
	/// </summary>
	public static bool logWarnings {
		get {
			if( !_self ) return true;
			return _self._logWarnings;
		}
		set {
			if( !_self ) Init();
			_self._logWarnings = value;
		}
	}


	static void Init()
	{
		_self = new GameObject( typeof( OscGlobals ).Name ).AddComponent<OscGlobals>();
	}


	void Awake()
	{
		// Only allow one OscSettings in the scene.
		OscGlobals[] settings = FindObjectsOfType<OscGlobals>();
		if( settings.Length > 1 ) {
			DestroyImmediate( this );
		}

		_self = this;
	}
}