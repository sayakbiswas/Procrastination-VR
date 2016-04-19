using UnityEngine;
using System.Collections;
using RogoDigital.Lipsync;

public class ClassroomScript : MonoBehaviour {
	private float waitBeforeProfessorStarts = 2.0f;
	private bool hasProfStartedSpeaking = false;
	public GameObject theProfessor;
	public LipSyncData profLipSyncData;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(waitBeforeProfessorStarts > 0.0f) {
			waitBeforeProfessorStarts -= Time.deltaTime;
		} else {
			if(!hasProfStartedSpeaking) {
				theProfessor.GetComponent<LipSync> ().Play (profLipSyncData);
				hasProfStartedSpeaking = true;
			}
		}
	}
}
