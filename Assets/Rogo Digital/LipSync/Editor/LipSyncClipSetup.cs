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

public class LipSyncClipSetup : ModalParent {

	public AudioClip clip;

	private const float version = 1;
	private const string versionNumber = "Pro 1.0";

	private Texture2D waveform;
	private float seekPosition;

	private bool isPlaying = false;
	private bool isPaused = false;
	private bool previewing = false;
	private bool looping = false;

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

	private int draggingScrollbar;
	private float scrollbarStartOffset;

	private List<int> selection;
	private int firstSelection;
	private float[] selectionOffsets;
	private float[] sequentialStartOffsets;
	private float[] sequentialEndOffsets;

	private int currentMarker = -1;
	private int highlightedMarker = -1;
	private int currentComponent = 0;
	private int markerTab = 0;

	private bool dragging = false;

	private int filterMask = -1;
	private int[] phonemeFlags = new int[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 };

	private float startOffset;
	private float endOffset;
	private float lowSnapPoint;
	private float highSnapPoint;

	private Color nextColor;
	private Color lastColor;

	private EmotionMarker nextMarker = null;
	private EmotionMarker previousMarker = null;

	private string lastLoad = "";
	private string fileName = "Untitled";
	public bool changed = false;

	private bool waitingForKeyUp;

	private Texture2D playhead_top;
	private Texture2D playhead_line;
	private Texture2D playhead_bottom;
	private Texture2D track_top;
	private Texture2D playIcon;
	private Texture2D stopIcon;
	private Texture2D pauseIcon;
	private Texture2D loopIcon;
	private Texture2D settingsIcon;
	private Texture2D previewIcon;
	private Texture2D windowIcon;
	private Texture2D indicator;

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
	private Texture2D emotion_error;
	private Texture2D emotion_end_hover;
	private Texture2D emotion_end_selected;
	private Texture2D emotion_blend_start;
	private Texture2D emotion_blend_end;

	private Texture2D gesture_normal;
	private Texture2D gesture_hover;
	private Texture2D gesture_selected;

	private Texture2D preview_bar;
	private Texture2D preview_icon;

	public List<PhonemeMarker> phonemeData = new List<PhonemeMarker>();
	public List<GestureMarker> gestureData = new List<GestureMarker>();

	public List<EmotionMarker> emotionData = new List<EmotionMarker>();
	public List<EmotionMarker> unorderedEmotionData = new List<EmotionMarker>();

	public float fileLength = 10;
	public string transcript = "";

	private string[] languageModelNames;

	private LipSyncProject settings;
	private bool settingsOpen = false;

	private bool visualPreview = false;
	private LipSync previewTarget = null;
	public bool previewOutOfDate = true;

	private bool useColors;
	private int defaultColor;

	private int defaultLanguageModel;
	private bool soXAvailable;
	private string soXPath;
	private bool continuousUpdate;
	private float snappingDistance;
	private bool snapping;
	private bool setViewportOnLoad;
	private int maxWaveformWidth;
	private bool showExtensionsOnLoad;
	private bool showTimeline;
	private float scrubLength;
	private float volume;

	private Vector2 settingsScroll;

	void OnEnable() {
		//Load Resources;
		playhead_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_top.png");
		playhead_line = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_middle.png");
		playhead_bottom = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_bottom.png");

		marker_normal = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker.png");
		marker_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker-selected.png");
		marker_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker-highlight.png");
		marker_line = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/white.png");
		indicator = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/indicator.png");

		gesture_normal = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/gesture.png");
		gesture_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/gesture-selected.png");
		gesture_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/gesture-highlight.png");

		emotion_start = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-start.png");
		emotion_start_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-start-highlight.png");
		emotion_start_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-start-select.png");
		emotion_area = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-area.png");
		emotion_end = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-end.png");
		emotion_blend_in = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-in.png");
		emotion_blend_out = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-out.png");
		emotion_error = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-error.png");
		emotion_end_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-end-highlight.png");
		emotion_end_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-end-select.png");
		emotion_blend_start = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-start.png");
		emotion_blend_end = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-end.png");

		preview_bar = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/preview-bar.png");
		preview_icon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/preview-icon.png");

		if (!EditorGUIUtility.isProSkin) {
			track_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/track.png");
			playIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/play.png");
			stopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/stop.png");
			pauseIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/pause.png");
			loopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/loop.png");
			settingsIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/settings.png");
			previewIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/eye.png");
		} else {
			track_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/track.png");
			playIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/play.png");
			stopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/stop.png");
			pauseIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/pause.png");
			loopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/loop.png");
			settingsIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/settings.png");
			previewIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/eye.png");
		}

		//Get Settings File
		settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset", typeof(LipSyncProject));
		if (settings == null) {
			settings = ScriptableObject.CreateInstance<LipSyncProject>();

			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = new string[] { "default" };
			newSettings.emotionColors = new Color[] { new Color(1f, 0.7f, 0.1f) };

			EditorUtility.CopySerialized(newSettings, settings);
			AssetDatabase.CreateAsset(settings, "Assets/Rogo Digital/LipSync/ProjectSettings.asset");
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}

		//Get Editor Settings
		continuousUpdate = EditorPrefs.GetBool("LipSync_ContinuousUpdate", true);

		useColors = EditorPrefs.GetBool("LipSync_UseColors", true);
		defaultColor = EditorPrefs.GetInt("LipSync_DefaultColor", 0xAAAAAA);
		setViewportOnLoad = EditorPrefs.GetBool("LipSync_SetViewportOnLoad", true);
		showTimeline = EditorPrefs.GetBool("LipSync_ShowTimeline", true);
		showExtensionsOnLoad = EditorPrefs.GetBool("LipSync_ShowExtensionsOnLoad", true);
		maxWaveformWidth = EditorPrefs.GetInt("LipSync_MaxWaveformWidth", 2048);
		scrubLength = EditorPrefs.GetFloat("LipSync_ScrubLength", 0.075f);
		volume = EditorPrefs.GetFloat("LipSync_Volume", 1f);
		defaultLanguageModel = EditorPrefs.GetInt("LipSync_DefaultLanguageModel", 0);
		soXAvailable = EditorPrefs.GetBool("LipSync_SoXAvailable", false);
		soXPath = EditorPrefs.GetString("LipSync_SoXPath", "");

		if (EditorPrefs.HasKey("LipSync_Snapping")) {
			if (EditorPrefs.GetBool("LipSync_Snapping")) {
				if (EditorPrefs.HasKey("LipSync_SnappingDistance")) {
					snappingDistance = EditorPrefs.GetFloat("LipSync_SnappingDistance");
					snapping = true;
				} else {
					snappingDistance = 0;
					snapping = false;
				}
			} else {
				snappingDistance = 0;
				snapping = false;
			}
		} else {
			snappingDistance = 0;
			snapping = false;
		}

		if (languageModelNames == null) {
			languageModelNames = AutoSyncLanguageModel.FindModels();
		}

		SceneView.onSceneGUIDelegate += OnSceneGUI;
		
		selection = new List<int>();
		oldPos = this.position;
		oldClip = clip;
	}

	void OnDisable() {
		if (previewTarget != null) {
			UpdatePreview(0);
			previewTarget = null;
		}
	}

	void OnSceneGUI(SceneView sceneView) {
		if (visualPreview) {
			Camera cam = sceneView.camera;
			Handles.BeginGUI();
			
			Rect bottom = new Rect(0, cam.pixelHeight - 3, cam.pixelWidth, 3);
			GUI.DrawTexture(bottom, preview_bar);

			GUI.DrawTexture(new Rect(cam.pixelWidth - 256, cam.pixelHeight - 64, 256, 64), preview_icon);
			Handles.EndGUI();
		}
	}

