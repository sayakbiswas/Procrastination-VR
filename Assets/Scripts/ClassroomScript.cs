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
			if(!hasProfStartedSpeaking) {
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
								theClassmate.GetComponent<LipSync> ().Play (classmateLipSyncData);
								hasClassmateStartedTalking = true;
							}
						}
					}
					if(hasClassmateStartedTalking && !theClassmate.GetComponent<LipSync> ().isPlaying) {
						if(waitBeforeSceneChange > 0.0f) {
							waitBeforeSceneChange -= Time.deltaTime;
						} else {
							SceneManager.LoadScene ("Scene 2");
						}
					}
				}
			}
		}
	}
}
