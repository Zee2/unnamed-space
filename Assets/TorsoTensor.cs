using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorsoTensor : MonoBehaviour {

	// Use this for initialization
	void OnEnable () {
        Rigidbody r = GetComponent<Rigidbody>();
        foreach (Collider c in GetComponentsInChildren<Collider>()) {
            c.enabled = false;
        }
        r.ResetInertiaTensor();
        Vector3 tensor = r.inertiaTensor;
        Quaternion rot = r.inertiaTensorRotation;
        foreach(Collider c in GetComponentsInChildren<Collider>()) {
            c.enabled = true;
        }
        r.inertiaTensor = tensor;
        r.inertiaTensorRotation = rot;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
