using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Animations;

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System;

using RogoDigital.Lipsync;

[CustomEditor(typeof(LipSync))]
public class LipSyncEditor : Editor {
	private LipSync lsTarget;

	private string[] blendables;
	private SerializedObject serializedTarget;
	private int currentToggle = -1;
	private int oldToggle = -1;
	private int markerTab = 0;
	private bool saving = false;
	private string savingName = "";

	private AnimBool showBoneOptions;
	private AnimBool showPlayOnAwake;

	private LipSyncProject settings;
	private Editor blendSystemEditor;
	private AnimatorController controller;

	private int blendSystemNumber = 0;
	private List<System.Type> blendSystems;
	private List<string> blendSystemNames;
	private BlendSystemButton.Reference[] blendSystemButtons = new BlendSystemButton.Reference[0];

	private Texture2D logo;

	private Texture2D guideAI;
	private Texture2D guideE;
	private Texture2D guideEtc;
	private Texture2D guideFV;
	private Texture2D guideL;
	private Texture2D guideMBP;
	private Texture2D guideO;
	private Texture2D guideU;
	private Texture2D guideWQ;

	private Texture2D locked;
	private Texture2D unlocked;
	private Texture2D presetsIcon;
	private Texture2D delete;
	private Texture2D lightToolbarTexture;

	private GUIStyle lightToolbar;
	private GUIStyle miniLabelDark;

	private SerializedProperty audioSource;
	private SerializedProperty restTime;
	private SerializedProperty restHoldTime;
	private SerializedProperty playOnAwake;
	private SerializedProperty defaultClip;
	private SerializedProperty defaultDelay;
	private SerializedProperty scaleAudioSpeed;
	private SerializedProperty useBones;
	private SerializedProperty boneUpdateAnimation;
	private SerializedProperty onFinishedPlaying;
	private SerializedProperty gesturesAnimator;

	private float versionNumber = 0.61f;
	private List<Texture2D> guides; 

	void OnEnable () {
		logo = Resources.Load<Texture2D>("Lipsync/Dark/logo_component");

		guideAI = Resources.Load<Texture2D>("Lipsync/Guides/AI");
		guideE = Resources.Load<Texture2D>("Lipsync/Guides/E");
		guideEtc = Resources.Load<Texture2D>("Lipsync/Guides/etc");
		guideFV = Resources.Load<Texture2D>("Lipsync/Guides/FV");
		guideL = Resources.Load<Texture2D>("Lipsync/Guides/L");
		guideMBP = Resources.Load<Texture2D>("Lipsync/Guides/MBP");
		guideO = Resources.Load<Texture2D>("Lipsync/Guides/O");
		guideU = Resources.Load<Texture2D>("Lipsync/Guides/U");
		guideWQ = Resources.Load<Texture2D>("Lipsync/Guides/WQ");
		
		locked = Resources.Load<Texture2D>("Lipsync/Dark/locked");
		unlocked = Resources.Load<Texture2D>("Lipsync/Dark/unlocked");
		presetsIcon = Resources.Load<Texture2D>("LipSync/Dark/presets");
		delete = Resources.Load<Texture2D>("LipSync/Dark/bin");
		lightToolbarTexture = Resources.Load<Texture2D>("Lipsync/light-toolbar");

		if(!EditorGUIUtility.isProSkin){ 
			logo = Resources.Load<Texture2D>("Lipsync/Light/logo_component");
			locked = Resources.Load<Texture2D>("Lipsync/Light/locked");
			unlocked = Resources.Load<Texture2D>("Lipsync/Light/unlocked");
			presetsIcon = Resources.Load<Texture2D>("LipSync/Light/presets");
			delete = Resources.Load<Texture2D>("LipSync/Light/bin");
		}

		guides = new List<Texture2D>(){
			guideAI,
			guideE,
			guideU,
			guideO,
			guideEtc,
			guideFV,
			guideL,
			guideMBP,
			guideWQ,
			Texture2D.blackTexture
		};

		lsTarget = (LipSync)target;
		lsTarget.reset.AddListener(OnEnable);
		if (lsTarget.blendSystem == null) lsTarget.blendSystem = lsTarget.GetComponent<BlendSystem>();

		if(lsTarget.lastUsedVersion < versionNumber) {
			AutoUpdate(lsTarget.lastUsedVersion);
			lsTarget.lastUsedVersion = versionNumber;
		}

		if(lsTarget.gesturesAnimator != null){
			string assetPath = AssetDatabase.GetAssetPath(lsTarget.gesturesAnimator.runtimeAnimatorController);
			controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
		}

		if(lsTarget.gestures == null) {
			lsTarget.gestures = new List<GestureInstance>();
		}

		serializedTarget = new SerializedObject(target);
		FindBlendSystems();

		audioSource = serializedTarget.FindProperty("audioSource");
		restTime = serializedTarget.FindProperty("restTime");
		restHoldTime = serializedTarget.FindProperty("restHoldTime");
		playOnAwake = serializedTarget.FindProperty("playOnAwake");
		defaultClip = serializedTarget.FindProperty("defaultClip");
		defaultDelay = serializedTarget.FindProperty("defaultDelay");
		scaleAudioSpeed = serializedTarget.FindProperty("scaleAudioSpeed");
		useBones = serializedTarget.FindProperty("useBones");
		boneUpdateAnimation = serializedTarget.FindProperty("boneUpdateAnimation");
		onFinishedPlaying = serializedTarget.FindProperty("onFinishedPlaying");
		gesturesAnimator = serializedTarget.FindProperty("gesturesAnimator");

		showBoneOptions = new AnimBool(lsTarget.useBones , Repaint);
		showPlayOnAwake = new AnimBool(lsTarget.playOnAwake , Repaint);

		CreateBlendSystemEditor();

		if(lsTarget.blendSystem != null){
			if(lsTarget.blendSystem.isReady){
				GetBlendShapes();
				blendSystemButtons = GetBlendSystemButtons();
			}
		}

		//Get Settings File
		settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset" , typeof(LipSyncProject));
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

		if(lsTarget.phonemes == null || lsTarget.phonemes.Count < 9){
			lsTarget.phonemes = new List<PhonemeShape>();
			
			for(int A = 0 ; A < 10 ; A++){
				lsTarget.phonemes.Add(new PhonemeShape((Phoneme)A));
			}
			
			EditorUtility.SetDirty(lsTarget);
			serializedTarget.SetIsDifferentCacheDirty();
		}else if(lsTarget.phonemes.Count == 9){
			//Update pre 0.4 characters with the new Rest phoneme
			lsTarget.phonemes.Add(new PhonemeShape(Phoneme.Rest));
		}

		if(lsTarget.emotions == null){
			lsTarget.emotions = new List<EmotionShape>();
			
			for(int A = 0 ; A < settings.emotions.Length ; A++){
				lsTarget.emotions.Add(new EmotionShape(settings.emotions[A]));
			}
			
			EditorUtility.SetDirty(lsTarget);
			serializedTarget.SetIsDifferentCacheDirty();
		}else if(lsTarget.emotions.Count < settings.emotions.Length){

			for(int A = 0 ; A < settings.emotions.Length ; A++){
				bool exists = false;
				foreach(EmotionShape eShape in lsTarget.emotions){
					if(eShape.emotion == settings.emotions[A]){
						exists = true;
					}
				}

				if(!exists) lsTarget.emotions.Add(new EmotionShape(settings.emotions[A]));
			}

			EditorUtility.SetDirty(lsTarget);
			serializedTarget.SetIsDifferentCacheDirty();
		}else{
			foreach(EmotionShape eShape in lsTarget.emotions){
				bool exists = false;
				foreach(string emotion in settings.emotions){
					if(eShape.emotion == emotion){
						exists = true;
						eShape.verified = true;
					}
				}

				if(!exists){
					eShape.verified = false;
				}
			}

			EditorUtility.SetDirty(lsTarget);
			serializedTarget.SetIsDifferentCacheDirty();
		}
	}

