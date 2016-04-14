using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace RogoDigital.Lipsync {
	public class AutoSync {
		public static List<string> sOutput;

		public static List<PhonemeMarker> ProcessAudio (AudioClip clip) {
			return ProcessAudio(clip , null);
		}

		public static List<PhonemeMarker> ProcessAudio (AudioClip clip , TextAsset text) {
			sOutput = new List<string>();

			string audioPath = AssetDatabase.GetAssetPath(clip);
			Uri fromUri = new Uri(Application.dataPath+"/Rogo Digital/LipSync/AutoSync/SAPI/sapi_lipsync.exe");
			Uri toUri = new Uri(Application.dataPath+audioPath.Substring(6));

			if (fromUri.Scheme == toUri.Scheme) {
				Uri relativeUri = fromUri.MakeRelativeUri(toUri);
				audioPath = Uri.UnescapeDataString(relativeUri.ToString());
			}

			string textPath = "";

			if(text != null){
				textPath = AssetDatabase.GetAssetPath(text);
				toUri = new Uri(Application.dataPath+textPath.Substring(6));
				
				if (fromUri.Scheme == toUri.Scheme) {
					Uri relativeUri = fromUri.MakeRelativeUri(toUri);
					textPath = Uri.UnescapeDataString(relativeUri.ToString());
				}
			}

			Process process = new Process();
			process.StartInfo.FileName = Application.dataPath+"/Rogo Digital/LipSync/AutoSync/SAPI/sapi_lipsync.exe";
			process.StartInfo.Arguments = audioPath + " " + textPath;

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = Application.dataPath+"/Rogo Digital/LipSync/AutoSync/SAPI/";
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;

			process.OutputDataReceived += new DataReceivedEventHandler(RecievedData);
			process.ErrorDataReceived += new DataReceivedEventHandler(RecievedError);

			EditorUtility.DisplayProgressBar("Analyzing Audio File" , "Please wait, analyzing file" , 0.33333f);

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			process.WaitForExit();

			EditorUtility.DisplayProgressBar("Generating Data" , "Please wait, generating LipSync data" , 0.66666f);

			List<PhonemeMarker> output = ParseOutput (sOutput , clip);

			EditorUtility.ClearProgressBar();
			return output;

		}

		public static void RecievedData(object e, DataReceivedEventArgs outLine) {
			sOutput.Add(outLine.Data);
		}

		public static void RecievedError(object e, DataReceivedEventArgs outLine) {
			if(outLine.Data.StartsWith("Phoneme label not found")){
				UnityEngine.Debug.LogError("SAPI error: " + outLine.Data);
			}
		}

		public static List<PhonemeMarker> ParseOutput (List<string> lines , AudioClip clip){
			List<PhonemeMarker> results = new List<PhonemeMarker>();

			//Phoneme Mapper - This can be edited for custom mapping
			Dictionary<string , Phoneme> phonemeMapper = new Dictionary<string, Phoneme>() {
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
				{"l"           , Phoneme.L},
				{"r"           , Phoneme.CDGKNRSThYZ},
				{"y"           , Phoneme.CDGKNRSThYZ},
				{"w"           , Phoneme.WQ},
				{"m"           , Phoneme.MBP},
				{"n"           , Phoneme.CDGKNRSThYZ},
				{"NG"          , Phoneme.CDGKNRSThYZ},
				{"CH"          , Phoneme.CDGKNRSThYZ},
				{"j"           , Phoneme.CDGKNRSThYZ},
				{"DH"          , Phoneme.CDGKNRSThYZ},
				{"b"           , Phoneme.MBP},
				{"d"           , Phoneme.CDGKNRSThYZ},
				{"g"           , Phoneme.CDGKNRSThYZ},
				{"p"           , Phoneme.MBP},
				{"t"           , Phoneme.CDGKNRSThYZ},
				{"k"           , Phoneme.CDGKNRSThYZ},
				{"z"           , Phoneme.CDGKNRSThYZ},
				{"ZH"          , Phoneme.CDGKNRSThYZ},
				{"v"           , Phoneme.FV},
				{"f"           , Phoneme.FV},
				{"TH"          , Phoneme.CDGKNRSThYZ},
				{"s"           , Phoneme.CDGKNRSThYZ},
				{"SH"          , Phoneme.CDGKNRSThYZ},
				{"h"           , Phoneme.CDGKNRSThYZ},
				{"etc"         , Phoneme.CDGKNRSThYZ},
			};

			foreach(string line in lines){
				if(line == "" || line == null)break;
				string[] tokens = line.Split(new string[]{" "} , StringSplitOptions.RemoveEmptyEntries);

				if(tokens.Length > 0){
					if(tokens[0] == "phn"){
						float startTime = float.Parse(tokens[1]);
						string phon = tokens[4].Remove(tokens[4].Length-1);

						if(phonemeMapper.ContainsKey(phon)){
							Phoneme phoneme = phonemeMapper[phon];
							results.Add(new PhonemeMarker(phoneme , startTime/(clip.length * 1000)));
						}
					}
				}
			}
			return results;
		}
	}
}