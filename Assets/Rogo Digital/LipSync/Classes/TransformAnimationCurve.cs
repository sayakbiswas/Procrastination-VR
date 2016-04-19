using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	public class TransformAnimationCurve  {
		private AnimationCurve _posX;
		private AnimationCurve _posY;
		private AnimationCurve _posZ;
		private AnimationCurve _rotX;
		private AnimationCurve _rotY;
		private AnimationCurve _rotZ;
		private AnimationCurve _rotW;

		public TransformKeyframe[] keys {
			get{
				List<TransformKeyframe> keyframes = new List<TransformKeyframe>();
				for(int k = 0 ; k < _posX.length ; k++) {
					keyframes.Add(new TransformKeyframe(
						_posX.keys[k].time ,
						new Vector3(
							_posX.keys[k].value,
							_posY.keys[k].value,
							_posZ.keys[k].value
						) , new Quaternion(
							_rotX.keys[k].value,
							_rotY.keys[k].value,
							_rotZ.keys[k].value,
							_rotW.keys[k].value
						),
						_posX.keys[k].inTangent,
						_posX.keys[k].outTangent
					));
				}

				return keyframes.ToArray();
			}
		}

		public int length {
			get {
				return _posX.length;
			}
		}

		public WrapMode postWrapMode {
			get {
				return _posX.postWrapMode;
			}

			set {
				_posX.postWrapMode = value;
				_posY.postWrapMode = value;
				_posZ.postWrapMode = value;
				_rotX.postWrapMode = value;
				_rotY.postWrapMode = value;
				_rotZ.postWrapMode = value;
				_rotW.postWrapMode = value;
			}
		}

		public WrapMode preWrapMode {
			get {
				return _posX.preWrapMode;
			}

			set {
				_posX.preWrapMode = value;
				_posY.preWrapMode = value;
				_posZ.preWrapMode = value;
				_rotX.preWrapMode = value;
				_rotY.preWrapMode = value;
				_rotZ.preWrapMode = value;
				_rotW.preWrapMode = value;
			}
		}

		public int AddKey (float time , Vector3 position , Quaternion rotation , float inTangent , float outTangent) {
			int index = _posX.AddKey(new Keyframe(time , position.x , inTangent , outTangent));
			_posY.AddKey(new Keyframe(time , position.y , inTangent , outTangent));
			_posZ.AddKey(new Keyframe(time , position.z , inTangent , outTangent));

			_rotX.AddKey(new Keyframe(time , rotation.x , inTangent , outTangent));
			_rotY.AddKey(new Keyframe(time , rotation.y , inTangent , outTangent));
			_rotZ.AddKey(new Keyframe(time , rotation.z , inTangent , outTangent));
			_rotW.AddKey(new Keyframe(time , rotation.w , inTangent , outTangent));

			return index;
		}

		public int AddKey (float time , Quaternion rotation , float inTangent , float outTangent) {
			int index = _rotX.AddKey(new Keyframe(time , rotation.x , inTangent , outTangent));
			_rotY.AddKey(new Keyframe(time , rotation.y , inTangent , outTangent));
			_rotZ.AddKey(new Keyframe(time , rotation.z , inTangent , outTangent));
			_rotW.AddKey(new Keyframe(time , rotation.w , inTangent , outTangent));

			return index;
		}

		public int AddKey (float time , Vector3 position , float inTangent , float outTangent) {
			int index = _posX.AddKey(new Keyframe(time , position.x , inTangent , outTangent));
			_posY.AddKey(new Keyframe(time , position.y , inTangent , outTangent));
			_posZ.AddKey(new Keyframe(time , position.z , inTangent , outTangent));

			return index;
		}

		public int AddKey(TransformKeyframe keyframe) {
			int index = _posX.AddKey(new Keyframe(keyframe.time, keyframe.position.x, keyframe.inTangent, keyframe.outTangent));
			_posY.AddKey(new Keyframe(keyframe.time, keyframe.position.y, keyframe.inTangent, keyframe.outTangent));
			_posZ.AddKey(new Keyframe(keyframe.time, keyframe.position.z, keyframe.inTangent, keyframe.outTangent));

			_rotX.AddKey(new Keyframe(keyframe.time, keyframe.rotation.x, keyframe.inTangent, keyframe.outTangent));
			_rotY.AddKey(new Keyframe(keyframe.time, keyframe.rotation.y, keyframe.inTangent, keyframe.outTangent));
			_rotZ.AddKey(new Keyframe(keyframe.time, keyframe.rotation.z, keyframe.inTangent, keyframe.outTangent));
			_rotW.AddKey(new Keyframe(keyframe.time, keyframe.rotation.w, keyframe.inTangent, keyframe.outTangent));

			return index;
		}

		public Vector3 EvaluatePosition (float time) {
			float x = _posX.Evaluate(time);
			float y = _posY.Evaluate(time);
			float z = _posZ.Evaluate(time);

			return new Vector3(x,y,z);
		}

		public Quaternion EvaluateRotation (float time) {
			float x = _rotX.Evaluate(time);
			float y = _rotY.Evaluate(time);
			float z = _rotZ.Evaluate(time);
			float w = _rotW.Evaluate(time);

			return new Quaternion(x,y,z,w);
		}

		public TransformAnimationCurve () {
			_posX = new AnimationCurve();
			_posY = new AnimationCurve();
			_posZ = new AnimationCurve();

			_rotX = new AnimationCurve();
			_rotY = new AnimationCurve();
			_rotZ = new AnimationCurve();
			_rotW = new AnimationCurve();
		}

		public struct TransformKeyframe {
			public float time;
			public Quaternion rotation;
			public Vector3 position;
			public float inTangent;
			public float outTangent;

			public TransformKeyframe (float time , Vector3 position, Quaternion rotation, float inTangent, float outTangent) {
				this.time = time;
				this.position = position;
				this.rotation = rotation;
				this.inTangent = inTangent;
				this.outTangent = outTangent;
			}
		}
	}
}