	void OnDisable () {
		if(lsTarget.blendSystem != null){
			if(lsTarget.blendSystem.isReady){
				foreach(Shape shape in lsTarget.phonemes){
					for(int blendable = 0 ; blendable < shape.weights.Count ; blendable++){
						lsTarget.blendSystem.SetBlendableValue(shape.blendShapes[blendable] , 0);
					}
				}
			}
		}

		if(currentToggle > -1 && lsTarget.useBones){
			foreach(Shape shape in lsTarget.phonemes){
				foreach(BoneShape bone in shape.bones){
					if(bone.bone != null){
						bone.bone.localPosition = bone.neutralPosition;
						bone.bone.localEulerAngles = bone.neutralRotation;
					}
				}
			}
		}
	}
	 
	void ChangeBlendSystem () {
		if(lsTarget.GetComponent<BlendSystem>() != null){
			if(blendSystems[blendSystemNumber] != lsTarget.GetComponent<BlendSystem>().GetType()){
				BlendSystem[] oldSystems = lsTarget.GetComponents<BlendSystem>();
				foreach(BlendSystem system in oldSystems){
					DestroyImmediate(system);
				}

				lsTarget.gameObject.AddComponent(blendSystems[blendSystemNumber]);
				lsTarget.blendSystem = lsTarget.GetComponent<BlendSystem>();
				CreateBlendSystemEditor();
				blendSystemButtons = GetBlendSystemButtons();
			}
		}else{
			lsTarget.gameObject.AddComponent(blendSystems[blendSystemNumber]);
			lsTarget.blendSystem = lsTarget.GetComponent<BlendSystem>();
			CreateBlendSystemEditor();
			blendSystemButtons = GetBlendSystemButtons();
		}
	}

