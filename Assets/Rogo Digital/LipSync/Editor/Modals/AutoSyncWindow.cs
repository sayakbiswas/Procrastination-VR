using UnityEngine;
using UnityEditor;
using RogoDigital;
using RogoDigital.Lipsync;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AutoSyncWindow : ModalWindow {
	private LipSyncClipSetup setup;

	// Single Mode
	private AudioClip clip;

	// Multiple Mode
	private List<AudioClip> clips;
	private int currentClip = 0;

	private string[] languageModelNames = new string[0];
	private int languageModel;
	private bool attemptAudioConversion = false;

	private Vector2 scroll;
	private int tab = 0;
	private bool ready;

	private bool soXDefined = false;

	void OnEnable() {
		languageModelNames = AutoSyncLanguageModel.FindModels();
		attemptAudioConversion = EditorPrefs.GetBool("LipSync_SoXAvailable", false);
		soXDefined = attemptAudioConversion;

		clips = new List<AudioClip>();
	}

	void OnGUI () {
		GUILayout.Space(10);
		tab = GUILayout.Toolbar(tab, new string[] { "AutoSync Settings", "Batch Process" });
		GUILayout.Space(10);

		if (tab == 0) {
			if (languageModelNames.Length > 0) {
				languageModel = EditorGUILayout.Popup("Language Model", languageModel, languageModelNames, GUILayout.MaxWidth(400));
				if (clip == null) {
					ready = false;
				} else {
					ready = true;
				}
			} else {
				EditorGUILayout.HelpBox("No language models found. You can download language models from the extensions window or the LipSync website.", MessageType.Warning);
				ready = false;
			}
			GUILayout.Space(5);
			EditorGUI.BeginDisabledGroup(!soXDefined);
			attemptAudioConversion = EditorGUILayout.Toggle(new GUIContent("Enable Audio Conversion", "Improves compatibility with a wider range of Audio Formats by creating a temporary copy of your file and converting it to the correct format."), attemptAudioConversion);
			EditorGUI.EndDisabledGroup();
			if (!soXDefined) {
				GUILayout.Space(5);
				EditorGUILayout.HelpBox("SoX Audio Converter is not defined. See \"Using SoX with AutoSync.pdf\" for more info.", MessageType.Warning);
			}
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			EditorGUI.BeginDisabledGroup(!ready);
			if (GUILayout.Button("Start Single Process", GUILayout.Height(25))) {
				AutoSync.ProcessAudio(clip, languageModelNames[languageModel], FinishedProcessingSingle, attemptAudioConversion);
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		} else {
			if (languageModelNames.Length > 0) {
				ready = true;
			} else {
				EditorGUILayout.HelpBox("No language models found. You can download language models from the extensions window or the LipSync website.", MessageType.Warning);
				ready = false;
			}
			GUILayout.Space(5);
			GUILayout.Box("Select AudioClips", EditorStyles.boldLabel);
			GUILayout.Space(10);
			scroll = GUILayout.BeginScrollView(scroll);
			for (int a = 0; a < clips.Count; a++) {
				GUILayout.BeginHorizontal();
				GUILayout.Space(5);
				clips[a] = (AudioClip)EditorGUILayout.ObjectField(clips[a], typeof(AudioClip), false);
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Remove", GUILayout.MaxWidth(200))){
					clips.RemoveAt(a);
					break;
				}
				GUILayout.Space(5);
				GUILayout.EndHorizontal();
			}
			GUILayout.Space(5);
			GUILayout.EndScrollView();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add AudioClip")) {
				clips.Add(null);
			}
			if (GUILayout.Button("Add Selected")) {
				foreach (AudioClip c in Selection.GetFiltered(typeof(AudioClip), SelectionMode.Assets)) {
					if (!clips.Contains(c))
						clips.Add(c);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.HelpBox("Settings from the AutoSync Settings tab will be used. Make sure they are correct.", MessageType.Info);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUI.BeginDisabledGroup(!ready);
			if (GUILayout.Button("Start Batch Process", GUILayout.Height(25))) {
				if (clips.Count > 0) {
					currentClip = 1;
					AutoSync.ProcessAudio(clips[0], languageModelNames[languageModel], FinishedProcessingMulti, "1/" + clips.Count.ToString(), attemptAudioConversion);
				} else {
					ShowNotification(new GUIContent("No clips added for batch processing!"));
				}
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
		}
	}

	void FinishedProcessingMulti(AudioClip finishedClip, List<PhonemeMarker> markers) {
		// Create File
		string path = AssetDatabase.GetAssetPath(finishedClip);
		path = Path.ChangeExtension(path, "asset");

		LipSyncData file = ScriptableObject.CreateInstance<LipSyncData>();
		file.phonemeData = markers.ToArray();
		file.emotionData = new EmotionMarker[0];
		file.gestureData = new GestureMarker[0];
		file.version = 1.0f;
		file.clip = finishedClip;
		file.length = finishedClip.length;
		file.transcript = "";

		LipSyncData outputFile = (LipSyncData)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncData));

		if (outputFile != null) {
			EditorUtility.CopySerialized(file, outputFile);
			AssetDatabase.SaveAssets();
		} else {
			outputFile = ScriptableObject.CreateInstance<LipSyncData>();
			EditorUtility.CopySerialized(file, outputFile);
			AssetDatabase.CreateAsset(outputFile, path);
		}

		DestroyImmediate(file);

		if (currentClip < clips.Count) {
			AutoSync.ProcessAudio(clips[currentClip], languageModelNames[languageModel], FinishedProcessingMulti, (currentClip + 1).ToString() + "/" + clips.Count.ToString(), attemptAudioConversion);
			currentClip++;
		} else {
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
			setup.ShowNotification(new GUIContent("Batch AutoSync Complete."));
			Close();
		}
	}

	void FinishedProcessingSingle(AudioClip clip, List<PhonemeMarker> markers) {
		if(markers.Count > 0){
			setup.phonemeData = markers;
			setup.changed = true;
			setup.previewOutOfDate = true;
			Close();
		}
	}

	public static void CreateWindow (ModalParent parent , LipSyncClipSetup setup, int mode) {
		AutoSyncWindow window = GetWindow<AutoSyncWindow>();

		window.position = new Rect(parent.center.x-250 , parent.center.y-150 , 500 , 300);
		window.minSize = new Vector2(500,300);
		window.titleContent = new GUIContent("AutoSync");

		window.setup = setup;

		window.tab = mode;
		window.clip = setup.clip;
		window.Show(parent);
	}
}