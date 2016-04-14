using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class EmotionShape : Shape {
		
		[SerializeField]
		public string emotion;
		[SerializeField]
		public bool verified = true;

		public EmotionShape (string eEmotion){
			emotion = eEmotion;
			blendShapes = new List<int>();
			weights = new List<float>();
			bones = new List<BoneShape>();
		}
	}
}