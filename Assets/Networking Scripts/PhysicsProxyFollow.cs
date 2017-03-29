using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
[RequireComponent(typeof(Rigidbody))]
public class PhysicsProxyFollow : MonoBehaviour {
    public Transform target;
    public Vector3 vel;
    Rigidbody r;
	// Use this for initialization
	void OnEnable () {
        r = gameObject.GetComponent<Rigidbody>();
	}
	// Update is called once per frame
	void FixedUpdate () {
        r.MovePosition(target.position);
        r.MoveRotation(target.rotation);
        vel = r.velocity;
	}
}
