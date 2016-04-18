using UnityEngine;
using System.Collections;

public class MoveGameScreen : MonoBehaviour {

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

	}

	public void moveScreen() {
		iTween.MoveTo (gameObject, iTween.Hash ("path", iTweenPath.GetPath ("GameScreen1Path"), "time", 500.0f));
	}
}
