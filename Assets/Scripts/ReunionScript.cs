using UnityEngine;
using System.Collections;
using RogoDigital.Lipsync;

public class ReunionScript : MonoBehaviour {
	private float waitBeforeClassmateStarts = 1.0f;
	public GameObject theClassmate;
	private bool hasClassmateStartedTalking = false;
	public LipSyncData classmateHelloLipSyncData;
	private Animator classmateAnimator;
	public AudioClip bossClip;
	public LayerMask layerMask;
	public CardboardReticle reticle;
	public LipSyncData classmatePositiveLipSyncData;
	public LipSyncData classmateNegativeLipSyncData;
	private bool hasBossCalled = false;
	private AudioSource playerAudioSource;
	public AudioClip ringtoneClip;
	public GameObject squareButton;
	public GameObject squareButtonText;
	private bool gameOver = false;
	private float waitBeforeExit = 1.0f;

	// Use this for initialization
	void Start () {
		classmateAnimator = theClassmate.GetComponent<Animator> ();
		playerAudioSource = gameObject.GetComponent<AudioSource> ();
	}
	
	// Update is called once per frame
	void Update () {
		RaycastHit hitInfo;
		if(waitBeforeClassmateStarts > 0.0f) {
			waitBeforeClassmateStarts -= Time.deltaTime;
		} else {
			if(!hasClassmateStartedTalking) {
				classmateAnimator.SetTrigger ("sayHi");
				classmateAnimator.GetComponent<LipSync> ().Play (classmateHelloLipSyncData);
				hasClassmateStartedTalking = true;
			} else {
				if (Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, 
						layerMask)) {
					GameObject hitObject = hitInfo.transform.gameObject;
					if (hitObject.name.Contains ("Classmate")) {
						if (!gameOver) {
							reticle.GetComponent<CardboardReticle> ().OnGazeStart (this.gameObject.GetComponentInChildren<Camera> (), 
								hitObject, hitInfo.point);
						}
					} else {
						reticle.GetComponent<CardboardReticle> ().OnGazeExit (this.gameObject.GetComponentInChildren<Camera> (), 
							hitObject);
					}
				}

				if(!theClassmate.GetComponent <LipSync>().isPlaying && !hasBossCalled 
					&& (ProcrastinationScript.choseGaming || ProcrastinationScript.choseSocial)) {
					playBossCall ();
					hasBossCalled = true;
				}

				if(hasBossCalled && squareButton.activeSelf) {
					if(Input.GetButtonDown ("Fire2")) {
						hideActionButton ();
						playerAudioSource.clip = bossClip;
						playerAudioSource.Play ();
					}
				}

				if (Input.GetButtonDown ("Fire1")) {
					if (Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, 
							layerMask)) {
						GameObject hitObject = hitInfo.transform.gameObject;
						if (hitObject.name.Contains ("Classmate")) {
							if(!theClassmate.GetComponent<LipSync> ().isPlaying && !playerAudioSource.isPlaying) {
								classmateAnimator.SetTrigger ("startTalking");
								if(hasBossCalled) {
									classmateAnimator.GetComponent<LipSync> ().Play (classmatePositiveLipSyncData);
								} else {
									classmateAnimator.GetComponent<LipSync> ().Play (classmateNegativeLipSyncData);
								}
								gameOver = true;
							}
						}
					}
				}
			}
		}

		if(gameOver && !theClassmate.GetComponent<LipSync> ().isPlaying) {
			classmateAnimator.SetTrigger ("isIdle");
			if(waitBeforeExit > 0.0f) {
				waitBeforeExit -= Time.deltaTime;
			} else {
				Debug.Log ("That's all folks!");
				Application.Quit ();
			}
		}
	}

	private void playBossCall() {
		playerAudioSource.clip = ringtoneClip;
		playerAudioSource.Play ();
		displayActionButton ();
	}

	private void displayActionButton() {
		squareButton.SetActive (true);
		squareButtonText.SetActive (true);
	}

	private void hideActionButton() {
		squareButton.SetActive (false);
		squareButtonText.SetActive (false);
	}
}
