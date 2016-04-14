using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class EmotionMarker : System.Object {
		[SerializeField]
		public string emotion;
		[SerializeField]
		public float startTime;
		[SerializeField]
		public float endTime;
		[SerializeField]
		public float blendInTime;
		[SerializeField]
		public float blendOutTime;
		[SerializeField]
		public bool blendToMarker;
		[SerializeField]
		public bool blendFromMarker;
		[SerializeField]
		public float intensity = 1;

		public EmotionMarker (string eEmotion , float eStartTime , float eEndTime , float eBlendInTime , float eBlendOutTime , bool eBlendToMarker , bool eBlendFromMarker) {
			emotion = eEmotion;
			startTime = eStartTime;
			endTime = eEndTime;
			blendInTime = eBlendInTime;
			blendOutTime = eBlendOutTime;
			blendToMarker = eBlendToMarker;
			blendFromMarker = eBlendFromMarker;
		}
	}
}