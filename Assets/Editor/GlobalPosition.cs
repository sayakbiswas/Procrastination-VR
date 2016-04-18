using UnityEngine;
using UnityEditor;
using System.Collections;

public class GlobalPosition : MonoBehaviour {
	[MenuItem("Debug/Print Global Position")]
	public static void PrintGlobalPosition() {
		if (Selection.activeGameObject != null)
		{
			Debug.Log(Selection.activeGameObject.name + " is at " + Selection.activeGameObject.transform.position);
		}
	}
}
