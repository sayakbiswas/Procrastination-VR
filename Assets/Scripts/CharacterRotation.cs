using UnityEngine;
using System.Collections;

public class CharacterRotation : MonoBehaviour {

    public Transform target;
	private bool shouldFacePlayer = false;

	void Start() {
		if(!gameObject.name.Contains ("Mom")) {
			shouldFacePlayer = true;
		}
	}

	// Update is called once per frame
	void Update()
	{
		if(shouldFacePlayer) {
			var lookDir = target.position - transform.position;
			lookDir.y = 0; // keep only the horizontal direction
			transform.rotation = Quaternion.LookRotation(lookDir);
		}
	}

	public void facePlayer() {
		var lookDir = target.position - transform.position;
		lookDir.y = 0; // keep only the horizontal direction
		transform.rotation = Quaternion.LookRotation(lookDir);
		shouldFacePlayer = true;
	}
}
