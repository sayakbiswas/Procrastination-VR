using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class PhonemeShape : Shape {
		
		[SerializeField]
		public Phoneme phoneme;

		public PhonemeShape (Phoneme ePhoneme){
			phoneme = ePhoneme;
			blendShapes = new List<int>();
			weights = new List<float>();
			bones = new List<BoneShape>();
		}
	}
}