using UnityEngine;
using System.Collections.Generic;
using RogoDigital.Lipsync;

public class GUISceneChanger : MonoBehaviour {

	public void LoadScene(int sceneNumber) {
		Application.LoadLevel(sceneNumber);
	}
}
