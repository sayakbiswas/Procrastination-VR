using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace RogoDigital.Lipsync {

	[AddComponentMenu("Rogo Digital/LipSync")]
	[DisallowMultipleComponent]
	[HelpURL("http://updates.rogodigital.com/lipsync-api/class_rogo_digital_1_1_lipsync_1_1_lip_sync.html")]
	public class LipSync : BlendSystemUser {

		// Public Variables

		/// <summary>
		/// AudioSource used for playing dialogue
		/// </summary>
		public AudioSource audioSource;

		/// <summary>
		/// Allow bones to be used in phoneme shapes.
		/// </summary>
		public bool useBones = false;

		/// <summary>
		/// Used for deciding if/when to repose boneshapes in LateUpdate.
		/// </summary>
		public bool boneUpdateAnimation = false;

		/// <summary>
		/// All PhonemeShapes on this LipSync instance.
		/// PhonemeShapes are a list of blendables and
		/// weights associated with a particular phoneme.
		/// </summary>
		[SerializeField]
		public List<PhonemeShape> phonemes;

		/// <summary>
		/// All EmotionShapes on this LipSync instance.
		/// EmotionShapes are simply PhonemeShapes, but
		/// with a string identifier instead of a Phoneme.
		/// Emotions are set up in the Project Settings.
		/// </summary>
		[SerializeField]
		public List<EmotionShape> emotions;

		/// <summary>
		/// If checked, the component will play defaultClip on awake.
		/// </summary>
		public bool playOnAwake = false;

		/// <summary>
		/// If checked, the clip will play again when it finishes.
		/// </summary>
		public bool loop = false;

		/// <summary>
		/// The clip to be played when playOnAwake is checked.
		/// </summary>
		public LipSyncData defaultClip = null;

		/// <summary>
		/// The delay between calling Play() and the clip playing.
		/// </summary>
		public float defaultDelay = 0f;

		/// <summary>
		/// If true, audio playback speed will match the timescale setting (allows slow or fast motion speech)
		/// </summary>
		public bool scaleAudioSpeed = true;

		/// <summary>
		/// If there are no phonemes within this many seconds
		/// of the previous one, a rest will be inserted.
		/// </summary>
		public float restTime = 0.2f;

		/// <summary>
		/// The time, in seconds, that a shape will be held for
		/// before blending to neutral when a rest is inserted.
		/// </summary>
		public float restHoldTime = 0.1f;

		/// <summary>
		/// The method used for generating curve tangents. Tight will ensure poses
		/// are matched exactly, but can make movement robotic, Loose will look
		/// more natural but can can cause poses to be over-emphasized.
		/// </summary>
		public CurveGenerationMode phonemeCurveGenerationMode = CurveGenerationMode.Loose;

		/// <summary>
		/// The method used for generating curve tangents. Tight will ensure poses
		/// are matched exactly, but can make movement robotic, Loose will look
		/// more natural but can can cause poses to be over-emphasized.
		/// </summary>
		public CurveGenerationMode emotionCurveGenerationMode = CurveGenerationMode.Tight;

		/// <summary>
		/// Whether or not there is currently a LipSync animation playing.
		/// </summary>
		public bool isPlaying {
			get;
			private set;
		}

		/// <summary>
		/// Whether the currently playing animation is paused.
		/// </summary>
		public bool isPaused {
			get;
			private set;
		}

		/// <summary>
		/// The Animator component used for Gestures.
		/// </summary>
		public Animator gesturesAnimator;

		/// <summary>
		/// The Gestures layer.
		/// </summary>
		public int gesturesLayer;

		/// <summary>
		/// The animation clips used for gestures.
		/// </summary>
		public List<GestureInstance> gestures;

		/// <summary>
		/// Called when a clip finished playing.
		/// </summary>
		public UnityEvent onFinishedPlaying;

		// Private Variables

		private AudioClip audioClip;
		private bool ready = false;
		private Dictionary<string, EmotionShape> emotionCache;
		private int currentFileID = 0;

		private float emotionBlendTime = 0;
		private float emotionTimer = 0;
		private bool changingEmotion = false;
		private int customEmotion = -1;

		// Marker Data
		private List<PhonemeMarker> phonemeMarkers;
		private List<EmotionMarker> emotionMarkers;
		private List<GestureMarker> gestureMarkers;
		private float fileLength;

		private int nextGesture = 0;

		// Curves
		private List<int> indexBlendables;
		private List<Object> referenceBlendables;
		private List<AnimationCurve> animCurves;

		private List<Transform> bones;
		private List<TransformAnimationCurve> boneCurves;

		private List<Vector3> boneNeutralPositions;
		private List<Quaternion> boneNeutralRotations;

		// Used by the editor
		public UnityEvent reset;
		public float lastUsedVersion = 0;

		void Reset() {
			if (reset == null) reset = new UnityEvent();
			reset.Invoke();
		}

		void Awake() {

			// Get reference to attached AudioSource
			if (audioSource == null) audioSource = GetComponent<AudioSource>();

			// Ensure BlendSystem is set to allow animation
			if (audioSource == null) {
				Debug.LogError("[LipSync - " + gameObject.name + "] No AudioSource specified or found.");
				return;
			} else if (blendSystem == null) {
				Debug.LogError("[LipSync - " + gameObject.name + "] No BlendSystem set.");
				return;
			} else if (blendSystem.isReady == false) {
				Debug.LogError("[LipSync - " + gameObject.name + "] BlendSystem is not set up.");
				return;
			} else {
				ready = true;
			}

			// Check for old-style settings
			if (restTime < 0.1f) {
				Debug.LogWarning("[LipSync - " + gameObject.name + "] Rest Time and/or Hold Time are lower than recommended and may cause animation errors. From LipSync 0.6, Rest Time is recommended to be 0.2 and Hold Time is recommended to be 0.1");
			}

			// Cache Emotions for more performant cross-checking
			emotionCache = new Dictionary<string, EmotionShape>();
			foreach (EmotionShape emotionShape in emotions) {
				emotionCache.Add(emotionShape.emotion, emotionShape);
			}

			// Check validity of Gestures
			if (gesturesAnimator == null) {
				Debug.Log("[LipSync - " + gameObject.name + "] No Animator specified. Gestures won't be played.");
			} else {
				foreach (GestureInstance gesture in gestures) {
					if (!gesture.IsValid(gesturesAnimator)) {
						Debug.LogWarning("[LipSync - " + gameObject.name + "] Animator does not contain a trigger called '" + gesture.triggerName + "'. This Gesture will be ignored.");
					}
				}
			}

			// Start Playing if playOnAwake is set
			if (playOnAwake && defaultClip != null) Play(defaultClip, defaultDelay);
		}

		void LateUpdate() {
			if ((isPlaying && !isPaused) || changingEmotion) {
				// Scale audio speed if set
				if (scaleAudioSpeed && !changingEmotion) audioSource.pitch = Time.timeScale;

				float normalisedTimer = 0;
				if (isPlaying) {
					// Get normalised timer from audio playback
					normalisedTimer = audioSource.time / audioSource.clip.length;

					// Gesture cues
					if (gestures.Count > 0 && nextGesture < gestureMarkers.Count && gesturesAnimator != null) {
						if (normalisedTimer >= gestureMarkers[nextGesture].time) {
							// Gesture Cue has been reached
							if (GetGesture(gestureMarkers[nextGesture].gesture) != null) {
								gesturesAnimator.SetTrigger(GetGesture(gestureMarkers[nextGesture].gesture).triggerName);
							}
							nextGesture++;
						}
					}
				} else {
					// Get normalised timer from custom timer
					emotionTimer += scaleAudioSpeed ? Time.deltaTime : Time.unscaledDeltaTime;
					normalisedTimer = emotionTimer / emotionBlendTime;
				}


				// Go through each animCurve and update blendables
				for (int curve = 0; curve < animCurves.Count; curve++) {
					blendSystem.SetBlendableValue(indexBlendables[curve], animCurves[curve].Evaluate(normalisedTimer));
				}

				// Do the same for bones
				if (useBones) {
					for (int curve = 0; curve < boneCurves.Count; curve++) {
						if (boneUpdateAnimation == false) {
							bones[curve].localPosition = boneCurves[curve].EvaluatePosition(normalisedTimer);
							bones[curve].localRotation = boneCurves[curve].EvaluateRotation(normalisedTimer);
						} else {
							// Get transform relative to current animation frame
							Vector3 newPos = boneCurves[curve].EvaluatePosition(normalisedTimer) - boneNeutralPositions[curve];
							Vector3 newRot = boneCurves[curve].EvaluateRotation(normalisedTimer).eulerAngles - boneNeutralRotations[curve].eulerAngles;

							bones[curve].localPosition += newPos;
							bones[curve].localEulerAngles += newRot;
						}
					}
				}

				if (changingEmotion && normalisedTimer > 1)
					changingEmotion = false;

				if ((normalisedTimer >= 0.999f) && !changingEmotion) {
					if (loop) {
						onFinishedPlaying.Invoke();
						audioSource.Play();
					} else {
						Stop(false);
					}
				}
			}
		}

		// Public Functions

		/// <summary>
		/// Sets the emotion.
		/// Only works when not playing an animation.
		/// </summary>
		/// <param name="emotion">Emotion.</param>
		/// <param name="blendTime">Blend time.</param>
		public void SetEmotion(string emotion, float blendTime) {
			if (!isPlaying && ready && enabled) {

				if (emotions.IndexOf(emotionCache[emotion]) == customEmotion) return;

				// Init Curves
				animCurves = new List<AnimationCurve>();
				indexBlendables = new List<int>();

				if (useBones) {
					boneCurves = new List<TransformAnimationCurve>();
					bones = new List<Transform>();
				}

				// Get Blendables
				EmotionShape emote = emotionCache[emotion];

				for (int b = 0; b < emote.blendShapes.Count; b++) {
					indexBlendables.Add(emote.blendShapes[b]);
					animCurves.Add(new AnimationCurve());
				}

				if (useBones) {
					for (int b = 0; b < emote.bones.Count; b++) {
						bones.Add(emote.bones[b].bone);
						boneCurves.Add(new TransformAnimationCurve());
					}
				}

				if (customEmotion > -1) {
					// Add Previous Emotion blendables
					for (int b = 0; b < emotions[customEmotion].blendShapes.Count; b++) {
						if (!indexBlendables.Contains(emotions[customEmotion].blendShapes[b])) {
							indexBlendables.Add(emotions[customEmotion].blendShapes[b]);
							animCurves.Add(new AnimationCurve());
						}
					}

					if (useBones) {
						for (int b = 0; b < emotions[customEmotion].bones.Count; b++) {
							if (!bones.Contains(emotions[customEmotion].bones[b].bone)) {
								bones.Add(emotions[customEmotion].bones[b].bone);
								boneCurves.Add(new TransformAnimationCurve());
							}
						}
					}
				}

				// Get Keys
				if (customEmotion > -1) {
					for (int b = 0; b < emotions[customEmotion].blendShapes.Count; b++) {
						int matchingCurve = indexBlendables.IndexOf(emotions[customEmotion].blendShapes[b]);
						animCurves[matchingCurve].AddKey(new Keyframe(0, blendSystem.GetBlendableValue(emotions[customEmotion].blendShapes[b]), 90, 0));
					}

					for (int b = 0; b < animCurves.Count; b++) {
						if (animCurves[b].keys.Length > 0) {
							if (emote.blendShapes.Contains(indexBlendables[b])) {
								animCurves[b].AddKey(new Keyframe(1, emote.weights[emote.blendShapes.IndexOf(indexBlendables[b])], 0, 90));
							} else {
								animCurves[b].AddKey(new Keyframe(1, 0, 0, 90));
							}
						} else {
							animCurves[b].AddKey(new Keyframe(0, blendSystem.GetBlendableValue(indexBlendables[b]), 90, 0));
							int match = emote.blendShapes.IndexOf(indexBlendables[b]);
							animCurves[b].AddKey(new Keyframe(1, emote.weights[match], 0, 90));
						}
					}

					if (useBones) {
						for (int b = 0; b < emotions[customEmotion].bones.Count; b++) {
							int matchingCurve = bones.IndexOf(emotions[customEmotion].bones[b].bone);
							boneCurves[matchingCurve].AddKey(0, emotions[customEmotion].bones[b].bone.localPosition, emotions[customEmotion].bones[b].bone.localRotation, 90, 0);
						}

						for (int b = 0; b < boneCurves.Count; b++) {
							if (boneCurves[b].length > 0) {
								if (emote.HasBone(bones[b])) {
									boneCurves[b].AddKey(1, emote.bones[emote.IndexOfBone(bones[b])].endPosition, Quaternion.Euler(emote.bones[emote.IndexOfBone(bones[b])].endRotation), 0, 90);
								} else {
									boneCurves[b].AddKey(1, emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(bones[b])].neutralPosition, Quaternion.Euler(emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(bones[b])].neutralRotation), 0, 90);
								}
							} else {
								boneCurves[b].AddKey(0, bones[b].localPosition, bones[b].localRotation, 90, 0);
								int match = emote.IndexOfBone(bones[b]);
								boneCurves[b].AddKey(1, emote.bones[match].endPosition, Quaternion.Euler(emote.bones[match].endRotation), 0, 90);
							}
						}
					}
				} else {
					for (int b = 0; b < animCurves.Count; b++) {
						animCurves[b].AddKey(new Keyframe(0, blendSystem.GetBlendableValue(indexBlendables[b]), 90, 0));
						animCurves[b].AddKey(new Keyframe(1, emote.weights[b], 0, 90));
					}

					if (useBones) {
						for (int b = 0; b < boneCurves.Count; b++) {
							boneCurves[b].AddKey(0, bones[b].localPosition, bones[b].localRotation, 90, 0);
							boneCurves[b].AddKey(1, emote.bones[b].endPosition, Quaternion.Euler(emote.bones[b].endRotation), 0, 90);
						}
					}
				}

				emotionTimer = 0;
				emotionBlendTime = blendTime;
				customEmotion = emotions.IndexOf(emote);
				changingEmotion = true;
			}
		}

		/// <summary>
		/// Loads a LipSyncData file if necessary and
		/// then plays it on the current LipSync component.
		/// </summary>
		public void Play(LipSyncData dataFile, float delay) {
			if (ready && enabled) {
				// Load File if not already loaded
				if (dataFile.GetInstanceID() != currentFileID) {
					LoadData(dataFile);
					ProcessData();
				}

				if (gesturesAnimator != null) gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				isPlaying = true;
				isPaused = false;
				nextGesture = 0;

				// Play audio
				audioSource.PlayDelayed(delay);
			}
		}

		/// <summary>
		/// Overload of Play with no delay specified. For compatibility with pre 0.4 scripts.
		/// </summary>
		public void Play(LipSyncData dataFile) {
			Play(dataFile, 0);
		}

		/// <summary>
		/// Loads an XML file and parses LipSync data from it,
		/// then plays it on the current LipSync component.
		/// </summary>
		public void Play(TextAsset xmlFile, AudioClip clip, float delay) {
			if (ready && enabled) {
				// Load File
				LoadXML(xmlFile, clip);

				if (gesturesAnimator != null) gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				isPlaying = true;
				isPaused = false;
				nextGesture = 0;

				ProcessData();

				// Play audio
				audioSource.PlayDelayed(delay);
			}
		}

		/// <summary>
		/// Overload of Play with no delay specified. For compatibility with pre 0.4 scripts.
		/// </summary>
		public void Play(TextAsset xmlFile, AudioClip clip) {
			Play(xmlFile, clip, 0);
		}

		/// <summary>
		/// Loads a LipSyncData file if necessary and
		/// then plays it on the current LipSync component
		/// from a certain point in seconds.
		/// </summary>
		public void PlayFromTime(LipSyncData dataFile, float delay, float time) {
			if (ready && enabled) {
				// Load File if not already loaded
				if (dataFile.GetInstanceID() != currentFileID) {
					LoadData(dataFile);
					ProcessData();
				}

				// Check that time is within range
				if (time >= fileLength) {
					Debug.LogError("[LipSync - " + gameObject.name + "] Couldn't play animation. Time parameter is greater than clip length.");
					return;
				}

				if (gesturesAnimator != null) gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				isPlaying = true;
				isPaused = false;
				nextGesture = 0;

				// Play audio
				audioSource.Play();
				audioSource.time = time + delay;
			}
		}

		/// <summary>
		/// Overload of PlayFromTime with no delay specified.
		/// </summary>
		public void PlayFromTime(LipSyncData dataFile, float time) {
			PlayFromTime(dataFile, 0, time);
		}

		/// <summary>
		/// Loads an XML file and parses LipSync data from it,
		/// then plays it on the current LipSync component
		/// from a certain point in seconds.
		/// </summary>
		public void PlayFromTime(TextAsset xmlFile, AudioClip clip, float delay, float time) {
			if (ready && enabled) {
				// Load File
				LoadXML(xmlFile, clip);

				// Check that time is within range
				if (time >= fileLength) {
					Debug.LogError("[LipSync - " + gameObject.name + "] Couldn't play animation. Time parameter is greater than clip length.");
					return;
				}

				if (gesturesAnimator != null) gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				isPlaying = true;
				isPaused = false;
				nextGesture = 0;

				ProcessData();

				// Play audio
				audioSource.Play();
				audioSource.time = time + delay;
			}
		}

		/// <summary>
		/// Overload of PlayFromTime with no delay specified.
		/// </summary>
		public void PlayFromTime(TextAsset xmlFile, AudioClip clip, float time) {
			PlayFromTime(xmlFile, clip, 0, time);
		}

		/// <summary>
		/// Pauses the currently playing animation.
		/// </summary>
		public void Pause() {
			if (isPlaying && !isPaused && enabled) {
				isPaused = true;
				audioSource.Pause();
			}
		}

		/// <summary>
		/// Resumes the current animation after pausing.
		/// </summary>
		public void Resume() {
			if (isPlaying && isPaused && enabled) {
				isPaused = false;
				audioSource.UnPause();
			}
		}

		/// <summary>
		/// Completely stops the current animation to be
		/// started again from the begining.
		/// </summary>
		public void Stop(bool stopAudio) {
			if (isPlaying && enabled) {
				isPlaying = false;
				isPaused = false;

				PreviewAtTime(0);

				// Stop Audio
				if (stopAudio) audioSource.Stop();

				//Invoke Callback
				onFinishedPlaying.Invoke();
			}
		}

		/// <summary>
		/// Sets blendables to their state at a certain time in the animation.
		/// ProcessData must have already been called.
		/// </summary>
		/// <param name="time">Time.</param>
		public void PreviewAtTime(float time) {
			if (!isPlaying && enabled && animCurves != null) {
				// Go through each animCurve and update blendables
				for (int curve = 0; curve < animCurves.Count; curve++) {
					blendSystem.SetBlendableValue(indexBlendables[curve], animCurves[curve].Evaluate(time));
				}

				if (useBones) {
					for (int curve = 0; curve < boneCurves.Count; curve++) {
						if (bones[curve] != null) bones[curve].localPosition = boneCurves[curve].EvaluatePosition(time);
						if (bones[curve] != null) bones[curve].localRotation = boneCurves[curve].EvaluateRotation(time);
					}
				}
			}
		}

		/// <summary>
		/// Loads raw data instead of using a serialised asset.
		/// Used for previewing animations in the editor.
		/// </summary>
		/// <param name="pData">Phoneme data.</param>
		/// <param name="eData">Emotion data.</param>
		/// <param name="clip">Audio Clip.</param>
		public void TempLoad(List<PhonemeMarker> pData, List<EmotionMarker> eData, AudioClip clip, float duration) {
			if (enabled) {
				if (emotionCache == null) {
					// Cache Emotions for more performant cross-checking
					emotionCache = new Dictionary<string, EmotionShape>();
					foreach (EmotionShape emotionShape in emotions) {
						emotionCache.Add(emotionShape.emotion, emotionShape);
					}
				}

				// Clear/define marker lists, to overwrite any previous file
				phonemeMarkers = new List<PhonemeMarker>();
				emotionMarkers = new List<EmotionMarker>();

				// Copy data from file into new lists
				foreach (PhonemeMarker marker in pData) {
					phonemeMarkers.Add(marker);
				}
				foreach (EmotionMarker marker in eData) {
					emotionMarkers.Add(marker);
				}

				// Phonemes are stored out of sequence in the file, for depth sorting in the editor
				// Sort them by timestamp to make finding the current one faster
				phonemeMarkers.Sort(SortTime);

				audioClip = clip;
				fileLength = duration;
			}
		}

		/// <summary>
		/// Processes the data into readable animation curves.
		/// Do not call before loading data.
		/// </summary>
		public void ProcessData() {
			if (enabled) {

				boneNeutralPositions = null;
				boneNeutralRotations = null;
				
				List<Transform> tempEmotionBones = null;
				List<TransformAnimationCurve> tempEmotionBoneCurves = null;

				List<Transform> tempBones = null;
				List<TransformAnimationCurve> tempBoneCurves = null;

				List<int> tempEmotionIndexBlendables = new List<int>();
				List<AnimationCurve> tempEmotionCurves = new List<AnimationCurve>();

				List<int> tempIndexBlendables = new List<int>();
				List<AnimationCurve> tempCurves = new List<AnimationCurve>();

				indexBlendables = new List<int>();
				animCurves = new List<AnimationCurve>();

				phonemeMarkers.Sort(SortTime);

				if (useBones) {
					boneNeutralPositions = new List<Vector3>();
					boneNeutralRotations = new List<Quaternion>();

					bones = new List<Transform>();
					boneCurves = new List<TransformAnimationCurve>();

					tempBones = new List<Transform>();
					tempBoneCurves = new List<TransformAnimationCurve>();

					tempEmotionBones = new List<Transform>();
					tempEmotionBoneCurves = new List<TransformAnimationCurve>();
				}

				List<Shape> shapes = new List<Shape>();

				// Add phonemes used
				foreach (PhonemeMarker marker in phonemeMarkers) {
					if (shapes.Count == System.Enum.GetNames(typeof(Phoneme)).Length) {
						break;
					}

					if (!shapes.Contains(phonemes[(int)marker.phoneme])) {
						shapes.Add(phonemes[(int)marker.phoneme]);

						foreach (int blendable in phonemes[(int)marker.phoneme].blendShapes) {
							if (!tempIndexBlendables.Contains(blendable)) {
								AnimationCurve curve = new AnimationCurve();
								curve.postWrapMode = WrapMode.Loop;
								tempCurves.Add(curve);
								tempIndexBlendables.Add(blendable);
							}

							if (!indexBlendables.Contains(blendable)) {
								AnimationCurve curve = new AnimationCurve();
								curve.postWrapMode = WrapMode.Loop;
								animCurves.Add(curve);
								indexBlendables.Add(blendable);
							}
						}

						if (useBones) {
							foreach (BoneShape boneShape in phonemes[(int)marker.phoneme].bones) {
								if (!tempBones.Contains(boneShape.bone)) {
									TransformAnimationCurve curve = new TransformAnimationCurve();
									curve.postWrapMode = WrapMode.Loop;
									tempBoneCurves.Add(curve);
									tempBones.Add(boneShape.bone);
								}

								if (!bones.Contains(boneShape.bone)) {
									TransformAnimationCurve curve = new TransformAnimationCurve();
									curve.postWrapMode = WrapMode.Loop;
									boneCurves.Add(curve);
									bones.Add(boneShape.bone);

									boneNeutralPositions.Add(boneShape.neutralPosition);
									boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation));
								}
							}
						}
					}
				}

				// Add emotions used
				foreach (EmotionMarker marker in emotionMarkers) {
					if (emotionCache.ContainsKey(marker.emotion)) {
						if (!shapes.Contains(emotionCache[marker.emotion])) {
							shapes.Add(emotionCache[marker.emotion]);

							foreach (int blendable in emotionCache[marker.emotion].blendShapes) {
								if (!tempEmotionIndexBlendables.Contains(blendable)) {
									AnimationCurve curve = new AnimationCurve();
									curve.postWrapMode = WrapMode.Loop;
									tempEmotionCurves.Add(curve);
									tempEmotionIndexBlendables.Add(blendable);
								}

								if (!indexBlendables.Contains(blendable)) {
									AnimationCurve curve = new AnimationCurve();
									curve.postWrapMode = WrapMode.Loop;
									animCurves.Add(curve);
									indexBlendables.Add(blendable);
								}
							}

							if (useBones) {
								foreach (BoneShape boneShape in emotionCache[marker.emotion].bones) {
									if (!tempEmotionBones.Contains(boneShape.bone)) {
										TransformAnimationCurve curve = new TransformAnimationCurve();
										curve.postWrapMode = WrapMode.Loop;
										tempEmotionBoneCurves.Add(curve);
										tempEmotionBones.Add(boneShape.bone);
									}

									if (!bones.Contains(boneShape.bone)) {
										TransformAnimationCurve curve = new TransformAnimationCurve();
										curve.postWrapMode = WrapMode.Loop;
										boneCurves.Add(curve);
										bones.Add(boneShape.bone);

										boneNeutralPositions.Add(boneShape.neutralPosition);
										boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation));
									}
								}
							}
						}
					} else {
						emotionMarkers.Remove(marker);
						break;
					}
				}

				// Add neutral start and end keys
				for (int index = 0; index < tempCurves.Count; index++) {
					tempCurves[index].AddKey(0, 0);
					tempCurves[index].AddKey(1, 0);
				}
				for (int index = 0; index < tempEmotionCurves.Count; index++) {
					tempEmotionCurves[index].AddKey(0, 0);
					tempEmotionCurves[index].AddKey(1, 0);
				}

				if (useBones) {
					for (int index = 0; index < tempBoneCurves.Count; index++) {
						tempBoneCurves[index].AddKey(0, boneNeutralPositions[bones.IndexOf(tempBones[index])], boneNeutralRotations[bones.IndexOf(tempBones[index])], 0, 0);
						tempBoneCurves[index].AddKey(1, boneNeutralPositions[bones.IndexOf(tempBones[index])], boneNeutralRotations[bones.IndexOf(tempBones[index])], 0, 0);
					}

					for (int index = 0; index < tempEmotionBoneCurves.Count; index++) {
						tempEmotionBoneCurves[index].AddKey(0, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], 0, 0);
						tempEmotionBoneCurves[index].AddKey(1, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], 0, 0);
					}
				}

				// Get temp keys from emotion markers
				foreach (EmotionMarker marker in emotionMarkers) {
					EmotionShape shape = emotionCache[marker.emotion];

					for (int index = 0; index < tempEmotionCurves.Count; index++) {
						if (shape.blendShapes.Contains(tempEmotionIndexBlendables[index])) {
							int b = shape.blendShapes.IndexOf(tempEmotionIndexBlendables[index]);

							float startWeight = 0;
							float endWeight = 0;

							if (marker.blendFromMarker) {
								EmotionMarker prevMarker = emotionMarkers[emotionMarkers.IndexOf(marker) - 1];
								EmotionShape prevShape = emotionCache[prevMarker.emotion];
								// Check if previous emotion used this blendable.
								if (prevShape.blendShapes.Contains(tempEmotionIndexBlendables[index])) {
									startWeight = prevShape.weights[prevShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * prevMarker.intensity;
								}
							}

							if (marker.blendToMarker) {
								EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
								EmotionShape nextShape = emotionCache[nextMarker.emotion];
								// Check if next emotion uses this blendable.
								if (nextShape.blendShapes.Contains(tempEmotionIndexBlendables[index])) {
									endWeight = nextShape.weights[nextShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * nextMarker.intensity;
								}
							}

							if (emotionCurveGenerationMode == CurveGenerationMode.Tight) {
								tempEmotionCurves[index].AddKey(new Keyframe(marker.startTime, startWeight, 0, 0));
								tempEmotionCurves[index].AddKey(new Keyframe(marker.startTime + marker.blendInTime, shape.weights[b] * marker.intensity, 0, 0));
								tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime + marker.blendOutTime, shape.weights[b] * marker.intensity, 0, 0));
								tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime, endWeight, 0, 0));
							} else if (emotionCurveGenerationMode == CurveGenerationMode.Loose) {
								tempEmotionCurves[index].AddKey(marker.startTime, startWeight);
								tempEmotionCurves[index].AddKey(marker.startTime + marker.blendInTime, shape.weights[b] * marker.intensity);
								tempEmotionCurves[index].AddKey(marker.endTime + marker.blendOutTime, shape.weights[b] * marker.intensity);
								tempEmotionCurves[index].AddKey(marker.endTime, endWeight);
							}
							
						} else {
							if (emotionCurveGenerationMode == CurveGenerationMode.Tight) {
								tempEmotionCurves[index].AddKey(new Keyframe(marker.startTime + marker.blendInTime, 0, 0, 0));
								tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime + marker.blendOutTime, 0, 0, 0));
							} else if (emotionCurveGenerationMode == CurveGenerationMode.Loose) {
								tempEmotionCurves[index].AddKey(marker.startTime + marker.blendInTime, 0);
								tempEmotionCurves[index].AddKey(marker.endTime + marker.blendOutTime, 0);
							}
							
							if (marker.blendToMarker) {
								EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
								EmotionShape nextShape = emotionCache[nextMarker.emotion];

								// Check if next emotion uses this blendable.
								if (nextShape.blendShapes.Contains(tempEmotionIndexBlendables[index])) {
									if (emotionCurveGenerationMode == CurveGenerationMode.Tight) {
										tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime, nextShape.weights[nextShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * nextMarker.intensity, 0, 0));
									} else if (emotionCurveGenerationMode == CurveGenerationMode.Loose) {
										tempEmotionCurves[index].AddKey(marker.endTime, nextShape.weights[nextShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * nextMarker.intensity);
									}
								}
							}
						}
					}

					if (useBones) {
						for (int index = 0; index < tempEmotionBoneCurves.Count; index++) {
							if (shape.HasBone(tempEmotionBones[index])) {
								int b = shape.IndexOfBone(tempEmotionBones[index]);

								Vector3 startPosition = shape.bones[b].neutralPosition;
								Vector3 startRotation = shape.bones[b].neutralRotation;
								Vector3 endPosition = shape.bones[b].neutralPosition;
								Vector3 endRotation = shape.bones[b].neutralRotation;

								if (marker.blendFromMarker) {
									EmotionMarker prevMarker = emotionMarkers[emotionMarkers.IndexOf(marker) - 1];
									EmotionShape prevShape = emotionCache[prevMarker.emotion];
									// Check if previous emotion used this blendable.
									if (prevShape.HasBone(tempEmotionBones[index])) {
										startPosition = prevShape.bones[prevShape.IndexOfBone(tempEmotionBones[index])].endPosition;
										startRotation = prevShape.bones[prevShape.IndexOfBone(tempEmotionBones[index])].endRotation;
									}
								}

								if (marker.blendToMarker) {
									EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
									EmotionShape nextShape = emotionCache[nextMarker.emotion];
									// Check if next emotion uses this blendable.
									if (nextShape.HasBone(tempEmotionBones[index])) {
										endPosition = nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])].endPosition;
										endRotation = nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])].endRotation;
									}
								}

								if (shape.bones[b].lockPosition && shape.bones[b].lockRotation) {
								} else if (shape.bones[b].lockPosition) {
									tempEmotionBoneCurves[index].AddKey(marker.startTime, Quaternion.Euler(startRotation), 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.startTime + marker.blendInTime, Quaternion.Euler(shape.bones[b].endRotation), 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.endTime + marker.blendOutTime, Quaternion.Euler(shape.bones[b].endRotation), 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.endTime, Quaternion.Euler(endRotation), 0, 0);
								} else if (shape.bones[b].lockRotation) {
									tempEmotionBoneCurves[index].AddKey(marker.startTime, startPosition, 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.startTime + marker.blendInTime, shape.bones[b].endPosition, 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.endTime - marker.blendOutTime, shape.bones[b].endPosition, 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.endTime, endPosition, 0, 0);
								} else {
									tempEmotionBoneCurves[index].AddKey(marker.startTime, startPosition, Quaternion.Euler(startRotation), 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.startTime + marker.blendInTime, shape.bones[b].endPosition, Quaternion.Euler(shape.bones[b].endRotation), 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.endTime + marker.blendOutTime, shape.bones[b].endPosition, Quaternion.Euler(shape.bones[b].endRotation), 0, 0);
									tempEmotionBoneCurves[index].AddKey(marker.endTime, endPosition, Quaternion.Euler(endRotation), 0, 0);
								}

							} else {
								tempEmotionBoneCurves[index].AddKey(marker.startTime + marker.blendInTime, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], 0, 0);
								tempEmotionBoneCurves[index].AddKey(marker.endTime + marker.blendOutTime, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], 0, 0);

								if (marker.blendToMarker) {
									EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
									EmotionShape nextShape = emotionCache[nextMarker.emotion];

									// Check if next emotion uses this blendable.
									if (nextShape.HasBone(tempEmotionBones[index])) {
										BoneShape b = nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])];
										if (b.lockPosition && b.lockRotation) {
										} else if (b.lockPosition) {
											tempEmotionBoneCurves[index].AddKey(marker.endTime, Quaternion.Euler(b.endRotation), 0, 0);
										} else if (b.lockRotation) {
											tempEmotionBoneCurves[index].AddKey(marker.endTime, b.endPosition, 0, 0);
										} else {
											tempEmotionBoneCurves[index].AddKey(marker.endTime, b.endPosition, Quaternion.Euler(b.endRotation), 0, 0);
										}
									}
								}
							}
						}
					}
				}

				// Get keys from phoneme track
				for (int m = 0; m < phonemeMarkers.Count; m++) {
					PhonemeMarker marker = phonemeMarkers[m];
					PhonemeShape shape = phonemes[(int)marker.phoneme];

					bool addRest = false;

					// Check for rests
					if (m + 1 < phonemeMarkers.Count) {
						if (phonemeMarkers[m + 1].time > marker.time + (restTime / fileLength) + (restHoldTime / fileLength)) {
							addRest = true;
						}
					} else {
						// Last marker, add rest after hold time
						addRest = true;
					}

					for (int index = 0; index < tempCurves.Count; index++) {
						if (shape.blendShapes.Contains(tempIndexBlendables[index])) {
							int b = shape.blendShapes.IndexOf(tempIndexBlendables[index]);

							if (phonemeCurveGenerationMode == CurveGenerationMode.Tight) {
								tempCurves[index].AddKey(new Keyframe(marker.time, shape.weights[b] * marker.intensity, 0, 0));

								//Check for pre-rest
								if (m == 0) {
									tempCurves[index].AddKey(new Keyframe(phonemeMarkers[m].time - (restHoldTime / fileLength), 0, 0, 0));
								}

								if (addRest) {
									// Add rest
									tempCurves[index].AddKey(new Keyframe(marker.time + (restHoldTime / fileLength), shape.weights[b] * marker.intensity, 0, 0));
									tempCurves[index].AddKey(new Keyframe(marker.time + ((restHoldTime / fileLength) * 2), 0, 0, 0));
									if (m + 1 < phonemeMarkers.Count) {
										tempCurves[index].AddKey(new Keyframe(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), 0, 0, 0));
									}
								}
							} else if (phonemeCurveGenerationMode == CurveGenerationMode.Loose) {
								tempCurves[index].AddKey(marker.time, shape.weights[b] * marker.intensity);

								//Check for pre-rest
								if (m == 0) {
									tempCurves[index].AddKey(phonemeMarkers[m].time - (restHoldTime / fileLength), 0);
								}

								if (addRest) {
									// Add rest
									tempCurves[index].AddKey(marker.time + (restHoldTime / fileLength), shape.weights[b] * marker.intensity);
									tempCurves[index].AddKey(marker.time + ((restHoldTime / fileLength) * 2), 0);
									if (m + 1 < phonemeMarkers.Count) {
										tempCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), 0);
									}
								}
							}
							
						} else {
							// Blendable isn't in this marker
							if (phonemeCurveGenerationMode == CurveGenerationMode.Tight) {
								tempCurves[index].AddKey(new Keyframe(marker.time, 0, 0, 0));
							} else if (phonemeCurveGenerationMode == CurveGenerationMode.Loose) {
								tempCurves[index].AddKey(marker.time, 0);
							}
							if (addRest) {
								if (m + 1 < phonemeMarkers.Count) {
									if (phonemeCurveGenerationMode == CurveGenerationMode.Tight) {
										tempCurves[index].AddKey(new Keyframe(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), 0, 0, 0));
									} else if (phonemeCurveGenerationMode == CurveGenerationMode.Loose) {
										tempCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), 0);
									}
								}
							}
						}
					}

					if (useBones) {
						for (int index = 0; index < tempBoneCurves.Count; index++) {
							if (shape.HasBone(bones[index])) {
								int b = shape.IndexOfBone(bones[index]);

								if (shape.bones[b].lockPosition && shape.bones[b].lockRotation) {
								} else if (shape.bones[b].lockPosition) {
									tempBoneCurves[index].AddKey(marker.time, Quaternion.Euler(shape.bones[b].endRotation), 0, 0);
								} else if (shape.bones[b].lockRotation) {
									tempBoneCurves[index].AddKey(marker.time, shape.bones[b].endPosition, 0, 0);
								} else {
									tempBoneCurves[index].AddKey(marker.time, shape.bones[b].endPosition, Quaternion.Euler(shape.bones[b].endRotation), 0, 0);
								}

								//Check for pre-rest
								if (m == 0) {
									tempBoneCurves[index].AddKey(phonemeMarkers[m].time - (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], 0, 0);
								}

								if (addRest) {
									// Add rest
									tempBoneCurves[index].AddKey(marker.time + (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], 0, 0);
									if (m + 1 < phonemeMarkers.Count) {
										tempBoneCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], 0, 0);
									}
								}
							} else {
								// Blendable isn't in this marker, get value from matching emotion curve if available

								tempBoneCurves[index].AddKey(marker.time, boneNeutralPositions[index], boneNeutralRotations[index], 0, 0);

								if (addRest) {
									if (m + 1 < phonemeMarkers.Count) {
										tempBoneCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], 0, 0);
									}
								}
							}
						}
					}
				}

				// Merge curve sets
				for (int c = 0; c < animCurves.Count; c++) {
					if (tempIndexBlendables.Contains(indexBlendables[c]) && tempEmotionIndexBlendables.Contains(indexBlendables[c])) {
						int pIndex = tempIndexBlendables.IndexOf(indexBlendables[c]);
						int eIndex = tempEmotionIndexBlendables.IndexOf(indexBlendables[c]);

						for (int k = 0; k < tempCurves[pIndex].keys.Length; k++) {
							Keyframe key = tempCurves[pIndex].keys[k];

							animCurves[c].AddKey(key);
						}

						for (int k = 0; k < tempEmotionCurves[eIndex].keys.Length; k++) {
							Keyframe key = tempEmotionCurves[eIndex].keys[k];

							animCurves[c].AddKey(key);
						}

					} else if (tempIndexBlendables.Contains(indexBlendables[c])) {
						int pIndex = tempIndexBlendables.IndexOf(indexBlendables[c]);
						animCurves[c] = tempCurves[pIndex];
					} else {
						int eIndex = tempEmotionIndexBlendables.IndexOf(indexBlendables[c]);
						animCurves[c] = tempEmotionCurves[eIndex];
					}
				}

				if (useBones) {
					for (int c = 0; c < boneCurves.Count; c++) {
						if (tempBones.Contains(bones[c]) && tempEmotionBones.Contains(bones[c])) {
							int pIndex = tempBones.IndexOf(bones[c]);
							int eIndex = tempEmotionBones.IndexOf(bones[c]);

							foreach (TransformAnimationCurve.TransformKeyframe key in tempBoneCurves[pIndex].keys) {
								boneCurves[c].AddKey(key.time, key.position, key.rotation, 0, 0);
							}

							foreach (TransformAnimationCurve.TransformKeyframe key in tempEmotionBoneCurves[eIndex].keys) {
								boneCurves[c].AddKey(key.time, key.position, key.rotation, 0, 0);
							}

						} else if (tempBones.Contains(bones[c])) {
							int pIndex = tempBones.IndexOf(bones[c]);
							boneCurves[c] = tempBoneCurves[pIndex];
						} else {
							int eIndex = tempEmotionBones.IndexOf(bones[c]);
							boneCurves[c] = tempEmotionBoneCurves[eIndex];
						}
					}
				}

			}
		}

		/// <summary>
		/// Clears the data cache, forcing the animation curves to be recalculated.
		/// </summary>
		public void ClearDataCache() {
			currentFileID = 0;
		}

		// -----------------
		// Private Functions
		// -----------------

		void FixEmotionBlends(ref List<EmotionMarker> data) {
			EmotionMarker[] markers = data.ToArray();
			FixEmotionBlends(ref markers);
			data.Clear();

			foreach (EmotionMarker marker in markers) {
				data.Add(marker);
			}
		}

		void FixEmotionBlends(ref EmotionMarker[] data) {

			foreach (EmotionMarker eMarker in data) {
				eMarker.blendFromMarker = false;
				eMarker.blendToMarker = false;
				if (!eMarker.customBlendIn) eMarker.blendInTime = 0;
				if (!eMarker.customBlendOut) eMarker.blendOutTime = 0;
				eMarker.invalid = false;
			}

			foreach (EmotionMarker eMarker in data) {
				foreach (EmotionMarker tMarker in data) {
					if (eMarker != tMarker) {
						if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime) {
							if (eMarker.customBlendIn) {
								eMarker.customBlendIn = false;
								FixEmotionBlends(ref data);
								return;
							}
							eMarker.blendFromMarker = true;

							if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime) {
								eMarker.invalid = true;
							} else {
								eMarker.blendInTime = tMarker.endTime - eMarker.startTime;
							}
						}

						if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime) {
							if (eMarker.customBlendOut) {
								eMarker.customBlendOut = false;
								FixEmotionBlends(ref data);
								return;
							}
							eMarker.blendToMarker = true;

							if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime) {
								eMarker.invalid = true;
							} else {
								eMarker.blendOutTime = tMarker.startTime - eMarker.endTime;
							}
						}
					}
				}
			}
		}

		private void LoadXML(TextAsset xmlFile, AudioClip linkedClip) {
			XmlDocument document = new XmlDocument();
			document.LoadXml(xmlFile.text);

			// Clear/define marker lists, to overwrite any previous file
			phonemeMarkers = new List<PhonemeMarker>();
			emotionMarkers = new List<EmotionMarker>();
			gestureMarkers = new List<GestureMarker>();

			audioClip = linkedClip;
			audioSource.clip = audioClip;

			//Create Dictionary for loading phonemes
			Dictionary<string, Phoneme> phonemeLookup = new Dictionary<string, Phoneme>() {
				{"AI"          , Phoneme.AI},
				{"CDGKNRSThYZ" , Phoneme.CDGKNRSThYZ},
				{"E"           , Phoneme.E},
				{"FV"          , Phoneme.FV},
				{"L"           , Phoneme.L},
				{"MBP"         , Phoneme.MBP},
				{"O"           , Phoneme.O},
				{"U"           , Phoneme.U},
				{"WQ"          , Phoneme.WQ}
			};

			string version = ReadXML(document, "LipSyncData", "version");

			if (string.IsNullOrEmpty(version)) {
				// Update data and show warning
				Debug.LogWarning("[LipSync - " + gameObject.name + "] Loading XML data file created in an alpha/beta version of LipSync. This data will need updating to play correctly.");

				if (fileLength == 0) {
					fileLength = audioClip.length;
				}
			} else {
				fileLength = float.Parse(ReadXML(document, "LipSyncData", "length"));
			}

			//Phonemes
			XmlNode phonemesNode = document.SelectSingleNode("//LipSyncData//phonemes");
			if (phonemesNode != null) {
				XmlNodeList phonemeNodes = phonemesNode.ChildNodes;

				for (int p = 0; p < phonemeNodes.Count; p++) {
					XmlNode node = phonemeNodes[p];

					if (node.LocalName == "marker") {
						Phoneme phoneme = (Phoneme)phonemeLookup[node.Attributes["phoneme"].Value];
						float time = float.Parse(node.Attributes["time"].Value) / fileLength;

						phonemeMarkers.Add(new PhonemeMarker(phoneme, time));
					}
				}
			}

			//Emotions
			XmlNode emotionsNode = document.SelectSingleNode("//LipSyncData//emotions");
			if (emotionsNode != null) {
				XmlNodeList emotionNodes = emotionsNode.ChildNodes;

				for (int p = 0; p < emotionNodes.Count; p++) {
					XmlNode node = emotionNodes[p];

					if (node.LocalName == "marker") {
						string emotion = node.Attributes["emotion"].Value;
						float startTime = float.Parse(node.Attributes["start"].Value) / fileLength;
						float endTime = float.Parse(node.Attributes["end"].Value) / fileLength;
						float blendInTime = float.Parse(node.Attributes["blendIn"].Value);
						float blendOutTime = float.Parse(node.Attributes["blendOut"].Value);
						bool blendTo = bool.Parse(node.Attributes["blendToMarker"].Value);
						bool blendFrom = bool.Parse(node.Attributes["blendFromMarker"].Value);
						bool customBlendIn = bool.Parse(node.Attributes["customBlendIn"].Value);
						bool customBlendOut = bool.Parse(node.Attributes["customBlendOut"].Value);

						emotionMarkers.Add(new EmotionMarker(emotion, startTime, endTime, blendInTime, blendOutTime, blendTo, blendFrom, customBlendIn, customBlendOut));
					}
				}

				if (string.IsNullOrEmpty(version)) {
					for (int e = 0; e < emotionMarkers.Count; e++) {
						if (emotionMarkers[e].blendFromMarker) {
							emotionMarkers[e].startTime -= emotionMarkers[e].blendInTime;
							emotionMarkers[e - 1].endTime += emotionMarkers[e].blendInTime;
						} else {
							emotionMarkers[e].customBlendIn = true;
						}

						if (emotionMarkers[e].blendToMarker) {
							emotionMarkers[e + 1].startTime -= emotionMarkers[e].blendOutTime;
							emotionMarkers[e].endTime += emotionMarkers[e].blendOutTime;
						} else {
							emotionMarkers[e].customBlendOut = true;
							emotionMarkers[e].blendOutTime = -emotionMarkers[e].blendOutTime;
						}
					}

					FixEmotionBlends(ref emotionMarkers);
				}
			}

			//Gestures
			XmlNode gesturesNode = document.SelectSingleNode("//LipSyncData//gestures");
			if (gesturesNode != null) {
				XmlNodeList gestureNodes = gesturesNode.ChildNodes;

				for (int p = 0; p < gestureNodes.Count; p++) {
					XmlNode node = gestureNodes[p];

					if (node.LocalName == "marker") {
						string gesture = node.Attributes["gesture"].Value;
						float time = float.Parse(node.Attributes["time"].Value) / fileLength;

						gestureMarkers.Add(new GestureMarker(gesture, time));
					}
				}
			}

			phonemeMarkers.Sort(SortTime);
			gestureMarkers.Sort(SortTime);
		}

		private bool LoadData(LipSyncData dataFile) {
			// Check that the referenced file contains data
			if (dataFile.phonemeData.Length > 0 || dataFile.emotionData.Length > 0 || dataFile.gestureData.Length > 0) {
				// Store reference to the associated AudioClip.
				audioClip = dataFile.clip;
				fileLength = dataFile.length;

				if (dataFile.version < 1) {
					// Update data and show warning
					Debug.LogWarning("[LipSync - " + gameObject.name + "] Loading LipSyncData file created in an alpha/beta version of LipSync. This data will need updating to play correctly.");

					for (int e = 0; e < dataFile.emotionData.Length; e++) {
						if (dataFile.emotionData[e].blendFromMarker) {
							dataFile.emotionData[e].startTime -= dataFile.emotionData[e].blendInTime;
							dataFile.emotionData[e - 1].endTime += dataFile.emotionData[e].blendInTime;
						} else {
							dataFile.emotionData[e].customBlendIn = true;
						}

						if (dataFile.emotionData[e].blendToMarker) {
							dataFile.emotionData[e + 1].startTime -= dataFile.emotionData[e].blendOutTime;
							dataFile.emotionData[e].endTime += dataFile.emotionData[e].blendOutTime;
						} else {
							dataFile.emotionData[e].customBlendOut = true;
							dataFile.emotionData[e].blendOutTime = -dataFile.emotionData[e].blendOutTime;
						}
					}

					FixEmotionBlends(ref dataFile.emotionData);

					if(dataFile.length == 0){
						fileLength = audioClip.length;
					}
				}

				// Clear/define marker lists, to overwrite any previous file
				phonemeMarkers = new List<PhonemeMarker>();
				emotionMarkers = new List<EmotionMarker>();
				gestureMarkers = new List<GestureMarker>();

				// Copy data from file into new lists
				foreach (PhonemeMarker marker in dataFile.phonemeData) {
					phonemeMarkers.Add(marker);
				}
				foreach (EmotionMarker marker in dataFile.emotionData) {
					emotionMarkers.Add(marker);
				}
				foreach (GestureMarker marker in dataFile.gestureData) {
					gestureMarkers.Add(marker);
				}

				// Phonemes are stored out of sequence in the file, for depth sorting in the editor
				// Sort them by timestamp to make finding the current one faster
				emotionMarkers.Sort(EmotionSort);
				phonemeMarkers.Sort(SortTime);
				gestureMarkers.Sort(SortTime);

				// Set current AudioClip in the AudioSource
				audioSource.clip = audioClip;

				// Save file InstanceID for later, to skip loading data that is already loaded
				currentFileID = dataFile.GetInstanceID();

				return true;
			} else {
				return false;
			}
		}

		GestureInstance GetGesture(string name) {
			for (int a = 0; a < gestures.Count; a++) {
				if (gestures[a].gesture == name) return gestures[a];
			}
			return null;
		}

		public LipSync() {
			// Constructor used to set version value on new component
			this.lastUsedVersion = 1.0f;
		}

		// Sort PhonemeMarker by timestamp
		public static int SortTime(PhonemeMarker a, PhonemeMarker b) {
			float sa = a.time;
			float sb = b.time;

			return sa.CompareTo(sb);
		}

		public static int SortTime(GestureMarker a, GestureMarker b) {
			float sa = a.time;
			float sb = b.time;

			return sa.CompareTo(sb);
		}

		static int EmotionSort(EmotionMarker a, EmotionMarker b) {
			return a.startTime.CompareTo(b.startTime);
		}

		public static string ReadXML(XmlDocument xml, string parentElement, string elementName) {
			XmlNode node = xml.SelectSingleNode("//" + parentElement + "//" + elementName);

			if (node == null) {
				return null;
			}

			return node.InnerText;
		}

		public enum CurveGenerationMode {
			Tight,
			Loose,
		}
	}

	// Master Phoneme List - Do not add additional phonemes to this list,
	// doing so may invalidate existing LipSyncData files.
	public enum Phoneme {
		AI,
		E,
		U,
		O,
		CDGKNRSThYZ,
		FV,
		L,
		MBP,
		WQ,
		Rest
	}
}