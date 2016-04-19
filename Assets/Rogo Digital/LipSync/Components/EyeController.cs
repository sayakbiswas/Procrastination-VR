using UnityEngine;
using System.Collections;
using RogoDigital.Lipsync;

namespace RogoDigital{
	[AddComponentMenu("Rogo Digital/Eye Controller")] 
	public class EyeController : BlendSystemUser {

		/// <summary>
		/// Is blinking enabled.
		/// </summary>
		public bool blinkingEnabled = false;

		/// <summary>
		/// The left eye blink blendable index.
		/// </summary>
		public int leftEyeBlinkBlendshape = 0;

		/// <summary>
		/// The right eye blink blendable index.
		/// </summary>
		public int rightEyeBlinkBlendshape = 1;

		/// <summary>
		/// The minimum time between blinks.
		/// </summary>
		public float minimumBlinkGap = 1;

		/// <summary>
		/// The maximum time between blinks.
		/// </summary>
		public float maximumBlinkGap = 4;

		/// <summary>
		/// How long each blink takes.
		/// </summary>
		public float blinkSpeed = 0.14f;

		/// <summary>
		/// Keeps the eyes closed.
		/// </summary>
		public bool keepEyesClosed {
			get {
				return _keepEyesClosed;
			}
			set {
				if(value == true) {
					if(_keepEyesClosed != value) StartCoroutine(CloseEyes());
				}else{
					if(_keepEyesClosed != value) StartCoroutine(OpenEyes());
				}

				_keepEyesClosed = value;
			}
		}

		/// <summary>
		/// Is random looking enabled.
		/// </summary>
		public bool randomLookingEnabled = false;

		/// <summary>
		/// Transform for the left eye.
		/// </summary>
		public Transform lefteye;

		/// <summary>
		/// Transform for the right eye.
		/// </summary>
		public Transform righteye;

		/// <summary>
		/// The eye rotation range along the X axis.
		/// </summary>
		public Vector2 eyeRotationRangeX = new Vector2(-6.5f , 6.5f);

		/// <summary>
		/// The eye rotation range along the Y axis.
		/// </summary>
		public Vector2 eyeRotationRangeY = new Vector2(-17.2f , 17.2f);

		/// <summary>
		/// The eye look offset.
		/// </summary>
		public Vector3 eyeLookOffset;

		/// <summary>
		/// The eye turn speed.
		/// </summary>
		public float eyeTurnSpeed = 18;

		/// <summary>
		/// The minimum time between look direction changes.
		/// </summary>
		public float minimumChangeDirectionGap = 2;

		/// <summary>
		/// The maximum time between look direction changes.
		/// </summary>
		public float maximumChangeDirectionGap = 10;

		/// <summary>
		/// Is look targeting enabled.
		/// </summary>
		public bool targetEnabled = false;

		/// <summary>
		/// Should targets be found automatically.
		/// </summary>
		public bool autoTarget = false;

		/// <summary>
		/// Tag to use when looking for targets.
		/// </summary>
		public string autoTargetTag = "EyeControllerTarget";

		/// <summary>
		/// The maximum distance between a target and the character for it to be targeted.
		/// </summary>
		public float autoTargetDistance = 10;

		/// <summary>
		/// Transform to look at.
		/// </summary>
		public Transform viewTarget;

		/// <summary>
		/// The target weight.
		/// </summary>
		public float targetWeight = 1;


		// Blinking
		private float blinkTimer;
		private bool blinking = false;

		// keepEyesClosed backing field
		private bool _keepEyesClosed = false;
		private bool _asyncBlending = false;

		// Shared Looking
		private Quaternion leftRotation;
		private Quaternion rightRotation;

		// Random Look
		private float lookTimer;
		private Quaternion randomAngle;

		// Look Target
		private Transform target;
		private Quaternion leftTargetAngle;
		private Quaternion rightTargetAngle;

		private Transform[] markedTargets;

		void Start () {
			// Get Starting Info
			randomAngle = Quaternion.identity;
			leftTargetAngle = Quaternion.identity;
			rightTargetAngle = Quaternion.identity;

			if(lefteye != null && righteye != null) {
				leftRotation = lefteye.rotation;
				rightRotation = righteye.rotation;
			}

			if(targetEnabled && autoTarget) {
				FindTargets();
			}
		}
		
