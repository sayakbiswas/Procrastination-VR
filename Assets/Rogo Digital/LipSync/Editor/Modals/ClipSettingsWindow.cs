using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using RogoDigital;

namespace RogoDigital.Lipsync {
	public class ClipSettingsWindow : ModalWindow {
		private LipSyncClipSetup setup;

		private float length;
		private string transcript;
		private Vector2 scroll;

		void OnGUI() {
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Clip Settings" , EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			scroll = GUILayout.BeginScrollView(scroll);
			TimeSpan time = TimeSpan.FromSeconds(length);

			int minutes = time.Minutes;
			int seconds = time.Seconds;
			int milliseconds = time.Milliseconds;

			GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
			EditorGUI.BeginDisabledGroup(setup.clip != null);
			GUILayout.Label("Duration");
			minutes = EditorGUILayout.IntField(minutes);
			GUILayout.Label("m" , EditorStyles.miniLabel);
			seconds = EditorGUILayout.IntField(seconds);
			GUILayout.Label("s", EditorStyles.miniLabel);
			milliseconds = EditorGUILayout.IntField(milliseconds);
			GUILayout.Label("ms", EditorStyles.miniLabel);
			EditorGUI.EndDisabledGroup();	
			GUILayout.EndHorizontal();
			if (setup.clip != null) EditorGUILayout.HelpBox("File duration matches AudioClip duration when a clip is set.", MessageType.Info);
			length = (minutes * 60) + seconds + ((milliseconds)/1000f);

			GUILayout.Space(10);
			GUILayout.Label("Transcript");
			transcript = GUILayout.TextArea(transcript , GUILayout.MinHeight(90));

			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Accept", GUILayout.MinWidth(100), GUILayout.Height(20))) {
				setup.fileLength = length;
				setup.transcript = transcript;

				setup.changed = true;
				setup.previewOutOfDate = true;
				Close();
			}
			GUILayout.Space(10);
			if (GUILayout.Button("Cancel", GUILayout.MinWidth(100), GUILayout.Height(20))) {
				Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		}

		public static void CreateWindow(ModalParent parent, LipSyncClipSetup setup) {
			ClipSettingsWindow window = Create(parent, setup);
			window.length = setup.fileLength;
			window.transcript = setup.transcript;
		}

		private static ClipSettingsWindow Create(ModalParent parent, LipSyncClipSetup setup) {
			ClipSettingsWindow window = CreateInstance<ClipSettingsWindow>();

			window.position = new Rect(parent.center.x - 250, parent.center.y - 100, 500, 200);
			window.minSize = new Vector2(500, 200);
			window.titleContent = new GUIContent("Clip Settings");

			window.setup = setup;
			window.Show(parent);
			return window;
		}
	}
}