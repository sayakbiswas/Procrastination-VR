using UnityEngine;
using UnityEditor;
using System.Collections;
using RogoDigital.Lipsync;

[CustomEditor(typeof(LipSyncData))]
public class LipSyncDataEditor : Editor {

	public override void OnInspectorGUI () {
		EditorGUILayout.HelpBox("LipSyncData files are edited with the LipSync Clip Editor." , MessageType.Info);
		EditorGUILayout.Space();
		if(GUILayout.Button("Edit LipSync Data" , GUILayout.Height(50))) {
			LipSyncClipSetup.ShowWindow(AssetDatabase.GetAssetPath(target) , false);
		}
	}

	static LipSyncDataEditor () {
		EditorApplication.projectWindowItemOnGUI += LipSyncDataEditor.WindowItemDelegate();
	}
	
	static EditorApplication.ProjectWindowItemCallback WindowItemDelegate() {
		// Provide Unity with a reference to the function that will be called every update. 
		return new EditorApplication.ProjectWindowItemCallback(OnProjectWindowItem);
	}
	
	static void OnProjectWindowItem(string itemGUID, Rect itemRect) {
		if(Selection.activeObject is LipSyncData){
			//Draw project view summary

			// Sanity check - make sure we're dealing with the item we selected. The itemGUID parameter
			// contains the asset GUID of the current item being updated by the project window.
			if (AssetDatabase.GUIDToAssetPath(itemGUID) == AssetDatabase.GetAssetPath(Selection.activeObject)) {
				if (Event.current.isMouse && Event.current.clickCount == 2) {
					LipSyncClipSetup.ShowWindow(AssetDatabase.GUIDToAssetPath(itemGUID) , false);
					
					Event.current.Use();
				}
			}
		}
	}
}
