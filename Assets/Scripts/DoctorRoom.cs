using UnityEngine;
using System.Collections;
using RogoDigital.Lipsync;
using UnityEngine.SceneManagement;

public class DoctorRoom : MonoBehaviour {

    public GameObject theDoctor;
    public LipSyncData doctorLipSyncData;
    private Animator doctorAnimator;
    private bool hasDoctorStartedTalking = false;
    public LayerMask layerMask;
    public CardboardReticle reticle;
    private float waitBeforeSceneChange = 2.0f;

    // Use this for initialization
    void Start () {
        doctorAnimator = theDoctor.GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update () {

        RaycastHit hitInfo;

        if (Physics.Raycast(Cardboard.SDK.GetComponentInChildren<CardboardHead>().Gaze, out hitInfo, Mathf.Infinity,
        layerMask))
        {
            GameObject hitObject = hitInfo.transform.gameObject;
            if (hitObject.name.Contains("Doctor Talking") && !hasDoctorStartedTalking)
            {
                reticle.GetComponent<CardboardReticle>().OnGazeStart(this.gameObject.GetComponentInChildren<Camera>(),
                    hitObject, hitInfo.point);
            }
            else
            {
                reticle.GetComponent<CardboardReticle>().OnGazeExit(this.gameObject.GetComponentInChildren<Camera>(),
                    hitObject);
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            if (Physics.Raycast(Cardboard.SDK.GetComponentInChildren<CardboardHead>().Gaze, out hitInfo, Mathf.Infinity,
                    layerMask))
            {
                GameObject hitObject = hitInfo.transform.gameObject;
                if (hitObject.name.Contains("Doctor Talking"))
                {
                    doctorAnimator.enabled = true;
                    theDoctor.GetComponent<LipSync>().Play(doctorLipSyncData);
                    hasDoctorStartedTalking = true;
                }
            }
        }

        if (hasDoctorStartedTalking && !theDoctor.GetComponent<LipSync>().isPlaying)
        {
            if (waitBeforeSceneChange > 0.0f)
            {
                waitBeforeSceneChange -= Time.deltaTime;
            }
            else
            {
                SceneManager.LoadScene("Scene 2");
            }
        }

        } //Update Ends

} //Class Ends
