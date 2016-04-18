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

	// Use this for initialization
	void Start () {
		screen1OriginalPosition = gameScreen1.transform.localPosition;
		screen2OriginalPosition = gameScreen2.transform.localPosition;
		screen3OriginalPosition = gameScreen3.transform.localPosition;
		screen4OriginalPosition = gameScreen4.transform.localPosition;
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
					if(!isOnSocialMedia) {
						StartSocialMedia ();
					} else {
						StopSocialMedia ();
					}
				}
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
		RenderSettings.ambientIntensity = 0.0f;
		DynamicGI.UpdateEnvironment ();
		pointLight.enabled = true;
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
		pointLight.enabled = false;
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
		pointLight.enabled = true;
		float i = 0.0f;
		foreach(GameObject gameObject in procrastinationObjects) {
			i++;
			gameObject.SetActive (true);
			iTween.MoveTo (gameObject, iTween.Hash ("path", iTweenPath.GetPath (gameObject.name + "Path"), "time", 3.0f+i, 
				"looptype", iTween.LoopType.loop, "orientToPath", true, "easetype", iTween.EaseType.linear, "islocal", false));
		}
		isOnSocialMedia = true;
	}

	private void StopSocialMedia() {
		foreach(GameObject gameObject in procrastinationObjects){
			iTween.Stop (gameObject);
			gameObject.SetActive (false);
		}
		pointLight.enabled = false;
		directionalLight.enabled = true;
		RenderSettings.ambientIntensity = 1.0f;
		DynamicGI.UpdateEnvironment ();
		isOnSocialMedia = false;
	}
}
