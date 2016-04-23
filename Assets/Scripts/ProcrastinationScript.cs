using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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
	private static bool choseSocial = false;
	private static bool chosePaper = false;
	private static bool choseGaming = false;
	private static bool choseDoctor = false;
	private bool hasStartPaperAudioBeenPlayed = false;
	private bool hasChosenBetweenSocialAndPaper = false;
	private bool chooseBetweenGameAndPaper = false;
	private bool hasSocialAndPaperAudioBeenPlayed = false;
	private bool hasGameAndPaperAudioBeenPlayed = false;
	public GameObject laptop;
	public GameObject mom;
	private Animator momAnimator;
	private bool hasChosenBetweenGameAndPaper = false;
	private bool medicalEmergencyStarted = false;
	public GameObject chair;
	private bool chooseBetweenDoctorAndPaper = false;
	private int paperCompletionPercent = 0;
	public Texture[] paperImages;
	public Texture[] webSearchImages;
	private bool showDocWindow = false;

	// Use this for initialization
	void Start () {
		screen1OriginalPosition = gameScreen1.transform.localPosition;
		screen2OriginalPosition = gameScreen2.transform.localPosition;
		screen3OriginalPosition = gameScreen3.transform.localPosition;
		screen4OriginalPosition = gameScreen4.transform.localPosition;
		playerAudioSource = GetComponent <CardboardAudioSource> ();
		momAnimator = mom.GetComponent <Animator> ();
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
			Debug.Log ("I should get started on the paper. But what topic? I should google for some."); //TODO: Play audio.
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
						Debug.Log ("Displaying web search window.");
						displayWebSearchWindow (paperCompletionPercent);
						Debug.Log ("Whatever topic I choose should be different than the rest of the class. " +
							"I desperately need an A in this. Maybe I should check facebook for a bit and " +
							"approach this with a fresh mind."); //TODO: Play audio.
						playerAudioSource.Play ();
						chooseBetweenSocialAndPaper = true;
						hasSocialAndPaperAudioBeenPlayed = true;
					}
					if(isOnSocialMedia) {
						StopSocialMedia ();
						Debug.Log ("Displaying web search window after social.");
						displayWebSearchWindow (paperCompletionPercent);
						Debug.Log ("Maybe I should go and play for a bit!"); //TODO: Play audio.
						playerAudioSource.Play ();
						chooseBetweenGameAndPaper = true;
						hasGameAndPaperAudioBeenPlayed = true;
					}
				} else if(hitObject.name.Contains ("Mom")) {
					if(medicalEmergencyStarted) {
						momAnimator.SetTrigger ("sit_talk");
						mom.GetComponent <CharacterRotation> ().facePlayer ();
						chooseBetweenDoctorAndPaper = true;
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
				hideWebSearchWindow ();
			}
			displayChoiceButtons ("Play Game");
			chooseBetweenGameAndPaper = false;
		}

		if(!playerAudioSource.isPlaying && hasChosenBetweenGameAndPaper && !medicalEmergencyStarted) {
			Debug.Log ("Son! Son!"); //TODO: Play Audio.
			medicalEmergencyStarted = true;
		}

		if(!playerAudioSource.isPlaying && medicalEmergencyStarted && chooseBetweenDoctorAndPaper) {
			displayChoiceButtons ("Go to Doctor");
			chooseBetweenDoctorAndPaper = false;
		}

		if(Input.GetKeyDown (KeyCode.Z)) {
			hideChoiceButtons ();
			if(squareButtonText.GetComponent <TextMesh>().text.Contains ("FB")) {
				choseSocial = true;
				hasChosenBetweenSocialAndPaper = true;
				StartSocialMedia ();
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Game")) {
				choseGaming = true;
				hasChosenBetweenGameAndPaper = true;
				Debug.Log ("Yes I should play on the XBOX for sometime! " +
					"I will start working again with a fresh mind!"); //TODO: Play audio.
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Doctor")) {
				choseDoctor = true;
				SceneManager.LoadScene ("Scene 3");
			}
		} else if(Input.GetKeyDown (KeyCode.C)) {
			hideChoiceButtons ();
			if(squareButtonText.GetComponent <TextMesh>().text.Contains ("FB")) {
				chosePaper = true;
				paperCompletionPercent = 33;
				hasChosenBetweenSocialAndPaper = true;
				chooseBetweenGameAndPaper = true;
				displayDocWindow (paperCompletionPercent);
				Debug.Log ("Maybe I should go and play for a bit!"); //TODO: Play audio.
				playerAudioSource.Play ();
				chooseBetweenGameAndPaper = true;
				hasGameAndPaperAudioBeenPlayed = true;
			} else if(squareButtonText.GetComponent <TextMesh>().text.Contains ("Game")) {
				chosePaper = true;
				paperCompletionPercent = 66;
				hasChosenBetweenGameAndPaper = true;
				displayDocWindow (paperCompletionPercent);
			} else if(chooseBetweenDoctorAndPaper) {
				chosePaper = true;
				paperCompletionPercent = 100;
				SceneManager.LoadScene ("Scene 1");
			}
		}

		if(isPlayingGame) {
			PlayGame ();
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

	private void PlayGame() {
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
	}

	private void hideChoiceButtons() {
		squareButton.SetActive (false);
		squareButtonText.SetActive (false);
		circleButton.SetActive (false);
		circleButtonText.SetActive (false);
	}
}