using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using RogoDigital.Lipsync;

[CustomEditor(typeof(LipSyncProject))]
public class LipSyncProjectSettings : Editor {
	private Texture2D logo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/Dark/logo_component.png");

	private LipSyncProject myTarget;
	private SerializedObject serializedTarget;

	void OnEnable () {
		myTarget = (LipSyncProject)target;
		serializedTarget = new SerializedObject(target);

		if(!EditorGUIUtility.isProSkin){
			logo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/Light/logo_component.png");
		}
	}

	void OnDisable () {
		LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));

		LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
		newSettings.emotions = myTarget.emotions;
		newSettings.emotionColors = myTarget.emotionColors;
		newSettings.gestures = myTarget.gestures;

		EditorUtility.CopySerialized (newSettings, settings);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		DestroyImmediate(newSettings);
	}

	public override void OnInspectorGUI () {

		serializedTarget.Update();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Box(logo , GUIStyle.none);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Box("Project Settings" , EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(30);
		GUILayout.Box("Animation Emotions" , EditorStyles.boldLabel);
		EditorGUILayout.Space();

		for(int a = 0 ; a < myTarget.emotions.Length ; a++){
			Rect lineRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
			if(a%2 == 0) {
				GUI.Box(lineRect , "" , (GUIStyle)"hostview");
			}
			GUILayout.Space(10);
			GUILayout.Box((a+1).ToString() , EditorStyles.label);
			EditorGUILayout.Space();

			myTarget.emotions[a] = GUILayout.TextArea(myTarget.emotions[a] , EditorStyles.label , GUILayout.MinWidth(130));
			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect() , MouseCursor.Text);
			GUILayout.FlexibleSpace();
			myTarget.emotionColors[a] = EditorGUILayout.ColorField(myTarget.emotionColors[a] ,  GUILayout.MaxWidth(280));
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = new Color(0.8f , 0.3f, 0.3f);
			if(GUILayout.Button("Delete" , GUILayout.MaxWidth(70) , GUILayout.Height(18))){
				List<string> tempemote = new List<string>();
				List<Color> tempcolors = new List<Color>();

				for(int b = 0 ; b < myTarget.emotions.Length ; b++){
					tempemote.Add(myTarget.emotions[b]);
					tempcolors.Add(myTarget.emotionColors[b]);
				}

				tempemote.Remove(myTarget.emotions[a]);
				tempcolors.Remove(myTarget.emotionColors[a]);

				myTarget.emotions = tempemote.ToArray();
				myTarget.emotionColors = tempcolors.ToArray();

				LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));

				LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
				newSettings.emotions = myTarget.emotions;
				newSettings.emotionColors = myTarget.emotionColors;
				newSettings.gestures = myTarget.gestures;

				EditorUtility.CopySerialized (newSettings, settings);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				DestroyImmediate(newSettings);
			}
			GUI.backgroundColor = Color.white;
			GUILayout.Space(10);
			EditorGUILayout.EndHorizontal();
		}

		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Add Emotion" , GUILayout.MaxWidth(300) , GUILayout.Height(25))){
			List<string> tempemote = new List<string>();
			List<Color> tempcolors = new List<Color>();
			for(int b = 0 ; b < myTarget.emotions.Length ; b++){
				tempemote.Add(myTarget.emotions[b]);
				tempcolors.Add(myTarget.emotionColors[b]);
			}

			string tempName = "New Emotion";
			
			tempemote.Add(tempName);
			tempcolors.Add(Color.white);

			myTarget.emotions = tempemote.ToArray();
			myTarget.emotionColors = tempcolors.ToArray();

			LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));
			
			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = myTarget.emotions;
			newSettings.emotionColors = myTarget.emotionColors;
			newSettings.gestures = myTarget.gestures;

			EditorUtility.CopySerialized (newSettings, settings);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(20);

		GUILayout.Box("Animation Gestures" , EditorStyles.boldLabel);
		EditorGUILayout.Space();

		for(int a = 0 ; a < myTarget.gestures.Count ; a++){
			Rect lineRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
			if(a%2 == 0) {
				GUI.Box(lineRect , "" , (GUIStyle)"hostview");
			}
			GUILayout.Box((a+1).ToString() , EditorStyles.label);
			EditorGUILayout.Space();

			myTarget.gestures[a] = GUILayout.TextArea(myTarget.gestures[a] , EditorStyles.label , GUILayout.MinWidth(130));
			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect() , MouseCursor.Text);
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = new Color(0.8f , 0.3f, 0.3f);
			if(GUILayout.Button("Delete" , GUILayout.MaxWidth(70) , GUILayout.Height(18))){
				myTarget.gestures.Remove(myTarget.gestures[a]);

				LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));

				LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
				newSettings.emotions = myTarget.emotions;
				newSettings.emotionColors = myTarget.emotionColors;
				newSettings.gestures = myTarget.gestures;

				EditorUtility.CopySerialized (newSettings, settings);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				DestroyImmediate(newSettings);
			}
			GUI.backgroundColor = Color.white;
			GUILayout.Space(10);
			EditorGUILayout.EndHorizontal();
		}

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Add Gesture" , GUILayout.MaxWidth(300) , GUILayout.Height(25))){
			myTarget.gestures.Add("New Gesture");

			LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));

			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = myTarget.emotions;
			newSettings.emotionColors = myTarget.emotionColors;
			newSettings.gestures = myTarget.gestures;

			EditorUtility.CopySerialized (newSettings, settings);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(20);

		EditorGUILayout.HelpBox("Thank you for buying LipSync Pro! If you are finding it useful, please help us out by leaving a review on the Asset Store." , MessageType.Info);

		if(GUILayout.Button("Get LipSync Extensions")){
			RDExtensionWindow.ShowWindow("LipSync_Pro");
		}
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Asset Store")){
			Application.OpenURL("http://u3d.as/cag");
		}
		if(GUILayout.Button("Forum Thread")){
			Application.OpenURL("http://forum.unity3d.com/threads/released-lipsync-and-eye-controller-lipsyncing-and-facial-animation-tools.309324/");
		}
		if(GUILayout.Button("Email Support")){
			Application.OpenURL("mailto:contact@rogodigital.com");
		}
		if(GUILayout.Button("Website")){
			Application.OpenURL("http://lipsync.rogodigital.com/");
		}
		EditorGUILayout.EndHorizontal();
		serializedTarget.ApplyModifiedProperties();
	}

	[MenuItem("Edit/Project Settings/LipSync")]
	[MenuItem("Window/Rogo Digital/LipSync Pro/LipSync Project Settings", false, 12)]
	public static void ShowWindow () {
		LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));
		if(settings == null){
			settings = ScriptableObject.CreateInstance<LipSyncProject>();
			
			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = new string[]{"default"};
			newSettings.emotionColors = new Color[]{new Color(1f,0.7f,0.1f)};
			
			EditorUtility.CopySerialized (newSettings, settings);
			AssetDatabase.CreateAsset(settings , "Assets/Rogo Digital/LipSync/ProjectSettings.asset");
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}
		Selection.activeObject = settings;
	}
}
