using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using RogoDigital.Lipsync;

public class ProcrastinationScript : MonoBehaviour {
	public CardboardReticle reticle;
	public LayerMask layerMask;
	private bool isPlayingGame = false;
	public GameObject gameScreen;
	public GameObject tv;
	public GameObject gameScreen1;
	public GameObject gameScreen2;
	public GameObject gameScreen3;
	public GameObject gameScreen4;
	public Texture[] gameTextures;
	private Vector3 screen1OriginalPosition;
	private Vector3 screen2OriginalPosition;
	private Vector3 screen3OriginalPosition;
	private Vector3 screen4OriginalPosition;
	private int screen1textureChangeCount = 0;
	private int screen2textureChangeCount = 1;
	private int screen3textureChangeCount = 2;
	private int screen4textureChangeCount = 3;
	public GameObject[] procrastinationObjects;
	public Light directionalLight;
	public Light pointLight;
	private bool isOnSocialMedia = false;
	public GameObject webSearchWindow;
	public GameObject docWindow;
	public GameObject squareButton;
	public GameObject circleButton;
	public GameObject squareButtonText;
	public GameObject circleButtonText;
	private bool showWebSearchWindow = false;
	private CardboardAudioSource playerAudioSource;
	private bool chooseBetweenSocialAndPaper = false;
	public static bool choseSocial = false;
	public static bool chosePaper = false;
	public static bool choseGaming = false;
	public static bool choseDoctor = false;
	private bool hasStartPaperAudioBeenPlayed = false;
	public static bool hasChosenBetweenSocialAndPaper = false;
	private bool chooseBetweenGameAndPaper = false;
	private bool hasSocialAndPaperAudioBeenPlayed = false;
	private bool hasGameAndPaperAudioBeenPlayed = false;
	public GameObject laptop;
	public GameObject mom;
	private Animator momAnimator;
	public static bool hasChosenBetweenGameAndPaper = false;
	private bool medicalEmergencyStarted = false;
	public GameObject chair;
	private bool chooseBetweenDoctorAndPaper = false;
	private int paperCompletionPercent = 0;
	public Texture[] paperImages;
	public Texture[] webSearchImages;
	private bool showDocWindow = false;
	public AudioClip[] momAudioClips;
	private CardboardAudioSource momCardboardAudioSource;
	public LipSyncData momLipSyncData;
	public AudioClip[] userAudioClips;
	private bool areChoiceButtonsDisplayed = false;

	// Use this for initialization
	void Start () {
		screen1OriginalPosition = gameScreen1.transform.localPosition;
		screen2OriginalPosition = gameScreen2.transform.localPosition;
		screen3OriginalPosition = gameScreen3.transform.localPosition;
		screen4OriginalPosition = gameScreen4.transform.localPosition;
		playerAudioSource = GetComponent <CardboardAudioSource> ();
		momAnimator = mom.GetComponent <Animator> ();
		momCardboardAudioSource = mom.GetComponent<CardboardAudioSource> ();
	}

	// Update is called once per frame
	void Update () {
		RaycastHit hitInfo;
		if(Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, layerMask)) {
			GameObject hitObject = hitInfo.transform.gameObject;
			if(hitObject.name.Contains ("TV") || hitObject.name.Contains ("controller") || hitObject.name.Contains ("laptop")
				|| hitObject.name.Contains ("WebSearch") || hitObject.name.Contains ("Document") 
				|| hitObject.name.Contains ("Mom")) {
				reticle.GetComponent<CardboardReticle> ().OnGazeStart (this.gameObject.GetComponentInChildren<Camera> (), 
					hitObject, hitInfo.point);
			} else {
				reticle.GetComponent<CardboardReticle> ().OnGazeExit (this.gameObject.GetComponentInChildren <Camera> (), hitObject);
			}
		}

		if(!hasStartPaperAudioBeenPlayed) {
			playerAudioSource.clip = userAudioClips [0];
			playerAudioSource.Play ();
			hasStartPaperAudioBeenPlayed = true;
		}