	public override void OnModalGUI() {
		GUIStyle centeredStyle = new GUIStyle(EditorStyles.whiteLabel);
		centeredStyle.alignment = TextAnchor.MiddleCenter;

		// Keyboard Shortucts
		if (Event.current.type != EventType.Repaint) {
			Event e = Event.current;

			if (!waitingForKeyUp) {
				if (e.modifiers == EventModifiers.Control) {
					if (e.keyCode == KeyCode.A) {
						SelectAll();
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.S) {
						OnSaveClick();
						waitingForKeyUp = true;
					}
				} else if (e.modifiers == EventModifiers.Shift) {
					if (e.keyCode == KeyCode.Comma) {
						seekPosition = Mathf.Clamp01(seekPosition - 0.01f);
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.Period) {
						seekPosition = Mathf.Clamp01(seekPosition + 0.01f);
						waitingForKeyUp = true;
					}
				} else if (e.modifiers == (EventModifiers.Control | EventModifiers.Shift)) {
					if (e.keyCode == KeyCode.A) {
						PhonemePicked(new object[] { Phoneme.AI, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.E) {
						PhonemePicked(new object[] { Phoneme.E, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.U) {
						PhonemePicked(new object[] { Phoneme.U, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.O) {
						PhonemePicked(new object[] { Phoneme.O, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.C) {
						PhonemePicked(new object[] { Phoneme.CDGKNRSThYZ, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.F) {
						PhonemePicked(new object[] { Phoneme.FV, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.L) {
						PhonemePicked(new object[] { Phoneme.L, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.M) {
						PhonemePicked(new object[] { Phoneme.MBP, seekPosition });
						waitingForKeyUp = true;
					} else if (e.keyCode == KeyCode.W) {
						PhonemePicked(new object[] { Phoneme.WQ, seekPosition });
						waitingForKeyUp = true;
					}
				}
			} else {
				if (e.type == EventType.KeyUp) {
					waitingForKeyUp = false;
				}
			}
		}

		//Toolbar
		Rect topToolbarRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(topToolbarRect, "", EditorStyles.toolbar);
		Rect fileRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(60))) {
			GenericMenu fileMenu = new GenericMenu();

			fileMenu.AddItem(new GUIContent("New File"), false, OnNewClick);
			fileMenu.AddItem(new GUIContent("Open File"), false, OnLoadClick);
			fileMenu.AddItem(new GUIContent("Import XML"), false, OnXMLImport);
			fileMenu.AddSeparator("");
			if (clip && phonemeData.Count > 0 || clip && emotionData.Count > 0) {
				fileMenu.AddItem(new GUIContent("Save"), false, OnSaveClick);
				fileMenu.AddItem(new GUIContent("Save As"), false, OnSaveAsClick);
				fileMenu.AddItem(new GUIContent("Export"), false, OnUnityExport);
				fileMenu.AddItem(new GUIContent("Export XML"), false, OnXMLExport);
			} else {
				fileMenu.AddDisabledItem(new GUIContent("Save"));
				fileMenu.AddDisabledItem(new GUIContent("Save As"));
				fileMenu.AddDisabledItem(new GUIContent("Export"));
				fileMenu.AddDisabledItem(new GUIContent("Export XML"));
			}
			fileMenu.AddSeparator("");
			fileMenu.AddItem(new GUIContent("Project Settings"), false, ShowProjectSettings);
			fileMenu.AddSeparator("");
			fileMenu.AddItem(new GUIContent("Exit"), false, Close);
			fileMenu.DropDown(fileRect);
		}
		GUILayout.EndHorizontal();
		Rect editRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Edit", EditorStyles.toolbarDropDown, GUILayout.Width(60))) {
			GenericMenu editMenu = new GenericMenu();

			editMenu.AddItem(new GUIContent("Select All"), false, SelectAll);
			editMenu.AddItem(new GUIContent("Select None"), false, SelectNone);
			editMenu.AddItem(new GUIContent("Invert Selection"), false, InvertSelection);
			editMenu.AddSeparator("");
			if (markerTab == 0) {
				if (clip != null) {
					editMenu.AddItem(new GUIContent("Set Intensity From Volume"), false, SetIntensitiesVolume);
				} else {
					editMenu.AddDisabledItem(new GUIContent("Set Intensity From Volume"));
				}
				
				editMenu.AddItem(new GUIContent("Reset Intensities"), false, ResetIntensities);
			} else if (markerTab == 1) {
				editMenu.AddItem(new GUIContent("Remove Missing Emotions"), false, RemoveMissingEmotions);
				editMenu.AddItem(new GUIContent("Reset Intensities"), false, ResetIntensities);
			} else if (markerTab == 2) {
				editMenu.AddItem(new GUIContent("Remove Missing Gestures"), false, RemoveMissingGestures);
			}
			editMenu.AddSeparator("");
			editMenu.AddItem(new GUIContent("Clip Settings"), false, ClipSettings);
			editMenu.DropDown(editRect);
		}
		GUILayout.EndHorizontal();
		Rect autoRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("AutoSync", EditorStyles.toolbarDropDown, GUILayout.Width(70))) {
			GenericMenu autoMenu = new GenericMenu();

			autoMenu.AddDisabledItem(new GUIContent("AutoSync 2"));
			autoMenu.AddDisabledItem(new GUIContent("Powered by CMU Sphinx"));
			autoMenu.AddSeparator("");
			if (clip != null) {
				autoMenu.AddItem(new GUIContent("Start (Default Settings)"), false, StartAutoSync);
				autoMenu.AddItem(new GUIContent("Custom Settings"), false, StartAutoSyncText , 0);
			} else {
				autoMenu.AddDisabledItem(new GUIContent("Start (Default Settings)"));
				autoMenu.AddDisabledItem(new GUIContent("Custom Settings"));
			}
			autoMenu.AddSeparator("");
			autoMenu.AddItem(new GUIContent("Batch Process"), false, StartAutoSyncText, 1);
			autoMenu.DropDown(autoRect);
		}
		GUILayout.EndHorizontal();
		Rect helpRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Help", EditorStyles.toolbarDropDown, GUILayout.Width(60))) {
			GenericMenu helpMenu = new GenericMenu();

			helpMenu.AddDisabledItem(new GUIContent("LipSync " + versionNumber));
			helpMenu.AddDisabledItem(new GUIContent("© Rogo Digital " + DateTime.Now.Year.ToString()));
			helpMenu.AddSeparator("");
			helpMenu.AddItem(new GUIContent("Get LipSync Extensions"), false, RDExtensionWindow.ShowWindowGeneric, "LipSync");
			helpMenu.AddSeparator("");
			helpMenu.AddItem(new GUIContent("Forum Thread"), false, OpenURL, "http://forum.unity3d.com/threads/beta-lipsync-a-flexible-lipsyncing-and-facial-animation-system.309324/");
			helpMenu.AddItem(new GUIContent("Email Support"), false, OpenURL, "mailto:contact@rogodigital.com");

			helpMenu.DropDown(helpRect);
		}
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		if (changed == true) {
			GUILayout.Box(fileName + "*", EditorStyles.label);
		} else {
			GUILayout.Box(fileName, EditorStyles.label);
		}
		GUILayout.FlexibleSpace();

		settingsOpen = GUILayout.Toggle(settingsOpen, new GUIContent(settingsIcon, "Settings"), EditorStyles.toolbarButton, GUILayout.MaxWidth(40));
		bool oldPreview = visualPreview;
		visualPreview = GUILayout.Toggle(visualPreview, new GUIContent(previewIcon, "Realtime Preview"), EditorStyles.toolbarButton, GUILayout.MaxWidth(40));
		if (visualPreview != oldPreview) {
			SceneView.RepaintAll();
		}

		GUILayout.Space(20);
		GUILayout.Box(versionNumber, EditorStyles.label);
		GUILayout.Box("", EditorStyles.toolbar);
		EditorGUILayout.EndHorizontal();

		if (settingsOpen) {
			//Settings Screen
			settingsScroll = GUILayout.BeginScrollView(settingsScroll);
			GUILayout.Space(10);
			GUILayout.Box("Settings", EditorStyles.largeLabel);
			GUILayout.Space(15);
			GUILayout.Box("Emotion Editing", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			bool oldsnapping = snapping;
			snapping = GUILayout.Toggle(snapping, "Emotion Snapping");
			if (oldsnapping != snapping) {
				EditorPrefs.SetBool("LipSync_Snapping", snapping);
				snappingDistance = 0;
			}
			if (snapping) {
				GUILayout.Space(10);
				float oldSnappingDistance = snappingDistance;
				snappingDistance = EditorGUILayout.Slider(new GUIContent("Snapping Distance", "The strength of the emotion snapping."), (snappingDistance * 200), 0, 10) / 200;
				GUILayout.FlexibleSpace();
				if (oldSnappingDistance != snappingDistance) {
					EditorPrefs.SetFloat("LipSync_SnappingDistance", snappingDistance);
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			bool oldcolors = useColors;
			useColors = GUILayout.Toggle(useColors, "Use Emotion Colors");
			if (oldcolors != useColors) {
				EditorPrefs.SetBool("LipSync_UseColors", useColors);
			}
			if (!useColors) {
				GUILayout.Space(10);
				int oldColour = defaultColor;
				defaultColor = ColorToHex(EditorGUILayout.ColorField("Default Color", HexToColor(defaultColor)));
				GUILayout.FlexibleSpace();
				if (oldColour != defaultColor) {
					EditorPrefs.SetInt("LipSync_DefaultColor", defaultColor);
				}
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(15);
			GUILayout.Box("AutoSync Settings", EditorStyles.boldLabel);
			if (languageModelNames.Length > 0) {
				int oldLanguageModel = defaultLanguageModel;
				defaultLanguageModel = EditorGUILayout.Popup("Default Language Model", defaultLanguageModel, languageModelNames, GUILayout.MaxWidth(300));
				if (oldLanguageModel != defaultLanguageModel) {
					EditorPrefs.SetInt("LipSync_DefaultLanguageModel", defaultLanguageModel);
				}
			} else {
				EditorGUILayout.HelpBox("No language models found. You can download language models from the extensions window or the LipSync website.", MessageType.Warning);
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("SoX Path");
			GUILayout.Space(5);
			GUI.color = soXAvailable ? Color.green : Color.red;
			GUILayout.Box(new GUIContent(indicator, soXAvailable?"SoX is installed. Audio conversion available.":"SoX is not installed."), GUIStyle.none);
			GUI.color = Color.white;
			GUILayout.Space(10);
			soXPath = EditorGUILayout.TextField(soXPath);
			if (GUILayout.Button("Browse")) {
				string path = EditorUtility.OpenFilePanel("Find SoX Application", "", "");
				if (!string.IsNullOrEmpty(path)) soXPath = path;
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Verify")) {
				EditorPrefs.SetString("LipSync_SoXPath", soXPath);

				soXAvailable = AutoSync.CheckSoX();
				EditorPrefs.SetBool("LipSync_SoXAvailable", soXAvailable);

				if (soXAvailable) {
					ShowNotification(new GUIContent("Verification Successful. SoX is installed."));
				} else {
					ShowNotification(new GUIContent("Verification Failed. Incorrect File."));
				}
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(15);
			GUILayout.Box("General Settings", EditorStyles.boldLabel);
			bool oldUpdate = continuousUpdate;
			continuousUpdate = GUILayout.Toggle(continuousUpdate, new GUIContent("Continuous Update", "Whether to update the window every frame. This makes editing more responsive, but may be taxing on low-powered systems."));
			if (oldUpdate != continuousUpdate) {
				EditorPrefs.SetBool("LipSync_ContinuousUpdate", continuousUpdate);
			}

			bool oldSetViewportOnLoad = setViewportOnLoad;
			setViewportOnLoad = GUILayout.Toggle(setViewportOnLoad, new GUIContent("Set Viewport on File Load", "Whether to set the viewport to show the entire clip when a new file is loaded."));
			if (oldSetViewportOnLoad != setViewportOnLoad) {
				EditorPrefs.SetBool("LipSync_SetViewportOnLoad", setViewportOnLoad);
			}

			bool oldShowTimeline = showTimeline;
			showTimeline = GUILayout.Toggle(showTimeline, new GUIContent("Show Time Markers", "Whether to show time markers under the timeline."));
			if (oldShowTimeline != showTimeline) {
				EditorPrefs.SetBool("LipSync_ShowTimeline", showTimeline);
			}

			float oldScrubLength = scrubLength;
			scrubLength = EditorGUILayout.FloatField(new GUIContent("Scrubbing Preview Length", "The duration, in seconds, the clip will be played for when scrubbing."), scrubLength, GUILayout.MaxWidth(300));
			if (oldScrubLength != scrubLength) {
				EditorPrefs.SetFloat("LipSync_ScrubLength", scrubLength);
			}

			float oldVolume = volume;
			volume = EditorGUILayout.Slider(new GUIContent("Preview Volume"), volume, 0, 1, GUILayout.MaxWidth(300));
			if (oldVolume != volume) {
				EditorPrefs.SetFloat("LipSync_Volume", volume);
				AudioUtility.SetVolume(volume);
			}
			GUILayout.Space(10);

			int oldMaxWaveformWidth = maxWaveformWidth;
			maxWaveformWidth = EditorGUILayout.IntField(new GUIContent("Max Waveform Width", "The Maximum width for the waveform preview image. Warning: very high values can cause crashes when zooming in on the clip."), maxWaveformWidth, GUILayout.MaxWidth(300));
			if (oldMaxWaveformWidth != maxWaveformWidth) {
				EditorPrefs.SetInt("LipSync_MaxWaveformWidth", maxWaveformWidth);
			}

			bool oldShowExtensionsOnLoad = showExtensionsOnLoad;
			showExtensionsOnLoad = GUILayout.Toggle(showExtensionsOnLoad, new GUIContent("Show Extensions Window", "Whether to automatically dock an extensions window to this one when it is opened."));
			if (oldShowExtensionsOnLoad != showExtensionsOnLoad) {
				EditorPrefs.SetBool("LipSync_ShowExtensionsOnLoad", showExtensionsOnLoad);
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
		clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", clip, typeof(AudioClip), false, GUILayout.MaxWidth(800));
		if (EditorGUI.EndChangeCheck()) {
			DestroyImmediate(waveform);
			changed = true;
		}
		GUILayout.Space(20);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(15);

		float viewportSeconds = (viewportEnd - viewportStart);
		float pixelsPerSecond = position.width / viewportSeconds;
		float mouseX = Event.current.mousePosition.x;
		float mouseY = Event.current.mousePosition.y;

		//Tab Controls
		int oldTab = markerTab;
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Space(20);
		markerTab = GUILayout.Toolbar(markerTab, new string[] { "Phonemes", "Emotions", "Gestures" }, GUILayout.MaxWidth(800));
		GUILayout.Space(20);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		if (oldTab != markerTab) {
			selection.Clear();
			firstSelection = 0;
		}

		GUILayout.Space(40);
		//Preview Box
		GUILayout.Box("", (GUIStyle)"PreBackground", GUILayout.Width(position.width), GUILayout.Height((position.height - waveformHeight) - 18));
		Rect previewRect = GUILayoutUtility.GetLastRect();

		//Right Click Menu
		if (Event.current.type == EventType.ContextClick && previewRect.Contains(Event.current.mousePosition)) {
			GenericMenu previewMenu = new GenericMenu();
			float cursorTime = (viewportStart + (Event.current.mousePosition.x / pixelsPerSecond))/fileLength;

			if (markerTab == 0) {
				for (int a = 0; a < 9; a++) {
					Phoneme phon = (Phoneme)a;
					previewMenu.AddItem(new GUIContent("Add Marker Here/" + phon.ToString()), false, PhonemePicked, new object[] { (Phoneme)a, cursorTime });
				}
			} else if (markerTab == 1) {
				for (int a = 0; a < settings.emotions.Length; a++) {
					string emote = settings.emotions[a];
					previewMenu.AddItem(new GUIContent("Add Marker Here/" + emote), false, EmotionPicked, new object[] { emote, cursorTime });
				}
				previewMenu.AddSeparator("Add Marker Here/");
				previewMenu.AddItem(new GUIContent("Add Marker Here/Add New Emotion"), false, ShowProjectSettings);
			} else if (markerTab == 2) {
				for (int a = 0; a < settings.gestures.Count; a++) {
					string gesture = settings.gestures[a];
					previewMenu.AddItem(new GUIContent("Add Marker Here/" + gesture), false, GesturePicked, new object[] { gesture, cursorTime });
				}
				previewMenu.AddSeparator("Add Marker Here/");
				previewMenu.AddItem(new GUIContent("Add Marker Here/Add New Gesture"), false, ShowProjectSettings);
			}

			previewMenu.ShowAsContext();
		}

		waveformHeight = (int)previewRect.y;
		if (clip != null && waveform != null) GUI.DrawTexture(new Rect(-(viewportStart * pixelsPerSecond), previewRect.y + 3, clip.length * pixelsPerSecond, (position.height - waveformHeight) - 33), waveform);

		//Playhead
		if (Event.current.button != 1) {
			if (clip != null) {
				seekPosition = GUI.HorizontalSlider(new Rect(-(viewportStart * pixelsPerSecond), previewRect.y + 3, clip.length * pixelsPerSecond, (position.height - waveformHeight) - 33), seekPosition, 0, 1, GUIStyle.none, GUIStyle.none);
			} else {
				seekPosition = GUI.HorizontalSlider(new Rect(-(viewportStart * pixelsPerSecond), previewRect.y + 3, fileLength * pixelsPerSecond, (position.height - waveformHeight) - 33), seekPosition, 0, 1, GUIStyle.none, GUIStyle.none);
			}
		}

		GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + (seekPosition * (fileLength * pixelsPerSecond)) - 3, previewRect.y, 7, previewRect.height - 20), playhead_line);
		GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + (seekPosition * (fileLength * pixelsPerSecond)) - 7, previewRect.y, 15, 15), playhead_top);
		GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + (seekPosition * (fileLength * pixelsPerSecond)) - 7, position.height - 48, 15, 15), playhead_bottom);

		GUI.DrawTexture(new Rect(0, previewRect.y - 35, position.width, 36), track_top);

		//Time Lines
		// todo: show/hide markers based on length/zoom
		if (showTimeline) {
			float timeOffset = (viewportStart % 1) * pixelsPerSecond;
			for (int a = 0; a < viewportSeconds + 1; a++) {
				GUI.DrawTexture(new Rect((a * pixelsPerSecond) - timeOffset, previewRect.y + 1, 1, 12), marker_line);
				GUI.Box(new Rect((a * pixelsPerSecond) - (timeOffset - 5), previewRect.y, 30, 20), ((Mathf.FloorToInt(viewportStart) + a) % 60).ToString().PadLeft(2, '0') + "s", EditorStyles.whiteMiniLabel);
			}
		}

		//Preview Warning
		if (visualPreview) {
			bool error = true;
			if (Selection.activeGameObject != null) {
				if (previewTarget == null) {
					previewTarget = Selection.activeGameObject.GetComponent<LipSync>();
					if (previewTarget != null) {
						error = false;
					}
				} else if (previewTarget.gameObject != Selection.activeGameObject) {
					previewTarget = Selection.activeGameObject.GetComponent<LipSync>();
					if (previewTarget != null) {
						error = false;
					}
				} else {
					error = false;
				}
			}

			if (error) {
				EditorGUI.HelpBox(new Rect(20, previewRect.y + previewRect.height - 45, position.width - 40, 25), "Preview mode active. Select a GameObject with a valid LipSync component in the scene to preview.", MessageType.Warning);
			} else {
				EditorGUI.HelpBox(new Rect(20, previewRect.y + previewRect.height - 45, position.width - 40, 25), "Preview mode active. Note: only Phonemes and Emotions will be shown in the preview.", MessageType.Info);
			}
		} else if (previewTarget != null) {
			UpdatePreview(0);
			previewTarget = null;
		}

		// Viewport Scrolling
		if (MinMaxScrollbar(new Rect(0, (previewRect.y + previewRect.height) - 15, previewRect.width, 15), previewRect, ref viewportStart, ref viewportEnd, 0, fileLength , 0.5f)) {
			if (fileLength * pixelsPerSecond <= maxWaveformWidth) {
				DestroyImmediate(waveform);
			}
		}

		GUIContent tip = null;
		MessageType tipType = MessageType.None;

		if (markerTab == 0) {

			//Phoneme Markers
			if (currentModal == null) {
				highlightedMarker = -1;
			}

			if (dragging == false) {
				foreach (PhonemeMarker marker in phonemeData) {
					if ((filterMask & phonemeFlags[(int)marker.phoneme]) == phonemeFlags[(int)marker.phoneme]) {
						Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);
						if (mouseX > markerRect.x + 5 && mouseX < markerRect.x + markerRect.width - 5 && mouseY > markerRect.y && mouseY < markerRect.y + markerRect.height - 4 && currentMarker == -1) {
							highlightedMarker = phonemeData.IndexOf(marker);
						}
					}
				}
			}

			if (dragging == false && highlightedMarker > -1 && focusedWindow == this) {
				PhonemeMarker cm = phonemeData[highlightedMarker];

				if (Event.current.type == EventType.MouseDrag) {
					currentMarker = highlightedMarker;
					startOffset = cm.time - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));

					if (selection.Count > 0) {
						selectionOffsets = new float[selection.Count];
						for (int marker = 0; marker < selectionOffsets.Length; marker++) {
							selectionOffsets[marker] = phonemeData[currentMarker].time - phonemeData[selection[marker]].time;
						}
					}

					dragging = true;
				} else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
					if (Event.current.modifiers == EventModifiers.Shift) {
						if (selection.Count > 0) {
							List<PhonemeMarker> tempData = new List<PhonemeMarker>(phonemeData);
							tempData.Sort(LipSync.SortTime);

							int tempIndex = tempData.IndexOf(cm);
							int tempStart = tempData.IndexOf(phonemeData[firstSelection]);
							selection.Clear();

							if (tempStart > tempIndex) {
								for (int m = tempIndex; m <= tempStart; m++) {
									int realIndex = phonemeData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							} else if (tempStart < tempIndex) {
								for (int m = tempStart; m <= tempIndex; m++) {
									int realIndex = phonemeData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
						} else {
							firstSelection = highlightedMarker;
							selection.Add(highlightedMarker);
						}
					} else if (Event.current.modifiers == EventModifiers.Control) {
						if (!selection.Contains(highlightedMarker)) {
							selection.Add(highlightedMarker);
							if (cm.time < phonemeData[firstSelection].time || selection.Count == 1) {
								firstSelection = highlightedMarker;
							}
						} else {
							selection.Remove(highlightedMarker);
							if (highlightedMarker == firstSelection) {
								firstSelection = 0;
							}
						}
						selection.Sort(SortInt);
					} else {
						selection.Clear();
						selection.Add(highlightedMarker);
						firstSelection = highlightedMarker;
					}
				} else if (Event.current.type == EventType.ContextClick) {
					GenericMenu markerMenu = new GenericMenu();
					if (selection.Count > 1) {
						markerMenu.AddItem(new GUIContent("Delete Selection"), false, DeleteSelectedMarkers);
					} else {
						if (selection.Count > 1) {
							markerMenu.AddItem(new GUIContent("Delete"), false, DeleteSelectedMarkers);
						} else {
							markerMenu.AddItem(new GUIContent("Delete"), false, DeleteMarker, cm);
						}
						
						for (int a = 0; a < 9; a++) {
							Phoneme phon = (Phoneme)a;
							markerMenu.AddItem(new GUIContent("Change/" + phon.ToString()), false, ChangeMarkerPicked, new List<int> { phonemeData.IndexOf(cm), a });
						}
						markerMenu.AddSeparator("");
						markerMenu.AddItem(new GUIContent("Marker Settings"), false, PhonemeMarkerSettings, cm);
					}

					markerMenu.ShowAsContext();
				} else if (Event.current.type == EventType.KeyUp && selection.Count == 0) {
					if (Event.current.keyCode == KeyCode.Delete) {
						DeleteMarker(phonemeData[highlightedMarker]);
					}
				}
			} else if (dragging == false && focusedWindow == this) {
				if (Event.current.type == EventType.MouseUp && !(Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Shift)) {
					selection.Clear();
				} else if (Event.current.type == EventType.KeyUp && selection.Count > 0) {
					if (Event.current.keyCode == KeyCode.Delete) {
						DeleteSelectedMarkers();
					}
				}
			}

			foreach (PhonemeMarker marker in phonemeData) {
				if ((filterMask & phonemeFlags[(int)marker.phoneme]) == phonemeFlags[(int)marker.phoneme]) {
					Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);

					GUI.color = Color.Lerp(Color.gray, Color.white, marker.intensity);
					if (currentMarker == phonemeData.IndexOf(marker)) {
						GUI.Box(markerRect, marker_selected, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
						tip = new GUIContent(marker.phoneme.ToString()+" - "+Mathf.RoundToInt(marker.intensity * 100f).ToString()+"%");
					} else if (highlightedMarker == phonemeData.IndexOf(marker)) {
						GUI.Box(markerRect, marker_hover, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
						tip = new GUIContent(marker.phoneme.ToString() + " - " + Mathf.RoundToInt(marker.intensity * 100f).ToString() + "%");
					} else if (selection.Contains(phonemeData.IndexOf(marker))) {
						GUI.Box(markerRect, marker_selected, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
					} else {
						GUI.Box(markerRect, marker_normal, GUIStyle.none);
					}

					GUI.color = Color.white;
				}
			}
		} else if (markerTab == 1) {
			if (currentModal == null) {
				highlightedMarker = -1;
			}
			int highlightComponent = -1;

			foreach (EmotionMarker marker in unorderedEmotionData) {
				Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)), previewRect.y - 30, (marker.endTime - marker.startTime) * (fileLength * pixelsPerSecond), 31);
				Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
				Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);

				if (focusedWindow == this) {
					// Blend In/Out Handles
					if (mouseX > startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)) && mouseX < startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)) + startMarkerRect.width && mouseY > startMarkerRect.y && mouseY < startMarkerRect.y + startMarkerRect.height - 4 && currentMarker == -1 && !marker.blendFromMarker) {
						EditorGUIUtility.AddCursorRect(new Rect(startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)), startMarkerRect.y, startMarkerRect.width, startMarkerRect.height), MouseCursor.SlideArrow);
						highlightedMarker = emotionData.IndexOf(marker);
						highlightComponent = 3;
					} else if (mouseX > endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)) && mouseX < endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)) + endMarkerRect.width && mouseY > endMarkerRect.y && mouseY < endMarkerRect.y + endMarkerRect.height - 4 && currentMarker == -1 && !marker.blendToMarker) {
						EditorGUIUtility.AddCursorRect(new Rect(endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)), endMarkerRect.y, endMarkerRect.width, endMarkerRect.height), MouseCursor.SlideArrow);
						highlightedMarker = emotionData.IndexOf(marker);
						highlightComponent = 4;
					}else if (mouseX > markerRect.x && mouseX < markerRect.x + markerRect.width && mouseY > markerRect.y && mouseY < markerRect.y + markerRect.height - 4 && currentMarker == -1) {
						// Bars
						highlightedMarker = emotionData.IndexOf(marker);
						highlightComponent = 1;

						if (marker.invalid) {
							tip = new GUIContent("Markers are not allowed inside one another.");
							tipType = MessageType.Error;
						}
					}
				}
			}

			foreach (EmotionMarker marker in unorderedEmotionData) {
				Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
				Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);

				if (focusedWindow == this) {
					// Start/End Handles
					if (mouseX > startMarkerRect.x && mouseX < startMarkerRect.x + startMarkerRect.width - 7 && mouseY > startMarkerRect.y && mouseY < startMarkerRect.y + startMarkerRect.height - 4 && currentMarker == -1) {
						highlightedMarker = emotionData.IndexOf(marker);
						highlightComponent = 0;
						EditorGUIUtility.AddCursorRect(startMarkerRect, MouseCursor.ResizeHorizontal);
					} else if (mouseX > endMarkerRect.x + 7 && mouseX < endMarkerRect.x + endMarkerRect.width && mouseY > endMarkerRect.y && mouseY < endMarkerRect.y + endMarkerRect.height - 4 && currentMarker == -1) {
						highlightedMarker = emotionData.IndexOf(marker);
						highlightComponent = 2;
						EditorGUIUtility.AddCursorRect(endMarkerRect, MouseCursor.ResizeHorizontal);
					}
				}
			}

			if (dragging == false && highlightedMarker > -1 && focusedWindow == this) {
				EmotionMarker em = emotionData[highlightedMarker];

				if (Event.current.type == EventType.MouseDrag) {
					dragging = true;
					currentMarker = highlightedMarker;
					currentComponent = highlightComponent;

					if (currentComponent == 0 || currentComponent == 2) {
						selection.Clear();
						firstSelection = 0;
					}

					if (selection.Count > 0) {
						selectionOffsets = new float[selection.Count];
						sequentialStartOffsets = new float[selection.Count];
						sequentialEndOffsets = new float[selection.Count];

						for (int marker = 0; marker < selectionOffsets.Length; marker++) {
							selectionOffsets[marker] = em.startTime - emotionData[selection[marker]].startTime;
							sequentialStartOffsets[marker] = emotionData[selection[marker]].startTime - emotionData[selection[0]].startTime;
							sequentialEndOffsets[marker] = emotionData[selection[selection.Count - 1]].endTime - emotionData[selection[marker]].endTime;
						}
					}

					if (currentComponent < 3) {
						startOffset = em.startTime - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
						endOffset = em.endTime - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));

						lowSnapPoint = snappingDistance;
						highSnapPoint = 1 - snappingDistance;

						if (selection.Count <= 1) {
							if (currentMarker > 0) {
								lowSnapPoint = emotionData[currentMarker - 1].endTime + snappingDistance;
							}

							if (currentMarker < emotionData.Count - 1) {
								highSnapPoint = emotionData[currentMarker + 1].startTime - snappingDistance;
							}
						} else {
							if (currentMarker > 0) {
								for (int m = currentMarker-1; m >= 0; m--) {
									if (!selection.Contains(m)) {
										lowSnapPoint = emotionData[m].endTime + snappingDistance;
									}
								}
							}

							if (currentMarker < emotionData.Count - 1) {
								for (int m = currentMarker+1; m < emotionData.Count; m++) {
									if (!selection.Contains(m)) {
										highSnapPoint = emotionData[m].startTime - snappingDistance;
									}
								}
							}
						}
						
					} else {
						startOffset = (em.blendInTime) - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
						endOffset = (em.blendOutTime) - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
					}
					
					previousMarker = null;
					nextMarker = null;
				} else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
					if (Event.current.modifiers == EventModifiers.Shift) {
						if (selection.Count > 0) {
							List<EmotionMarker> tempData = new List<EmotionMarker>(emotionData);

							int tempIndex = tempData.IndexOf(em);
							int tempStart = tempData.IndexOf(emotionData[firstSelection]);
							selection.Clear();

							if (tempStart > tempIndex) {
								for (int m = tempIndex; m <= tempStart; m++) {
									int realIndex = emotionData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							} else if (tempStart < tempIndex) {
								for (int m = tempStart; m <= tempIndex; m++) {
									int realIndex = emotionData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
						} else {
							firstSelection = highlightedMarker;
							selection.Add(highlightedMarker);
						}
					} else if (Event.current.modifiers == EventModifiers.Control) {
						if (!selection.Contains(highlightedMarker)) {
							selection.Add(highlightedMarker);
							if (em.startTime < emotionData[firstSelection].startTime || selection.Count == 1) {
								firstSelection = highlightedMarker;
							}
						} else {
							selection.Remove(highlightedMarker);
							if (highlightedMarker == firstSelection) {
								firstSelection = 0;
							}
						}
						selection.Sort(SortInt);
					} else {
						selection.Clear();
						selection.Add(highlightedMarker);
						firstSelection = highlightedMarker;
					}
				} else if (Event.current.type == EventType.ContextClick) {
					GenericMenu markerMenu = new GenericMenu();

					if (selection.Count > 1) {
						markerMenu.AddItem(new GUIContent("Delete"), false, DeleteSelectedEmotions);
					} else {
						markerMenu.AddItem(new GUIContent("Delete"), false, DeleteEmotion, emotionData[highlightedMarker]);
					}
					
					for (int a = 0; a < settings.emotions.Length; a++) {
						string emote = settings.emotions[a];
						markerMenu.AddItem(new GUIContent("Change/" + emote), false, ChangeEmotionPicked, new List<object> { emotionData[highlightedMarker], emote });
					}
					markerMenu.AddSeparator("Change/");
					markerMenu.AddItem(new GUIContent("Change/Add New Emotion"), false, ShowProjectSettings);
					markerMenu.AddSeparator("");
					markerMenu.AddItem(new GUIContent("Marker Settings"), false, EmotionMarkerSettings, emotionData[highlightedMarker]);
					markerMenu.ShowAsContext();
				} else if (Event.current.type == EventType.KeyUp && selection.Count == 0) {
					if (Event.current.keyCode == KeyCode.Delete) {
						DeleteEmotion(emotionData[highlightedMarker]);
					}
				}
			} else if (dragging == false && focusedWindow == this) {
				if (Event.current.type == EventType.MouseUp && !(Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Shift)) {
					selection.Clear();
				} else if (Event.current.type == EventType.KeyUp && selection.Count > 0) {
					if (Event.current.keyCode == KeyCode.Delete) {
						DeleteSelectedEmotions();
					}
				}
			}

			foreach (EmotionMarker marker in unorderedEmotionData) {
				Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
				Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);
				Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)), previewRect.y - 30, (marker.endTime - marker.startTime) * (fileLength * pixelsPerSecond), 31);

				Color barColor = Color.gray;
				lastColor = Color.gray;
				nextColor = Color.gray;
				if (useColors) {
					for (int em = 0; em < settings.emotions.Length; em++) {
						if (settings.emotions[em] == marker.emotion) {
							barColor = settings.emotionColors[em];
						}

						if (emotionData.IndexOf(marker) - 1 >= 0) {
							if (settings.emotions[em] == emotionData[emotionData.IndexOf(marker) - 1].emotion && emotionData[emotionData.IndexOf(marker) - 1].endTime > marker.startTime) {
								lastColor = settings.emotionColors[em];
							}
						}

						if (emotionData.IndexOf(marker) + 1 < emotionData.Count) {
							if (settings.emotions[em] == emotionData[emotionData.IndexOf(marker) + 1].emotion && emotionData[emotionData.IndexOf(marker) + 1].startTime < marker.endTime) {
								nextColor = settings.emotionColors[em];
							}
						}
					}

					if (lastColor == Color.gray) {
						lastColor = Darken(barColor, 0.5f);
					}
					if (nextColor == Color.gray) {
						nextColor = Darken(barColor, 0.5f);
					}
				} else {
					barColor = HexToColor(defaultColor);
					lastColor = Darken(HexToColor(defaultColor), 0.5f);
					nextColor = Darken(HexToColor(defaultColor), 0.5f);
				}

				if (marker.invalid) {
					barColor.a = 0.5f;
				}

				// Drawing
				// Bar
				if ((currentMarker == emotionData.IndexOf(marker) && currentComponent == 1) || selection.Contains(emotionData.IndexOf(marker))) {
					GUI.color = new Color(0.4f, 0.6f, 1f);
					GUI.DrawTexture(markerRect, emotion_area);
				} else if (highlightedMarker == emotionData.IndexOf(marker) && highlightComponent == 1) {
					GUI.color = Color.white;
					GUI.DrawTexture(markerRect, emotion_area);
				} else {
					GUI.color = barColor;
					GUI.DrawTexture(markerRect, emotion_area);
				}

				// Blends
				GUI.color = lastColor;
				GUI.DrawTexture(new Rect(markerRect.x, markerRect.y, marker.blendInTime * (fileLength * pixelsPerSecond), markerRect.height), marker.blendFromMarker?emotion_blend_in:emotion_blend_out);
				GUI.color = nextColor;
				GUI.DrawTexture(new Rect(markerRect.x + markerRect.width, markerRect.y, marker.blendOutTime * (fileLength * pixelsPerSecond), markerRect.height), emotion_blend_out);
				GUI.color = Color.white;

				// Blend Handles
				if (!marker.blendFromMarker && (currentComponent == 3 && currentMarker == emotionData.IndexOf(marker) || highlightComponent == 3 && highlightedMarker == emotionData.IndexOf(marker))) GUI.DrawTexture(new Rect(startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)), startMarkerRect.y, startMarkerRect.width, startMarkerRect.height), emotion_blend_start);
				if (!marker.blendToMarker && (currentComponent == 4 && currentMarker == emotionData.IndexOf(marker) || highlightComponent == 4 && highlightedMarker == emotionData.IndexOf(marker))) GUI.DrawTexture(new Rect(endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)), endMarkerRect.y, endMarkerRect.width, endMarkerRect.height), emotion_blend_end);

				// Start Handle
				if (currentMarker == emotionData.IndexOf(marker) && (currentComponent == 0 || currentComponent == 1)) {
					GUI.DrawTexture(startMarkerRect, emotion_start_selected);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.startTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				} else if (highlightedMarker == emotionData.IndexOf(marker) && (highlightComponent == 0 || highlightComponent == 1)) {
					GUI.DrawTexture(startMarkerRect, emotion_start_hover);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.startTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				} else {
					if (!marker.blendFromMarker) GUI.DrawTexture(startMarkerRect, emotion_start);
				}

				// End Handle
				if (currentMarker == emotionData.IndexOf(marker) && (currentComponent == 1 || currentComponent == 2)) {
					GUI.DrawTexture(endMarkerRect, emotion_end_selected);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.endTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				} else if (highlightedMarker == emotionData.IndexOf(marker) && (highlightComponent == 1 || highlightComponent == 2)) {
					GUI.DrawTexture(endMarkerRect, emotion_end_hover);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.endTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				} else {
					if (!marker.blendToMarker) GUI.DrawTexture(endMarkerRect, emotion_end);
				}

				if (marker.invalid) {
					GUI.DrawTexture(new Rect(markerRect.x+10, markerRect.y+8, 14, 14), emotion_error);
				}

				float lum = (0.299f * barColor.r + 0.587f * barColor.g + 0.114f * barColor.b);
				if (lum > 0.5f || highlightedMarker == emotionData.IndexOf(marker) && highlightComponent == 1) {
					GUI.contentColor = Color.black;
					GUI.Box(new Rect(markerRect.x, markerRect.y + 2, markerRect.width, markerRect.height - 9), marker.emotion, centeredStyle);
					GUI.contentColor = Color.white;
				} else {
					GUI.Box(new Rect(markerRect.x, markerRect.y + 2, markerRect.width, markerRect.height - 9), marker.emotion, centeredStyle);
				}
			}
		} else if (markerTab == 2) {
			//Gesture Markers
			int highlightedMarker = -1;

			foreach (GestureMarker marker in gestureData) {
				Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);
				if (mouseX > markerRect.x + 5 && mouseX < markerRect.x + markerRect.width - 5 && mouseY > markerRect.y && mouseY < markerRect.y + markerRect.height - 4 && currentMarker == -1) {
					highlightedMarker = gestureData.IndexOf(marker);
				}
			}

			if (dragging == false && highlightedMarker > -1 && focusedWindow == this) {
				GestureMarker cm = gestureData[highlightedMarker];

				if (Event.current.type == EventType.MouseDrag) {
					currentMarker = gestureData.IndexOf(cm);
					dragging = true;
					startOffset = cm.time - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
				} else if (Event.current.type == EventType.ContextClick) {
					GenericMenu markerMenu = new GenericMenu();
					markerMenu.AddItem(new GUIContent("Delete"), false, DeleteGesture, cm);

					for (int a = 0; a < settings.gestures.Count; a++) {
						string gesture = settings.gestures[a];
						markerMenu.AddItem(new GUIContent("Change/" + gesture), false, ChangeGesturePicked, new List<object> { highlightedMarker, gesture });
					}
					markerMenu.AddSeparator("Change/");
					markerMenu.AddItem(new GUIContent("Change/Add New Gesture"), false, ShowProjectSettings);
					markerMenu.ShowAsContext();
				}
			}

			foreach (GestureMarker marker in gestureData) {
				Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);

				if (currentMarker == gestureData.IndexOf(marker)) {
					GUI.Box(markerRect, gesture_selected, GUIStyle.none);
					tip = new GUIContent(marker.gesture);
					GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
				} else {
					if (highlightedMarker == gestureData.IndexOf(marker)) {
						GUI.Box(markerRect, gesture_hover, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
						tip = new GUIContent(marker.gesture);
					} else {
						GUI.Box(markerRect, gesture_normal, GUIStyle.none);
					}
				}
			}
		}

		if (tip != null && dragging == false) {
			Rect tooltipRect = new Rect();
			float tooltipWidth = Mathf.Clamp(((GUIStyle)"flow node 0").CalcSize(tip).x + 20, 40, 450);
			if (Event.current.mousePosition.x + tooltipWidth + 10 > this.position.width) {
				tooltipRect = new Rect(Event.current.mousePosition.x - (tooltipWidth + 10), Event.current.mousePosition.y - 10, tooltipWidth, 30);
			} else {
				tooltipRect = new Rect(Event.current.mousePosition.x + 10, Event.current.mousePosition.y - 10, tooltipWidth, 30);
			}

			if (tipType == MessageType.None) {
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 0");
			} else if (tipType == MessageType.Info) {
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 1");
			} else if (tipType == MessageType.Warning) {
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 5");
			} else if (tipType == MessageType.Error) {
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 6");
			}
		}

		if (markerTab == 0) {
			if (dragging == true && Event.current.type == EventType.MouseDrag && currentMarker > -1) {
				if (selection.Count > 0) {
					selection.Sort(SortInt);
					float firstMarkerOffset = phonemeData[currentMarker].time - phonemeData[selection[0]].time;
					float lastMarkerOffset = phonemeData[selection[selection.Count - 1]].time - phonemeData[currentMarker].time;
					float currentMarkerNewTime = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);

					if (currentMarkerNewTime - firstMarkerOffset >= 0 && currentMarkerNewTime + lastMarkerOffset <= 1) {
						for (int marker = 0; marker < selection.Count; marker++) {
							phonemeData[selection[marker]].time = currentMarkerNewTime - selectionOffsets[marker];
						}
					}
				} else {
					phonemeData[currentMarker].time = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);
				}

				changed = true;
				previewOutOfDate = true;
			}
		} else if (markerTab == 1) {
			if (currentMarker > -1) {
				if (currentComponent == 0 || currentComponent == 2)
					EditorGUIUtility.AddCursorRect(new Rect(0, 0, this.position.width, this.position.height), MouseCursor.SlideArrow);
			}

			if (dragging == true && Event.current.type == EventType.MouseDrag && currentMarker > -1) {
				if (currentComponent == 0) {
					float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset;
					if (tempChange > emotionData[currentMarker].endTime - 0.008f) {
					} else {
						emotionData[currentMarker].startTime = tempChange;
					}
					changed = true;
					previewOutOfDate = true;

					emotionData[currentMarker].startTime = Mathf.Clamp01(emotionData[currentMarker].startTime);

					if (emotionData[currentMarker].startTime <= lowSnapPoint && emotionData[currentMarker].startTime >= lowSnapPoint - (snappingDistance * 2)) {
						emotionData[currentMarker].startTime = (lowSnapPoint - snappingDistance);
					} 
				} else if (currentComponent == 1) {
					if (selection.Count > 1) {
						selection.Sort(SortInt);
						float currentMarkerNewTime = ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond)) + startOffset;

						for (int marker = 0; marker < selection.Count; marker++) {
							float length = emotionData[selection[marker]].endTime - emotionData[selection[marker]].startTime;

							emotionData[selection[marker]].startTime = currentMarkerNewTime - selectionOffsets[marker];
							emotionData[selection[marker]].endTime = emotionData[selection[marker]].startTime + length;

							if (emotionData[selection[0]].startTime <= lowSnapPoint && emotionData[selection[0]].startTime >= lowSnapPoint - (snappingDistance * 2)) {
								emotionData[selection[marker]].startTime = (lowSnapPoint - snappingDistance) + sequentialStartOffsets[marker];
								emotionData[selection[marker]].endTime = ((lowSnapPoint - snappingDistance) + sequentialStartOffsets[marker]) + length;
							} else if (emotionData[selection[selection.Count - 1]].endTime >= highSnapPoint && emotionData[selection[selection.Count - 1]].endTime <= highSnapPoint + (snappingDistance * 2)) {
								emotionData[selection[marker]].endTime = (highSnapPoint + snappingDistance) - sequentialEndOffsets[marker];
								emotionData[selection[marker]].startTime = ((highSnapPoint + snappingDistance) - sequentialEndOffsets[marker]) - length;
							}
						}
					} else {
						float length = emotionData[currentMarker].endTime - emotionData[currentMarker].startTime;
						emotionData[currentMarker].startTime = ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond)) + startOffset;
						emotionData[currentMarker].endTime = emotionData[currentMarker].startTime + length;

						if (emotionData[currentMarker].startTime <= lowSnapPoint && emotionData[currentMarker].startTime >= lowSnapPoint - (snappingDistance * 2)) {
							emotionData[currentMarker].startTime = (lowSnapPoint - snappingDistance);
							emotionData[currentMarker].endTime = (lowSnapPoint - snappingDistance) + length;
						} else if (emotionData[currentMarker].endTime >= highSnapPoint && emotionData[currentMarker].endTime <= highSnapPoint + (snappingDistance * 2)) {
							emotionData[currentMarker].endTime = (highSnapPoint + snappingDistance);
							emotionData[currentMarker].startTime = (highSnapPoint + snappingDistance) - length;
						}
					}

					changed = true;
					previewOutOfDate = true;
				} else if (currentComponent == 2) {
					float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + endOffset;

					if (tempChange >= emotionData[currentMarker].startTime + 0.008f) {
						emotionData[currentMarker].endTime = tempChange;
					}

					changed = true;
					previewOutOfDate = true;
					emotionData[currentMarker].endTime = Mathf.Clamp01(emotionData[currentMarker].endTime);

					if (emotionData[currentMarker].endTime >= highSnapPoint && emotionData[currentMarker].endTime <= highSnapPoint + (snappingDistance * 2)) {
						emotionData[currentMarker].endTime = (highSnapPoint + snappingDistance);
					}
				} else if (currentComponent == 3) {
					emotionData[currentMarker].customBlendIn = true;
					changed = true;
					previewOutOfDate = true;

					float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset;
					emotionData[currentMarker].blendInTime = Mathf.Clamp(tempChange, 0, (emotionData[currentMarker].endTime - emotionData[currentMarker].startTime)+emotionData[currentMarker].blendOutTime);

					if (selection.Count > 1) {
						for (int m = 0; m < selection.Count; m++) {
							emotionData[selection[m]].customBlendIn = true;
							emotionData[selection[m]].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime, 0, (emotionData[selection[m]].endTime - emotionData[selection[m]].startTime) + emotionData[selection[m]].blendOutTime);
						}
					}
				} else if (currentComponent == 4) {
					changed = true;
					previewOutOfDate = true;
					emotionData[currentMarker].customBlendOut = true;

					float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + endOffset;
					emotionData[currentMarker].blendOutTime = Mathf.Clamp(tempChange, -(emotionData[currentMarker].endTime - emotionData[currentMarker].startTime) + emotionData[currentMarker].blendInTime, 0);

					if (selection.Count > 1) {
						for (int m = 0; m < selection.Count; m++) {
							emotionData[selection[m]].customBlendOut = true;
							emotionData[selection[m]].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime, -(emotionData[selection[m]].endTime - emotionData[selection[m]].startTime) + emotionData[selection[m]].blendInTime, 0);
						}
					}
				}
				FixEmotionBlends();
			}
		} else if (markerTab == 2) {
			if (dragging == true && Event.current.type == EventType.MouseDrag && currentMarker > -1) {
				gestureData[currentMarker].time = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);
				changed = true;
				previewOutOfDate = true;
			}
		}

		if (Event.current.type == EventType.MouseUp && dragging == true) {
			dragging = false;
			currentMarker = -1;
			emotionData.Sort(EmotionSort);
		}

		//Controls
		Rect bottomToolbarRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(bottomToolbarRect, "", EditorStyles.toolbar);
		timeSpan = TimeSpan.FromSeconds(seekPosition * fileLength);
		Char pad = '0';
		string minutes = timeSpan.Minutes.ToString().PadLeft(2, pad);
		string seconds = timeSpan.Seconds.ToString().PadLeft(2, pad);
		string milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3, pad);

		string currentTime = minutes + ":" + seconds + ":" + milliseconds;

		timeSpan = TimeSpan.FromSeconds(fileLength);
		minutes = timeSpan.Minutes.ToString().PadLeft(2, pad);
		seconds = timeSpan.Seconds.ToString().PadLeft(2, pad);
		milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3, pad);

		string totalTime = minutes + ":" + seconds + ":" + milliseconds;
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(new GUIContent(currentTime + " / " + totalTime, "Change Clip Length"), EditorStyles.toolbarTextField)) {
			ClipSettings();
		}
		GUILayout.FlexibleSpace();
		if (isPlaying) {
			if (isPaused) {
				if (GUILayout.Button(playIcon, EditorStyles.toolbarButton, GUILayout.Width(50))) {
					AudioUtility.ResumeClip(clip);
					isPaused = false;
					isPlaying = true;
				}
			} else {
				if (GUILayout.Button(pauseIcon, EditorStyles.toolbarButton, GUILayout.Width(50))) {
					AudioUtility.PauseClip(clip);
					isPaused = true;
				}
			}
		} else {
			if (GUILayout.Button(playIcon, EditorStyles.toolbarButton, GUILayout.Width(50))) {
				AudioUtility.PlayClip(clip);
				isPaused = false;
				isPlaying = true;
			}
		}
		if (GUILayout.Button(stopIcon, EditorStyles.toolbarButton, GUILayout.Width(50))) {
			isPaused = true;
			isPlaying = false;
			if (clip) AudioUtility.StopClip(clip);
			seekPosition = 0;
			oldSeekPosition = seekPosition;
			float vpDiff = viewportEnd - viewportStart;
			viewportStart = 0;
			viewportEnd = vpDiff;
		}
		looping = GUILayout.Toggle(looping, loopIcon, EditorStyles.toolbarButton, GUILayout.Width(50));

		GUILayout.FlexibleSpace();

		switch (markerTab) {
			case 0:
				if (GUILayout.Button("Add Phoneme", EditorStyles.toolbarButton, GUILayout.Width(160))) {
					GenericMenu phonemeMenu = new GenericMenu();

					for (int a = 0; a < 9; a++) {
						Phoneme phon = (Phoneme)a;
						phonemeMenu.AddItem(new GUIContent(phon.ToString()), false, PhonemePicked, new object[] { (Phoneme)a, seekPosition });
					}
					phonemeMenu.ShowAsContext();
				}

				GUILayout.Space(40);
				GUILayout.Box("Filters:", EditorStyles.label);
				filterMask = EditorGUILayout.MaskField(filterMask, new String[] { "AI", "E", "U", "O", "CDGKNRSThYZ", "FV", "L", "MBP", "WQ" }, EditorStyles.toolbarPopup, GUILayout.MaxWidth(100));
				break;
			case 1:
				if (GUILayout.Button("Add Emotion", EditorStyles.toolbarButton, GUILayout.Width(160))) {
					GenericMenu emotionMenu = new GenericMenu();

					for (int a = 0; a < settings.emotions.Length; a++) {
						string emote = settings.emotions[a];
						emotionMenu.AddItem(new GUIContent(emote), false, EmotionPicked, new object[] { emote, seekPosition });
					}
					emotionMenu.AddSeparator("");
					emotionMenu.AddItem(new GUIContent("Add New Emotion"), false, ShowProjectSettings);

					emotionMenu.ShowAsContext();
				}

				break;
			case 2:
				if (GUILayout.Button("Add Gesture", EditorStyles.toolbarButton, GUILayout.Width(160))) {
					GenericMenu gestureMenu = new GenericMenu();

					for (int a = 0; a < settings.gestures.Count; a++) {
						string gesture = settings.gestures[a];
						gestureMenu.AddItem(new GUIContent(gesture), false, GesturePicked, new object[] { gesture, seekPosition });
					}
					gestureMenu.AddSeparator("");
					gestureMenu.AddItem(new GUIContent("Add New Gesture"), false, ShowProjectSettings);

					gestureMenu.ShowAsContext();
				}

				break;
		}

		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(1);
	}

	//Context Menu Functions
	void PhonemePicked(object raw) {
		object[] data = (object[])raw;
		Phoneme picked = (Phoneme)data[0];
		float time = (float)data[1];

		Undo.RecordObject(this, "Add Phoneme Marker");
		phonemeData.Add(new PhonemeMarker(picked, time));
		changed = true;
		previewOutOfDate = true;
	}

	void EmotionPicked(object raw) {
		object[] data = (object[])raw;
		string picked = (string)data[0];
		float time = (float)data[1];

		Undo.RecordObject(this, "Add Emotion Marker");
		EmotionMarker newMarker = new EmotionMarker(picked, time, Mathf.Clamp(time + 0.1f, 0, 1), 0.025f, -0.025f, false, false, true, true);
		emotionData.Add(newMarker);
		emotionData.Sort(EmotionSort);
		unorderedEmotionData.Add(newMarker);
		int newMarkerIndex = emotionData.IndexOf(newMarker);
		emotionData[newMarkerIndex].endTime = Mathf.Clamp(emotionData[newMarkerIndex].endTime, emotionData[newMarkerIndex].startTime + 0.003f, 1);
		FixEmotionBlends();
		changed = true;
		previewOutOfDate = true;
	}

	void GesturePicked(object raw) {
		object[] data = (object[])raw;
		string picked = (string)data[0];
		float time = (float)data[1];

		Undo.RecordObject(this, "Add Gesture Marker");
		GestureMarker newMarker = new GestureMarker(picked, time);
		gestureData.Add(newMarker);
		changed = true;
	}

	void OnNewClick() {
		if (changed) {
			if (EditorUtility.DisplayDialog("Unsaved Data", "You have made changes to the current file, are you sure you want to clear it?", "Yes", "No")) {
				lastLoad = "";
				fileName = "Untitled";
				AudioUtility.StopAllClips();
				seekPosition = 0;
				oldSeekPosition = 0;
				clip = null;
				DestroyImmediate(waveform);
				phonemeData = new List<PhonemeMarker>();
				emotionData = new List<EmotionMarker>();
				unorderedEmotionData = new List<EmotionMarker>();
				oldClip = null;
				changed = false;
				fileLength = 10;
				transcript = "";
				previewOutOfDate = true;
				waveform = null;
				fileLength = 10;
			}
		} else {
			lastLoad = "";
			fileName = "Untitled";
			AudioUtility.StopAllClips();
			seekPosition = 0;
			oldSeekPosition = 0;
			clip = null;
			phonemeData = new List<PhonemeMarker>();
			emotionData = new List<EmotionMarker>();
			unorderedEmotionData = new List<EmotionMarker>();
			oldClip = null;
			changed = false;
			previewOutOfDate = true;
		}
	}

	void OnLoadClick() {
		string loadPath = EditorUtility.OpenFilePanel("Load LipSync Data File", "Assets", "asset");

		if (loadPath != "") {
			loadPath = "Assets" + loadPath.Substring(Application.dataPath.Length);
			LoadFile(loadPath);
		}
	}

	void OnSaveClick() {
		if (lastLoad != "") {
			string savePath = "Assets" + lastLoad + clip.name + ".asset";
			SaveFile(savePath, false);
			changed = false;
		} else {
			OnSaveAsClick();
		}
	}

	void OnSaveAsClick() {
		string defaultName = "New Asset";

		if (clip != null) {
			defaultName = clip.name;
		}

		string savePath = EditorUtility.SaveFilePanel("Save LipSync Data File", "Assets" + lastLoad, defaultName + ".asset", "asset");
		if (savePath != "") {
			savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
			SaveFile(savePath, false);
			changed = false;
			lastLoad = savePath;
		}
	}

	void OnUnityExport() {
		string savePath = EditorUtility.SaveFilePanel("Export LipSync Data and audio", "Assets" + lastLoad, clip.name + ".unitypackage", "unitypackage");
		if (savePath != "") {
			savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
			string folderPath = savePath.Remove(savePath.Length - Path.GetFileName(savePath).Length) + Path.GetFileNameWithoutExtension(savePath);
			AssetDatabase.CreateFolder(savePath.Remove(savePath.Length - (Path.GetFileName(savePath).Length + 1)), Path.GetFileNameWithoutExtension(savePath));

			string originalName = fileName;

			if (clip != null) {
				AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(clip), folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
				AssetDatabase.ImportAsset(folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
			}

			AudioClip newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
			AudioClip originalClip = clip;
			if (newClip != null) {
				clip = newClip;
			} else {
				Debug.Log("LipSync: AudioClip copy at " + folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)) + " could not be reloaded for compression. Proceding without AudioClip.");
			}

			SaveFile(folderPath + "/" + Path.ChangeExtension(Path.GetFileName(savePath), ".asset"), false);

			LipSyncData file = AssetDatabase.LoadAssetAtPath<LipSyncData>(folderPath + "/" + Path.ChangeExtension(Path.GetFileName(savePath), ".asset"));
			if (file != null) {
				AssetDatabase.ExportPackage(folderPath, savePath, ExportPackageOptions.Recurse);
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
				AssetDatabase.DeleteAsset(folderPath);

				fileName = originalName;
				lastLoad = "";
			} else {
				Debug.LogError("LipSync: File could not be reloaded for compression. Aborting Export.");
			}

			clip = originalClip;
		}
	}

	void OnXMLExport() {
		string savePath = EditorUtility.SaveFilePanel("Export LipSync Data to XML", "Assets" + lastLoad, clip.name + ".xml", "xml");
		if (savePath != "") {
			savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
			SaveFile(savePath, true);
		}
	}

	void OnXMLImport() {
		string xmlPath = EditorUtility.OpenFilePanel("Import LipSync Data from XML", "Assets" + lastLoad, "xml");
		string audioPath = EditorUtility.OpenFilePanel("Load AudioClip", "Assets" + lastLoad, "wav;*.mp3;*.ogg");

		if (xmlPath != "") {
			xmlPath = "Assets" + xmlPath.Substring(Application.dataPath.Length);
			audioPath = "Assets" + audioPath.Substring(Application.dataPath.Length);
			AudioClip linkedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
			TextAsset xmlFile = AssetDatabase.LoadAssetAtPath<TextAsset>(xmlPath);

			LoadXML(xmlFile, linkedClip);
		}
	}

	void SelectAll() {
		if (markerTab == 0) {
			if (phonemeData.Count > 0) {
				selection.Clear();
				firstSelection = 0;
				for (int marker = 0; marker < phonemeData.Count; marker++) {
					selection.Add(marker);
				}
			}
		} else if (markerTab == 1) {
			if (emotionData.Count > 0) {
				selection.Clear();
				firstSelection = 0;
				for (int marker = 0; marker < emotionData.Count; marker++) {
					selection.Add(marker);
				}
			}
		} else if (markerTab == 2) {
			if (gestureData.Count > 0) {
				selection.Clear();
				firstSelection = 0;
				for (int marker = 0; marker < gestureData.Count; marker++) {
					selection.Add(marker);
				}
			}
		}
	}

	void SelectNone() {
		selection.Clear();
		firstSelection = 0;
	}

	void InvertSelection() {
		if (markerTab == 0) {
			if (phonemeData.Count > 0) {
				List<int> tempSelection = new List<int>();
				for (int marker = 0; marker < phonemeData.Count; marker++) {
					if (!selection.Contains(marker)) {
						if (tempSelection.Count == 0) {
							firstSelection = marker;
						}
						tempSelection.Add(marker);
					}
				}
				selection = tempSelection;
			}
		} else if (markerTab == 1) {
			if (emotionData.Count > 0) {
				List<int> tempSelection = new List<int>();
				for (int marker = 0; marker < emotionData.Count; marker++) {
					if (!selection.Contains(marker)) {
						if (tempSelection.Count == 0) {
							firstSelection = marker;
						}
						tempSelection.Add(marker);
					}
				}
				selection = tempSelection;
			}
		} else if (markerTab == 2) {
			if (gestureData.Count > 0) {
				List<int> tempSelection = new List<int>();
				for (int marker = 0; marker < gestureData.Count; marker++) {
					if (!selection.Contains(marker)) {
						if (tempSelection.Count == 0) {
							firstSelection = marker;
						}
						tempSelection.Add(marker);
					}
				}
				selection = tempSelection;
			}
		}
	}

	void SetIntensitiesVolume() {
		SetIntensityWindow.CreateWindow(this, this);
	}

	void ResetIntensities () {
		if (markerTab == 0) {
			for (int m = 0; m < phonemeData.Count; m++) {
				phonemeData[m].intensity = 1;
			}
		} else if (markerTab == 1) {
			for (int m = 0; m < emotionData.Count; m++) {
				emotionData[m].intensity = 1;
			}
		}
	}

	void ClipSettings() {
		ClipSettingsWindow.CreateWindow(this, this);
	}

	void OpenURL(object url) {
		Application.OpenURL((string)url);
	}

	void ShowProjectSettings() {
		LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath("Assets/Rogo Digital/LipSync/ProjectSettings.asset", typeof(LipSyncProject));
		if (settings == null) {
			settings = ScriptableObject.CreateInstance<LipSyncProject>();

			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = new string[] { "default" };
			newSettings.emotionColors = new Color[] { new Color(1f, 0.7f, 0.1f) };

			EditorUtility.CopySerialized(newSettings, settings);
			AssetDatabase.CreateAsset(settings, "Assets/Rogo Digital/LipSync/ProjectSettings.asset");
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}
		Selection.activeObject = settings;
	}

	void ChangeMarkerPicked(object info) {
		Undo.RecordObject(this, "Change Phoneme Marker");
		List<int> finalInfo = (List<int>)info;

		PhonemeMarker marker = phonemeData[finalInfo[0]];
		marker.phoneme = (Phoneme)finalInfo[1];
		changed = true;
		previewOutOfDate = true;
	}

	void ChangeGesturePicked(object info) {
		Undo.RecordObject(this, "Change Gesture Marker");
		List<object> finalInfo = (List<object>)info;

		GestureMarker marker = gestureData[(int)finalInfo[0]];
		marker.gesture = (string)finalInfo[1];
		changed = true;
	}

	void PhonemeMarkerSettings(object info) {
		PhonemeMarker marker = (PhonemeMarker)info;
		highlightedMarker = phonemeData.IndexOf(marker);
		MarkerSettingsWindow.CreateWindow(this, this, marker);
	}

	void EmotionMarkerSettings(object info) {
		EmotionMarker marker = (EmotionMarker)info;
		highlightedMarker = emotionData.IndexOf(marker);
		MarkerSettingsWindow.CreateWindow(this, this, marker);
	}

	void RemoveMissingEmotions() {
		foreach (EmotionMarker marker in emotionData) {
			bool verified = false;
			foreach (string emotion in settings.emotions) {
				if (marker.emotion == emotion)
					verified = true;
			}

			if (!verified) {
				DeleteEmotion(marker);
			}
		}
	}

	void RemoveMissingGestures() {
		foreach (GestureMarker marker in gestureData) {
			bool verified = false;
			foreach (string gesture in settings.gestures) {
				if (marker.gesture == gesture)
					verified = true;
			}

			if (!verified) {
				DeleteGesture(marker);
			}
		}
	}

	void ChangeEmotionPicked(object info) {
		Undo.RecordObject(this, "Change Emotion Marker");
		List<object> finalInfo = (List<object>)info;

		EmotionMarker marker = (EmotionMarker)finalInfo[0];
		marker.emotion = (string)finalInfo[1];
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteMarker(object marker) {
		Undo.RecordObject(this, "Delete Phoneme Marker");
		phonemeData.Remove((PhonemeMarker)marker);
		selection.Clear();
		firstSelection = 0;
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteSelectedMarkers() {
		Undo.RecordObject(this, "Delete Phoneme Markers");
		selection.Sort(SortInt);
		for (int marker = selection.Count - 1; marker >= 0; marker--) {
			phonemeData.Remove((phonemeData[selection[marker]]));
		}
		selection.Clear();
		firstSelection = 0;
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteGesture(object marker) {
		Undo.RecordObject(this, "Delete Gesture Marker");
		gestureData.Remove((GestureMarker)marker);
		changed = true;
		selection.Clear();
		firstSelection = 0;
	}

	void DeleteSelectedGestures() {
		Undo.RecordObject(this, "Delete Gesture Markers");
		selection.Sort(SortInt);
		for (int marker = selection.Count - 1; marker >= 0; marker--) {
			gestureData.Remove((gestureData[selection[marker]]));
		}
		selection.Clear();
		firstSelection = 0;
		changed = true;
	}

	void DeleteEmotion(object marker) {
		Undo.RecordObject(this, "Delete Emotion Marker");
		currentMarker = emotionData.IndexOf((EmotionMarker)marker);
		changed = true;
		previewOutOfDate = true;
		selection.Clear();
		firstSelection = 0;

		if (currentMarker - 1 > -1) {
			if (emotionData[currentMarker - 1].endTime == emotionData[currentMarker].startTime) {
				previousMarker = emotionData[currentMarker - 1];
				previousMarker.blendOutTime = 0;
				previousMarker.blendToMarker = false;
			}
		}
		if (currentMarker + 1 < emotionData.Count) {
			if (emotionData[currentMarker + 1].startTime == emotionData[currentMarker].endTime) {
				nextMarker = emotionData[currentMarker + 1];
				nextMarker.blendInTime = 0;
				nextMarker.blendFromMarker = false;
			}
		}
		currentMarker = -1;
		emotionData.Remove((EmotionMarker)marker);
		unorderedEmotionData.Remove((EmotionMarker)marker);
		FixEmotionBlends();
	}

	void DeleteSelectedEmotions() {
		Undo.RecordObject(this, "Delete Emotion Markers");
		selection.Sort(SortInt);

		for (int marker = selection.Count - 1; marker >= 0; marker--) {
			currentMarker = selection[marker];

			if (currentMarker - 1 > -1) {
				if (emotionData[currentMarker - 1].endTime == emotionData[currentMarker].startTime) {
					previousMarker = emotionData[currentMarker - 1];
					previousMarker.blendOutTime = 0;
					previousMarker.blendToMarker = false;
				}
			}
			if (currentMarker + 1 < emotionData.Count) {
				if (emotionData[currentMarker + 1].startTime == emotionData[currentMarker].endTime) {
					nextMarker = emotionData[currentMarker + 1];
					nextMarker.blendInTime = 0;
					nextMarker.blendFromMarker = false;
				}
			}

			unorderedEmotionData.Remove(emotionData[selection[marker]]);
			emotionData.RemoveAt(selection[marker]);
		}

		currentMarker = -1;
		FixEmotionBlends();

		selection.Clear();
		firstSelection = 0;
		changed = true;
		previewOutOfDate = true;
	}

	void Update() {
		float deltaTime = Time.realtimeSinceStartup - prevTime;
		prevTime = Time.realtimeSinceStartup;

		if (looping && isPlaying) {
			if (AudioUtility.GetClipPosition(clip) > fileLength - 0.05f) {
				AudioUtility.SetClipSamplePosition(clip, 0);
				seekPosition = 0;
				oldSeekPosition = 0;
				AudioUtility.PlayClip(clip);
			}
		}

		if (clip != null) {
			isPlaying = AudioUtility.IsClipPlaying(clip);
		}

		if (isPlaying && !isPaused) {
			if ((seekPosition * (fileLength * (position.width / (viewportEnd - viewportStart)))) > position.width + (viewportStart * (position.width / (viewportEnd - viewportStart)))) {
				float viewportSeconds = viewportEnd - viewportStart;
				viewportStart = seekPosition * fileLength;
				viewportEnd = viewportStart + viewportSeconds;
			} else if ((seekPosition * (fileLength * (position.width / (viewportEnd - viewportStart)))) < viewportStart * (position.width / (viewportEnd - viewportStart))) {
				float viewportSeconds = viewportEnd - viewportStart;
				viewportStart = seekPosition * fileLength;
				viewportEnd = viewportStart + viewportSeconds;
			}
		}

		//Check for clip change
		if (oldClip != clip) {
			Undo.RecordObject(this, "Change AudioClip");
			DestroyImmediate(waveform);
			oldClip = clip;
			if (setViewportOnLoad) {
				if (clip) fileLength = clip.length;
				viewportEnd = fileLength;
				viewportStart = 0;
			}
		}

		//Check for resize;
		if (oldPos.width != this.position.width || oldPos.height != this.position.height) {
			oldPos = this.position;
			if (clip) DestroyImmediate(waveform);
		}

		//Check for Seek Position change
		if (oldSeekPosition != seekPosition) {
			oldSeekPosition = seekPosition;
			if (!isPlaying || isPaused) {
				if (!previewing && clip != null) {
					AudioUtility.PlayClip(clip);
				}
				previewing = true;
				stopTimer = scrubLength;
				prevTime = Time.realtimeSinceStartup;
				resetTime = seekPosition;

				FixEmotionBlends();

				if (clip) AudioUtility.SetClipSamplePosition(clip, (int)(seekPosition * AudioUtility.GetSampleCount(clip)));
			}
		}

		if (isPlaying && !isPaused && clip != null && focusedWindow == this || continuousUpdate && focusedWindow == this) {
			this.Repaint();
		}

		if (clip != null) {
			seekPosition = AudioUtility.GetClipPosition(clip) / fileLength;
		} else if (isPlaying && !isPaused) {
			seekPosition += deltaTime/fileLength;
			if (seekPosition >= 1) {
				isPlaying = false;
				seekPosition = 0;
			}
		}
		
		oldSeekPosition = seekPosition;

		if (previewing) {

			stopTimer -= deltaTime;

			if (stopTimer <= 0) {
				previewing = false;
				isPaused = true;
				seekPosition = resetTime;
				oldSeekPosition = seekPosition;
				if (clip != null) {
					AudioUtility.PauseClip(clip);
					AudioUtility.SetClipSamplePosition(clip, (int)(seekPosition * AudioUtility.GetSampleCount(clip)));
				}
			}
		}

		if (waveform == null && waveformHeight > 0 && clip != null) {
			waveform = AudioUtility.GetWaveForm(clip, 0, (int)(fileLength * (position.width / (viewportEnd - viewportStart))), (position.height - waveformHeight) - 18);
			Repaint();
		}

		if (isPlaying && !isPaused && visualPreview || previewing && visualPreview) {
			UpdatePreview(seekPosition);
		}
	}

	[MenuItem("Window/Rogo Digital/LipSync Pro/Open Clip Editor %&a", false, 11)]
	public static LipSyncClipSetup ShowWindow() {
		return ShowWindow("", false, "", "", 0, 0);
	}

	public static LipSyncClipSetup ShowWindow(string loadPath, bool newWindow) {
		return ShowWindow(loadPath, newWindow, "", "", 0, 0);
	}

	public static LipSyncClipSetup ShowWindow(string loadPath, bool newWindow, string oldFileName, string oldLastLoad, int oldMarkerTab, float oldSeekPosition) {
		LipSyncClipSetup window;

		UnityEngine.Object[] current = Selection.GetFiltered(typeof(AudioClip), SelectionMode.Assets);

		if (newWindow) {
			window = ScriptableObject.CreateInstance<LipSyncClipSetup>();
			window.Show();
		} else {
			window = EditorWindow.GetWindow<LipSyncClipSetup>();
		}

		if (current.Length > 0) {
			window.clip = (AudioClip)current[0];
			window.fileLength = window.clip.length;
			window.waveform = null;
		} else if (loadPath == "") {
			current = Selection.GetFiltered(typeof(LipSyncData), SelectionMode.Assets);
			if (current.Length > 0) {
				loadPath = AssetDatabase.GetAssetPath(current[0]);
			}
		}

		if (EditorPrefs.GetBool("LipSync_ShowExtensionsOnLoad", true)) {
			EditorWindow.GetWindow<RDExtensionWindow>("Extensions", false, typeof(LipSyncClipSetup));
			RDExtensionWindow.ShowWindow("LipSync");
		}

		window.Focus();

		if (window.changed) {
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");
			if (choice != 2) {
				if (choice == 0) {
					window.OnSaveClick();
				}

				window.changed = false;
				window.fileName = "Untitled";
				window.oldClip = window.clip;
				window.phonemeData = new List<PhonemeMarker>();
				window.emotionData = new List<EmotionMarker>();
				window.unorderedEmotionData = new List<EmotionMarker>();
				window.gestureData = new List<GestureMarker>();

				window.seekPosition = 0;
				AudioUtility.StopAllClips();
				window.currentMarker = -1;

				if (loadPath != "") {
					window.LoadFile(loadPath);
					window.previewOutOfDate = true;
				}
			} else {
				window.clip = window.oldClip;
			}
		} else {
			window.oldClip = window.clip;
			window.fileName = "Untitled";
			window.phonemeData = new List<PhonemeMarker>();
			window.emotionData = new List<EmotionMarker>();
			window.unorderedEmotionData = new List<EmotionMarker>();
			window.gestureData = new List<GestureMarker>();

			window.seekPosition = 0;
			AudioUtility.StopAllClips();
			window.currentMarker = -1;

			if (loadPath != "") {
				window.LoadFile(loadPath);
				window.previewOutOfDate = true;
			}
		}


		if (EditorGUIUtility.isProSkin) {
			window.windowIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/icon.png");
		} else {
			window.windowIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/icon.png");
		}

		window.titleContent = new GUIContent("LipSync", window.windowIcon);
		window.minSize = new Vector2(700, 200);
		if (newWindow) {
			window.changed = true;
			window.lastLoad = oldLastLoad;
			window.fileName = oldFileName;
			window.markerTab = oldMarkerTab;
			window.seekPosition = oldSeekPosition;
			window.oldSeekPosition = oldSeekPosition;
			if(window.clip != null) AudioUtility.SetClipSamplePosition(window.clip, (int)(window.seekPosition * AudioUtility.GetSampleCount(window.clip)));
			AssetDatabase.DeleteAsset("Assets/Rogo Digital/LipSync/AUTOSAVE.asset");
		}

		if (EditorPrefs.GetBool("LipSync_SetViewportOnLoad", true)) {
			if (window.clip != null) {
				window.viewportEnd = window.fileLength;
			}
			window.viewportStart = 0;
		}

		return window;
	}

	void LoadFile(string path) {
		if (changed) {
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");

			if (choice == 1) {
				OnSaveClick();
			} else if (choice == 2) {
				return;
			}
		}
		LipSyncData file = (LipSyncData)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncData));

		if(file.clip != null) clip = file.clip;
		oldClip = clip;
		if (clip != null)  waveform = AudioUtility.GetWaveForm(clip, 0, (int)(fileLength * (position.width / (viewportEnd - viewportStart))), (position.height - waveformHeight) - 18);
		fileName = file.name + ".Asset";

		fileLength = file.length;
		if(file.transcript != null) transcript = file.transcript;

		if (setViewportOnLoad) {
			if (clip != null) fileLength = clip.length;
			viewportEnd = fileLength;
			viewportStart = 0;
		}

		phonemeData = new List<PhonemeMarker>();
		foreach (PhonemeMarker marker in file.phonemeData) {
			phonemeData.Add(new PhonemeMarker(marker.phoneme, marker.time));
		}

		emotionData = new List<EmotionMarker>();
		unorderedEmotionData = new List<EmotionMarker>();
		foreach (EmotionMarker marker in file.emotionData) {
			EmotionMarker newMarker = new EmotionMarker(marker.emotion, marker.startTime, marker.endTime, marker.blendInTime, marker.blendOutTime, marker.blendToMarker, marker.blendFromMarker, marker.customBlendIn, marker.customBlendOut);
			emotionData.Add(newMarker);
			unorderedEmotionData.Add(newMarker);
		}

		gestureData = new List<GestureMarker>();
		if (file.gestureData != null) {
			foreach (GestureMarker marker in file.gestureData) {
				gestureData.Add(new GestureMarker(marker.gesture, marker.time));
			}
		}

		currentMarker = -1;
		previewOutOfDate = true;
		changed = false;

		if (file.version < version) {
			UpdateFile(file.version);
		}

		FixEmotionBlends();

		string[] pathParts = path.Split(new string[] { "/" }, StringSplitOptions.None);
		lastLoad = path.Remove(path.Length - pathParts[pathParts.Length - 1].Length).Substring(6);
	}

	private void LoadXML(TextAsset xmlFile, AudioClip linkedClip) {
		XmlDocument document = new XmlDocument();
		document.LoadXml(xmlFile.text);

		// Clear/define marker lists, to overwrite any previous file
		phonemeData = new List<PhonemeMarker>();
		emotionData = new List<EmotionMarker>();
		unorderedEmotionData = new List<EmotionMarker>();
		gestureData = new List<GestureMarker>();

		clip = linkedClip;

		//Create Dictionary for loading phonemes
		Dictionary<string, Phoneme> phonemeLookup = new Dictionary<string, Phoneme>() {
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

		float fileVersion = 0;

		if (LipSync.ReadXML(document, "LipSyncData", "version") == null) {
			// Update data
			if (fileLength == 0) {
				fileLength = clip.length;
			}
		} else {
			fileVersion = float.Parse(LipSync.ReadXML(document, "LipSyncData", "version"));
			fileLength = float.Parse(LipSync.ReadXML(document, "LipSyncData", "length"));
		}

		//Phonemes
		XmlNode phonemesNode = document.SelectSingleNode("//LipSyncData//phonemes");
		if (phonemesNode != null) {
			XmlNodeList phonemeNodes = phonemesNode.ChildNodes;

			for (int p = 0; p < phonemeNodes.Count; p++) {
				XmlNode node = phonemeNodes[p];

				if (node.LocalName == "marker") {
					Phoneme phoneme = (Phoneme)phonemeLookup[node.Attributes["phoneme"].Value];
					float time = float.Parse(node.Attributes["time"].Value) / fileLength;

					phonemeData.Add(new PhonemeMarker(phoneme, time));
				}
			}
		}

		//Emotions
		XmlNode emotionsNode = document.SelectSingleNode("//LipSyncData//emotions");
		if (phonemesNode != null) {
			XmlNodeList emotionNodes = emotionsNode.ChildNodes;

			for (int p = 0; p < emotionNodes.Count; p++) {
				XmlNode node = emotionNodes[p];

				if (node.LocalName == "marker") {
					string emotion = node.Attributes["emotion"].Value;
					float startTime = float.Parse(node.Attributes["start"].Value) / fileLength;
					float endTime = float.Parse(node.Attributes["end"].Value) / fileLength;
					float blendInTime = float.Parse(node.Attributes["blendIn"].Value);
					float blendOutTime = float.Parse(node.Attributes["blendOut"].Value);
					bool blendTo = bool.Parse(node.Attributes["blendToMarker"].Value);
					bool blendFrom = bool.Parse(node.Attributes["blendFromMarker"].Value);
					bool customBlendIn = bool.Parse(node.Attributes["customBlendIn"].Value);
					bool customBlendOut = bool.Parse(node.Attributes["customBlendOut"].Value);

					EmotionMarker newMarker = new EmotionMarker(emotion, startTime, endTime, blendInTime, blendOutTime, blendTo, blendFrom, customBlendIn, customBlendOut);
					emotionData.Add(newMarker);
					unorderedEmotionData.Add(newMarker);
				}
			}
		}

		//Gestures
		XmlNode gesturesNode = document.SelectSingleNode("//LipSyncData//gestures");
		if (gesturesNode != null) {
			XmlNodeList gestureNodes = gesturesNode.ChildNodes;

			for (int p = 0; p < gestureNodes.Count; p++) {
				XmlNode node = gestureNodes[p];

				if (node.LocalName == "marker") {
					string gesture = node.Attributes["gesture"].Value;
					float time = float.Parse(node.Attributes["time"].Value) / fileLength;

					gestureData.Add(new GestureMarker(gesture, time));
				}
			}
		}

		phonemeData.Sort(LipSync.SortTime);
		gestureData.Sort(LipSync.SortTime);
		FixEmotionBlends();

		if (fileVersion < version) {
			UpdateFile(fileVersion);
		}
	}

	void SaveFile(string path, bool isXML) {
		if (isXML) {
			XmlWriterSettings settings = new XmlWriterSettings { Indent = true, IndentChars = "\t" };
			XmlWriter writer = XmlWriter.Create(path, settings);

			writer.WriteStartDocument();

			//Header
			writer.WriteComment("Exported from RogoDigital LipSync " + versionNumber + ". Exported at " + DateTime.Now.ToString());
			writer.WriteComment("Note: This format cannot directly reference the linked AudioClip like a LipSyncData asset can. It is advised that you use that format instead unless you need to process the data further outside of Unity.");

			writer.WriteStartElement("LipSyncData");
			writer.WriteElementString("version", version.ToString());
			writer.WriteElementString("transcript", transcript);
			writer.WriteElementString("length", fileLength.ToString());
			//Data
			writer.WriteStartElement("phonemes");
			foreach (PhonemeMarker marker in phonemeData) {
				writer.WriteStartElement("marker");

				writer.WriteAttributeString("time", (marker.time * fileLength).ToString());
				writer.WriteAttributeString("phoneme", marker.phoneme.ToString());

				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteStartElement("emotions");
			foreach (EmotionMarker marker in emotionData) {
				writer.WriteStartElement("marker");
				writer.WriteAttributeString("start", (marker.startTime * fileLength).ToString());
				writer.WriteAttributeString("end", (marker.endTime * fileLength).ToString());
				writer.WriteAttributeString("blendFromMarker", (marker.blendFromMarker ? "true" : "false"));
				writer.WriteAttributeString("blendToMarker", (marker.blendToMarker ? "true" : "false"));
				writer.WriteAttributeString("blendIn", marker.blendInTime.ToString());
				writer.WriteAttributeString("blendOut", marker.blendOutTime.ToString());
				writer.WriteAttributeString("customBlendIn", marker.customBlendIn.ToString());
				writer.WriteAttributeString("customBlendOut", marker.customBlendOut.ToString());

				writer.WriteAttributeString("emotion", marker.emotion);
				writer.WriteEndElement();
			}

			writer.WriteStartElement("gestures");
			foreach (GestureMarker marker in gestureData) {
				writer.WriteStartElement("marker");

				writer.WriteAttributeString("time", (marker.time * fileLength).ToString());
				writer.WriteAttributeString("gesture", marker.gesture);

				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteEndDocument();
			writer.Close();
			AssetDatabase.Refresh();
		} else {
			LipSyncData file = ScriptableObject.CreateInstance<LipSyncData>();
			file.phonemeData = phonemeData.ToArray();
			file.emotionData = emotionData.ToArray();
			file.gestureData = gestureData.ToArray();
			file.version = version;
			file.clip = clip;
			file.length = fileLength;
			file.transcript = transcript;

			LipSyncData outputFile = (LipSyncData)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncData));

			if (outputFile != null) {
				EditorUtility.CopySerialized(file, outputFile);
				AssetDatabase.SaveAssets();
			} else {
				outputFile = ScriptableObject.CreateInstance<LipSyncData>();
				EditorUtility.CopySerialized(file, outputFile);
				AssetDatabase.CreateAsset(outputFile, path);
			}

			fileName = outputFile.name + ".Asset";
			DestroyImmediate(file);
			AssetDatabase.Refresh();
		}
	}

	void UpdatePreview(float time) {
		if (previewTarget != null) {
			if (previewTarget.blendSystem != null) {
				if (previewTarget.blendSystem.isReady) {
					if (previewOutOfDate) {
						previewTarget.TempLoad(phonemeData, emotionData, clip, fileLength);
						previewTarget.ProcessData();
						previewOutOfDate = false;
					}

					previewTarget.PreviewAtTime(time);
					EditorUtility.SetDirty(previewTarget.blendSystem);
				}
			}
		}
	}

	//Save Changes
	void OnDestroy() {
		AudioUtility.StopAllClips();
		SceneView.onSceneGUIDelegate -= OnSceneGUI;

		if (changed) {
			string oldName = fileName;
			string oldLastLoad = lastLoad;
			float localOldSeekPosition = seekPosition; ;
			SaveFile("Assets/Rogo Digital/LipSync/AUTOSAVE.asset", false);
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");

			if (choice == 0) {
				OnSaveClick();
				AssetDatabase.DeleteAsset("Assets/Rogo Digital/LipSync/AUTOSAVE.asset");
			} else if (choice == 1) {
				AssetDatabase.DeleteAsset("Assets/Rogo Digital/LipSync/AUTOSAVE.asset");
			} else {
				ShowWindow("Assets/Rogo Digital/LipSync/AUTOSAVE.asset", true, oldName, oldLastLoad, markerTab, localOldSeekPosition);
			}
		}
	}

	public void StartAutoSync() {
		if (languageModelNames.Length > 0) {
			AutoSync.ProcessAudio(clip, languageModelNames[defaultLanguageModel], OnAutoSyncDataReady, EditorPrefs.GetBool("LipSync_SoXAvailable", false));
		} else {
			Debug.Log("AutoSync Failed: Language Model could not be loaded. (Check Clip Editor settings)");
		}
	}

	void OnAutoSyncDataReady(AudioClip clip, List<PhonemeMarker> markers) {
		if (markers.Count > 0) {
			phonemeData = markers;
			changed = true;
			previewOutOfDate = true;
		}
	}

	public void StartAutoSyncText(object mode) {
		AutoSyncWindow.CreateWindow(this, this, (int)mode);
	}

	void UpdateFile(float version) {
		switch (version.ToString()) {
			default:
				// Pre 1.0 - Update Emotion Markers
				emotionData.Sort(EmotionSort);
				for (int e = 0; e < emotionData.Count; e++) {
					if (emotionData[e].blendFromMarker) {
						emotionData[e].startTime -= emotionData[e].blendInTime;
						emotionData[e - 1].endTime += emotionData[e].blendInTime;
					} else {
						emotionData[e].customBlendIn = true;
					}

					if (emotionData[e].blendToMarker) {
						emotionData[e + 1].startTime -= emotionData[e].blendOutTime;
						emotionData[e].endTime += emotionData[e].blendOutTime;
					} else {
						emotionData[e].customBlendOut = true;
						emotionData[e].blendOutTime = -emotionData[e].blendOutTime;
					}
				}
				FixEmotionBlends();

				previewOutOfDate = true;
				changed = true;
				break;
		}
		Repaint();
		EditorUtility.DisplayDialog("Loading Old File", "This file was created in an old version of LipSync. It has been automatically updated to work with the current version, but the original file has not been overwritten.", "Ok");
	}

	public bool MinMaxScrollbar(Rect position, Rect viewportRect, ref float minValue, ref float maxValue, float minLimit, float maxLimit , float minThumbSize) {
		float thumbWidth = (maxValue - minValue) / (maxLimit - minLimit);
		Rect thumbRect = new Rect((position.x + 32) + ((position.width - 64) * (minValue / maxLimit)), position.y, (position.width - 64) * thumbWidth, position.height);
		Rect thumbLeftRect = new Rect(thumbRect.x - 15, thumbRect.y, 15, thumbRect.height);
		Rect thumbRightRect = new Rect(thumbRect.x + thumbRect.width, thumbRect.y, 15, thumbRect.height);

		// Draw Dummy Scrollbar
		GUI.Box(new Rect(position.x + 17, position.y, position.width - 34, position.height), "", (GUIStyle)"horizontalScrollbar");

		if (GUI.Button(new Rect(position.x, position.y, 17, position.height), "", (GUIStyle)"horizontalScrollbarLeftButton")) {
			float size = maxValue - minValue;
			minValue -= 0.2f;
			maxValue = minValue + size;

			if (minValue < minLimit) {
				minValue = minLimit;
				maxValue = minValue + size;
			}
		}

		if (GUI.Button(new Rect((position.x + position.width) - 17, position.y, 17, position.height), "", (GUIStyle)"horizontalScrollbarRightButton")) {
			float size = maxValue - minValue;
			minValue += 0.2f;
			maxValue = minValue + size;

			if (maxValue > maxLimit) {
				maxValue = maxLimit;
				minValue = maxValue - size;
			}
		}

		GUI.Box(new Rect(thumbRect.x - 15, thumbRect.y, thumbRect.width + 30, thumbRect.height), "", (GUIStyle)"HorizontalMinMaxScrollbarThumb");

		// Logic
		if (Event.current.type == EventType.MouseDown && draggingScrollbar == 0) {
			if (thumbRect.Contains(Event.current.mousePosition)) {
				draggingScrollbar = 1;
				scrollbarStartOffset = Event.current.mousePosition.x - thumbRect.x + 32;
				Event.current.Use();
			} else if (thumbLeftRect.Contains(Event.current.mousePosition)) {
				draggingScrollbar = 2;
				scrollbarStartOffset = Event.current.mousePosition.x - thumbLeftRect.x + 17;
				Event.current.Use();
			} else if (thumbRightRect.Contains(Event.current.mousePosition)) {
				draggingScrollbar = 3;
				scrollbarStartOffset = Event.current.mousePosition.x - thumbRightRect.x + 32;
				Event.current.Use();
			}
		}

		if (Event.current.type == EventType.MouseDrag && draggingScrollbar == 1) {
			float size = maxValue - minValue;
			minValue = ((Event.current.mousePosition.x - scrollbarStartOffset) / (position.width - 64)) * maxLimit;
			maxValue = minValue + size;

			if (minValue < minLimit) {
				minValue = minLimit;
				maxValue = minValue + size;
			} else if (maxValue > maxLimit) {
				maxValue = maxLimit;
				minValue = maxValue - size;
			}

			Event.current.Use();
		} else if (Event.current.type == EventType.MouseDrag && draggingScrollbar == 2) {
			minValue = ((Event.current.mousePosition.x - scrollbarStartOffset) / (position.width - 64)) * maxLimit;

			if (minValue < minLimit) {
				minValue = minLimit;
			}
			minValue = Mathf.Clamp(minValue, 0, maxValue - minThumbSize);

			Event.current.Use();
		} else if (Event.current.type == EventType.MouseDrag && draggingScrollbar == 3) {
			maxValue = ((Event.current.mousePosition.x - scrollbarStartOffset) / (position.width - 64)) * maxLimit;

			if (maxValue > maxLimit) {
				maxValue = maxLimit;
			}
			maxValue = Mathf.Clamp(maxValue, minValue + minThumbSize, fileLength);
			
			Event.current.Use();
		}

		if (Event.current.type == EventType.ScrollWheel && viewportRect.Contains(Event.current.mousePosition)) {
			float viewportSeconds = (maxValue - minValue);
			float pixelsPerSecond = viewportRect.width / viewportSeconds;

			var pointerTimeA = minValue + (Event.current.mousePosition.x / pixelsPerSecond);
			var t = (Mathf.Abs(Event.current.delta.y * 25) / viewportRect.width) * viewportSeconds;

			minValue += Event.current.delta.y > 0 ? -t : t;
			maxValue += Event.current.delta.y > 0 ? t : -t;
			
			var pointerTimeB = minValue + (Event.current.mousePosition.x / pixelsPerSecond);
			var diff = pointerTimeA - pointerTimeB;

			minValue += diff;
			maxValue += diff;

			minValue = Mathf.Clamp(minValue, 0, maxValue - minThumbSize);
			maxValue = Mathf.Clamp(maxValue, minValue + minThumbSize, fileLength);

			Event.current.Use();
		}

		if (Event.current.type == EventType.MouseUp && draggingScrollbar > 0) {
			draggingScrollbar = 0;
			Event.current.Use();
			return true;
		}

		return false;
	}

	void FixEmotionBlends() {
		unorderedEmotionData.Sort(EmotionSortSize);

		foreach (EmotionMarker eMarker in emotionData) {
			eMarker.blendFromMarker = false;
			eMarker.blendToMarker = false;
			if (!eMarker.customBlendIn) eMarker.blendInTime = 0;
			if (!eMarker.customBlendOut) eMarker.blendOutTime = 0;
			eMarker.invalid = false;
		}

		foreach (EmotionMarker eMarker in emotionData) {
			foreach (EmotionMarker tMarker in emotionData) {
				if (eMarker != tMarker) {
					if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime) {
						if (eMarker.customBlendIn) {
							eMarker.customBlendIn = false;
							FixEmotionBlends();
							return;
						}
						eMarker.blendFromMarker = true;

						if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime) {
							eMarker.invalid = true;
						} else {
							eMarker.blendInTime = tMarker.endTime - eMarker.startTime;
						}
					}

					if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime) {
						if (eMarker.customBlendOut) {
							eMarker.customBlendOut = false;
							FixEmotionBlends();
							return;
						}
						eMarker.blendToMarker = true;

						if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime) {
							eMarker.invalid = true;
						} else {
							eMarker.blendOutTime = tMarker.startTime - eMarker.endTime;
						}
					}
				}
			}
		}
	}

	static int EmotionSort(EmotionMarker a, EmotionMarker b) {
		return a.startTime.CompareTo(b.startTime);
	}

	static int EmotionSortSize(EmotionMarker a, EmotionMarker b) {
		float aLength = a.endTime - a.startTime;
		float bLength = b.endTime - b.startTime;

		return bLength.CompareTo(aLength);
	}

	static int SortInt(int a, int b) {
		return a.CompareTo(b);
	}

	static Color HexToColor(int color) {
		string hex = color.ToString("X").PadLeft(6, (char)'0');

		int R = Convert.ToInt32(hex.Substring(0, 2), 16);
		int G = Convert.ToInt32(hex.Substring(2, 2), 16);
		int B = Convert.ToInt32(hex.Substring(4, 2), 16);
		return new Color(R / 255f, G / 255f, B / 255f);
	}

	static int ColorToHex(Color color) {
		string R = ((int)(color.r * 255)).ToString("X").PadLeft(2, (char)'0');
		string G = ((int)(color.g * 255)).ToString("X").PadLeft(2, (char)'0');
		string B = ((int)(color.b * 255)).ToString("X").PadLeft(2, (char)'0');

		string hex = R + G + B;
		return Convert.ToInt32(hex, 16);
	}

	static Color Darken(Color color, float amount) {
		return new Color(color.r * amount, color.g * amount, color.b * amount);
	}
}