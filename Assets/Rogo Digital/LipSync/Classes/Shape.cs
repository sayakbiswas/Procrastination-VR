using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RogoDigital.Lipsync{
	[System.Serializable]
	public class Shape : System.Object {

		/// <summary>
		/// The blendable indexes. Used with dropdown-type blend systems.
		/// </summary>
		[SerializeField]
		public List<int> blendShapes;

		/// <summary>
		/// The blendable objects. Used with reference-type blend systems.
		/// </summary>
		[SerializeField]
		public List<Object> referenceBlendables;

		/// <summary>
		/// The associated weights.
		/// </summary>
		[SerializeField]
		public List<float> weights;

		/// <summary>
		/// List of bone shapes.
		/// </summary>
		[SerializeField]
		public List<BoneShape> bones;

		public bool HasBone (Transform bone) {
			for(int b = 0 ; b < bones.Count ; b++) {
				if(bones[b].bone == bone) return true;
			}
			return false;
		}

		public int IndexOfBone (Transform bone) {
			for(int b = 0 ; b < bones.Count ; b++) {
				if(bones[b].bone == bone) return b;
			}
			return -1;
		}
	}
}