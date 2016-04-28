using UnityEngine;
using System.Collections;
using RogoDigital.Lipsync;
using UnityEngine.SceneManagement;

public class ClassroomScript : MonoBehaviour {
	private float waitBeforeProfessorStarts = 2.0f;
	private bool hasProfStartedSpeaking = false;
	public GameObject theProfessor;
	public LipSyncData profLipSyncData;
	private Animator profAnimator;
	public GameObject theClassmate;
	public LipSyncData classmateLipSyncData;
	private Animator classmateAnimator;
	public LayerMask layerMask;
	public CardboardReticle reticle;
	private bool hasClassmateStartedTalking = false;
	private float waitBeforeSceneChange = 1.0f;
	public LipSyncData classmatePositiveLipSyncData;
	public LipSyncData classmateNegativeLipSyncData;
	public AudioClip userMomDoctorClip;
	public bool momDoctorClipPlayed = false;

	// Use this for initialization
	void Start () {
		profAnimator = theProfessor.GetComponent<Animator> ();
		classmateAnimator = theClassmate.GetComponent<Animator> ();
	}
	
	// Update is called once per frame
	void Update () {
		RaycastHit hitInfo;
		if(waitBeforeProfessorStarts > 0.0f) {
			waitBeforeProfessorStarts -= Time.deltaTime;
		} else {
			if(!hasProfStartedSpeaking && !ProcrastinationScript.hasChosenBetweenSocialAndPaper && !ProcrastinationScript.hasChosenBetweenGameAndPaper) {
				profAnimator.SetBool ("isTalking", true);
				theProfessor.GetComponent<LipSync> ().Play (profLipSyncData);
				hasProfStartedSpeaking = true;
			} else {
				if(!theProfessor.GetComponent<LipSync> ().isPlaying) {
					profAnimator.SetBool ("isTalking", false);
					if (Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, 
							layerMask)) {
						GameObject hitObject = hitInfo.transform.gameObject;
						if(hitObject.name.Contains ("Student_Blendshapes") && !hasClassmateStartedTalking) {
							reticle.GetComponent<CardboardReticle> ().OnGazeStart (this.gameObject.GetComponentInChildren<Camera> (), 
								hitObject, hitInfo.point);
						} else {
							reticle.GetComponent<CardboardReticle> ().OnGazeExit (this.gameObject.GetComponentInChildren <Camera> (), 
								hitObject);
						}
					}
					if(Input.GetButtonDown ("Fire1")) {
						if (Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, 
								layerMask)) {
							GameObject hitObject = hitInfo.transform.gameObject;
							if (hitObject.name.Contains ("Student_Blendshapes")) {
								if(!ProcrastinationScript.hasChosenBetweenSocialAndPaper && !ProcrastinationScript.hasChosenBetweenGameAndPaper) {
									theClassmate.GetComponent<LipSync> ().Play (classmateLipSyncData);
								} else {
									if(ProcrastinationScript.choseSocial || ProcrastinationScript.choseGaming) {
										theClassmate.GetComponent<LipSync> ().Play(classmatePositiveLipSyncData);
									}

									if(!ProcrastinationScript.choseSocial && !ProcrastinationScript.choseGaming) {
										theClassmate.GetComponent<LipSync> ().Play(classmateNegativeLipSyncData);
									}
								}
								hasClassmateStartedTalking = true;
							}
						}
					}
					if(hasClassmateStartedTalking && !theClassmate.GetComponent<LipSync> ().isPlaying) {
						/*if(waitBeforeSceneChange > 0.0f) {
							waitBeforeSceneChange -= Time.deltaTime;
						} else {*/
							if(!ProcrastinationScript.hasChosenBetweenSocialAndPaper && !ProcrastinationScript.hasChosenBetweenGameAndPaper) {
								Invoke ("loadScene2", 1.0f);
							} else {
								if(ProcrastinationScript.choseDoctor) {
									Invoke ("loadScene4", 1.0f);
								} else {
									if(!momDoctorClipPlayed) {
										CardboardAudioSource playerAudioSource = this.gameObject.GetComponent<CardboardAudioSource> ();
										playerAudioSource.clip = userMomDoctorClip;
										playerAudioSource.Play ();
										momDoctorClipPlayed = true;
										Invoke ("loadScene3", userMomDoctorClip.length + 0.5f);
									}
								}
							}
						/*}*/
					}
				}
			}
		}
	}

	public void loadScene3() {
		SceneManager.LoadScene ("Scene 3");
	}

	public void loadScene4() {
		SceneManager.LoadScene ("Scene 4");
	}

	public void loadScene2() {
		SceneManager.LoadScene ("Scene 2");
	}
}
