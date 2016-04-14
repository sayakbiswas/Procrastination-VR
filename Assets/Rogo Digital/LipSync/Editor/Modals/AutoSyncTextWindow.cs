using UnityEngine;
using UnityEditor;
using RogoDigital;
using RogoDigital.Lipsync;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AutoSyncTextWindow : ModalWindow {
	private int mode = 0;
	private string text;
	private TextAsset asset;
	private AudioClip clip;
	private LipSyncClipSetup setup;

	void OnGUI () {
		GUILayout.Space(20);
		GUILayout.BeginHorizontal();
		GUILayout.Space(50);
		mode = GUILayout.Toolbar(mode , new string[]{"From Text File" , "From Textbox"} , GUILayout.ExpandWidth(true));
		GUILayout.Space(50);
		GUILayout.EndHorizontal();
		GUILayout.Space(30);

		switch (mode){
		case 0:
			asset = (TextAsset)EditorGUILayout.ObjectField("Text Transcript" , asset , typeof(TextAsset) , false);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.Space(50);
			if(GUILayout.Button("AutoSync" , GUILayout.Height(20)) && asset != null){
				setup.phonemeData = AutoSync.ProcessAudio(clip , asset);
				setup.changed = true;
				Close();
			}
			GUILayout.Space(50);
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
			break;
		case 1:
			EditorGUILayout.LabelField("Text Transcript");
			text = EditorGUILayout.TextArea(text , GUILayout.ExpandHeight(true));
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.Space(50);
			if(GUILayout.Button("AutoSync" , GUILayout.Height(20))){
				StreamWriter file = new StreamWriter(Application.dataPath + "/Rogo Digital/TEMP.txt");
				file.WriteLine(text);
				file.Close();

				AssetDatabase.Refresh();
				TextAsset tempAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Rogo Digital/TEMP.txt");

				setup.phonemeData = AutoSync.ProcessAudio(clip , tempAsset);
				setup.changed = true;
				AssetDatabase.DeleteAsset("Assets/Rogo Digital/TEMP.txt");
				Close();
			}
			GUILayout.Space(50);
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
			break;
		}

	}

	public static void CreateWindow (ModalParent parent , LipSyncClipSetup setup) {
		AutoSyncTextWindow window = GetWindow<AutoSyncTextWindow>();

		window.position = new Rect(parent.center.x-250 , parent.center.y-150 , 500 , 300);
		window.minSize = new Vector2(500,300);
		window.titleContent = new GUIContent("AutoSync");

		window.setup = setup;

		window.clip = setup.clip;
		window.Show(parent);
	}
}