using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RogoDigital {
	public static class AudioUtility {

		public static AudioSource source;

		public static void Initialize () {
			GameObject go = new GameObject("LipSync Editor Audio" , typeof(AudioSource));
			go.hideFlags = HideFlags.HideAndDontSave;

			source = go.GetComponent<AudioSource>();
			source.playOnAwake = false;
		}

		public static void PlayClip(AudioClip clip) {
			if(source == null) Initialize();

			source.clip = clip;
			source.Play ();
		}
		
		public static void StopClip(AudioClip clip) {
			if(source == null) Initialize();

			SetClipSamplePosition(clip, 0);
			source.Stop();
		}
		
		public static void PauseClip(AudioClip clip) {
			if(source == null) Initialize();

			source.Pause();
		}
		
		public static void ResumeClip(AudioClip clip) {
			if(source == null) Initialize();

			source.UnPause();
		}

		public static bool IsClipPlaying(AudioClip clip) {
			if(source == null) Initialize();

			return source.isPlaying;
		}

		public static void StopAllClips () {
			if(source == null) Initialize();

			source.Stop();
		}
		
		public static float GetClipPosition(AudioClip clip) {
			if(source == null) Initialize();

			return source.time;
		}

		public static void SetClipSamplePosition(AudioClip clip , int iSamplePosition) {
			source.timeSamples = Mathf.Clamp(iSamplePosition , 0 , clip.samples-1);
		}

		public static int GetSampleCount(AudioClip clip) {
			return clip.samples;
		}

		public static Texture2D GetWaveForm(AudioClip clip , int channel , float width , float height) {
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
				"GetWaveForm",
				BindingFlags.Static | BindingFlags.Public
				);
			
			string path = AssetDatabase.GetAssetPath(clip);
			AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
			
			Texture2D texture = (Texture2D)method.Invoke(
				null,
				new object[] {
				clip,
				importer,
				channel,
				width,
				height
			}
			);
			
			return texture;
		}
	}
}