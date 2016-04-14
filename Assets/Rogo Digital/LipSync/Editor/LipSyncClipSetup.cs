using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using RogoDigital;
using RogoDigital.Lipsync;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Reflection;

public class LipSyncClipSetup : ModalParent {

	public AudioClip clip;

	private string versionNumber = "Beta 0.61";

	private Texture2D waveform;
	private float seekPosition;

	private bool isPlaying = false;
	private bool isPaused = false;
	private bool previewing = false;
	
	private Rect oldPos;
	private int waveformHeight;
	private AudioClip oldClip;

	private TimeSpan timeSpan;
	private float oldSeekPosition;
	private float stopTimer = 0;
	private float prevTime = 0;
	private float resetTime = 0;
	private float viewportStart = 0;
	private float viewportEnd = 10;

	private int currentMarker = -1;
	private int currentComponent = 0;
	private int markerTab = 0;

	private bool dragging = false;
	private bool markerPlaceable = true;
	
	private int filterMask = -1;
	private int[] phonemeFlags = new int[]{1 , 2 , 4 , 8 , 16 , 32 , 64 , 128 , 256};

	private float startOffset;
	private float endOffset;
	private float lowerMarkerLimit;
	private float upperMarkerLimit;

	private Color nextColor;
	private Color lastColor;

	private EmotionMarker nextMarker = null;
	private EmotionMarker previousMarker = null;
	
	private string lastLoad = "";
	private string fileName = "Untitled";
	public bool changed = false;

	private Texture2D playhead_top;
	private Texture2D playhead_line;
	private Texture2D playhead_bottom;
	private Texture2D track_top;
	private Texture2D playIcon;
	private Texture2D stopIcon;
	private Texture2D pauseIcon;
	private Texture2D settingsIcon;
	private Texture2D previewIcon;
	private Texture2D windowIcon;

	private Texture2D marker_normal;
	private Texture2D marker_hover;
	private Texture2D marker_selected;
	private Texture2D marker_line;

	private Texture2D emotion_start;
	private Texture2D emotion_start_hover;
	private Texture2D emotion_start_selected;
	private Texture2D emotion_area;
	private Texture2D emotion_end;
	private Texture2D emotion_blend_in;
	private Texture2D emotion_blend_out;
	private Texture2D emotion_end_hover;
	private Texture2D emotion_end_selected;

	private Texture2D gesture_normal;
	private Texture2D gesture_hover;
	private Texture2D gesture_selected;

	public List<PhonemeMarker> phonemeData = new List<PhonemeMarker>();
	public List<EmotionMarker> emotionData = new List<EmotionMarker>();
	public List<GestureMarker> gestureData = new List<GestureMarker>();

	private LipSyncProject settings;
	private bool settingsOpen = false;

	private bool visualPreview = false;
	private LipSync previewTarget = null;
	private bool previewOutOfDate = true;

	private bool useColors;
	private int defaultColor;

	private bool continuousUpdate;
	private float snappingDistance;
	private bool snapping;
	private bool setViewportOnLoad;
	private int maxWaveformWidth;
	private bool showExtensionsOnLoad;
	private bool showTimeline;
	private float scrubLength;

	private Vector2 settingsScroll;

	void OnEnable () {
		//Load Resources;
		playhead_top = Resources.Load<Texture2D>("Lipsync/Playhead_top");
		playhead_line = Resources.Load<Texture2D>("Lipsync/Playhead_middle");
		playhead_bottom = Resources.Load<Texture2D>("Lipsync/Playhead_bottom");
		
		marker_normal = Resources.Load<Texture2D>("Lipsync/marker");
		marker_hover = Resources.Load<Texture2D>("Lipsync/marker-selected");
		marker_selected = Resources.Load<Texture2D>("Lipsync/marker-highlight");
		marker_line = Resources.Load<Texture2D>("Lipsync/white");

		gesture_normal = Resources.Load<Texture2D>("Lipsync/gesture");
		gesture_hover = Resources.Load<Texture2D>("Lipsync/gesture-selected");
		gesture_selected = Resources.Load<Texture2D>("Lipsync/gesture-highlight");

		emotion_start = Resources.Load<Texture2D>("Lipsync/emotion-start");
		emotion_start_hover = Resources.Load<Texture2D>("Lipsync/emotion-start-highlight");
		emotion_start_selected = Resources.Load<Texture2D>("Lipsync/emotion-start-select");
		emotion_area = Resources.Load<Texture2D>("Lipsync/emotion-area");
		emotion_end = Resources.Load<Texture2D>("Lipsync/emotion-end");
		emotion_blend_in = Resources.Load<Texture2D>("Lipsync/emotion-blend-in");
		emotion_blend_out = Resources.Load<Texture2D>("Lipsync/emotion-blend-out");
		emotion_end_hover = Resources.Load<Texture2D>("Lipsync/emotion-end-highlight");
		emotion_end_selected = Resources.Load<Texture2D>("Lipsync/emotion-end-select");

		if(!EditorGUIUtility.isProSkin){
			track_top = Resources.Load<Texture2D>("Lipsync/Light/track");
			playIcon = Resources.Load<Texture2D>("Lipsync/Light/play");
			stopIcon = Resources.Load<Texture2D>("Lipsync/Light/stop");
			pauseIcon = Resources.Load<Texture2D>("Lipsync/Light/pause");
			settingsIcon = Resources.Load<Texture2D>("Lipsync/Light/settings");
			previewIcon = Resources.Load<Texture2D>("Lipsync/Light/eye");
		}else{
			track_top = Resources.Load<Texture2D>("Lipsync/Dark/track");
			playIcon = Resources.Load<Texture2D>("Lipsync/Dark/play");
			stopIcon = Resources.Load<Texture2D>("Lipsync/Dark/stop");
			pauseIcon = Resources.Load<Texture2D>("Lipsync/Dark/pause");
			settingsIcon = Resources.Load<Texture2D>("Lipsync/Dark/settings");
			previewIcon = Resources.Load<Texture2D>("Lipsync/Dark/eye");
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

		//Get Editor Settings
		continuousUpdate = EditorPrefs.GetBool("LipSync_ContinuousUpdate" , true);

		useColors = EditorPrefs.GetBool("LipSync_UseColors" , true);
		defaultColor = EditorPrefs.GetInt("LipSync_DefaultColor" , 0xAAAAAA);
		setViewportOnLoad = EditorPrefs.GetBool("LipSync_SetViewportOnLoad" , true);
		showTimeline = EditorPrefs.GetBool("LipSync_ShowTimeline" , true);
		showExtensionsOnLoad = EditorPrefs.GetBool("LipSync_ShowExtensionsOnLoad" , true);
		maxWaveformWidth = EditorPrefs.GetInt("LipSync_MaxWaveformWidth" , 2048);
		scrubLength = EditorPrefs.GetFloat("LipSync_ScrubLength" , 0.075f);

		if(EditorPrefs.HasKey("LipSync_Snapping")){
			if(EditorPrefs.GetBool("LipSync_Snapping")){
				if(EditorPrefs.HasKey("LipSync_SnappingDistance")){
					snappingDistance = EditorPrefs.GetFloat("LipSync_SnappingDistance");
					snapping = true;
				}else{
					snappingDistance = 0;
					snapping = false;
				}
			}else{
				snappingDistance = 0;
				snapping = false;
			}
		}else{
			snappingDistance = 0;
			snapping = false;
		}

		oldPos = this.position;
		if(phonemeData == null)phonemeData = new List<PhonemeMarker>();
		oldClip = clip;
	}

	void OnDisable () {
		if(previewTarget != null) {
			UpdatePreview(0);
			previewTarget = null;
		}
	}

	public override void OnModalGUI () {
		Event currentEvent = Event.current;

		//Runtime
		if(Application.isPlaying){
			Rect box = EditorGUILayout.BeginHorizontal();
			GUI.Box (box , "" , EditorStyles.toolbar);
			GUI.color = Color.gray;
			GUILayout.Box("File" , EditorStyles.toolbarDropDown , GUILayout.Width(60));
			GUILayout.Box("Help" , EditorStyles.toolbarDropDown , GUILayout.Width(60));
			GUI.color = Color.white;
			GUILayout.FlexibleSpace();
			GUILayout.Box(versionNumber , EditorStyles.label);
			GUILayout.Box("" , EditorStyles.toolbar);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.HelpBox("You can only edit clips while not playing." , MessageType.Warning);
			return;
		}

		GUIStyle centeredStyle = new GUIStyle(EditorStyles.whiteLabel);
		centeredStyle.alignment = TextAnchor.MiddleCenter;

		//Toolbar
		Rect previewBox = EditorGUILayout.BeginHorizontal();
		GUI.Box (previewBox , "" , EditorStyles.toolbar);
		Rect fileRect = EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("File" , EditorStyles.toolbarDropDown , GUILayout.Width(60))){
			GenericMenu fileMenu = new GenericMenu();

			fileMenu.AddItem(new GUIContent("New File") , false , OnNewClick);
			fileMenu.AddItem(new GUIContent("Open File") , false , OnLoadClick);
			fileMenu.AddItem(new GUIContent("Import XML") , false , OnXMLImport);
			fileMenu.AddSeparator("");
			if(clip && phonemeData.Count > 0 || clip && emotionData.Count > 0){
				fileMenu.AddItem(new GUIContent("Save") , false , OnSaveClick);
				fileMenu.AddItem(new GUIContent("Save As") , false , OnSaveAsClick);
				fileMenu.AddItem(new GUIContent("Export" , "Export asset and audioclip as a .unitypackage for transferring to a later Unity version or another OS.") , false , OnUnityExport);
				fileMenu.AddItem(new GUIContent("Export XML") , false , OnXMLExport);
			}else{
				fileMenu.AddDisabledItem(new GUIContent("Save"));
				fileMenu.AddDisabledItem(new GUIContent("Save As"));
				fileMenu.AddDisabledItem(new GUIContent("Export"));
				fileMenu.AddDisabledItem(new GUIContent("Export XML"));
			}
			fileMenu.AddSeparator("");
			fileMenu.AddItem(new GUIContent("Project Settings") , false , ShowProjectSettings);
			fileMenu.AddSeparator("");
			fileMenu.AddItem(new GUIContent("Exit") , false , Close);
			fileMenu.DropDown(fileRect);
		}
		GUILayout.EndHorizontal();
		#if UNITY_EDITOR_WIN
		Rect autoRect = EditorGUILayout.BeginHorizontal();
		if(clip != null){
			if(GUILayout.Button("AutoSync" , EditorStyles.toolbarDropDown , GUILayout.Width(80))){
				GenericMenu autoMenu = new GenericMenu();
				
				autoMenu.AddDisabledItem(new GUIContent("Powered by Annosoft"));
				autoMenu.AddSeparator("");
				autoMenu.AddItem(new GUIContent("Process Audio") , false , StartAutoSync);
				autoMenu.AddItem(new GUIContent("Process Audio + Text") , false , StartAutoSyncText);

				autoMenu.DropDown(autoRect);
			}
		}else{
			GUI.color = Color.gray;
			GUILayout.Box(new GUIContent("AutoSync" , "Select a clip to use AutoSync.") , EditorStyles.toolbarDropDown , GUILayout.Width(80));
			GUI.color = Color.white;
		}
		GUILayout.EndHorizontal();
		#else
		GUI.color = Color.gray;
		GUILayout.Box(new GUIContent("AutoSync" , "Coming in version 1.0") , EditorStyles.toolbarDropDown , GUILayout.Width(80));
		GUI.color = Color.white;
		#endif
		Rect helpRect = EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Help" , EditorStyles.toolbarDropDown , GUILayout.Width(60))){
			GenericMenu helpMenu = new GenericMenu();

			helpMenu.AddDisabledItem(new GUIContent("LipSync "+versionNumber));
			helpMenu.AddDisabledItem(new GUIContent("© Rogo Digital "+DateTime.Now.Year.ToString()));
			helpMenu.AddSeparator("");
			helpMenu.AddItem(new GUIContent("Get LipSync Extensions") , false , RDExtensionWindow.ShowWindowGeneric , "LipSync");
			helpMenu.AddSeparator("");
			helpMenu.AddItem(new GUIContent("Forum Thread") , false , OpenURL , "http://forum.unity3d.com/threads/beta-lipsync-a-flexible-lipsyncing-and-facial-animation-system.309324/");
			helpMenu.AddItem(new GUIContent("Email Support") , false , OpenURL , "mailto:contact@rogodigital.com");

			helpMenu.DropDown(helpRect);
		}
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		if(changed == true){
			GUILayout.Box (fileName+"*", EditorStyles.label);
		}else{
			GUILayout.Box (fileName, EditorStyles.label);
		}
		GUILayout.FlexibleSpace();

