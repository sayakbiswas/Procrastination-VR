using UnityEngine;
using System.Collections;

namespace RogoDigital.Lipsync{
	public class LipSyncData : ScriptableObject  {
		[SerializeField]
		public AudioClip clip;
		[SerializeField]
		public PhonemeMarker[] phonemeData;
		[SerializeField]
		public EmotionMarker[] emotionData;
		[SerializeField]
		public GestureMarker[] gestureData;

		public LipSyncData () {
		}

		public LipSyncData (AudioClip eClip , PhonemeMarker[] pData , EmotionMarker[] eData , GestureMarker[] gData) {
			clip = eClip;
			phonemeData = pData;
			emotionData = eData;
			gestureData = gData;
		}
	}
}