		if(Input.GetButtonDown ("Fire1")) {
			if (Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, layerMask)) {
				GameObject hitObject = hitInfo.transform.gameObject;
				if(hitObject.name.Contains ("TV") || hitObject.name.Contains ("controller")) {
					if(!isPlayingGame) {
						StartGaming ();
					} else {
						StopGaming ();
					}
				} else if(hitObject.name.Contains ("laptop") || hitObject.name.Contains ("paper")) {
					if(hasStartPaperAudioBeenPlayed && !hasSocialAndPaperAudioBeenPlayed) {
						displayWebSearchWindow (paperCompletionPercent);
						Invoke ("decideToSocial", 2.0f);
					}
					if(isOnSocialMedia) {
						StopSocialMedia ();
						displayWebSearchWindow (paperCompletionPercent);
						Invoke ("decideToPlay", 2.0f);
					}
				} else if(hitObject.name.Contains ("Mom")) {
					if(medicalEmergencyStarted) {
						momCardboardAudioSource.Stop ();
						playerAudioSource.clip = userAudioClips [4];
						playerAudioSource.Play ();
						momAnimator.SetTrigger ("sit_talk");
						mom.GetComponent <CharacterRotation> ().facePlayer ();
						Invoke ("startMomDialog", userAudioClips[4].length + 0.5f);
					}
				} else {
					if(showDocWindow) {
						showDocWindow = false;
						hideDocWindow ();
					}

					if(showWebSearchWindow) {
						showWebSearchWindow = false;
						hideWebSearchWindow ();
					}
				}
			}
		}

		if(!playerAudioSource.isPlaying && chooseBetweenSocialAndPaper && hasSocialAndPaperAudioBeenPlayed) {
			hideWebSearchWindow ();
			displayChoiceButtons ("Check FB");
			chooseBetweenSocialAndPaper = false;
		}

		if(!playerAudioSource.isPlaying && chooseBetweenGameAndPaper && hasGameAndPaperAudioBeenPlayed) {
			if(showWebSearchWindow) {
				hideWebSearchWindow ();
			}
			if(showDocWindow) {
				hideDocWindow ();
			}
			displayChoiceButtons ("Play Game");
			chooseBetweenGameAndPaper = false;
		}

		if(!playerAudioSource.isPlaying && hasChosenBetweenGameAndPaper && !medicalEmergencyStarted && !isPlayingGame) {
			momCardboardAudioSource.clip = momAudioClips [0];
			momCardboardAudioSource.volume = 1.0f;
			momCardboardAudioSource.loop = false;
			momCardboardAudioSource.Play ();
			medicalEmergencyStarted = true;
		}

		if(!playerAudioSource.isPlaying && medicalEmergencyStarted && chooseBetweenDoctorAndPaper 
			&& !mom.GetComponent<LipSync> ().isPlaying) {
			displayChoiceButtons ("Go to Doctor");
			chooseBetweenDoctorAndPaper = false;
		}

		if(Input.GetButtonDown ("Fire2") && areChoiceButtonsDisplayed) {
			hideChoiceButtons ();
			if(squareButtonText.GetComponent <TextMesh>().text.Contains ("FB")) {
				choseSocial = true;
				hasChosenBetweenSocialAndPaper = true;
				StartSocialMedia ();
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Game")) {
				choseGaming = true;
				playerAudioSource.clip = userAudioClips [3];
				playerAudioSource.Play ();
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Doctor")) {
				choseDoctor = true;
				displayDocWindow (paperCompletionPercent);
				Invoke ("goToDoctor", 2.0f);
			}
		} else if(Input.GetButtonDown ("Fire3") && areChoiceButtonsDisplayed) {
			hideChoiceButtons ();
			if(squareButtonText.GetComponent <TextMesh>().text.Contains ("FB")) {
				chosePaper = true;
				paperCompletionPercent = 33;
				hasChosenBetweenSocialAndPaper = true;
				chooseBetweenGameAndPaper = true;
				displayDocWindow (paperCompletionPercent);
				Invoke ("decideToPlay", 2.0f);
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Game")) {
				chosePaper = true;
				if(paperCompletionPercent == 33) {
					paperCompletionPercent = 66;
				} else {
					paperCompletionPercent = 33;
				}
				hasChosenBetweenGameAndPaper = true;
				displayDocWindow (paperCompletionPercent);
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Doctor")) {
				chosePaper = true;
				if(paperCompletionPercent == 0) {
					paperCompletionPercent = 33;
				} else if(paperCompletionPercent == 33) {
					paperCompletionPercent = 66;
				} else {
					paperCompletionPercent = 100;
				}
				displayDocWindow (paperCompletionPercent);
				Invoke ("backToSchool", 2.0f);
			}
		}

		if(isPlayingGame) {
			PlayGameEffects ();
		}
	}

	private void StartGaming() {
		directionalLight.enabled = false;
		pointLight.GetComponent <Light>().enabled = true;
		RenderSettings.ambientIntensity = 0.0f;
		DynamicGI.UpdateEnvironment ();
		gameScreen.SetActive (true);
		CardboardAudioSource tvAudioSource = tv.GetComponent<CardboardAudioSource> ();
		tvAudioSource.Play ();
		gameScreen1.SetActive (true);
		gameScreen2.SetActive (true);
		gameScreen3.SetActive (true);
		gameScreen4.SetActive (true);
		isPlayingGame = true;
		if(hasChosenBetweenSocialAndPaper) {
			hasChosenBetweenGameAndPaper = true;
		}
	}

	private void PlayGameEffects() {
		gameScreen1.transform.Translate (0, 0, -Time.deltaTime * 1.4f);
		if(gameScreen1.transform.localPosition.z <= -0.1f) {
			screen1textureChangeCount += 1;
			gameScreen1.GetComponent<Renderer> ().material.mainTexture = gameTextures[(screen1textureChangeCount % gameTextures.Length)];
			gameScreen1.transform.localPosition = screen1OriginalPosition;
		}

		gameScreen2.transform.Translate (0, 0, -Time.deltaTime * 1.4f);
		if(gameScreen2.transform.localPosition.z <= -0.1f) {
			screen2textureChangeCount += 1;
			gameScreen2.GetComponent<Renderer> ().material.mainTexture = gameTextures[(screen2textureChangeCount % gameTextures.Length)];
			gameScreen2.transform.localPosition = screen2OriginalPosition;
		}

		gameScreen3.transform.Translate (0, Time.deltaTime * 1.4f, 0);
		if(gameScreen3.transform.localPosition.z <= -0.1f) {
			screen3textureChangeCount += 1;
			gameScreen3.GetComponent<Renderer> ().material.mainTexture = gameTextures[(screen3textureChangeCount % gameTextures.Length)];
			gameScreen3.transform.localPosition = screen3OriginalPosition;
		}

		gameScreen4.transform.Translate (0, Time.deltaTime * 1.4f, 0);
		if(gameScreen4.transform.localPosition.z <= -0.1f) {
			screen4textureChangeCount += 1;
			gameScreen4.GetComponent<Renderer> ().material.mainTexture = gameTextures[(screen4textureChangeCount % gameTextures.Length)];
			gameScreen4.transform.localPosition = screen4OriginalPosition;
		}
	}

	private void StopGaming() {
		CardboardAudioSource tvAudioSource = tv.GetComponent<CardboardAudioSource> ();
		tvAudioSource.Stop ();
		pointLight.GetComponent <Light>().enabled = false;
		directionalLight.enabled = true;
		RenderSettings.ambientIntensity = 1.0f;
		DynamicGI.UpdateEnvironment ();
		gameScreen.SetActive (false);
		gameScreen1.SetActive (false);
		gameScreen2.SetActive (false);
		gameScreen3.SetActive (false);
		gameScreen4.SetActive (false);
		isPlayingGame = false;
	}

	private void StartSocialMedia() {
		directionalLight.enabled = false;
		RenderSettings.ambientIntensity = 0.0f;
		DynamicGI.UpdateEnvironment ();
		pointLight.GetComponent <Light>().enabled = true;
		float i = 0.0f;
		foreach(GameObject gameObject in procrastinationObjects) {
			i++;
			gameObject.SetActive (true);
			iTween.MoveTo (gameObject, iTween.Hash ("path", iTweenPath.GetPath (gameObject.name + "Path"), "time", 5.0f+i, 
				"looptype", iTween.LoopType.loop, "orientToPath", false, "easetype", iTween.EaseType.linear, 
				"islocal", true));
		}
		CardboardAudioSource laptopAudioSource = laptop.GetComponent<CardboardAudioSource> ();
		laptopAudioSource.Play ();
		isOnSocialMedia = true;
		hasChosenBetweenSocialAndPaper = true;
	}

	private void StopSocialMedia() {
		foreach(GameObject gameObject in procrastinationObjects){
			iTween.Stop (gameObject);
			gameObject.SetActive (false);
		}
		CardboardAudioSource laptopAudioSource = laptop.GetComponent<CardboardAudioSource> ();
		laptopAudioSource.Stop ();
		pointLight.GetComponent <Light>().enabled = true;
		directionalLight.enabled = true;
		RenderSettings.ambientIntensity = 1.0f;
		DynamicGI.UpdateEnvironment ();
		showWebSearchWindow = true;
		webSearchWindow.SetActive (true);
		isOnSocialMedia = false;
	}

	private void displayWebSearchWindow(int paperCompletionPercent) {
		pointLight.GetComponent <Light> ().enabled = true;
		showWebSearchWindow = true;
		Texture webSearchImage = webSearchWindow.GetComponent <Renderer>().material.mainTexture;
		if(paperCompletionPercent == 0) {
			webSearchImage = webSearchImages[0];
		} else if(paperCompletionPercent == 33) {
			webSearchImage = webSearchImages [1];
		} else if(paperCompletionPercent == 66) {
			webSearchImage = webSearchImages [2];
		} else if(paperCompletionPercent == 100) {
			webSearchImage = webSearchImages [3];
		}
		webSearchWindow.GetComponent <Renderer>().material.mainTexture = webSearchImage;
		webSearchWindow.SetActive (true);
	}

	private void hideWebSearchWindow() {
		pointLight.GetComponent <Light> ().enabled = false;
		showWebSearchWindow = false;
		webSearchWindow.SetActive (false);
	}

	private void displayDocWindow(int paperCompletionPercent) {
		pointLight.GetComponent <Light> ().enabled = true;
		showDocWindow = true;
		Texture paperImage = docWindow.GetComponent <Renderer>().material.mainTexture;
		if(paperCompletionPercent == 0) {
			paperImage = paperImages [0];
		} else if(paperCompletionPercent == 33) {
			paperImage = paperImages [1];
		} else if(paperCompletionPercent == 66) {
			paperImage = paperImages [2];
		} else if(paperCompletionPercent == 100) {
			paperImage = paperImages [3];
		}
		docWindow.GetComponent <Renderer> ().material.mainTexture = paperImage;
		docWindow.SetActive (true);
	}

	private void hideDocWindow() {
		pointLight.GetComponent <Light> ().enabled = false;
		showDocWindow = false;
		docWindow.SetActive (false);
	}

	private void displayChoiceButtons(string choiceText) {
		squareButton.SetActive (true);
		squareButtonText.GetComponent <TextMesh>().text = choiceText;
		squareButtonText.SetActive (true);
		circleButton.SetActive (true);
		circleButtonText.SetActive (true);
		areChoiceButtonsDisplayed = true;
	}

	private void hideChoiceButtons() {
		squareButton.SetActive (false);
		squareButtonText.SetActive (false);
		circleButton.SetActive (false);
		circleButtonText.SetActive (false);
		areChoiceButtonsDisplayed = false;
	}

	private void decideToPlay() {
		playerAudioSource.clip = userAudioClips [2];
		playerAudioSource.Play ();
		chooseBetweenGameAndPaper = true;
		hasGameAndPaperAudioBeenPlayed = true;
	}

	private void decideToSocial() {
		playerAudioSource.clip = userAudioClips [1];
		playerAudioSource.Play ();
		chooseBetweenSocialAndPaper = true;
		hasSocialAndPaperAudioBeenPlayed = true;
	}

	private void backToSchool() {
		SceneManager.LoadScene ("Scene 1");
	}

	private void goToDoctor() {
		SceneManager.LoadScene ("Scene 3");
	}

	private void startMomDialog() {
		mom.GetComponent <LipSync> ().Play (momLipSyncData);
		chooseBetweenDoctorAndPaper = true;
	}
}