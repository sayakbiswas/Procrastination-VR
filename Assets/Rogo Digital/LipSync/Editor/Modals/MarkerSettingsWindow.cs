using UnityEngine;
using UnityEditor;
using System.Collections;
using RogoDigital;

namespace RogoDigital.Lipsync {
	public class MarkerSettingsWindow : ModalWindow {
		private LipSyncClipSetup setup;
		private int markerType;
		private PhonemeMarker pMarker;
		private EmotionMarker eMarker;

		private float time;
		private float startTime;
		private float endTime;
		private Phoneme phoneme;
		private string emotion;
		private float intensity;

		void OnGUI () {
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(markerType == 0) {
				GUILayout.Label("Editing "+pMarker.phoneme.ToString()+" Phoneme Marker at "+(pMarker.time*setup.fileLength).ToString()+"s.");
			}else {
				GUILayout.Label("Editing " + eMarker.emotion + " Emotion Marker at " + (eMarker.startTime * setup.fileLength).ToString() + "s.");
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
			if (markerType == 0) {
				time = EditorGUILayout.FloatField("Marker Time" , time);
				phoneme = (Phoneme)EditorGUILayout.EnumPopup("Phoneme" , phoneme);
				GUILayout.Space(10);
				intensity = EditorGUILayout.Slider("Intensity" , intensity , 0 , 1);
			}else{
				startTime = EditorGUILayout.FloatField("Start Time" , startTime);
				endTime = EditorGUILayout.FloatField("End Time" , endTime);
				//emotion = EditorGUILayout.Popup("Emotion" , emotion);
				GUILayout.Space(10);
				intensity = EditorGUILayout.Slider("Intensity" , intensity , 0 , 1);
			}
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Accept" , GUILayout.MinWidth(100) , GUILayout.Height(20))) {
				if (markerType == 0) {
					pMarker.time = time;
					pMarker.phoneme = phoneme;
					pMarker.intensity = intensity;
				}else{
					eMarker.startTime = startTime;
					eMarker.endTime = endTime;
					eMarker.emotion = emotion;
					eMarker.intensity = intensity;
				}
				setup.changed = true;
				setup.previewOutOfDate = true;
				Close();
			}
			GUILayout.Space(10);
			if(GUILayout.Button("Cancel" , GUILayout.MinWidth(100) , GUILayout.Height(20))) {
				Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		public static void CreateWindow (ModalParent parent , LipSyncClipSetup setup , PhonemeMarker marker) {
			MarkerSettingsWindow window = Create (parent, setup, 0);
			window.pMarker = marker;

			window.time = marker.time;
			window.phoneme = marker.phoneme;
			window.intensity = marker.intensity;
		}

		public static void CreateWindow (ModalParent parent , LipSyncClipSetup setup , EmotionMarker marker) {
			MarkerSettingsWindow window = Create (parent, setup, 1);
			window.eMarker = marker;

			window.startTime = marker.startTime;
			window.endTime = marker.endTime;
			window.emotion = marker.emotion;
			window.intensity = marker.intensity;
		}

		private static MarkerSettingsWindow Create (ModalParent parent , LipSyncClipSetup setup , int markerType) {
			MarkerSettingsWindow window = CreateInstance<MarkerSettingsWindow>();

			window.position = new Rect(parent.center.x-250 , parent.center.y-100 , 500 , 200);
			window.minSize = new Vector2(500,200);
			window.titleContent = new GUIContent("Marker Settings");

			window.setup = setup;
			window.markerType = markerType;
			window.Show(parent);
			return window;
		}
	}
}