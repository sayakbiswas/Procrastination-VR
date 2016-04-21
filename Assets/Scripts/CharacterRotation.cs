using UnityEngine;
using System.Collections;

public class CharacterRotation : MonoBehaviour {

    public Transform target;

    // Update is called once per frame
    void Update()
    {
        var lookDir = target.position - transform.position;
        lookDir.y = 0; // keep only the horizontal direction
        transform.rotation = Quaternion.LookRotation(lookDir);
    }



}
