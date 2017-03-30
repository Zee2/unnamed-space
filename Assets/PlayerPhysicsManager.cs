using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPhysicsManager : MonoBehaviour {

    public Transform headTransform;
    Rigidbody playerRigidbody;
	void OnEnable() {
        playerRigidbody = gameObject.GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update () {
        playerRigidbody.centerOfMass = headTransform.localPosition;
	}
}
