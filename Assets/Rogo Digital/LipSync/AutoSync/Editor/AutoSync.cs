using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	public class AutoSync {

		public static bool CheckSoX() {
			string soXPath = EditorPrefs.GetString("LipSync_SoXPath");
			bool gotOutput = false;

			if (string.IsNullOrEmpty(soXPath)) return false;

			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = soXPath;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) => {
				if (!string.IsNullOrEmpty(outLine.Data)) {
					if (outLine.Data.Contains("SoX")) gotOutput = true;
				}
			};

			try {
				process.Start();
			} catch {
				return false;
			}
			
			process.BeginOutputReadLine();
			process.WaitForExit();

			return gotOutput;
		}

		public static void ProcessAudio(AudioClip clip, string languageModel, AutoSyncDataReady callback, string progressPrefix, bool enableConversion) {
			if (clip == null) return;
			EditorUtility.DisplayProgressBar(progressPrefix + " - Analysing Audio File", "Please wait, analysing file " + progressPrefix, 0.1f);
			
			bool converted = false;
			string audioPath = AssetDatabase.GetAssetPath(clip).Substring("/Assets".Length);

			if (audioPath != null) {
				// Get absolute path
				audioPath = Application.dataPath + "/" + audioPath;

				bool failed = false;
				// Convert to acceptable format
				if (enableConversion) {
					if (CheckSoX()) {
						EditorUtility.DisplayProgressBar(progressPrefix + " - Converting Audio File", "Please wait, converting file " + progressPrefix, 0.2f);
						converted = true;

						string newAudioPath = Application.dataPath + "/" + Path.GetFileNameWithoutExtension(audioPath) + "_temp_converted.wav";
						string soXPath = EditorPrefs.GetString("LipSync_SoXPath");

						string soXArgs = "\"" + audioPath + "\" -c 1 -b 16 -e s -r 16k \"" + newAudioPath + "\"";
						audioPath = newAudioPath;

						System.Diagnostics.Process process = new System.Diagnostics.Process();
						process.StartInfo.FileName = soXPath;
						process.StartInfo.Arguments = soXArgs;
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardError = true;

						process.ErrorDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) => {
							if (!string.IsNullOrEmpty(outLine.Data)) {
								if (outLine.Data.Contains("FAIL")) {
									Debug.LogError("AutoSync: SoX Conversion Failed: " + outLine.Data);
									failed = true;
									converted = false;
									process.Close();
								}
							}
						};

						process.Start();
						process.BeginErrorReadLine();
						process.WaitForExit(5000);
					}
				}

				if (!File.Exists(audioPath) || failed) {
					EditorUtility.ClearProgressBar();
					return;
				}

				EditorUtility.DisplayProgressBar(progressPrefix + " - Preparing AutoSync", "Please wait, preparing AutoSync.", 0.3f);

				// Load Language Model
				AutoSyncLanguageModel model = AutoSyncLanguageModel.Load(languageModel);
				if (model == null) return;

				string basePath = model.GetBasePath();

				string[] args = new string[16];
				args[0] = "-infile"; args[1] = audioPath;
				args[2] = "-hmm"; args[3] = basePath + model.hmmDir;
				args[4] = "-allphone"; args[5] = basePath + model.allphoneFile;
				args[6] = "-time"; args[7] = "yes";
				args[8] = "-allphone_ci"; args[9] = "yes";
				args[10] = "-beam"; args[11] = "1e-20";
				args[12] = "-pbeam"; args[13] = "1e-20";
				args[14] = "-lw"; args[15] = "2.5";

				EditorUtility.DisplayProgressBar(progressPrefix + " - Recognising Phonemes", "Please wait, recognising phonemes.", 0.5f);

				if (SphinxWrapper.Recognize(args, false) == "FAILED") {
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("AutoSync Failed", "AutoSync failed. Check the console for more information.", "OK");
					return;
				}

				ContinuationManager.Add(() => SphinxWrapper.dataReady, () => {
					EditorUtility.DisplayProgressBar(progressPrefix + " - Generating Data", "Please wait, generating LipSync data.", 0.85f);

					callback.Invoke(
						clip,
						ParseOutput(
							SphinxWrapper.result.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
							model,
							clip
						)
					);

					if (converted) {
						if (File.Exists(audioPath)) {
							File.Delete(audioPath);
							AssetDatabase.Refresh();
						}
					}
				});
			}
		}

		public static void ProcessAudio(AudioClip clip, string languageModel, AutoSyncDataReady callback, bool enableConversion) {
			ProcessAudio(clip, languageModel, callback, "", enableConversion);
		}

		private static List<PhonemeMarker> ParseOutput (string[] lines , AutoSyncLanguageModel lm, AudioClip clip){
			List<PhonemeMarker> results = new List<PhonemeMarker>();

			Dictionary<string, Phoneme> phonemeMapper = lm.phonemeMapper;

			//Default Phoneme Mapper - Language Models have the ability to contain their own, but this is not yet functional
			if(phonemeMapper == null)
			phonemeMapper = new Dictionary<string, Phoneme>() {
				//Vowels
				{"IY"          , Phoneme.E},
				{"IH" 		   , Phoneme.AI},
				{"EH"          , Phoneme.E},
				{"AE"          , Phoneme.AI},
				{"AH"          , Phoneme.U},
				{"UW"          , Phoneme.O},
				{"UH"          , Phoneme.U},
				{"AA"          , Phoneme.AI},
				{"AO"          , Phoneme.AI},
				{"EY"          , Phoneme.AI},
				{"AY"          , Phoneme.AI},
				{"OY"          , Phoneme.O},
				{"AW"          , Phoneme.AI},
				{"OW"          , Phoneme.O},
				{"ER"          , Phoneme.U},

				//Consonants
				{"JH"          , Phoneme.CDGKNRSThYZ},
				{"L"           , Phoneme.L},
				{"R"           , Phoneme.CDGKNRSThYZ},
				{"Y"           , Phoneme.CDGKNRSThYZ},
				{"W"           , Phoneme.WQ},
				{"M"           , Phoneme.MBP},
				{"N"           , Phoneme.CDGKNRSThYZ},
				{"NG"          , Phoneme.CDGKNRSThYZ},
				{"CH"          , Phoneme.CDGKNRSThYZ},
				{"J"           , Phoneme.CDGKNRSThYZ},
				{"DH"          , Phoneme.CDGKNRSThYZ},
				{"B"           , Phoneme.MBP},
				{"D"           , Phoneme.CDGKNRSThYZ},
				{"G"           , Phoneme.CDGKNRSThYZ},
				{"P"           , Phoneme.MBP},
				{"T"           , Phoneme.CDGKNRSThYZ},
				{"K"           , Phoneme.CDGKNRSThYZ},
				{"Z"           , Phoneme.CDGKNRSThYZ},
				{"ZH"          , Phoneme.CDGKNRSThYZ},
				{"V"           , Phoneme.FV},
				{"F"           , Phoneme.FV},
				{"TH"          , Phoneme.CDGKNRSThYZ},
				{"S"           , Phoneme.CDGKNRSThYZ},
				{"SH"          , Phoneme.CDGKNRSThYZ},
				{"HH"           , Phoneme.CDGKNRSThYZ},
			};

			foreach(string line in lines){
				if(string.IsNullOrEmpty(line))
					break;
				string[] tokens = line.Split(' ');

				try {
					if (tokens[0] != "SIL") {
						Phoneme phoneme = phonemeMapper[tokens[0]];
						float startTime = float.Parse(tokens[1]);
						results.Add(new PhonemeMarker(phoneme, startTime / clip.length));
						
					}
				} catch (ArgumentOutOfRangeException) {
					Debug.Log("Phoneme Label missing from return data. Skipping this entry.");
				} catch (KeyNotFoundException) {
					Debug.Log("Phoneme Label \"" + tokens[0] + "\" not found in phoneme mapper. Skipping this entry.");
				}
			}

			EditorUtility.ClearProgressBar();
			return results;
		}

		public delegate void AutoSyncDataReady(AudioClip clip, List<PhonemeMarker> markers);
	}
}