	public override void OnInspectorGUI () {
		if(serializedTarget == null){
			OnEnable();
		}

		if(lightToolbar == null) {
			lightToolbar = new GUIStyle(EditorStyles.toolbarDropDown);
			lightToolbar.normal.background = lightToolbarTexture;

			miniLabelDark = new GUIStyle(EditorStyles.miniLabel);
			miniLabelDark.normal.textColor = Color.black;
		}

		serializedTarget.Update();

		EditorGUI.BeginDisabledGroup(saving);
		Rect fullheight = EditorGUILayout.BeginVertical();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Box(logo ,  GUIStyle.none);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		if(blendSystems.Count == 0){
			EditorGUILayout.Popup("Blend System" , 0 , new string[]{"No BlendSystems Found"});
		}else{
			if(lsTarget.blendSystem == null){
				EditorGUI.BeginChangeCheck();
				blendSystemNumber = EditorGUILayout.Popup("Blend System" , blendSystemNumber , blendSystemNames.ToArray() , GUIStyle.none);
				if(EditorGUI.EndChangeCheck()){
					ChangeBlendSystem();
				}
				GUI.Box(new Rect(EditorGUIUtility.labelWidth+GUILayoutUtility.GetLastRect().x , GUILayoutUtility.GetLastRect().y , GUILayoutUtility.GetLastRect().width , GUILayoutUtility.GetLastRect().height) , "Select a BlendSystem" , EditorStyles.popup);
			}else{
				EditorGUI.BeginChangeCheck();
				blendSystemNumber = EditorGUILayout.Popup("Blend System" , blendSystemNumber , blendSystemNames.ToArray());
				if(EditorGUI.EndChangeCheck()){
					ChangeBlendSystem();
				}
			}
		}

		EditorGUILayout.Space();
		if(lsTarget.blendSystem == null){
			GUILayout.Label("No BlendSystem Selected");
		}else{
			if(blendSystemEditor == null) CreateBlendSystemEditor();
			blendSystemEditor.OnInspectorGUI();
			if(!lsTarget.blendSystem.isReady){
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Continue" , GUILayout.MaxWidth(200))) {
					lsTarget.blendSystem.OnVariableChanged();
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
			}
		}

		if(lsTarget.blendSystem != null){
			if(lsTarget.blendSystem.isReady){
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(audioSource , new GUIContent ("Audio Source" , "AudioSource to play dialogue from."));

				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(useBones , new GUIContent("Use Bone Transforms" , "Allow BoneShapes to be added to phoneme poses. This enables the use of bone based facial animation."));
				showBoneOptions.target = lsTarget.useBones;
				if(EditorGUILayout.BeginFadeGroup(showBoneOptions.faded)){
					EditorGUILayout.PropertyField(boneUpdateAnimation , new GUIContent("Account for Animation" , "If true, will calculate relative bone positions/rotations each frame. Improves results when using animation, but will cause errors when not."));
					EditorGUILayout.Space();
				}
				FixedEndFadeGroup(showBoneOptions.faded);
				EditorGUILayout.Space();
				if(blendSystemButtons.Length > 0 && blendSystemButtons.Length < 3) {	
					Rect buttonPanel = EditorGUILayout.BeginHorizontal();
					EditorGUI.HelpBox(new Rect(buttonPanel.x , buttonPanel.y-4 , buttonPanel.width , buttonPanel.height+8) , "BlendSystem Commands:" , MessageType.Info);
					GUILayout.FlexibleSpace();
					DrawBlendSystemButtons();
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}else if(blendSystemButtons.Length >= 3) {	
					Rect buttonPanel = EditorGUILayout.BeginHorizontal();
					EditorGUI.HelpBox(new Rect(buttonPanel.x , buttonPanel.y-4 , buttonPanel.width , buttonPanel.height+8) , "BlendSystem Commands:" , MessageType.Info);
					GUILayout.FlexibleSpace();
					DrawBlendSystemButtons();
					GUILayout.Space(5);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}
				int oldTab = markerTab;

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				markerTab = GUILayout.Toolbar(markerTab , new string[]{"Phonemes" , "Emotions" , "Gestures"} , GUILayout.MaxWidth(400) , GUILayout.MinHeight(23));

				Rect presetRect = EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button(new GUIContent(presetsIcon , "Presets") , GUILayout.MaxWidth(32)  , GUILayout.MinHeight(23))){
					GenericMenu menu = new GenericMenu();

					if(!Directory.Exists(Application.dataPath + "/Rogo Digital/LipSync/Presets")){
						Directory.CreateDirectory(Application.dataPath + "/Rogo Digital/LipSync/Presets");
					}

					string[] directories = Directory.GetDirectories(Application.dataPath , "Presets" , SearchOption.AllDirectories);

					bool noItems = true;
					foreach(string directory in directories) {
						foreach(string file in Directory.GetFiles(directory)){
							if(Path.GetExtension(file).ToLower() == ".asset"){
								BlendshapePreset preset = AssetDatabase.LoadAssetAtPath<BlendshapePreset>("Assets" + file.Substring((Application.dataPath).Length));
								if(preset != null){
									noItems = false;
									menu.AddItem(new GUIContent(Path.GetFileNameWithoutExtension(file)) , false , LoadPreset , file);
								}
							}
						}

						string[] subdirectories = Directory.GetDirectories(directory);
						foreach(string subdirectory in subdirectories){
							foreach(string file in Directory.GetFiles(subdirectory)){
								if(Path.GetExtension(file).ToLower() == ".asset"){
									BlendshapePreset preset = AssetDatabase.LoadAssetAtPath<BlendshapePreset>("Assets" + file.Substring((Application.dataPath).Length));
									if(preset != null){
										noItems = false;
										menu.AddItem(new GUIContent(Path.GetFileName(subdirectory)+"/"+Path.GetFileNameWithoutExtension(file)) , false , LoadPreset , file);
									}
								}
							}
						}
					}


					if(noItems)menu.AddDisabledItem(new GUIContent("No Presets Found"));

					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Save New Preset") , false , NewPreset);
					menu.DropDown(presetRect);
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.Space(10);

				if(markerTab != oldTab){
					if(oldTab == 0){
						foreach(PhonemeShape phoneme in lsTarget.phonemes){
							foreach(int shape in phoneme.blendShapes){
								lsTarget.blendSystem.SetBlendableValue(shape , 0);
							}
						}
					}else{
						foreach(EmotionShape emotion in lsTarget.emotions){
							foreach(int shape in emotion.blendShapes){
								lsTarget.blendSystem.SetBlendableValue(shape , 0);
							}
						}
					}
					currentToggle = -1;
				}

				if(markerTab == 0){
					int a = 0;
					foreach(PhonemeShape phoneme in lsTarget.phonemes){
						if(currentToggle == a){
							Rect box = EditorGUILayout.BeginHorizontal();
							GUI.backgroundColor = new Color(1f , 0.77f, 0f);
							if(GUI.Button(box , "" , lightToolbar)){
								currentToggle = -1;
							}
							GUI.backgroundColor = Color.white;
							
							GUILayout.Box(AddSpaces(phoneme.phoneme.ToString()) + " Phoneme" , miniLabelDark , GUILayout.Width(250));
							if(phoneme.weights.Count == 1){
								GUILayout.Box("1 " + lsTarget.blendSystem.blendableDisplayName , miniLabelDark);
							}else if(phoneme.weights.Count > 1){
								GUILayout.Box(phoneme.weights.Count.ToString() + " " + lsTarget.blendSystem.blendableDisplayNamePlural , miniLabelDark);
							}

							if(phoneme.bones.Count == 1 && lsTarget.useBones){
								GUILayout.Box("1 Bone Transform" , miniLabelDark);
							}else if(phoneme.bones.Count > 1 && lsTarget.useBones){
								GUILayout.Box(phoneme.bones.Count.ToString() + " Bone Transforms" , miniLabelDark);
							}
							EditorGUILayout.EndHorizontal();
							
							box = EditorGUILayout.BeginVertical();
							GUI.Box(new Rect(box.x+4 , box.y , box.width-7 , box.height) , "" , EditorStyles.helpBox);
							GUILayout.Space(20);

							for(int b = 0 ; b < phoneme.weights.Count ; b++){
								Rect newBox = EditorGUILayout.BeginHorizontal();
								GUI.Box(new Rect(newBox.x+5 , newBox.y , newBox.width-11 , newBox.height) , "" , EditorStyles.toolbarButton);
								GUILayout.Space(5);

								int oldShape = 0;
								oldShape = phoneme.blendShapes[b];
								phoneme.blendShapes[b] = EditorGUILayout.Popup(lsTarget.blendSystem.blendableDisplayName + " " + b.ToString() , phoneme.blendShapes[b] , blendables , EditorStyles.toolbarPopup);
								if(phoneme.blendShapes[b] != oldShape){
									lsTarget.blendSystem.SetBlendableValue(oldShape , 0);
								}

								GUI.backgroundColor = new Color(0.8f , 0.3f, 0.3f);
								if(GUILayout.Button(delete , EditorStyles.toolbarButton , GUILayout.MaxWidth(50))){
									Undo.RecordObject(lsTarget , "Delete " + lsTarget.blendSystem.blendableDisplayName);

									phoneme.blendShapes.RemoveAt(b);
									lsTarget.blendSystem.SetBlendableValue(oldShape , 0);

									phoneme.weights.RemoveAt(b);
									GetBlendShapes();
									EditorUtility.SetDirty(lsTarget.gameObject);
									serializedTarget.SetIsDifferentCacheDirty();
									return;
								}
								GUILayout.Space(4);
								GUI.backgroundColor = Color.white;
								EditorGUILayout.EndHorizontal();
								EditorGUILayout.BeginHorizontal();
								GUILayout.Space(15);
								phoneme.weights[b] = EditorGUILayout.Slider(phoneme.weights[b] , lsTarget.blendSystem.blendRangeLow , lsTarget.blendSystem.blendRangeHigh);
								GUILayout.Space(10);
								EditorGUILayout.EndHorizontal();
								GUILayout.Space(10);
							}

							if(EditorGUILayout.BeginFadeGroup(showBoneOptions.faded)){
								for(int b = 0 ; b < phoneme.bones.Count ; b++){
									Rect newBox = EditorGUILayout.BeginHorizontal();
									GUI.Box(new Rect(newBox.x+5 , newBox.y , newBox.width-11 , newBox.height) , "" , EditorStyles.toolbarButton);
									GUILayout.Space(5);
									Transform oldBone = phoneme.bones[b].bone;
									phoneme.bones[b].bone = (Transform)EditorGUILayout.ObjectField("Bone Transform "+b.ToString() , phoneme.bones[b].bone , typeof(Transform)  , true);

									if(oldBone != phoneme.bones[b].bone){
										if(phoneme.bones[b].bone != null){
											Transform newbone = phoneme.bones[b].bone;
											phoneme.bones[b].bone = oldBone;
											if(phoneme.bones[b].bone != null){
												phoneme.bones[b].bone.localPosition = phoneme.bones[b].neutralPosition;
												phoneme.bones[b].bone.localEulerAngles = phoneme.bones[b].neutralRotation;
											}

											phoneme.bones[b].bone = newbone;

											phoneme.bones[b].SetNeutral();

											phoneme.bones[b].endRotation = phoneme.bones[b].bone.localEulerAngles;
											phoneme.bones[b].endPosition = phoneme.bones[b].bone.localPosition;

											phoneme.bones[b].bone.localPosition = phoneme.bones[b].endPosition;
											phoneme.bones[b].bone.localEulerAngles = phoneme.bones[b].endRotation;
										}
									}

									GUI.backgroundColor = new Color(0.8f , 0.3f, 0.3f);
									if(GUILayout.Button(delete , EditorStyles.toolbarButton , GUILayout.MaxWidth(50))){
										Undo.RecordObject(lsTarget , "Delete Bone Transform");
										if(phoneme.bones[b].bone != null){
											phoneme.bones[b].bone.localPosition = phoneme.bones[b].neutralPosition;
											phoneme.bones[b].bone.localEulerAngles = phoneme.bones[b].neutralRotation;
										}
										phoneme.bones.RemoveAt(b);
										EditorUtility.SetDirty(lsTarget.gameObject);
										serializedTarget.SetIsDifferentCacheDirty();
										return;
									}
									GUILayout.Space(4);
									GUI.backgroundColor = Color.white;
									EditorGUILayout.EndHorizontal();
									GUILayout.Space(5);
									EditorGUILayout.BeginHorizontal();
									GUILayout.Space(10);
									GUILayout.Box("Position" , EditorStyles.label , GUILayout.MaxWidth(80));

									EditorGUI.BeginDisabledGroup(phoneme.bones[b].bone == null);
									EditorGUI.BeginDisabledGroup(phoneme.bones[b].lockPosition);
									phoneme.bones[b].endPosition = EditorGUILayout.Vector3Field("" , phoneme.bones[b].endPosition);
									EditorGUI.EndDisabledGroup();
									GUILayout.Space(10);
									if(GUILayout.Button(phoneme.bones[b].lockPosition?locked:unlocked , GUILayout.Width(30) , GUILayout.Height(16))){
										phoneme.bones[b].lockPosition = !phoneme.bones[b].lockPosition;
									}
									EditorGUI.EndDisabledGroup();
									if(phoneme.bones[b].bone != null){
										phoneme.bones[b].bone.localPosition = phoneme.bones[b].endPosition;
									}

									GUILayout.Space(10);
									EditorGUILayout.EndHorizontal();
									EditorGUILayout.BeginHorizontal();
									GUILayout.Space(10);
									GUILayout.Box("Rotation" , EditorStyles.label , GUILayout.MaxWidth(80));

									EditorGUI.BeginDisabledGroup(phoneme.bones[b].bone == null);
									EditorGUI.BeginDisabledGroup(phoneme.bones[b].lockRotation);
									phoneme.bones[b].endRotation = EditorGUILayout.Vector3Field("" , phoneme.bones[b].endRotation);
									EditorGUI.EndDisabledGroup();
									GUILayout.Space(10);
									if(GUILayout.Button(phoneme.bones[b].lockRotation?locked:unlocked , GUILayout.Width(30) , GUILayout.Height(16))){
										phoneme.bones[b].lockRotation = !phoneme.bones[b].lockRotation;
									}
									EditorGUI.EndDisabledGroup();
									if(phoneme.bones[b].bone != null){
										phoneme.bones[b].bone.localEulerAngles = phoneme.bones[b].endRotation;
									}

									GUILayout.Space(10);
									EditorGUILayout.EndHorizontal();
									GUILayout.Space(10);
								}
							}
							EditorGUILayout.EndFadeGroup();

							EditorGUILayout.Space();

							GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if(lsTarget.blendSystem.blendableCount > 0){
								if(GUILayout.Button("Add "+lsTarget.blendSystem.blendableDisplayName , GUILayout.MaxWidth(200))){
									Undo.RecordObject(lsTarget , "Add "+lsTarget.blendSystem.blendableDisplayName);
									phoneme.blendShapes.Add(0);
									phoneme.weights.Add(0);

									GetBlendShapes();

									EditorUtility.SetDirty(lsTarget);
									serializedTarget.SetIsDifferentCacheDirty();
								}
							}
							EditorGUILayout.Space();
							if(lsTarget.useBones){
								if(GUILayout.Button("Add Bone Transform" , GUILayout.MaxWidth(240))){
									Undo.RecordObject(lsTarget , "Add Bone Shape");
									phoneme.bones.Add(new BoneShape());
									EditorUtility.SetDirty(lsTarget);
									serializedTarget.SetIsDifferentCacheDirty();
								}
							}
							GUILayout.FlexibleSpace();
							GUILayout.EndHorizontal();
							if(lsTarget.blendSystem.blendableCount == 0 && !lsTarget.useBones){
								GUILayout.BeginHorizontal();
								GUILayout.Space(10);
								EditorGUILayout.HelpBox(lsTarget.blendSystem.noBlendablesMessage , MessageType.Warning);
								GUILayout.Space(10);
								GUILayout.EndHorizontal();
							}
							GUILayout.Space(14);
							EditorGUILayout.EndVertical();
							
						}else{
							Rect box = EditorGUILayout.BeginHorizontal();
							if(GUI.Button(box , "" , EditorStyles.toolbarDropDown)){
								currentToggle = a;
							}
							
							GUILayout.Box(AddSpaces(phoneme.phoneme.ToString()) + " Phoneme" , EditorStyles.miniLabel , GUILayout.Width(250));
							if(phoneme.weights.Count == 1){
								GUILayout.Box("1 " + lsTarget.blendSystem.blendableDisplayName , EditorStyles.miniLabel);
							}else if(phoneme.weights.Count > 1){
								GUILayout.Box(phoneme.weights.Count.ToString() + " " + lsTarget.blendSystem.blendableDisplayNamePlural , EditorStyles.miniLabel);
							}
							if(phoneme.bones.Count == 1 && lsTarget.useBones){
								GUILayout.Box("1 Bone Transform" , EditorStyles.miniLabel);
							}else if(phoneme.bones.Count > 1 && lsTarget.useBones){
								GUILayout.Box(phoneme.bones.Count.ToString() + " Bone Transforms" , EditorStyles.miniLabel);
							}

							EditorGUILayout.EndHorizontal();
						}
						a++;
					}
				}else if(markerTab == 1){
					int a = 0;
					foreach(EmotionShape emotion in lsTarget.emotions){
						if(emotion.blendShapes == null)emotion.blendShapes = new List<int>();

						if(currentToggle == a){
							Rect box = EditorGUILayout.BeginHorizontal();
							if(emotion.verified){
								GUI.backgroundColor = new Color(1f , 0.77f, 0f);
							}else{
								GUI.backgroundColor = new Color(0.4f , 0.4f, 0.4f);
							}
							if(GUI.Button(box , "" , lightToolbar)){
								currentToggle = -1;
							}
							GUI.backgroundColor = Color.white;

							GUILayout.Box(emotion.emotion + " Emotion" , miniLabelDark , GUILayout.Width(250));
							if(emotion.weights.Count == 1){
								GUILayout.Box("1 " + lsTarget.blendSystem.blendableDisplayName , miniLabelDark);
							}else if(emotion.weights.Count > 1){
								GUILayout.Box(emotion.weights.Count.ToString() + " " + lsTarget.blendSystem.blendableDisplayNamePlural , miniLabelDark);
							}
							if(emotion.bones.Count == 1 && lsTarget.useBones){
								GUILayout.Box("1 Bone Transform" , miniLabelDark);
							}else if(emotion.bones.Count > 1 && lsTarget.useBones){
								GUILayout.Box(emotion.bones.Count.ToString() + " Bone Transforms" ,miniLabelDark);
							}
							if(!emotion.verified){
								GUILayout.FlexibleSpace();
								GUILayout.Box("Missing" , miniLabelDark);
								GUILayout.FlexibleSpace();
							}

							EditorGUILayout.EndHorizontal();

							if(!emotion.verified){
								EditorGUILayout.HelpBox("There is no matching emotion in the project settings. This can occur when importing a LipSync character from one project to another. Animations using this emotion will still function on this character, but you will not be able to use this emotion shape in new animations unless you add it to the project settings. Alternatively you can delete this emotion shape." , MessageType.Warning);
								if(GUILayout.Button("Delete Emotion Shape")){
									Undo.RecordObject(lsTarget , "Delete Emotion Shape");
									foreach(int blendable in emotion.blendShapes){
										lsTarget.blendSystem.SetBlendableValue(blendable , 0);
									}
									lsTarget.emotions.Remove(emotion);
									EditorUtility.SetDirty(lsTarget.gameObject);
									serializedTarget.SetIsDifferentCacheDirty();
									return;
								}
							}

							box = EditorGUILayout.BeginVertical();
							GUI.Box(new Rect(box.x+4 , box.y , box.width-7 , box.height) , "" , EditorStyles.helpBox);
							GUILayout.Space(20);
							
							for(int b = 0 ; b < emotion.weights.Count ; b++){
								Rect newBox = EditorGUILayout.BeginHorizontal();
								GUI.Box(new Rect(newBox.x+5 , newBox.y , newBox.width-11 , newBox.height) , "" , EditorStyles.toolbarButton);
								GUILayout.Space(5);

								int oldShape = 0;

								oldShape = emotion.blendShapes[b];
								emotion.blendShapes[b] = EditorGUILayout.Popup(lsTarget.blendSystem.blendableDisplayName + " " + b.ToString() , emotion.blendShapes[b] , blendables , EditorStyles.toolbarPopup);
								if(emotion.blendShapes[b] != oldShape){
									lsTarget.blendSystem.SetBlendableValue(oldShape , 0);
								}

								GUI.backgroundColor = new Color(0.8f , 0.3f, 0.3f);
								if(GUILayout.Button(delete , EditorStyles.toolbarButton , GUILayout.MaxWidth(50))){
									Undo.RecordObject(lsTarget , "Delete " + lsTarget.blendSystem.blendableDisplayName);
									lsTarget.blendSystem.SetBlendableValue(emotion.blendShapes[b] , 0);
									emotion.blendShapes.RemoveAt(b);

									emotion.weights.RemoveAt(b);
									GetBlendShapes();
									EditorUtility.SetDirty(lsTarget.gameObject);
									serializedTarget.SetIsDifferentCacheDirty();
									return;
								}
								GUILayout.Space(4);
								GUI.backgroundColor = Color.white;
								EditorGUILayout.EndHorizontal();
								EditorGUILayout.BeginHorizontal();
								GUILayout.Space(15);
								emotion.weights[b] = EditorGUILayout.Slider(emotion.weights[b] , lsTarget.blendSystem.blendRangeLow , lsTarget.blendSystem.blendRangeHigh);
								GUILayout.Space(10);
								EditorGUILayout.EndHorizontal();
								GUILayout.Space(10);
							}
							
							if(EditorGUILayout.BeginFadeGroup(showBoneOptions.faded)){
								for(int b = 0 ; b < emotion.bones.Count ; b++){
									Rect newBox = EditorGUILayout.BeginHorizontal();
									GUI.Box(new Rect(newBox.x+5 , newBox.y , newBox.width-11 , newBox.height) , "" , EditorStyles.toolbarButton);
									GUILayout.Space(5);
									Transform oldBone = emotion.bones[b].bone;
									emotion.bones[b].bone = (Transform)EditorGUILayout.ObjectField("Bone Transform "+b.ToString() , emotion.bones[b].bone , typeof(Transform)  , true);
									
									if(oldBone != emotion.bones[b].bone){
										if(emotion.bones[b].bone != null){
											Transform newbone = emotion.bones[b].bone;
											emotion.bones[b].bone = oldBone;
											if(emotion.bones[b].bone != null){
												emotion.bones[b].bone.localPosition = emotion.bones[b].neutralPosition;
												emotion.bones[b].bone.localEulerAngles = emotion.bones[b].neutralRotation;
											}
											emotion.bones[b].bone = newbone;
											
											emotion.bones[b].SetNeutral();
											
											emotion.bones[b].endRotation = emotion.bones[b].bone.localEulerAngles;
											emotion.bones[b].endPosition = emotion.bones[b].bone.localPosition;

											emotion.bones[b].bone.localPosition = emotion.bones[b].endPosition;
											emotion.bones[b].bone.localEulerAngles = emotion.bones[b].endRotation;
										}
									}
									
									GUI.backgroundColor = new Color(0.8f , 0.3f, 0.3f);
									if(GUILayout.Button(delete , EditorStyles.toolbarButton , GUILayout.MaxWidth(50))){
										Undo.RecordObject(lsTarget , "Delete Bone Transform");
										if(emotion.bones[b].bone != null){
											emotion.bones[b].bone.localPosition = emotion.bones[b].neutralPosition;
											emotion.bones[b].bone.localEulerAngles = emotion.bones[b].neutralRotation;
										}
										emotion.bones.RemoveAt(b);
										EditorUtility.SetDirty(lsTarget.gameObject);
										serializedTarget.SetIsDifferentCacheDirty();
										return;
									}
									GUILayout.Space(4);
									GUI.backgroundColor = Color.white;
									EditorGUILayout.EndHorizontal();
									GUILayout.Space(5);
									EditorGUILayout.BeginHorizontal();
									GUILayout.Space(10);
									GUILayout.Box("Position" , EditorStyles.label , GUILayout.MaxWidth(80));
									
									EditorGUI.BeginDisabledGroup(emotion.bones[b].bone == null);
									EditorGUI.BeginDisabledGroup(emotion.bones[b].lockPosition);
									emotion.bones[b].endPosition = EditorGUILayout.Vector3Field("" , emotion.bones[b].endPosition);
									EditorGUI.EndDisabledGroup();
									GUILayout.Space(10);
									if(GUILayout.Button(emotion.bones[b].lockPosition?locked:unlocked , GUILayout.Width(30) , GUILayout.Height(16))){
										emotion.bones[b].lockPosition = !emotion.bones[b].lockPosition;
									}
									EditorGUI.EndDisabledGroup();
									
									if(emotion.bones[b].bone != null){
										emotion.bones[b].bone.localPosition = emotion.bones[b].endPosition;
									}
									
									GUILayout.Space(10);
									EditorGUILayout.EndHorizontal();
									EditorGUILayout.BeginHorizontal();
									GUILayout.Space(10);
									GUILayout.Box("Rotation" , EditorStyles.label , GUILayout.MaxWidth(80));
									
									EditorGUI.BeginDisabledGroup(emotion.bones[b].bone == null);
									EditorGUI.BeginDisabledGroup(emotion.bones[b].lockRotation);
									emotion.bones[b].endRotation = EditorGUILayout.Vector3Field("" , emotion.bones[b].endRotation);
									EditorGUI.EndDisabledGroup();
									GUILayout.Space(10);
									if(GUILayout.Button(emotion.bones[b].lockRotation?locked:unlocked , GUILayout.Width(30) , GUILayout.Height(16))){
										emotion.bones[b].lockRotation = !emotion.bones[b].lockRotation;
									}
									EditorGUI.EndDisabledGroup();
									
									if(emotion.bones[b].bone != null){
										emotion.bones[b].bone.localEulerAngles = emotion.bones[b].endRotation;
									}
									
									GUILayout.Space(10);
									EditorGUILayout.EndHorizontal();
									GUILayout.Space(10);
								}
							}
							EditorGUILayout.EndFadeGroup();
							
							EditorGUILayout.Space();

							GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if(lsTarget.blendSystem.blendableCount > 0 ){
								if(GUILayout.Button("Add "+lsTarget.blendSystem.blendableDisplayName , GUILayout.MaxWidth(200))){
									Undo.RecordObject(lsTarget , "Add "+lsTarget.blendSystem.blendableDisplayName);
									emotion.blendShapes.Add(0);

									emotion.weights.Add(0);
									GetBlendShapes();
									EditorUtility.SetDirty(lsTarget);
									serializedTarget.SetIsDifferentCacheDirty();
								}
							}
							EditorGUILayout.Space();
							if(lsTarget.useBones){
								if(GUILayout.Button("Add Bone Transform" , GUILayout.MaxWidth(240))){
									Undo.RecordObject(lsTarget , "Add Bone Shape");
									emotion.bones.Add(new BoneShape());
									EditorUtility.SetDirty(lsTarget);
									serializedTarget.SetIsDifferentCacheDirty();
								}
							}
							GUILayout.FlexibleSpace();
							GUILayout.EndHorizontal();
							if(lsTarget.blendSystem.blendableCount == 0 && !lsTarget.useBones){
								GUILayout.BeginHorizontal();
								GUILayout.Space(10);
								EditorGUILayout.HelpBox(lsTarget.blendSystem.noBlendablesMessage , MessageType.Warning);
								GUILayout.Space(10);
								GUILayout.EndHorizontal();
							}

							GUILayout.Space(14);
							EditorGUILayout.EndVertical();
							
						}else{
							Rect box = EditorGUILayout.BeginHorizontal();
							if(emotion.verified){
								GUI.backgroundColor = Color.white;
							}else{
								GUI.backgroundColor = new Color(0.7f , 0.7f, 0.7f);
							}
							if(GUI.Button(box , "" , EditorStyles.toolbarDropDown)){
								if(currentToggle > -1){
									foreach(BoneShape boneshape in lsTarget.emotions[currentToggle].bones){
										boneshape.bone.localPosition = boneshape.neutralPosition;
										boneshape.bone.localEulerAngles = boneshape.neutralRotation;
									}
								}
								
								currentToggle = a;
								
								if(lsTarget.useBones){
									foreach(BoneShape boneshape in lsTarget.emotions[a].bones){
										if(boneshape.bone != null) {
											boneshape.bone.localPosition = boneshape.endPosition;
											boneshape.bone.localEulerAngles = boneshape.endRotation;
										}
									}
								}
							}
							GUILayout.Box(emotion.emotion + " Emotion" , EditorStyles.miniLabel , GUILayout.Width(250));
							GUI.backgroundColor = Color.white;

							if(emotion.weights.Count == 1){
								GUILayout.Box("1 " + lsTarget.blendSystem.blendableDisplayName , EditorStyles.miniLabel);
							}else if(emotion.weights.Count > 1){
								GUILayout.Box(emotion.weights.Count.ToString() + " " + lsTarget.blendSystem.blendableDisplayNamePlural , EditorStyles.miniLabel);
							}
							if(emotion.bones.Count == 1 && lsTarget.useBones){
								GUILayout.Box("1 Bone Transform" , EditorStyles.miniLabel);
							}else if(emotion.bones.Count > 1 && lsTarget.useBones){
								GUILayout.Box(emotion.bones.Count.ToString() + " Bone Transforms" , EditorStyles.miniLabel);
							}

							if(!emotion.verified){
								GUILayout.FlexibleSpace();
								GUILayout.Box("Missing" , EditorStyles.miniLabel);
								GUILayout.FlexibleSpace();
							}
							EditorGUILayout.EndHorizontal();
						}
						a++;
					}
					EditorGUILayout.Space();

					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if(GUILayout.Button("Edit Emotions" , GUILayout.MaxWidth(300) , GUILayout.Height(25))){
						LipSyncProjectSettings.ShowWindow();
					}
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
				}else if(markerTab == 2){
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(gesturesAnimator , new GUIContent("Animator" , "The animator component used for playing Gestures."));
					if(EditorGUI.EndChangeCheck()){
						if(lsTarget.gesturesAnimator != null){
							string assetPath = AssetDatabase.GetAssetPath(lsTarget.gesturesAnimator.runtimeAnimatorController);
							controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
						}
					}
					EditorGUILayout.Space();

					if(lsTarget.gesturesAnimator != null){
						if(lsTarget.gesturesAnimator.runtimeAnimatorController == null){
							controller = null;
						}else if(controller == null) {
							string assetPath = AssetDatabase.GetAssetPath(lsTarget.gesturesAnimator.runtimeAnimatorController);
							controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
						}

						if(controller != null){
							if(lsTarget.gestures.Count < settings.gestures.Count){
								int missing = settings.gestures.Count-lsTarget.gestures.Count;
								for(int a = lsTarget.gestures.Count ; a < missing ; a++){
									lsTarget.gestures.Add(new GestureInstance(settings.gestures[a] , null , ""));
								}
							}

							if(lsTarget.gestures.Count > 0) {
								EditorGUILayout.LabelField("Gestures" , EditorStyles.boldLabel);
								bool allAssigned = true;
								for(int a = 0 ; a < lsTarget.gestures.Count ; a++){
									if(!settings.gestures.Contains(lsTarget.gestures[a].gesture)){
										lsTarget.gestures.Remove(lsTarget.gestures[a]);
										EditorGUI.EndDisabledGroup();
										EditorGUILayout.EndVertical();
										return;
									}

									Rect lineRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
									if(a%2 == 0) {
										GUI.Box(lineRect , "" , (GUIStyle)"hostview");
									}
									GUILayout.Space(4);
									EditorGUILayout.LabelField(lsTarget.gestures[a].gesture);
									GUILayout.FlexibleSpace();
									lsTarget.gestures[a].clip = (AnimationClip)EditorGUILayout.ObjectField(lsTarget.gestures[a].clip , typeof(AnimationClip) , false);
									if(lsTarget.gestures[a].clip == null) allAssigned = false;
									EditorGUILayout.EndHorizontal();
								}
								GUILayout.Space(10);
								EditorGUILayout.BeginHorizontal();
								GUILayout.FlexibleSpace();
								if(GUILayout.Button("Begin Setup" , GUILayout.MaxWidth(200) , GUILayout.Height(20))){
									GestureSetupWizard.ShowWindow(lsTarget , controller);
								}
								GUILayout.FlexibleSpace();
								EditorGUILayout.EndHorizontal();
								GUILayout.Space(10);
								if(!allAssigned) {
									EditorGUILayout.HelpBox("Not all Gestures have AnimationClips assigned. These gestures will have no effect on this character." , MessageType.Warning);
								}
							}else{
								EditorGUILayout.HelpBox("There are no Gestures defined in the project settings." , MessageType.Info);
							}
						}else{
							EditorGUILayout.HelpBox("Chosen Animator does not have an AnimatorController assigned." , MessageType.Error);
						}
					}else{
						EditorGUILayout.HelpBox("Select an Animator component to enable gesture support." , MessageType.Warning);
					}

					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if(GUILayout.Button("Edit Gestures" , GUILayout.MaxWidth(300) , GUILayout.Height(25))){
						LipSyncProjectSettings.ShowWindow();
					}
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				GUILayout.Box("General Animation Settings" , EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(playOnAwake , new GUIContent ("Play On Awake" , "If checked, the default clip will play when the script awakes."));
				showPlayOnAwake.target = lsTarget.playOnAwake;
				if(EditorGUILayout.BeginFadeGroup(showPlayOnAwake.faded)){
					EditorGUILayout.PropertyField(defaultClip , new GUIContent ("Default Clip" , "The clip to play on awake."));
					EditorGUILayout.PropertyField(defaultDelay , new GUIContent ("Default Delay" , "The audio delay to be used with the default clip. A value around 0.15f can improve results with files generated by AutoSync. If unsure, leave at 0."));
				}
				EditorGUILayout.EndFadeGroup();
				EditorGUILayout.PropertyField(scaleAudioSpeed , new GUIContent ("Scale Audio Speed" , "Whether or not the speed of the audio will be slowed/sped up to match Time.timeScale."));
				EditorGUILayout.Space();
				GUILayout.Box("Phoneme Animation Settings" , EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(restTime , new GUIContent ("Rest Time" , "If there are no phonemes within this many seconds of the previous one, a rest will be inserted."));
				EditorGUILayout.PropertyField(restHoldTime , new GUIContent ("Pre-Rest Hold Time" , "The time, in seconds, a shape will be held before blending when a rest is inserted."));

				GUILayout.Space(20);

				EditorGUILayout.PropertyField(onFinishedPlaying);

				if(markerTab == 0){
					if(GUI.changed){
						if(blendables == null){
							GetBlendShapes();
						}
						EditorUtility.SetDirty(lsTarget);
						serializedTarget.SetIsDifferentCacheDirty();
					}

					if(oldToggle != currentToggle){
						foreach(PhonemeShape phoneme in lsTarget.phonemes){
							foreach(int shape in phoneme.blendShapes){
								lsTarget.blendSystem.SetBlendableValue(shape , 0);
							}
						}

						if(oldToggle > -1){
							foreach(BoneShape boneshape in lsTarget.phonemes[oldToggle].bones){
								if(boneshape.bone != null) {
									boneshape.bone.localPosition = boneshape.neutralPosition;
									boneshape.bone.localEulerAngles = boneshape.neutralRotation;
								}
							}
						}

						if(currentToggle > -1){
							foreach(BoneShape boneshape in lsTarget.phonemes[currentToggle].bones){
								if(boneshape.bone != null) {
									boneshape.bone.localPosition = boneshape.endPosition;
									boneshape.bone.localEulerAngles = boneshape.endRotation;
								}
							}
						}
					}
					
					if(GUI.changed || oldToggle != currentToggle){
						if(currentToggle >= 0){
							for(int b = 0 ; b < lsTarget.phonemes[currentToggle].weights.Count ; b++){
								lsTarget.blendSystem.SetBlendableValue(lsTarget.phonemes[currentToggle].blendShapes[b] , lsTarget.phonemes[currentToggle].weights[b]);
							}
						}
						oldToggle = currentToggle;
					}
				}else if(markerTab == 1){
					if(GUI.changed){
						if(blendables == null){
							GetBlendShapes();
						}
						EditorUtility.SetDirty(lsTarget);
						serializedTarget.SetIsDifferentCacheDirty();
					}

					if(oldToggle != currentToggle){
						foreach(EmotionShape emotion in lsTarget.emotions){
							foreach(int shape in emotion.blendShapes){
								lsTarget.blendSystem.SetBlendableValue(shape , 0);
							}
						}

						if(oldToggle > -1){
							foreach(BoneShape boneshape in lsTarget.emotions[oldToggle].bones){
								if(boneshape.bone != null){
									boneshape.bone.localPosition = boneshape.neutralPosition;
									boneshape.bone.localEulerAngles = boneshape.neutralRotation;
								}
							}
						}
					}
					
					if(GUI.changed || oldToggle != currentToggle){
						if(currentToggle >= 0){
							for(int b = 0 ; b < lsTarget.emotions[currentToggle].weights.Count ; b++){
								lsTarget.blendSystem.SetBlendableValue(lsTarget.emotions[currentToggle].blendShapes[b] , lsTarget.emotions[currentToggle].weights[b]);
							}
						}
						oldToggle = currentToggle;
					}
				}
			}else{
				EditorGUILayout.HelpBox(lsTarget.blendSystem.notReadyMessage , MessageType.Warning);
			}
		}
		EditorGUI.EndDisabledGroup();
		EditorGUILayout.EndVertical();

		if(saving){
			GUI.Box(new Rect(40 , fullheight.y+(fullheight.height/2)-60 , fullheight.width-80 , 120) , "" , (GUIStyle)"flow node 0");
			GUI.Box(new Rect(50 , fullheight.y+(fullheight.height/2)-50 , fullheight.width-100 , 20) , "Create New Preset" , EditorStyles.label);
			GUI.Box(new Rect(50 , fullheight.y+(fullheight.height/2)-20 , 80 , 20) , "Preset Path" , EditorStyles.label);
			savingName = EditorGUI.TextField(new Rect(140 , fullheight.y+(fullheight.height/2)-20 , fullheight.width-290 , 20) , "" , savingName);

			if(GUI.Button(new Rect(fullheight.width-140 , fullheight.y+(fullheight.height/2)-20 , 80 , 20) , "Browse")){
				GUI.FocusControl("");
				string newPath = EditorUtility.SaveFilePanelInProject("Chose Preset Location" , "New Preset" , "asset" , "");

				if(newPath != "") {
					savingName = newPath.Substring("Assets/".Length);
				}
			}
			if(GUI.Button(new Rect(100 , fullheight.y+(fullheight.height/2)+15 , (fullheight.width/2)-110 , 25) , "Cancel")){
				GUI.FocusControl("");
				savingName = "";
				saving = false;
			}
			if(GUI.Button(new Rect((fullheight.width/2)+10 , fullheight.y+(fullheight.height/2)+15 , (fullheight.width/2)-110 , 25) , "Save")){
				if(!Path.GetDirectoryName(savingName).Contains("Presets")){
					EditorUtility.DisplayDialog("Invalid Path" , "Presets must be saved in a folder called Presets, or a subfolder of one." , "OK");
					return;
				}else if(!Directory.Exists(Application.dataPath+"/"+Path.GetDirectoryName(savingName))){
					EditorUtility.DisplayDialog("Directory Does Not Exist" , "The directory "+Path.GetDirectoryName(savingName)+" does not exist." , "OK");
					return;
				}else if(!Path.HasExtension(savingName)){
					savingName = Path.GetDirectoryName(savingName)+"/"+Path.GetFileNameWithoutExtension(savingName)+".asset";
				}else if(Path.GetExtension(savingName) != ".asset"){
					savingName = Path.GetDirectoryName(savingName)+"/"+Path.GetFileNameWithoutExtension(savingName)+".asset";
				}

				BlendshapePreset preset = ScriptableObject.CreateInstance<BlendshapePreset>();
				preset.phonemeShapes = new List<PhonemeShape>();
				preset.emotionShapes = new List<EmotionShape>();

				foreach(PhonemeShape ps in lsTarget.phonemes){
					PhonemeShape newPhoneme = new PhonemeShape(ps.phoneme);
					newPhoneme.blendShapes = new List<int>(ps.blendShapes);
					newPhoneme.weights = new List<float>(ps.weights);

					preset.phonemeShapes.Add(newPhoneme);
				}

				foreach(EmotionShape es in lsTarget.emotions){
					EmotionShape newEmotion = new EmotionShape(es.emotion);
					newEmotion.blendShapes = new List<int>(es.blendShapes);
					newEmotion.weights = new List<float>(es.weights);;
					
					preset.emotionShapes.Add(newEmotion);
				}

				AssetDatabase.CreateAsset(preset , "Assets/" + savingName);
				AssetDatabase.Refresh();
				savingName = "";
				saving = false;
			}
		}

		serializedTarget.ApplyModifiedProperties();
	}

	void DrawBlendSystemButtons (){
		foreach(BlendSystemButton.Reference button in blendSystemButtons) {
			if(GUILayout.Button(button.displayName , GUILayout.Height(20) , GUILayout.MinWidth(120))) {
				button.method.Invoke(lsTarget.blendSystem , null);
			}
		}
	}

	BlendSystemButton.Reference[] GetBlendSystemButtons () {
		MethodInfo[] methods = lsTarget.blendSystem.GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance);
		BlendSystemButton.Reference[] buttons = new BlendSystemButton.Reference[0];

		int buttonLength = 0;
		for(int m = 0 ; m < methods.Length ; m++) {
			BlendSystemButton[] button = (BlendSystemButton[])methods[m].GetCustomAttributes(typeof(BlendSystemButton) , false);
			if(button.Length > 0){
				buttonLength++;
			}
		}

		if(buttonLength > 0) {
			buttons = new BlendSystemButton.Reference[buttonLength];
			int b = 0;
			for(int m = 0 ; m < methods.Length ; m++) {
				BlendSystemButton[] button = (BlendSystemButton[])methods[m].GetCustomAttributes(typeof(BlendSystemButton) , false);
				if(button.Length > 0){
					buttons[b] = new BlendSystemButton.Reference(button[0].displayName , methods[m]);
					b++;
				}
			}
		}

		return buttons;
	}

	void OnSceneGUI () {
		if(markerTab == 0){
			if(currentToggle >= 0){
				Handles.BeginGUI();
				GUILayout.BeginArea(new Rect(Screen.width - 256 , Screen.height - 246 , 256, 256));

				GUILayout.Box(guides[currentToggle] , GUIStyle.none);
				GUILayout.EndArea();
				Handles.EndGUI();
			}
		}
	}

	void LoadPreset (object data) {
		string file = (string)data;
		if(file.EndsWith(".asset" , true , null)){
			BlendshapePreset preset = AssetDatabase.LoadAssetAtPath<BlendshapePreset>("Assets" + file.Substring((Application.dataPath).Length));

			if(preset != null){
				List<PhonemeShape> newPhonemes = new List<PhonemeShape>();
				List<EmotionShape> newEmotions = new List<EmotionShape>();

				foreach(PhonemeShape ps in preset.phonemeShapes){
					PhonemeShape newPhoneme = new PhonemeShape(ps.phoneme);
					newPhoneme.blendShapes = new List<int>(ps.blendShapes);
					newPhoneme.weights = new List<float>(ps.weights);

					foreach(int bs in ps.blendShapes){
						if(bs >= lsTarget.blendSystem.blendableCount){
							return;
						}
					}

					newPhonemes.Add(newPhoneme);
				}
				
				foreach(EmotionShape es in preset.emotionShapes){
					EmotionShape newEmotion = new EmotionShape(es.emotion);
					newEmotion.blendShapes = new List<int>(es.blendShapes);
					newEmotion.weights = new List<float>(es.weights);;

					foreach(int bs in es.blendShapes){
						if(bs >= lsTarget.blendSystem.blendableCount){
							return;
						}
					}

					newEmotions.Add(newEmotion);
				}

				lsTarget.phonemes = newPhonemes;
				lsTarget.emotions = newEmotions;

				for(int bShape = 0 ; bShape < lsTarget.blendSystem.blendableCount ; bShape++){
					lsTarget.blendSystem.SetBlendableValue(bShape , 0);
				}

				if(markerTab == 0){
					if(currentToggle >= 0){
						int b=0;
						foreach(int shape in lsTarget.phonemes[currentToggle].blendShapes){
							lsTarget.blendSystem.SetBlendableValue(shape , lsTarget.phonemes[currentToggle].weights[b]);
							b++;
						}
					}
				}else if(markerTab ==1){
					if(currentToggle >= 0){
						int b=0;
						foreach(int shape in lsTarget.emotions[currentToggle].blendShapes){
							lsTarget.blendSystem.SetBlendableValue(shape , lsTarget.emotions[currentToggle].weights[b]);
							b++;
						}
					}
				}
			}
		}
	}

	void NewPreset () {
		saving = true;
		savingName = "Rogo Digital/LipSync/Presets/New Preset.asset";
	}

	void GetBlendShapes () {
		if(lsTarget.blendSystem.isReady){
			blendables = lsTarget.blendSystem.GetBlendables();
		}
	}

	void FindBlendSystems () {
		blendSystems = new List<System.Type>();
		blendSystemNames = new List<string>();
		foreach(System.Type t in typeof(BlendSystem).Assembly.GetTypes()){
			if(t.IsSubclassOf(typeof(BlendSystem))){
				blendSystems.Add(t);
				blendSystemNames.Add(AddSpaces(t.Name));
			}
		}

		if(lsTarget.blendSystem != null){
			blendSystemNumber = blendSystems.IndexOf(lsTarget.blendSystem.GetType());
		}
	}

	void CreateBlendSystemEditor () {
		if(lsTarget.blendSystem != null){
			lsTarget.blendSystem = lsTarget.GetComponent<BlendSystem>();
			if(lsTarget.blendSystem != null) blendSystemEditor = Editor.CreateEditor(lsTarget.blendSystem);
		}
	}

	public static void FixedEndFadeGroup (float value) {
		if(value == 0f || value == 1f) {
			return;
		}
		EditorGUILayout.EndFadeGroup();
	}

	private void AutoUpdate (float oldVersion) {
		// Used for additional future-proofing
		switch(oldVersion.ToString()) {
		case "0":
			// Anything Pre-0.6
			if(EditorUtility.DisplayDialog("LipSync has been updated." , "This character was last used with an old version of LipSync prior to 0.6. The recommended values for Rest Time and Pre-Rest Hold Time have been changed to 0.2 and 0.1 respectively. Do you want to change these values automatically?" , "Yes" , "No")){
				lsTarget.restTime = 0.2f;
				lsTarget.restHoldTime = 0.1f;
			}
			break;
		}
	}

	public static string AddSpaces (string input){
		if (string.IsNullOrEmpty(input))
			return "";
		
		StringBuilder newText = new StringBuilder(input.Length * 2);
		newText.Append(input[0]);
		for (int i = 1; i < input.Length; i++)
		{
			if (char.IsUpper(input[i]) && input[i-1] != ' '){
				if(i+1 < input.Length){
					if(!char.IsUpper(input[i-1]) || !char.IsUpper(input[i+1])){
						newText.Append(' ');
					}
				}else{
					newText.Append(' ');
				}
			}
			newText.Append(input[i]);
		}
		return newText.ToString();
	}
}