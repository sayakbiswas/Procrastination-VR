using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class PhonemeMarker : System.Object {
		[SerializeField]
		public Phoneme phoneme;
		[SerializeField]
		public float time;
		[SerializeField]
		public float intensity = 1;

		public PhonemeMarker (Phoneme ePhoneme , float eTime) {
			phoneme = ePhoneme;
			time = eTime;
		}
	}
}