		settingsOpen = GUILayout.Toggle(settingsOpen , settingsIcon , EditorStyles.toolbarButton , GUILayout.MaxWidth(40));
		visualPreview = GUILayout.Toggle(visualPreview , previewIcon , EditorStyles.toolbarButton , GUILayout.MaxWidth(40));

		GUILayout.Space(20);
		GUILayout.Box(versionNumber , EditorStyles.label);
		GUILayout.Box("" , EditorStyles.toolbar);
		EditorGUILayout.EndHorizontal();

		if(settingsOpen){
			//Settings Screen
			settingsScroll = GUILayout.BeginScrollView(settingsScroll);
			GUILayout.Space(10);
			GUILayout.Box("Settings" , EditorStyles.largeLabel);
			GUILayout.Space(15);
			GUILayout.Box("Emotion Editing" , EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			bool oldsnapping = snapping;
			snapping = GUILayout.Toggle(snapping , "Emotion Snapping");
			if(oldsnapping != snapping){
				EditorPrefs.SetBool("LipSync_Snapping" , snapping);
				snappingDistance = 0;
			}
			if(snapping){
				GUILayout.Space(10);
				float oldSnappingDistance = snappingDistance;
				snappingDistance = EditorGUILayout.Slider(new GUIContent("Snapping Distance" , "The strength of the emotion snapping."), (snappingDistance*200) , 0 , 10)/200;
				GUILayout.FlexibleSpace();
				if(oldSnappingDistance != snappingDistance){
					EditorPrefs.SetFloat("LipSync_SnappingDistance" , snappingDistance);
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			bool oldcolors = useColors;
			useColors = GUILayout.Toggle(useColors , "Use Emotion Colors");
			if(oldcolors != useColors){
				EditorPrefs.SetBool("LipSync_UseColors" , useColors);
			}
			if(!useColors){
				GUILayout.Space(10);
				int oldColour = defaultColor;
				defaultColor = ColorToHex(EditorGUILayout.ColorField("Default Color" , HexToColor(defaultColor)));
				GUILayout.FlexibleSpace();
				if(oldColour != defaultColor){
					EditorPrefs.SetInt("LipSync_DefaultColor" , defaultColor);
				}
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(15);
			GUILayout.Box("General Settings" , EditorStyles.boldLabel);
			bool oldUpdate = continuousUpdate;
			continuousUpdate = GUILayout.Toggle(continuousUpdate , new GUIContent("Continuous Update" , "Whether to update the window every frame. This makes editing more responsive, but may be taxing on low-powered systems."));
			if(oldUpdate != continuousUpdate){
				EditorPrefs.SetBool("LipSync_ContinuousUpdate" , continuousUpdate);
			}

			bool oldSetViewportOnLoad = setViewportOnLoad;
			setViewportOnLoad = GUILayout.Toggle(setViewportOnLoad , new GUIContent("Set Viewport on File Load" , "Whether to set the viewport to show the entire clip when a new file is loaded."));
			if(oldSetViewportOnLoad != setViewportOnLoad){
				EditorPrefs.SetBool("LipSync_SetViewportOnLoad" , setViewportOnLoad);
			}

			bool oldShowTimeline = showTimeline;
			showTimeline = GUILayout.Toggle(showTimeline , new GUIContent("Show Time Markers" , "Whether to show time markers under the timeline."));
			if(oldShowTimeline != showTimeline){
				EditorPrefs.SetBool("LipSync_ShowTimeline" , showTimeline);
			}

			float oldScrubLength = scrubLength;
			scrubLength = EditorGUILayout.FloatField(new GUIContent("Scrubbing Preview Length" , "The duration, in seconds, the clip will be played for when scrubbing.") , scrubLength);
			if(oldScrubLength != scrubLength){
				EditorPrefs.SetFloat("LipSync_ScrubLength" , scrubLength);
			}
			GUILayout.Space(10);

			int oldMaxWaveformWidth = maxWaveformWidth;
			maxWaveformWidth = EditorGUILayout.IntField(new GUIContent("Max Waveform Width" , "The Maximum width for the waveform preview image. Warning: very high values can cause crashes when zooming in on the clip.") , maxWaveformWidth , GUILayout.MaxWidth(300));
			if(oldMaxWaveformWidth != maxWaveformWidth){
				EditorPrefs.SetInt("LipSync_MaxWaveformWidth" , maxWaveformWidth);
			}

			bool oldShowExtensionsOnLoad = showExtensionsOnLoad;
			showExtensionsOnLoad = GUILayout.Toggle(showExtensionsOnLoad , new GUIContent("Show Extension Window" , "Whether to automatically dock an extensions window to this one when it is opened."));
			if(oldShowExtensionsOnLoad != showExtensionsOnLoad){
				EditorPrefs.SetBool("LipSync_ShowExtensionsOnLoad" , showExtensionsOnLoad);
			}
			GUILayout.Space(20);
			GUILayout.EndScrollView();
			return;
		}

		//Main Body
		GUILayout.Space(15);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Space(20);
		EditorGUI.BeginChangeCheck();
		clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip" , clip , typeof(AudioClip) , false , GUILayout.MaxWidth(800));
		if(EditorGUI.EndChangeCheck()){
			DestroyImmediate(waveform);
			changed = true;
		}
		GUILayout.Space(20);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(15);
		
		if(clip){
			float viewportSeconds = (viewportEnd-viewportStart);
			float pixelsPerSecond = position.width/viewportSeconds;
			float mouseX = Event.current.mousePosition.x;
			float mouseY = Event.current.mousePosition.y;

			//Tab Controls
			int oldTab = markerTab;
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Space(20);
			markerTab = GUILayout.Toolbar(markerTab , new string[]{"Phonemes" , "Emotions" , "Gestures"} , GUILayout.MaxWidth(800));
			GUILayout.Space(20);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if(oldTab != markerTab){
				markerPlaceable = true;
				if(markerTab == 1){
					foreach(EmotionMarker tMarker in emotionData){
						if(seekPosition >= tMarker.startTime && seekPosition <= tMarker.endTime)markerPlaceable = false;
					}
				}else if(markerTab == 0){
					markerPlaceable = true;
				}
			}

			GUILayout.Space(35);
			//Preview Box
			previewBox = EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUILayout.Box("" , (GUIStyle)"PreBackground" , GUILayout.Width(position.width) , GUILayout.Height((position.height-waveformHeight)-18));
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			waveformHeight = (int)previewBox.y;
			if(waveform != null)GUI.DrawTexture(new Rect(-(viewportStart*pixelsPerSecond) , previewBox.y+3 , clip.length*pixelsPerSecond , (position.height-waveformHeight)-18) , waveform);

			//Playhead
			seekPosition = GUI.HorizontalSlider(new Rect(-(viewportStart*pixelsPerSecond) , previewBox.y+3 , clip.length*pixelsPerSecond , (position.height-waveformHeight)-18) , seekPosition , 0 , 1 , GUIStyle.none , GUIStyle.none);
			GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+(seekPosition*(clip.length*pixelsPerSecond))-3 , previewBox.y , 7 , previewBox.height) , playhead_line);
			GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+(seekPosition*(clip.length*pixelsPerSecond))-7 , previewBox.y , 15 , 15) , playhead_top);
			GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+(seekPosition*(clip.length*pixelsPerSecond))-7 , (previewBox.y+previewBox.height)-15 , 15 , 15) , playhead_bottom);

			GUI.DrawTexture(new Rect(0 , previewBox.y-25 , position.width , 26) , track_top);

			//Time Lines
			if(showTimeline){
				float timeOffset = (viewportStart%1)*pixelsPerSecond;
				for(int a = 0 ; a < viewportSeconds+1 ; a++){
					GUI.DrawTexture(new Rect((a*pixelsPerSecond)-timeOffset , previewBox.y+1 , 1 , 12) , marker_line);
					GUI.Box(new Rect((a*pixelsPerSecond)-(timeOffset-5) , previewBox.y , 30 , 20) , ((Mathf.FloorToInt(viewportStart)+a)%60).ToString().PadLeft(2,'0')+"s" , EditorStyles.whiteMiniLabel);
				}
			}

			//Preview Warning
			if(visualPreview){
				bool error = true;
				if(Selection.activeGameObject != null) {
					if(previewTarget == null) {
						previewTarget = Selection.activeGameObject.GetComponent<LipSync>();
						if(previewTarget != null) {
							error = false;
						}
					}else if(previewTarget.gameObject != Selection.activeGameObject) {
						previewTarget = Selection.activeGameObject.GetComponent<LipSync>();
						if(previewTarget != null) {
							error = false;
						}
					}else{
						error = false;
					}
				}

				if(error){
					EditorGUI.HelpBox(new Rect(20 , previewBox.y+previewBox.height-30 , position.width-40 , 25) , "Preview mode active. Select a GameObject with a valid LipSync component in the scene to preview." , MessageType.Warning);
				}else{
					EditorGUI.HelpBox(new Rect(20 , previewBox.y+previewBox.height-30 , position.width-40 , 25) , "Preview mode active. Note: only Phonemes and Emotions will be shown in the preview." , MessageType.Info);
				}
			}else if(previewTarget != null) {
				UpdatePreview(0);
				previewTarget = null;
			}

			Rect tooltipRect = new Rect();
			string tip = "";

			if(markerTab == 0){

				//Phoneme Markers
				int highlightedMarker = -1;

				foreach(PhonemeMarker marker in phonemeData){
					if((filterMask & phonemeFlags[(int)marker.phoneme]) == phonemeFlags[(int)marker.phoneme]){
						Rect markerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.time*(clip.length*pixelsPerSecond))-12.5f , previewBox.y-25 , 25 , 26);
						if(mouseX > markerRect.x+5 && mouseX < markerRect.x+markerRect.width-5 && mouseY > markerRect.y && mouseY < markerRect.y+markerRect.height-4 && currentMarker == -1){
							highlightedMarker = phonemeData.IndexOf(marker);
						}
					}
				}
				
				foreach(PhonemeMarker marker in phonemeData){
					if((filterMask & phonemeFlags[(int)marker.phoneme]) == phonemeFlags[(int)marker.phoneme]){
						Rect markerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.time*(clip.length*pixelsPerSecond))-12.5f , previewBox.y-25 , 25 , 26);
						if(dragging == false && highlightedMarker > -1 && focusedWindow == this){
							PhonemeMarker cm = phonemeData[highlightedMarker];
							
							if(currentEvent.type == EventType.MouseDrag){
								currentMarker = phonemeData.IndexOf(cm);
								dragging = true;
								break;
							}else if(currentEvent.type == EventType.ContextClick){
								GenericMenu markerMenu = new GenericMenu();
								markerMenu.AddItem(new GUIContent("Delete") , false , DeleteMarker , cm);
								for(int a = 0 ; a < 9 ; a++){
									Phoneme phon = (Phoneme)a;
									markerMenu.AddItem(new GUIContent("Change/"+phon.ToString()) , false , ChangeMarkerPicked , new List<int>{phonemeData.IndexOf(cm) , a});
								}
								//markerMenu.AddSeparator("");
								//markerMenu.AddItem(new GUIContent("Marker Settings") , false , PhonemeMarkerSettings , marker);
								markerMenu.ShowAsContext();
							}
						}
						
						if(currentMarker == phonemeData.IndexOf(marker)){
							GUI.Box(markerRect , marker_selected , GUIStyle.none);
							tip = marker.phoneme.ToString();
							GUI.DrawTexture(new Rect(markerRect.x+12 , previewBox.y , 1 , previewBox.height) , marker_line);
							tooltipRect = new Rect(markerRect.x+25 , markerRect.y , 120 , markerRect.height);
						}else{
							if(highlightedMarker == phonemeData.IndexOf(marker)){
								GUI.Box(markerRect , marker_hover , GUIStyle.none);
								GUI.DrawTexture(new Rect(markerRect.x+12 , previewBox.y , 1 , previewBox.height) , marker_line);
								tip = marker.phoneme.ToString();
								if(markerRect.x+145 > this.position.width){
									tooltipRect = new Rect(markerRect.x-125 , markerRect.y , 120 , markerRect.height);
								}else{
									tooltipRect = new Rect(markerRect.x+25 , markerRect.y , 120 , markerRect.height);
								}
							}else{
								GUI.Box(markerRect , marker_normal , GUIStyle.none);
							}
						}
					}
				}
			}else if(markerTab == 1){
				int highlightedMarker = -1;
				int highlightComponent = 0;

				foreach(EmotionMarker marker in emotionData){
					Rect startMarkerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.startTime*(clip.length*pixelsPerSecond))-6 , previewBox.y-25 , 25 , 26);
					Rect endMarkerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.endTime*(clip.length*pixelsPerSecond))-20 , previewBox.y-25 , 25 , 26);
					Rect markerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.startTime*(clip.length*pixelsPerSecond)) , previewBox.y-25 , (marker.endTime-marker.startTime)*(clip.length*pixelsPerSecond) , 26);

					if(focusedWindow == this){
						if(mouseX > startMarkerRect.x && mouseX < startMarkerRect.x+startMarkerRect.width-7 && mouseY > startMarkerRect.y && mouseY < startMarkerRect.y+startMarkerRect.height-4 && currentMarker == -1){
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 0;
							EditorGUIUtility.AddCursorRect(new Rect(0,0,this.position.width,this.position.height) , MouseCursor.SlideArrow);
						}else if(mouseX > endMarkerRect.x+7 && mouseX < endMarkerRect.x+endMarkerRect.width && mouseY > endMarkerRect.y && mouseY < endMarkerRect.y+endMarkerRect.height-4 && currentMarker == -1){
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 2;
							EditorGUIUtility.AddCursorRect(new Rect(0,0,this.position.width,this.position.height) , MouseCursor.SlideArrow);	
						}else if(mouseX > markerRect.x && mouseX < markerRect.x+markerRect.width && mouseY > markerRect.y && mouseY < markerRect.y+markerRect.height-4 && currentMarker == -1){
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 1;
						}
					}

				}
				
				foreach(EmotionMarker marker in emotionData){
					Rect startMarkerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.startTime*(clip.length*pixelsPerSecond))-6 , previewBox.y-25 , 25 , 26);
					Rect endMarkerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.endTime*(clip.length*pixelsPerSecond))-20 , previewBox.y-25 , 25 , 26);
					Rect markerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.startTime*(clip.length*pixelsPerSecond)) , previewBox.y-25 , (marker.endTime-marker.startTime)*(clip.length*pixelsPerSecond) , 26);

					Color barColor = Color.gray;
					lastColor = Color.gray;
					nextColor = Color.gray;
					if(useColors){
						for(int em = 0; em < settings.emotions.Length ; em++){
							if(settings.emotions[em] == marker.emotion){
								barColor = settings.emotionColors[em];
							}

							if(emotionData.IndexOf(marker)-1 > -1){
								if(settings.emotions[em] == emotionData[emotionData.IndexOf(marker)-1].emotion && emotionData[emotionData.IndexOf(marker)-1].endTime == marker.startTime){
									lastColor = settings.emotionColors[em];
								}
							}
							if(emotionData.IndexOf(marker)+1 < emotionData.Count){
								if(settings.emotions[em] == emotionData[emotionData.IndexOf(marker)+1].emotion && emotionData[emotionData.IndexOf(marker)+1].startTime == marker.endTime){
									nextColor = settings.emotionColors[em];
								}
							}
						}

						if(lastColor == Color.gray){
							lastColor = Darken(barColor , 0.5f);
						}
						if(nextColor == Color.gray){
							nextColor = Darken(barColor , 0.5f);
						}
					}else{
						barColor = HexToColor(defaultColor);
						lastColor = Darken(HexToColor(defaultColor) , 0.75f);
						nextColor = Darken(HexToColor(defaultColor) , 0.75f);
					}

					if(dragging == false && highlightedMarker > -1){

						if(currentEvent.type == EventType.MouseDrag){
							dragging = true;
							currentMarker = highlightedMarker;
							currentComponent = highlightComponent;
							startOffset = emotionData[currentMarker].startTime - (currentEvent.mousePosition.x / this.position.width);
							endOffset = emotionData[currentMarker].endTime - (currentEvent.mousePosition.x / this.position.width);
							previousMarker = null;
							nextMarker = null;
						}else if(currentEvent.type == EventType.ContextClick){
							GenericMenu markerMenu = new GenericMenu();
							markerMenu.AddItem(new GUIContent("Delete") , false , DeleteEmotion , emotionData[highlightedMarker]);

							for(int a = 0 ; a < settings.emotions.Length ; a++){
								string emote = settings.emotions[a];
								markerMenu.AddItem(new GUIContent("Change/"+emote) , false , ChangeEmotionPicked , new List<object>{emotionData[highlightedMarker] , emote});
							}
							markerMenu.AddSeparator("Change/");
							markerMenu.AddItem(new GUIContent("Change/Add New Emotion") , false , ShowProjectSettings);
							//markerMenu.AddSeparator("");
							//markerMenu.AddItem(new GUIContent("Marker Settings") , false , EmotionMarkerSettings , emotionData[highlightedMarker]);
							markerMenu.ShowAsContext();
						}
					}
					if(dragging == true && currentMarker > -1){
						if(currentMarker-1 > -1){
							lowerMarkerLimit = emotionData[currentMarker-1].endTime;
							if(emotionData[currentMarker-1].endTime == emotionData[currentMarker].startTime)previousMarker = emotionData[currentMarker-1];
						}else{
							lowerMarkerLimit = 0;
						}
						if(currentMarker+1 < emotionData.Count){
							upperMarkerLimit = emotionData[currentMarker+1].startTime;
							if(emotionData[currentMarker+1].startTime == emotionData[currentMarker].endTime)nextMarker = emotionData[currentMarker+1];
						}else{
							upperMarkerLimit = 1;
						}
					}

					if(currentMarker == emotionData.IndexOf(marker)){
						if(currentComponent == 0){
							GUI.color = barColor;
							GUI.DrawTexture(markerRect , emotion_area);
							GUI.color = lastColor;
							GUI.DrawTexture(new Rect(markerRect.x , markerRect.y , marker.blendInTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_in);
							GUI.color = nextColor;
							GUI.DrawTexture(new Rect((markerRect.x+markerRect.width)-(marker.blendOutTime*(clip.length*pixelsPerSecond)) , markerRect.y , marker.blendOutTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_out);
							
							GUI.color = Color.white;
							GUI.DrawTexture(startMarkerRect , emotion_start_selected);
							if(!marker.blendToMarker)GUI.DrawTexture(endMarkerRect , emotion_end);
							GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+emotionData[currentMarker].startTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
						}else if(currentComponent == 1){
							GUI.color = new Color(0.4f,0.6f,1f);
							GUI.DrawTexture(markerRect , emotion_area);
							GUI.color = Color.white;
							GUI.DrawTexture(startMarkerRect , emotion_start_selected);
							GUI.DrawTexture(endMarkerRect , emotion_end_selected);
							GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+emotionData[currentMarker].startTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
							GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+emotionData[currentMarker].endTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
						}else if(currentComponent == 2){
							GUI.color = barColor;
							GUI.DrawTexture(markerRect , emotion_area);
							GUI.color = lastColor;
							GUI.DrawTexture(new Rect(markerRect.x , markerRect.y , marker.blendInTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_in);
							GUI.color = nextColor;
							GUI.DrawTexture(new Rect((markerRect.x+markerRect.width)-(marker.blendOutTime*(clip.length*pixelsPerSecond)) , markerRect.y , marker.blendOutTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_out);
							
							GUI.color = Color.white;
							if(!marker.blendFromMarker)GUI.DrawTexture(startMarkerRect , emotion_start);
							GUI.DrawTexture(endMarkerRect , emotion_end_selected);
							GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+emotionData[currentMarker].endTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
						}
					}else{
						if(highlightedMarker == emotionData.IndexOf(marker)){
							if(highlightComponent == 0){
								GUI.color = barColor;
								GUI.DrawTexture(markerRect , emotion_area);
								GUI.color = lastColor;
								GUI.DrawTexture(new Rect(markerRect.x , markerRect.y , marker.blendInTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_in);
								GUI.color = nextColor;
								GUI.DrawTexture(new Rect((markerRect.x+markerRect.width)-(marker.blendOutTime*(clip.length*pixelsPerSecond)) , markerRect.y , marker.blendOutTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_out);

								if(currentEvent.modifiers == EventModifiers.Control && !marker.blendFromMarker){
									GUI.color = Color.yellow;
								}else{
									GUI.color = Color.white;
								}

								GUI.DrawTexture(startMarkerRect , emotion_start_hover);
								GUI.color = Color.white;
								if(!marker.blendToMarker)GUI.DrawTexture(endMarkerRect , emotion_end);

								GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+marker.startTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
							}else if(highlightComponent == 1){
								GUI.DrawTexture(markerRect , emotion_area);
								GUI.DrawTexture(startMarkerRect , emotion_start_hover);
								GUI.DrawTexture(endMarkerRect , emotion_end_hover);
								GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+marker.startTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
								GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+marker.endTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
							}else if(highlightComponent == 2){
								GUI.color = barColor;
								GUI.DrawTexture(markerRect , emotion_area);
								GUI.color = lastColor;
								GUI.DrawTexture(new Rect(markerRect.x , markerRect.y , marker.blendInTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_in);
								GUI.color = nextColor;
								GUI.DrawTexture(new Rect((markerRect.x+markerRect.width)-(marker.blendOutTime*(clip.length*pixelsPerSecond)) , markerRect.y , marker.blendOutTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_out);
								
								GUI.color = Color.white;
								if(!marker.blendFromMarker)GUI.DrawTexture(startMarkerRect , emotion_start);
								if(currentEvent.modifiers == EventModifiers.Control && !marker.blendToMarker){
									GUI.color = Color.yellow;
								}
								GUI.DrawTexture(endMarkerRect , emotion_end_hover);
								GUI.color = Color.white;
								GUI.DrawTexture(new Rect((-(viewportStart*pixelsPerSecond))+previewBox.x+marker.endTime*(clip.length*pixelsPerSecond) , previewBox.y , 1 , previewBox.height) , marker_line);
							}
						}else{
							GUI.color = barColor;
							GUI.DrawTexture(markerRect , emotion_area);

							GUI.color = lastColor;
							GUI.DrawTexture(new Rect(markerRect.x , markerRect.y , marker.blendInTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_in);
							GUI.color = nextColor;
							GUI.DrawTexture(new Rect((markerRect.x+markerRect.width)-(marker.blendOutTime*(clip.length*pixelsPerSecond)) , markerRect.y , marker.blendOutTime*(clip.length*pixelsPerSecond) , markerRect.height) , emotion_blend_out);

							GUI.color = Color.white;
							if(!marker.blendFromMarker)GUI.DrawTexture(startMarkerRect , emotion_start);
							if(!marker.blendToMarker)GUI.DrawTexture(endMarkerRect , emotion_end);
						}
					}

					float lum = (0.299f*barColor.r + 0.587f*barColor.g + 0.114f*barColor.b);
					if(lum > 0.5f || highlightedMarker == emotionData.IndexOf(marker) && highlightComponent == 1){
						GUI.contentColor = Color.black;
						GUI.Box(new Rect(markerRect.x , markerRect.y+2 , markerRect.width , markerRect.height-9) , marker.emotion , centeredStyle);
						GUI.contentColor = Color.white;
					}else{
						GUI.Box(new Rect(markerRect.x , markerRect.y+2 , markerRect.width , markerRect.height-9) , marker.emotion , centeredStyle);
					}
				}
			}else if(markerTab == 2) {
				//Gesture Markers
				int highlightedMarker = -1;

				foreach(GestureMarker marker in gestureData){
					Rect markerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.time*(clip.length*pixelsPerSecond))-12.5f , previewBox.y-25 , 25 , 26);
					if(mouseX > markerRect.x+5 && mouseX < markerRect.x+markerRect.width-5 && mouseY > markerRect.y && mouseY < markerRect.y+markerRect.height-4 && currentMarker == -1){
						highlightedMarker = gestureData.IndexOf(marker);
					}
				}

				foreach(GestureMarker marker in gestureData){
					Rect markerRect = new Rect((-(viewportStart*pixelsPerSecond))+(marker.time*(clip.length*pixelsPerSecond))-12.5f , previewBox.y-25 , 25 , 26);
					if(dragging == false && highlightedMarker > -1 && focusedWindow == this){
						GestureMarker cm = gestureData[highlightedMarker];

						if(currentEvent.type == EventType.MouseDrag){
							currentMarker = gestureData.IndexOf(cm);
							dragging = true;
							break;
						}else if(currentEvent.type == EventType.ContextClick){
							GenericMenu markerMenu = new GenericMenu();
							markerMenu.AddItem(new GUIContent("Delete") , false , DeleteGesture , cm);

							for(int a = 0 ; a < settings.gestures.Count ; a++){
								string gesture = settings.gestures[a];
								markerMenu.AddItem(new GUIContent("Change/"+gesture) , false , ChangeGesturePicked , new List<object>{highlightedMarker , gesture});
							}
							markerMenu.AddSeparator("Change/");
							markerMenu.AddItem(new GUIContent("Change/Add New Gesture") , false , ShowProjectSettings);
							//markerMenu.AddSeparator("");
							//markerMenu.AddItem(new GUIContent("Marker Settings") , false , PhonemeMarkerSettings , marker);
							markerMenu.ShowAsContext();
						}
					}

					if(currentMarker == gestureData.IndexOf(marker)){
						GUI.Box(markerRect , gesture_selected , GUIStyle.none);
						tip = marker.gesture;
						GUI.DrawTexture(new Rect(markerRect.x+12 , previewBox.y , 1 , previewBox.height) , marker_line);
						tooltipRect = new Rect(markerRect.x+25 , markerRect.y , 120 , markerRect.height);
					}else{
						if(highlightedMarker == gestureData.IndexOf(marker)){
							GUI.Box(markerRect , gesture_hover , GUIStyle.none);
							GUI.DrawTexture(new Rect(markerRect.x+12 , previewBox.y , 1 , previewBox.height) , marker_line);
							tip = marker.gesture;
							if(markerRect.x+145 > this.position.width){
								tooltipRect = new Rect(markerRect.x-125 , markerRect.y , 120 , markerRect.height);
							}else{
								tooltipRect = new Rect(markerRect.x+25 , markerRect.y , 120 , markerRect.height);
							}
						}else{
							GUI.Box(markerRect , gesture_normal , GUIStyle.none);
						}
					}
				}
			}

			if(tip != "")GUI.Box(tooltipRect , tip , (GUIStyle)"flow node 0");

			if(markerTab == 0){
				if(dragging == true && currentEvent.type == EventType.MouseDrag && currentMarker > -1){
					phonemeData[currentMarker].time = Mathf.Clamp01((currentEvent.mousePosition.x/(clip.length*pixelsPerSecond))+(viewportStart/clip.length));
					changed = true;
					previewOutOfDate = true;
				}
			}else if(markerTab == 1){
				if(currentMarker > -1){
					if(currentComponent == 0 || currentComponent == 2)
					EditorGUIUtility.AddCursorRect(new Rect(0,0,this.position.width,this.position.height) , MouseCursor.SlideArrow);
				}

				if(dragging == true && currentEvent.type == EventType.MouseDrag && currentMarker > -1){
					if(currentComponent == 0){
						float tempChange = (currentEvent.mousePosition.x/(clip.length*pixelsPerSecond))+(viewportStart/clip.length);

						if(currentEvent.modifiers == EventModifiers.Control && emotionData[currentMarker].blendFromMarker == false){
							emotionData[currentMarker].blendInTime = Mathf.Clamp(tempChange-emotionData[currentMarker].startTime , 0 , emotionData[currentMarker].endTime-emotionData[currentMarker].startTime);
							emotionData[currentMarker].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime , 0 , (emotionData[currentMarker].endTime-emotionData[currentMarker].startTime)-emotionData[currentMarker].blendInTime);
						}else{
							if(tempChange > emotionData[currentMarker].endTime-0.008f){
							}else if(tempChange < lowerMarkerLimit+snappingDistance){
								emotionData[currentMarker].startTime = lowerMarkerLimit;
								if(previousMarker != null){
									previousMarker.blendOutTime = Mathf.Clamp(lowerMarkerLimit-tempChange , 0 , previousMarker.endTime-previousMarker.startTime);
									previousMarker.blendInTime = Mathf.Clamp(previousMarker.blendInTime , 0 , (previousMarker.endTime-previousMarker.startTime)-previousMarker.blendOutTime);
									emotionData[currentMarker].blendInTime = 0;
									emotionData[currentMarker].blendFromMarker = true;
									previousMarker.blendToMarker = true;
								}
							}else{
								emotionData[currentMarker].startTime = tempChange;
								if(previousMarker != null){
									emotionData[currentMarker].blendFromMarker = false;
									previousMarker.blendToMarker = false;
								}
							}
							emotionData[currentMarker].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime , 0 , (emotionData[currentMarker].endTime-emotionData[currentMarker].startTime)-emotionData[currentMarker].blendInTime);
							emotionData[currentMarker].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime , 0 , (emotionData[currentMarker].endTime-emotionData[currentMarker].startTime)-emotionData[currentMarker].blendOutTime);
						}
						changed = true;
						previewOutOfDate = true;
					}else if(currentComponent == 1){
						float oldStart = emotionData[currentMarker].startTime;
						float oldEnd = emotionData[currentMarker].endTime;

						emotionData[currentMarker].startTime = (currentEvent.mousePosition.x/this.position.width)+startOffset;
						emotionData[currentMarker].endTime = (currentEvent.mousePosition.x/this.position.width)+endOffset;
						changed = true;
						previewOutOfDate = true;

						if(emotionData[currentMarker].startTime < lowerMarkerLimit+snappingDistance){
							emotionData[currentMarker].startTime = lowerMarkerLimit;
							emotionData[currentMarker].endTime = lowerMarkerLimit + (oldEnd-oldStart);

							if(previousMarker != null){
								emotionData[currentMarker].blendFromMarker = true;
								previousMarker.blendToMarker = true;

								if(previousMarker.blendOutTime > 0){
									emotionData[currentMarker].blendInTime = 0;
								}
							}
						}else if(emotionData[currentMarker].endTime > upperMarkerLimit-snappingDistance){
							emotionData[currentMarker].startTime = upperMarkerLimit - (oldEnd-oldStart);
							emotionData[currentMarker].endTime = upperMarkerLimit;

							if(nextMarker != null){
								emotionData[currentMarker].blendToMarker = true;
								nextMarker.blendFromMarker = true;

								if(nextMarker.blendInTime > 0){
									emotionData[currentMarker].blendOutTime = 0;
								}
							}
						}else{
							if(previousMarker != null){
								emotionData[currentMarker].blendFromMarker = false;
								previousMarker.blendToMarker = false;
							}
							if(nextMarker != null){
								emotionData[currentMarker].blendToMarker = false;
								nextMarker.blendFromMarker = false;
							}
						}
					}else if(currentComponent == 2){
						float tempChange = currentEvent.mousePosition.x/this.position.width;

						if(currentEvent.modifiers == EventModifiers.Control && emotionData[currentMarker].blendToMarker == false){
							emotionData[currentMarker].blendOutTime = Mathf.Clamp(emotionData[currentMarker].endTime-tempChange , 0 , emotionData[currentMarker].endTime-emotionData[currentMarker].startTime);
							emotionData[currentMarker].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime , 0 , (emotionData[currentMarker].endTime-emotionData[currentMarker].startTime)-emotionData[currentMarker].blendOutTime);
						}else{
							if(tempChange < emotionData[currentMarker].startTime+0.008f){
							}else if(tempChange > upperMarkerLimit-snappingDistance){
								emotionData[currentMarker].endTime = upperMarkerLimit;
								if(nextMarker != null){
									nextMarker.blendInTime = Mathf.Clamp(tempChange-upperMarkerLimit , 0 , nextMarker.endTime-nextMarker.startTime);
									nextMarker.blendOutTime = Mathf.Clamp(nextMarker.blendOutTime , 0 , (nextMarker.endTime-nextMarker.startTime)-nextMarker.blendInTime);
									emotionData[currentMarker].blendOutTime = 0;
									emotionData[currentMarker].blendToMarker = true;
									nextMarker.blendFromMarker = true;
								}
							}else{
								emotionData[currentMarker].endTime = tempChange;
								if(nextMarker != null){
									emotionData[currentMarker].blendToMarker = false;
									nextMarker.blendFromMarker = false;
								}
							}
							emotionData[currentMarker].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime , 0 , (emotionData[currentMarker].endTime-emotionData[currentMarker].startTime)-emotionData[currentMarker].blendOutTime);
							emotionData[currentMarker].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime , 0 , (emotionData[currentMarker].endTime-emotionData[currentMarker].startTime)-emotionData[currentMarker].blendInTime);
						}
						changed = true;
						previewOutOfDate = true;
					}
				}
			}else if(markerTab == 2) {
				if(dragging == true && currentEvent.type == EventType.MouseDrag && currentMarker > -1){
					gestureData[currentMarker].time = Mathf.Clamp01((currentEvent.mousePosition.x/(clip.length*pixelsPerSecond))+(viewportStart/clip.length));
					changed = true;
					previewOutOfDate = true;
				}
			}

			if(currentEvent.type == EventType.MouseUp){
				currentMarker = -1;
				dragging = false;
				emotionData.Sort(EmotionSort);
				if(markerTab == 1){
					markerPlaceable = true;
					foreach(EmotionMarker tMarker in emotionData){
						if(seekPosition >= tMarker.startTime && seekPosition <= tMarker.endTime)markerPlaceable = false;
					}
				}
			}
			
			//Controls
			previewBox = new Rect(0,position.height-18 , position.width , 18);
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUI.Box (previewBox , "" , EditorStyles.toolbar);
			GUILayout.FlexibleSpace();

			float oldViewportStart = viewportStart;
			float oldViewportEnd = viewportEnd;
			GUILayout.Space(10);
			EditorGUILayout.MinMaxSlider(ref viewportStart , ref viewportEnd , 0 , clip.length , GUILayout.MaxWidth(180));
			GUILayout.Space(10);
			if(viewportEnd-viewportStart != oldViewportEnd-oldViewportStart && clip.length*pixelsPerSecond <= maxWaveformWidth){
				DestroyImmediate(waveform);
			}
			if(viewportStart != oldViewportStart){
				viewportStart = Mathf.Clamp(viewportStart , 0 , viewportEnd-0.5f);
			}
			if(viewportEnd != oldViewportEnd){
				viewportEnd = Mathf.Clamp(viewportEnd , viewportStart+0.5f , clip.length);
			}

			GUILayout.FlexibleSpace();
			timeSpan = TimeSpan.FromSeconds(seekPosition*clip.length);
			Char pad = '0';
			string minutes = timeSpan.Minutes.ToString().PadLeft(2 , pad);
			string seconds = timeSpan.Seconds.ToString().PadLeft(2 , pad);
			string milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3 , pad);
			
			string currentTime = minutes + ":" + seconds + ":" + milliseconds;
			
			timeSpan = TimeSpan.FromSeconds(clip.length);
			minutes = timeSpan.Minutes.ToString().PadLeft(2 , pad);
			seconds = timeSpan.Seconds.ToString().PadLeft(2 , pad);
			milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3 , pad);
			
			string totalTime = minutes + ":" + seconds + ":" + milliseconds;
			
			GUILayout.Box(currentTime+" / "+totalTime , EditorStyles.toolbarTextField);
			GUILayout.FlexibleSpace();
			if(isPlaying){
				if(isPaused){
					if(GUILayout.Button(playIcon , EditorStyles.toolbarButton , GUILayout.Width(50))){
						AudioUtility.ResumeClip(clip);
						isPaused = false;
						markerPlaceable = false;
					}
				}else{
					if(GUILayout.Button(pauseIcon , EditorStyles.toolbarButton , GUILayout.Width(50))){
						AudioUtility.PauseClip(clip);
						isPaused = true;
						markerPlaceable = true;
						foreach(EmotionMarker tMarker in emotionData){
							if(seekPosition >= tMarker.startTime && seekPosition <= tMarker.endTime && markerTab==1)markerPlaceable = false;
						}
					}
				}
			}else{
				if(GUILayout.Button(playIcon , EditorStyles.toolbarButton , GUILayout.Width(50))){
					AudioUtility.PlayClip(clip);
					isPaused = false;
					markerPlaceable = false;
				}
			}
			if(GUILayout.Button(stopIcon , EditorStyles.toolbarButton , GUILayout.Width(50))){
				isPaused = true;
				AudioUtility.StopClip(clip);
				seekPosition = 0;
				oldSeekPosition = seekPosition;
				markerPlaceable = true;
				foreach(EmotionMarker tMarker in emotionData){
					if(seekPosition >= tMarker.startTime && seekPosition <= tMarker.endTime && markerTab==1)markerPlaceable = false;
				}
			}

			GUILayout.FlexibleSpace();

			switch(markerTab){
			case 0:
				if(markerPlaceable){
					if(GUILayout.Button("Add Phoneme" , EditorStyles.toolbarButton , GUILayout.Width(160))){
						GenericMenu phonemeMenu = new GenericMenu();
						
						for(int a = 0 ; a < 9 ; a++){
							Phoneme phon = (Phoneme)a;
							phonemeMenu.AddItem(new GUIContent(phon.ToString()) , false , PhonemePicked , (Phoneme)a);
						}
						phonemeMenu.ShowAsContext();
					}
				}else{
					GUI.color = Color.gray;
					GUILayout.Box("Add Phoneme" , EditorStyles.toolbarButton , GUILayout.Width(160));
					GUI.color = Color.white;
				}
				GUILayout.Space(40);
				GUILayout.Box("Filters:" , EditorStyles.label);
				filterMask = EditorGUILayout.MaskField(filterMask , new String[]{"AI" , "E" , "U" , "O" , "CDGKNRSThYZ" , "FV" , "L" , "MBP" , "WQ"} , EditorStyles.toolbarPopup , GUILayout.MaxWidth(100));
				break;
			case 1:
				if(markerPlaceable){
					if(GUILayout.Button("Add Emotion" , EditorStyles.toolbarButton , GUILayout.Width(160))){
						GenericMenu emotionMenu = new GenericMenu();
						
						for(int a = 0 ; a < settings.emotions.Length ; a++){
							string emote = settings.emotions[a];
							emotionMenu.AddItem(new GUIContent(emote) , false , EmotionPicked , emote);
						}
						emotionMenu.AddSeparator("");
						emotionMenu.AddItem(new GUIContent("Add New Emotion") , false , ShowProjectSettings);

						emotionMenu.ShowAsContext();
					}
				}else{
					GUI.color = Color.gray;
					GUILayout.Box("Add Emotion" , EditorStyles.toolbarButton , GUILayout.Width(160));
					GUI.color = Color.white;
				}
				break;
			case 2:
				if(markerPlaceable){
					if(GUILayout.Button("Add Gesture" , EditorStyles.toolbarButton , GUILayout.Width(160))){
						GenericMenu gestureMenu = new GenericMenu();

						for(int a = 0 ; a < settings.gestures.Count ; a++){
							string gesture = settings.gestures[a];
							gestureMenu.AddItem(new GUIContent(gesture) , false , GesturePicked , gesture);
						}
						gestureMenu.AddSeparator("");
						gestureMenu.AddItem(new GUIContent("Add New Gesture") , false , ShowProjectSettings);

						gestureMenu.ShowAsContext();
					}
				}else{
					GUI.color = Color.gray;
					GUILayout.Box("Add Gesture" , EditorStyles.toolbarButton , GUILayout.Width(160));
					GUI.color = Color.white;
				}
				break;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(1);
		}

		if(!clip){
			EditorGUILayout.HelpBox("Please Select an AudioClip to continue." , MessageType.Warning);
		}
	}

	//Context Menu Callbacks
	void PhonemePicked (object picked) {
		Undo.RecordObject(this , "Add Phoneme Marker");
		phonemeData.Add(new PhonemeMarker((Phoneme)picked , seekPosition));
		changed = true;
		previewOutOfDate = true;
	}

	void EmotionPicked (object picked) {
		Undo.RecordObject(this , "Add Emotion Marker");
		EmotionMarker newMarker = new EmotionMarker((string)picked , seekPosition , Mathf.Clamp(seekPosition+0.1f , 0 , 1) , 0 , 0 , false , false);
		emotionData.Add(newMarker);
		emotionData.Sort(EmotionSort);
		int newMarkerIndex = emotionData.IndexOf(newMarker);
		if(newMarkerIndex+1 < emotionData.Count){
			upperMarkerLimit = emotionData[newMarkerIndex+1].startTime;
		}else{
			upperMarkerLimit = 1;
		}
		emotionData[newMarkerIndex].endTime = Mathf.Clamp(emotionData[newMarkerIndex].endTime , emotionData[newMarkerIndex].startTime+0.003f , upperMarkerLimit);
		float duration = emotionData[newMarkerIndex].endTime-emotionData[newMarkerIndex].startTime;
		emotionData[newMarkerIndex].blendInTime = duration/2.5f;
		emotionData[newMarkerIndex].blendOutTime = duration/2.5f;
		changed = true;
		previewOutOfDate = true;
	}

	void GesturePicked (object picked) {
		Undo.RecordObject(this , "Add Gesture Marker");
		GestureMarker newMarker = new GestureMarker((string)picked , seekPosition);
		gestureData.Add(newMarker);
		changed = true;
	}

	void OnNewClick() {
		if(changed){
			if(EditorUtility.DisplayDialog("Unsaved Data" , "You have made changes to the current file, are you sure you want to clear it?" , "Yes" , "No")){
				lastLoad = "";
				fileName = "Untitled";
				AudioUtility.StopAllClips();
				seekPosition = 0;
				oldSeekPosition = 0;
				clip = null;
				phonemeData = new List<PhonemeMarker>();
				emotionData = new List<EmotionMarker>();
				oldClip = null;
				changed = false;
				previewOutOfDate = true;
			}
		}else{
			lastLoad = "";
			fileName = "Untitled";
			AudioUtility.StopAllClips();
			seekPosition = 0;
			oldSeekPosition = 0;
			clip = null;
			phonemeData = new List<PhonemeMarker>();
			emotionData = new List<EmotionMarker>();
			oldClip = null;
			changed = false;
			previewOutOfDate = true;
		}
	}

	void OnLoadClick() {
		string loadPath = EditorUtility.OpenFilePanel("Load LipSync Data File" , "Assets" , "asset");
		
		if(loadPath != ""){
			loadPath = "Assets"+loadPath.Substring(Application.dataPath.Length);
			LoadFile(loadPath);
		}
	}

	void OnSaveClick() {
		if(lastLoad != ""){
			string savePath = "Assets"+lastLoad + clip.name + ".asset";
			SaveFile(savePath , false);
			changed = false;
		}else{
			OnSaveAsClick();
		}
	}

	void OnSaveAsClick() {
		string savePath = EditorUtility.SaveFilePanel("Save LipSync Data File" , "Assets"+lastLoad , clip.name + ".asset" , "asset");
		if(savePath != ""){
			savePath = "Assets"+savePath.Substring(Application.dataPath.Length);
			SaveFile(savePath , false);
			changed = false;
			lastLoad = savePath;
		}
	}

	void OnUnityExport() {
		string savePath = EditorUtility.SaveFilePanel("Export LipSync Data and audio" , "Assets"+lastLoad , clip.name + ".unitypackage" , "unitypackage");
		if(savePath != ""){
			savePath = "Assets"+savePath.Substring(Application.dataPath.Length);
			string folderPath = savePath.Remove(savePath.Length-Path.GetFileName(savePath).Length)+Path.GetFileNameWithoutExtension(savePath);
			AssetDatabase.CreateFolder(savePath.Remove(savePath.Length-(Path.GetFileName(savePath).Length+1)) , Path.GetFileNameWithoutExtension(savePath));
	
			string originalName = fileName;

			if(clip != null){
				AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(clip) , folderPath+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
				AssetDatabase.ImportAsset(folderPath+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
			}

			AudioClip newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(folderPath+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
			AudioClip originalClip = clip;
			if(newClip != null){
				clip = newClip;
			}else{
				Debug.Log("LipSync: AudioClip copy at "+folderPath+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(clip))+" could not be reloaded for compression. Proceding without AudioClip.");
			}

			SaveFile(folderPath+"/"+Path.ChangeExtension(Path.GetFileName(savePath) , ".asset") , false);

			LipSyncData file = AssetDatabase.LoadAssetAtPath<LipSyncData>(folderPath+"/"+Path.ChangeExtension(Path.GetFileName(savePath) , ".asset"));
			if(file != null){
				AssetDatabase.ExportPackage(folderPath , savePath , ExportPackageOptions.Recurse);
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
				AssetDatabase.DeleteAsset(folderPath);

				fileName = originalName;
				lastLoad = "";
			}else{
				Debug.LogError("LipSync: File could not be reloaded for compression. Aborting Export.");
			}

			clip = originalClip;
		}
	}

	void OnXMLExport() {
		string savePath = EditorUtility.SaveFilePanel("Export LipSync Data to XML" , "Assets"+lastLoad , clip.name + ".xml" , "xml");
		if(savePath != ""){
			savePath = "Assets"+savePath.Substring(Application.dataPath.Length);
			SaveFile(savePath , true);
		}
	}

	void OnXMLImport() {
		string xmlPath = EditorUtility.OpenFilePanel("Import LipSync Data from XML" , "Assets"+lastLoad , "xml");
		string audioPath = EditorUtility.OpenFilePanel("Load AudioClip" , "Assets"+lastLoad , "wav;*.mp3;*.ogg");
		
		if(xmlPath != ""){
			xmlPath = "Assets"+xmlPath.Substring(Application.dataPath.Length);
			audioPath = "Assets"+audioPath.Substring(Application.dataPath.Length);
			AudioClip linkedClip = (AudioClip)AssetDatabase.LoadAssetAtPath(audioPath , typeof(AudioClip));
			
			LoadXML(xmlPath , linkedClip);
		}
	}

	void OpenURL (object url) {
		Application.OpenURL((string)url);
	}

	void ShowProjectSettings () {
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

	void ChangeMarkerPicked (object info) {
		Undo.RecordObject(this , "Change Phoneme Marker");
		List<int> finalInfo = (List<int>)info;

		PhonemeMarker marker = phonemeData[finalInfo[0]];
		marker.phoneme = (Phoneme)finalInfo[1];
		changed = true;
		previewOutOfDate = true;
	}

	void ChangeGesturePicked (object info) {
		Undo.RecordObject(this , "Change Gesture Marker");
		List<object> finalInfo = (List<object>)info;

		GestureMarker marker = gestureData[(int)finalInfo[0]];
		marker.gesture = (string)finalInfo[1];
		changed = true;
	}

//	void PhonemeMarkerSettings (object info) {
//		PhonemeMarker marker = (PhonemeMarker)info;
//
//	}
//
//	void EmotionMarkerSettings (object info) {
//		EmotionMarker marker = (EmotionMarker)info;
//
//	}

	void ChangeEmotionPicked (object info) {
		Undo.RecordObject(this , "Change Emotion Marker");
		List<object> finalInfo = (List<object>)info;
		
		EmotionMarker marker = (EmotionMarker)finalInfo[0];
		marker.emotion = (string)finalInfo[1];
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteMarker (object marker) {
		Undo.RecordObject(this , "Delete Phoneme Marker");
		phonemeData.Remove((PhonemeMarker)marker);
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteGesture (object marker) {
		Undo.RecordObject(this , "Delete Gesture Marker");
		gestureData.Remove((GestureMarker)marker);
		changed = true;
	}

	void DeleteEmotion (object marker) {
		Undo.RecordObject(this , "Delete Emotion Marker");
		currentMarker = emotionData.IndexOf((EmotionMarker)marker);
		changed = true;
		previewOutOfDate = true;

		if(currentMarker-1 > -1){
			if(emotionData[currentMarker-1].endTime == emotionData[currentMarker].startTime){
				previousMarker = emotionData[currentMarker-1];
				previousMarker.blendOutTime = 0;
				previousMarker.blendToMarker = false;
			}
		}
		if(currentMarker+1 < emotionData.Count){
			if(emotionData[currentMarker+1].startTime == emotionData[currentMarker].endTime){
				nextMarker = emotionData[currentMarker+1];
				nextMarker.blendInTime = 0;
				nextMarker.blendFromMarker = false;
			}
		}
		currentMarker = -1;
		emotionData.Remove((EmotionMarker)marker);

		markerPlaceable = true;
		foreach(EmotionMarker tMarker in emotionData){
			if(seekPosition >= tMarker.startTime && seekPosition <= tMarker.endTime)markerPlaceable = false;
		}
	}
	
	void Update () {
		if(!clip) return;
		isPlaying = AudioUtility.IsClipPlaying(clip);
		if(isPlaying && !isPaused) markerPlaceable = false;

		//Check for clip change
		if(oldClip != clip){
			Undo.RecordObject(this , "Change AudioClip");
			DestroyImmediate(waveform);
			oldClip = clip;
			if(setViewportOnLoad){
				viewportEnd = clip.length;
				viewportStart = 0;
			}
		}
		
		//Check for resize;
		if(oldPos.width != this.position.width || oldPos.height != this.position.height){
			oldPos = this.position;
			if(clip)DestroyImmediate(waveform);
		}

		//Check for Seek Position change
		if(oldSeekPosition != seekPosition){
			oldSeekPosition = seekPosition;
			if(!isPlaying || isPaused){
				if(!previewing){
					AudioUtility.PlayClip(clip);
				}
				previewing = true;
				stopTimer = scrubLength;
				prevTime = Time.realtimeSinceStartup;
				resetTime = seekPosition;
			}
			markerPlaceable = true;
			if(markerTab ==1){
				foreach(EmotionMarker tMarker in emotionData){
					if(seekPosition >= tMarker.startTime && seekPosition <= tMarker.endTime)markerPlaceable = false;
				}
			}
			AudioUtility.SetClipSamplePosition(clip , (int)(seekPosition*AudioUtility.GetSampleCount(clip)));

		}

		if(isPlaying && !isPaused && clip && focusedWindow == this || continuousUpdate && focusedWindow == this){
			this.Repaint();
		}
			
		seekPosition = AudioUtility.GetClipPosition(clip)/clip.length;
		oldSeekPosition = seekPosition;
		
		if(previewing){

			stopTimer -= (Time.realtimeSinceStartup-prevTime);
			prevTime = Time.realtimeSinceStartup;

			if(stopTimer <= 0){
				previewing = false;
				AudioUtility.PauseClip(clip);
				isPaused = true;
				seekPosition = resetTime;
				oldSeekPosition = seekPosition;
				AudioUtility.SetClipSamplePosition(clip , (int)(seekPosition*AudioUtility.GetSampleCount(clip)));
			}
		}
			
		if(waveform == null && waveformHeight > 0){
			waveform = AudioUtility.GetWaveForm(clip , 0 , (int)(clip.length*(position.width/(viewportEnd-viewportStart))) , (position.height-waveformHeight)-18);
			Repaint();
		}

		if(isPlaying && !isPaused && visualPreview || previewing && visualPreview) {
			UpdatePreview(seekPosition);
		}
	}
	
	[MenuItem("Window/Rogo Digital/Open Clip Editor %&a" , false , 11)]
	public static LipSyncClipSetup ShowWindow (){
		return ShowWindow("" , false , "" , "" , 0 , 0);
	}

	public static LipSyncClipSetup ShowWindow (string loadPath , bool newWindow){
		return ShowWindow(loadPath , newWindow , "" , "" , 0 , 0);
	}
	
	public static LipSyncClipSetup ShowWindow (string loadPath , bool newWindow , string oldFileName , string oldLastLoad , int oldMarkerTab , float oldSeekPosition){
		UnityEngine.Object[] current = Selection.GetFiltered(typeof(AudioClip) , SelectionMode.Assets);
		LipSyncClipSetup window;


		if(newWindow){
			window = ScriptableObject.CreateInstance <LipSyncClipSetup> ();
			window.Show();
		}else{
			window = EditorWindow.GetWindow <LipSyncClipSetup> ();
		}

		if(current.Length > 0){
			window.clip = (AudioClip)current[0];
			window.waveform = null;
		}else if(loadPath == ""){
			current = Selection.GetFiltered(typeof(LipSyncData) , SelectionMode.Assets);
			if(current.Length > 0){
				loadPath = AssetDatabase.GetAssetPath(current[0]);
			}
		}

		if(EditorPrefs.GetBool("LipSync_ShowExtensionsOnLoad" , true)){
			EditorWindow.GetWindow<RDExtensionWindow>("Extensions" , false , typeof(LipSyncClipSetup));
			RDExtensionWindow.ShowWindow("LipSync");
		}

		window.Focus();

		if(window.changed){
			int choice = EditorUtility.DisplayDialogComplex("Save Changes" , "You have made changes to the current file, do you want to save them before closing?" , "Yes" ,"No" , "Cancel");
			if(choice != 2){
				if(choice == 0) {
					window.OnSaveClick();
				}

				window.changed = false;
				window.fileName = "Untitled";
				window.oldClip = window.clip;
				window.phonemeData = new List<PhonemeMarker>();
				window.emotionData = new List<EmotionMarker>();
				window.gestureData = new List<GestureMarker>();

				window.seekPosition = 0;
				AudioUtility.StopAllClips();
				window.currentMarker = -1;

				if(loadPath != ""){
					window.LoadFile(loadPath);
					window.previewOutOfDate = true;
				}
			}else{
				window.clip = window.oldClip;
			}
		}else{
			window.oldClip = window.clip;
			window.fileName = "Untitled";
			window.phonemeData = new List<PhonemeMarker>();
			window.emotionData = new List<EmotionMarker>();
			window.gestureData = new List<GestureMarker>();

			window.seekPosition = 0;
			AudioUtility.StopAllClips();
			window.currentMarker = -1;

			if(loadPath != ""){
				window.LoadFile(loadPath);
				window.previewOutOfDate = true;
			}
		}


		if(EditorGUIUtility.isProSkin){
			window.windowIcon = Resources.Load<Texture2D>("Lipsync/Dark/icon");
		}else{
			window.windowIcon = Resources.Load<Texture2D>("Lipsync/Light/icon");
		}

		window.titleContent = new GUIContent("LipSync" , window.windowIcon);
		window.minSize = new Vector2(700, 200);
		if(newWindow){
			window.changed = true;
			window.lastLoad = oldLastLoad;
			window.fileName = oldFileName;
			window.markerTab = oldMarkerTab;
			window.seekPosition = oldSeekPosition;
			window.oldSeekPosition = oldSeekPosition;
			AudioUtility.SetClipSamplePosition(window.clip , (int)(window.seekPosition*AudioUtility.GetSampleCount(window.clip)));
			AssetDatabase.DeleteAsset("Assets/Rogo Digital/LipSync/AUTOSAVE.asset");
		}

		if(EditorPrefs.GetBool("LipSync_SetViewportOnLoad" , true)){
			if(window.clip != null){
				window.viewportEnd = window.clip.length;
			}
			window.viewportStart = 0;
		}

		return window;
	}

	void LoadFile (string path) {
		if(changed){
			int choice = EditorUtility.DisplayDialogComplex("Save Changes" , "You have made changes to the current file, do you want to save them before closing?" , "Yes" ,"No" , "Cancel");

			if(choice == 1){
				OnSaveClick();
			}else if(choice == 2){
				return;
			}
		}
		LipSyncData file = (LipSyncData)AssetDatabase.LoadAssetAtPath(path , typeof(LipSyncData));
		
		clip = file.clip;
		oldClip = clip;
		waveform = AudioUtility.GetWaveForm(clip , 0 , (int)(clip.length*(position.width/(viewportEnd-viewportStart))) , (position.height-waveformHeight)-18);
		fileName = file.name+".Asset";

		if(setViewportOnLoad){
			viewportEnd = clip.length;
			viewportStart = 0;
		}

		phonemeData = new List<PhonemeMarker>();
		foreach(PhonemeMarker marker in file.phonemeData){
			phonemeData.Add(new PhonemeMarker(marker.phoneme , marker.time));
		}

		emotionData = new List<EmotionMarker>();
		foreach(EmotionMarker marker in file.emotionData){
			emotionData.Add(new EmotionMarker(marker.emotion , marker.startTime , marker.endTime , marker.blendInTime , marker.blendOutTime , marker.blendToMarker , marker.blendFromMarker));
		}

		gestureData = new List<GestureMarker>();
		if(file.gestureData != null){
			foreach(GestureMarker marker in file.gestureData){
				gestureData.Add(new GestureMarker(marker.gesture , marker.time));
			}
		}

		currentMarker = -1;
		previewOutOfDate = true;
		changed = false;

		string[] pathParts = path.Split(new string[]{"/"} , StringSplitOptions.None);
		lastLoad = path.Remove(path.Length-pathParts[pathParts.Length-1].Length).Substring(6);
	}

	private void LoadXML (string path , AudioClip linkedClip) {
		TextAsset xmlFile = (TextAsset)AssetDatabase.LoadAssetAtPath(path , typeof(TextAsset));
		
		XmlReader reader = XmlReader.Create(new StringReader(xmlFile.text));
		int readingMode = 0;
		
		// Clear/define marker lists, to overwrite any previous file
		phonemeData = new List<PhonemeMarker>();
		emotionData = new List<EmotionMarker>();
		
		clip = linkedClip;
		oldClip = clip;
		fileName = xmlFile.name+".xml";
		
		//Create Dictionary for loading phonemes
		Dictionary<string , Phoneme> phonemeLookup = new Dictionary<string, Phoneme>() {
			{"AI"          , Phoneme.AI},
			{"CDGKNRSThYZ" , Phoneme.CDGKNRSThYZ},
			{"E"           , Phoneme.E},
			{"FV"          , Phoneme.FV},
			{"L"           , Phoneme.L},
			{"MBP"         , Phoneme.MBP},
			{"O"           , Phoneme.O},
			{"U"           , Phoneme.U},
			{"WQ"          , Phoneme.WQ}
		};
		
		try {
			while (reader.Read()) {
				if(reader.NodeType == XmlNodeType.Element){
					if(reader.Name == "phonemes"){
						readingMode = 1;
					}else if(reader.Name == "emotions"){
						readingMode = 2;
					}else if(reader.Name == "emotions"){
						readingMode = 3;
					}else if(reader.Name == "marker"){
						if(readingMode == 1){
							if(reader.HasAttributes){
								Phoneme phon = phonemeLookup[reader.GetAttribute("phoneme")];
								float time = float.Parse(reader.GetAttribute("time"))/linkedClip.length;
								
								phonemeData.Add(new PhonemeMarker(phon , time));
							}
						}else if(readingMode == 2){
							if(reader.HasAttributes){
								string emote = reader.GetAttribute("emotion");
								float startTime = float.Parse(reader.GetAttribute("start"))/linkedClip.length;
								float endTime = float.Parse(reader.GetAttribute("end"))/linkedClip.length;
								float blendInTime = float.Parse(reader.GetAttribute("blendIn"));
								float blendOutTime = float.Parse(reader.GetAttribute("blendOut"));
								bool blendFromMarker = bool.Parse(reader.GetAttribute("blendFromMarker"));
								bool blendToMarker = bool.Parse(reader.GetAttribute("blendToMarker"));
								
								emotionData.Add(new EmotionMarker(emote , startTime , endTime , blendInTime , blendOutTime , blendToMarker , blendFromMarker));
							}
						}else if(readingMode == 3){
							if(reader.HasAttributes){
								float time = float.Parse(reader.GetAttribute("time"))/linkedClip.length;

								gestureData.Add(new GestureMarker(reader.GetAttribute("gesture") , time));
							}
						}
					}
				}
			}
			
			currentMarker = -1;
			changed = true;
			previewOutOfDate = true;
			
			string[] pathParts = path.Split(new string[]{"/"} , StringSplitOptions.None);
			lastLoad = path.Remove(path.Length-pathParts[pathParts.Length-1].Length).Substring(6);
			
		}catch (System.Exception exception) {
			Debug.Log("Error when loading XML file: " + exception.Message);
		}
	}

	void SaveFile (string path , bool isXML){
		if(isXML){
			XmlWriterSettings settings = new XmlWriterSettings {Indent = true , IndentChars = "\t"};
			XmlWriter writer = XmlWriter.Create(path , settings);

			writer.WriteStartDocument();

			//Header
			writer.WriteComment("Exported from RogoDigital LipSync " + versionNumber + ". Exported at " + DateTime.Now.ToString());
			writer.WriteComment("Note: This format cannot directly reference the linked AudioClip like a LipSyncData asset can. It is advised that you use that format instead unless you need to process the data further outside of Unity.");

			writer.WriteStartElement("LipSyncData");

			//Data
			writer.WriteStartElement("phonemes");
			foreach(PhonemeMarker marker in phonemeData){
				writer.WriteStartElement("marker");

				writer.WriteAttributeString("time" , (marker.time * clip.length).ToString());
				writer.WriteAttributeString("phoneme" , marker.phoneme.ToString());

				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteStartElement("emotions");
			foreach(EmotionMarker marker in emotionData){
				writer.WriteStartElement("marker");
				writer.WriteAttributeString("start" , (marker.startTime * clip.length).ToString());
				writer.WriteAttributeString("end" , (marker.endTime * clip.length).ToString());
				writer.WriteAttributeString("blendFromMarker" , (marker.blendFromMarker?"true":"false"));
				writer.WriteAttributeString("blendToMarker" , (marker.blendToMarker?"true":"false"));
				writer.WriteAttributeString("blendIn" , marker.blendInTime.ToString());
				writer.WriteAttributeString("blendOut" , marker.blendOutTime.ToString());

				writer.WriteAttributeString("emotion" , marker.emotion);
				writer.WriteEndElement();
			}

			writer.WriteStartElement("gestures");
			foreach(GestureMarker marker in gestureData){
				writer.WriteStartElement("marker");

				writer.WriteAttributeString("time" , (marker.time * clip.length).ToString());
				writer.WriteAttributeString("gesture" , marker.gesture);

				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteEndDocument();
			writer.Close();
			AssetDatabase.Refresh();
		}else{
			LipSyncData file = ScriptableObject.CreateInstance<LipSyncData>();
			file.phonemeData = phonemeData.ToArray();
			file.emotionData = emotionData.ToArray();
			file.gestureData = gestureData.ToArray();

			file.clip = clip;
			LipSyncData outputFile = (LipSyncData)AssetDatabase.LoadAssetAtPath(path , typeof(LipSyncData));
			
			if(outputFile != null){
				EditorUtility.CopySerialized (file, outputFile);
				AssetDatabase.SaveAssets();
			}else{
				outputFile = ScriptableObject.CreateInstance<LipSyncData>();
				EditorUtility.CopySerialized (file, outputFile);
				AssetDatabase.CreateAsset(outputFile , path);
			}

			fileName = outputFile.name+".Asset";
			DestroyImmediate(file);
			AssetDatabase.Refresh();
		}
	}

	void UpdatePreview (float time) {
		if(previewTarget != null) {
			if(previewTarget.blendSystem != null) {
				if(previewTarget.blendSystem.isReady) {
					if(previewOutOfDate) {
						previewTarget.TempLoad(phonemeData , emotionData , clip);
						previewTarget.ProcessData();
						previewOutOfDate = false;
					}

					previewTarget.PreviewAtTime(time);
				}
			}
		}
	}

	//Save Changes
	void OnDestroy () {
		AudioUtility.StopAllClips();
		if(changed){
			string oldName = fileName;
			string oldLastLoad = lastLoad;
			float localOldSeekPosition = seekPosition;;
			SaveFile("Assets/Rogo Digital/LipSync/AUTOSAVE.asset" , false);
			int choice = EditorUtility.DisplayDialogComplex("Save Changes" , "You have made changes to the current file, do you want to save them before closing?" , "Yes" ,"No" , "Cancel");
			if(choice == 0){
				OnSaveClick();
				AssetDatabase.DeleteAsset("Assets/Rogo Digital/LipSync/AUTOSAVE.asset");
			}else if(choice == 1){
				AssetDatabase.DeleteAsset("Assets/Rogo Digital/LipSync/AUTOSAVE.asset");
			}else{
				ShowWindow("Assets/Rogo Digital/LipSync/AUTOSAVE.asset" , true , oldName , oldLastLoad , markerTab , localOldSeekPosition);
			}
		}
	}

	public void StartAutoSync () {
		Assembly assembly = typeof(LipSyncClipSetup).Assembly;
		Type autoSync = assembly.GetType("RogoDigital.Lipsync.AutoSync");

		MethodInfo method = autoSync.GetMethod(
			"ProcessAudio",
			BindingFlags.Static | BindingFlags.Public,
			null,
			new System.Type[] {
			typeof(AudioClip)
		},
		null
		);
		phonemeData = (List<PhonemeMarker>)method.Invoke(
			null,
			new object[] {
			clip
		}
		);
	}

	public void StartAutoSyncText () {
		AutoSyncTextWindow.CreateWindow(this , this);
	}

	static int EmotionSort (EmotionMarker a , EmotionMarker b){
		return a.startTime.CompareTo(b.startTime);
	}

	static Color HexToColor (int color){
		string hex = color.ToString("X").PadLeft(6 , (char)'0');

		int R = Convert.ToInt32(hex.Substring(0 , 2) , 16);
		int G = Convert.ToInt32(hex.Substring(2 , 2) , 16);
		int B = Convert.ToInt32(hex.Substring(4 , 2) , 16);
		return new Color(R/255f , G/255f , B/255f);
	}

	static int ColorToHex (Color color){
		string R = ((int)(color.r*255)).ToString("X").PadLeft(2 , (char)'0');
		string G = ((int)(color.g*255)).ToString("X").PadLeft(2 , (char)'0');
		string B = ((int)(color.b*255)).ToString("X").PadLeft(2 , (char)'0');

		string hex = R+G+B;
		return Convert.ToInt32(hex , 16);
	}

	static Color Invert (Color color){
		return new Color(1-color.r , 1-color.g , 1-color.b);
	}

	static Color Darken (Color color , float amount){
		return new Color(color.r*amount , color.g*amount , color.b*amount);
	}
}