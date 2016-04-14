using UnityEngine;
using System.Collections;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class BoneShape {
		[SerializeField]
		public Transform bone;
		[SerializeField]
		public Vector3 endPosition;
		[SerializeField]
		public Vector3 endRotation;

		[SerializeField]
		public bool lockPosition;
		[SerializeField]
		public bool lockRotation;

		public Vector3 neutralPosition;
		public Vector3 neutralRotation;

		public void SetNeutral () {
			if(bone != null) {
				neutralPosition = bone.localPosition;
				neutralRotation = bone.localEulerAngles;
			}
		}

		public BoneShape (Transform bone , Vector3 endPosition , Vector3 endRotation) {
			this.bone = bone;
			this.endPosition = endPosition;
			this.endRotation = endRotation;
		}

		public BoneShape () {
		}
	}
}