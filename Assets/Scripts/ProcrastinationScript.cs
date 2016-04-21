using UnityEngine;
using System.Collections;

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
	private bool hasStartPaperAudioBeenPlayed = false;
	private bool hasChosenBetweenSocialAndPaper = false;
	private bool chooseBetweenGameAndPaper = false;
	private bool hasSocialAndPaperAudioBeenPlayed = false;
	private bool hasGameAndPaperAudioBeenPlayed = false;

	// Use this for initialization
	void Start () {
		screen1OriginalPosition = gameScreen1.transform.localPosition;
		screen2OriginalPosition = gameScreen2.transform.localPosition;
		screen3OriginalPosition = gameScreen3.transform.localPosition;
		screen4OriginalPosition = gameScreen4.transform.localPosition;
		playerAudioSource = GetComponent <CardboardAudioSource> ();
	}

	// Update is called once per frame
	void Update () {
		RaycastHit hitInfo;
		if(Physics.Raycast (Cardboard.SDK.GetComponentInChildren<CardboardHead> ().Gaze, out hitInfo, Mathf.Infinity, layerMask)) {
			GameObject hitObject = hitInfo.transform.gameObject;
			if(hitObject.name.Contains ("TV") || hitObject.name.Contains ("controller") || hitObject.name.Contains ("laptop")) {
				reticle.GetComponent<CardboardReticle> ().OnGazeStart (this.gameObject.GetComponentInChildren<Camera> (), 
					hitObject, hitInfo.point);
			} else {
				reticle.GetComponent<CardboardReticle> ().OnGazeExit (this.gameObject.GetComponentInChildren <Camera> (), hitObject);
			}
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
					Debug.Log ("I should get started on the paper. But what topic? I should google for some."); //TODO: Play audio.
					hasStartPaperAudioBeenPlayed = true;
					playerAudioSource.Play ();
					if(isOnSocialMedia) {
						StopSocialMedia ();
					}
				}
			}
		}

		if(!playerAudioSource.isPlaying) {
			if(hasStartPaperAudioBeenPlayed && !hasSocialAndPaperAudioBeenPlayed) {
				showWebSearchWindow = true;
				webSearchWindow.SetActive (true);
				Debug.Log ("Whatever topic I choose should be different than the rest of the class. " +
					"I desperately need an A in this. Maybe I should check facebook for a bit and " +
					"approach this with a fresh mind."); //TODO: Play audio.
				playerAudioSource.Play ();
				chooseBetweenSocialAndPaper = true;
				hasSocialAndPaperAudioBeenPlayed = true;
			}

			if(hasSocialAndPaperAudioBeenPlayed && hasChosenBetweenSocialAndPaper  && !isOnSocialMedia 
				&& !hasGameAndPaperAudioBeenPlayed) {
				Debug.Log ("Maybe I should go and play for a bit!"); //TODO: Play audio.
				playerAudioSource.Play ();
				chooseBetweenGameAndPaper = true;
				hasGameAndPaperAudioBeenPlayed = true;
			}
		}

		if(showWebSearchWindow && !playerAudioSource.isPlaying && chooseBetweenSocialAndPaper) {
			showWebSearchWindow = false;
			webSearchWindow.SetActive (false);
			squareButton.SetActive (true);
			squareButtonText.SetActive (true);
			circleButton.SetActive (true);
			circleButtonText.SetActive (true);
		}

		if(showWebSearchWindow && !playerAudioSource.isPlaying && chooseBetweenGameAndPaper) {
			Debug.Log ("Enbling choices again");
			showWebSearchWindow = false;
			webSearchWindow.SetActive (false);
			squareButton.SetActive (true);
			squareButtonText.GetComponent <TextMesh>().text = "Play Game";
			squareButtonText.SetActive (true);
			circleButton.SetActive (true);
			circleButtonText.SetActive (true);
		}

		if(Input.GetKeyDown (KeyCode.Z)) {
			squareButton.SetActive (false);
			squareButtonText.SetActive (false);
			circleButton.SetActive (false);
			circleButtonText.SetActive (false);
			if(chooseBetweenSocialAndPaper) {
				choseSocial = true;
				chooseBetweenSocialAndPaper = false;
				StartSocialMedia ();
			} else if(chooseBetweenGameAndPaper) {
				choseGaming = true;
				chooseBetweenGameAndPaper = false;
				Debug.Log ("Yes I should play on the XBOX for sometime! " +
					"I will start working again with a fresh mind!"); //TODO: Play audio.
			}
		} else if(Input.GetKeyDown (KeyCode.C)) {
			squareButton.SetActive (false);
			squareButtonText.SetActive (false);
			circleButton.SetActive (false);
			circleButtonText.SetActive (false);
			if(chooseBetweenSocialAndPaper) {
				chosePaper = true;
				chooseBetweenSocialAndPaper = false;
				chooseBetweenGameAndPaper = true;
				showWebSearchWindow = true;
				webSearchWindow.SetActive (true);
			} else if(chooseBetweenGameAndPaper) {
				chosePaper = true;
				chooseBetweenGameAndPaper = false;
				showWebSearchWindow = true;
				webSearchWindow.SetActive (true);
			}
		}

		if(isPlayingGame) {
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
		isOnSocialMedia = true;
		hasChosenBetweenSocialAndPaper = true;
	}

	private void StopSocialMedia() {
		foreach(GameObject gameObject in procrastinationObjects){
			iTween.Stop (gameObject);
			gameObject.SetActive (false);
		}
		pointLight.GetComponent <Light>().enabled = false;
		directionalLight.enabled = true;
		RenderSettings.ambientIntensity = 1.0f;
		DynamicGI.UpdateEnvironment ();
		showWebSearchWindow = true;
		webSearchWindow.SetActive (true);
		isOnSocialMedia = false;
	}
}