		void LateUpdate () {
			// Blinking
			if(blinkingEnabled && blendSystem!= null && !keepEyesClosed && !_asyncBlending) {
				if(blendSystem.isReady) {
					if(blinking){
						float halfBlinkSpeed = blinkSpeed/2;

						if(blinkTimer < blinkSpeed/2) {
							blendSystem.SetBlendableValue(leftEyeBlinkBlendshape , Mathf.Lerp(0 , 100 , blinkTimer/halfBlinkSpeed));
							blendSystem.SetBlendableValue(rightEyeBlinkBlendshape , Mathf.Lerp(0 , 100 , blinkTimer/halfBlinkSpeed));
						}else{
							blendSystem.SetBlendableValue(leftEyeBlinkBlendshape , Mathf.Lerp(100 , 0 , (blinkTimer-halfBlinkSpeed)/halfBlinkSpeed));
							blendSystem.SetBlendableValue(rightEyeBlinkBlendshape , Mathf.Lerp(100 , 0 , (blinkTimer-halfBlinkSpeed)/halfBlinkSpeed));

							if(blinkTimer > blinkSpeed) {
								blinking = false;
								blinkTimer = Random.Range(minimumBlinkGap , maximumBlinkGap);
							}
						}

						blinkTimer += Time.deltaTime;
					}else{
						if(blinkTimer <= 0) {
							blinking = true;
							blinkTimer = 0;
						}else{
							blinkTimer -= Time.deltaTime;
						}
					}
				}
			}

			// Look Target
			if(targetEnabled && lefteye != null && righteye != null) {
				// Auto Target
				if(autoTarget) {
					try {
						float targetDistance = autoTargetDistance;
						target = null;
						for(int i = 0 ; i < markedTargets.Length ; i++) {
							if(Vector3.Distance(transform.position , markedTargets[i].position) < targetDistance) {
								targetDistance = Vector3.Distance(transform.position , markedTargets[i].position);
								target = markedTargets[i];
							}
						}
					}catch(System.NullReferenceException) {
						FindTargets();
					}
				}else{
					target = viewTarget;
				}

				if(target != null){
					leftTargetAngle =  Quaternion.LookRotation(target.position - lefteye.position)*Quaternion.Euler(eyeLookOffset);
					rightTargetAngle = Quaternion.LookRotation(target.position - righteye.position)*Quaternion.Euler(eyeLookOffset);
				}else{
					targetWeight = 0;
				}
			}else{
				targetWeight = 0;
			}

			// Random Look
			if(randomLookingEnabled && lefteye != null && righteye != null) {
				if(lookTimer <= 0) {
					lookTimer = Random.Range(minimumChangeDirectionGap , maximumChangeDirectionGap);
					randomAngle = Quaternion.Euler(Random.Range(eyeRotationRangeX.x , eyeRotationRangeX.y) , Random.Range(eyeRotationRangeY.x , eyeRotationRangeY.y) , 0);
				}else{
					lookTimer -= Time.deltaTime;
				}
			}

			// Shared Looking
			if(lefteye != null && righteye != null && (randomLookingEnabled || targetEnabled)) {
				lefteye.rotation = leftRotation;
				righteye.rotation = rightRotation;

				Quaternion leftAngle = Quaternion.Lerp(lefteye.parent.rotation*randomAngle , leftTargetAngle , targetWeight);
				Quaternion rightAngle = Quaternion.Lerp(righteye.parent.rotation*randomAngle , rightTargetAngle , targetWeight);

				lefteye.rotation = Quaternion.Lerp(lefteye.rotation , leftAngle , Time.deltaTime*eyeTurnSpeed);
				righteye.rotation = Quaternion.Lerp(righteye.rotation , rightAngle , Time.deltaTime*eyeTurnSpeed);

				leftRotation = lefteye.rotation;
				rightRotation = righteye.rotation;
			}
		}

		private IEnumerator CloseEyes () {
			bool end = false;
			blinkTimer = 0;
			_asyncBlending = true;

			while(end == false) {
				blendSystem.SetBlendableValue(leftEyeBlinkBlendshape , Mathf.Lerp(0 , 100 , blinkTimer/blinkSpeed));
				blendSystem.SetBlendableValue(rightEyeBlinkBlendshape , Mathf.Lerp(0 , 100 , blinkTimer/blinkSpeed));

				if(blinkTimer > blinkSpeed) {
					end = true;
					_asyncBlending = false;
				}

				blinkTimer += Time.deltaTime;
				yield return null;
			}
		}

		private IEnumerator OpenEyes () {
			bool end = false;
			blinkTimer = 0;
			_asyncBlending = true;

			while(end == false) {
				blendSystem.SetBlendableValue(leftEyeBlinkBlendshape , Mathf.Lerp(100 , 0 , blinkTimer/blinkSpeed));
				blendSystem.SetBlendableValue(rightEyeBlinkBlendshape , Mathf.Lerp(100 , 0 , blinkTimer/blinkSpeed));

				if(blinkTimer > blinkSpeed) {
					end = true;
					_asyncBlending = false;
				}

				blinkTimer += Time.deltaTime;
				yield return null;
			}
		}

		/// <summary>
		/// Finds potential look targets using the autoTargetTag.
		/// </summary>
		public void FindTargets () {
			GameObject[] gos = GameObject.FindGameObjectsWithTag(autoTargetTag);
			markedTargets = new Transform[gos.Length];

			for(int i = 0 ; i < markedTargets.Length ; i++) {
				markedTargets[i] = gos[i].transform;
			}
		}
			
		public void SetLookAtAmount (float amount) {
			targetWeight = amount;
		}
	}
}