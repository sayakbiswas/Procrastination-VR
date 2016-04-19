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
		public bool customBlendIn;
		[SerializeField]
		public bool customBlendOut;
		[SerializeField]
		public float intensity = 1;

		// Editor Only
		public bool invalid = false;

		public EmotionMarker (string emotion , float startTime , float endTime , float blendInTime , float blendOutTime , bool blendToMarker , bool blendFromMarker, bool customBlendIn, bool customBlendOut) {
			this.emotion = emotion;
			this.startTime = startTime;
			this.endTime = endTime;
			this.blendInTime = blendInTime;
			this.blendOutTime = blendOutTime;
			this.blendToMarker = blendToMarker;
			this.blendFromMarker = blendFromMarker;
			this.customBlendIn = customBlendIn;
			this.customBlendOut = customBlendOut;
		}
	}
}