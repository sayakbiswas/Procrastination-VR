using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.IO;

namespace RogoDigital.Lipsync {
	public class AutoSyncLanguageModel : ScriptableObject {
		[SerializeField]
		public string language;

		[SerializeField]
		public string hmmDir;
		[SerializeField]
		public string dictFile;
		[SerializeField]
		public string allphoneFile;
		[SerializeField]
		public string lmFile;
		[SerializeField]
		public Dictionary<string, Phoneme> phonemeMapper = null;

		public string GetBasePath() {
			string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this).Substring("/Assets".Length));
			return Application.dataPath + "/" + path + "/";
		}

		public static AutoSyncLanguageModel Load(string languageName) {
			string[] assets = AssetDatabase.FindAssets("t:AutoSyncLanguageModel");

			if (assets.Length > 0) {
				foreach (string guid in assets) {
					AutoSyncLanguageModel model = AssetDatabase.LoadAssetAtPath<AutoSyncLanguageModel>(AssetDatabase.GUIDToAssetPath(guid));
					if (model.language == languageName) {
						return model;
					}
				}
			}

			return null;
		}

		public static string[] FindModels() {
			return FindModels("");
		}

		public static string[] FindModels(string filter) {
			string[] assets = AssetDatabase.FindAssets("t:AutoSyncLanguageModel "+filter);

			for (int s = 0; s < assets.Length; s++ ) {
				AutoSyncLanguageModel model = AssetDatabase.LoadAssetAtPath<AutoSyncLanguageModel>(AssetDatabase.GUIDToAssetPath(assets[s]));
				assets[s] = model.language;
			}

			return assets;
		}
	}
}