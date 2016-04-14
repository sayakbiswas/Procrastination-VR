using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

namespace RogoDigital {
	[InitializeOnLoad]
	public class ImportControl {

		static ImportControl () {
			if(Directory.Exists(Application.dataPath+"/Rogo Digital/Gizmos")){

				if(Directory.Exists(Application.dataPath+"/Gizmos")){
					string[] files = Directory.GetFiles(Application.dataPath+"/Rogo Digital/Gizmos");

					foreach(string file in files){
						File.Move(file , Application.dataPath+"/Gizmos/"+Path.GetFileName(file));
					}
				}else{
					Directory.Move(Application.dataPath+"/Rogo Digital/Gizmos" , Application.dataPath+"/Gizmos");
				}

				AssetDatabase.Refresh();
			}
		}
	}
}