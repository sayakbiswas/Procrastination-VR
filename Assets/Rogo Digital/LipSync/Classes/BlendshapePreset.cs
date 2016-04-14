using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync{
	public class BlendshapePreset : ScriptableObject {
		[SerializeField]
		public List<PhonemeShape> phonemeShapes;
		[SerializeField]
		public List<EmotionShape> emotionShapes;
	}
}