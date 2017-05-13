using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stutterTest : MonoBehaviour {
    Rigidbody r;
	// Use this for initialization
	void Start () {
        r = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        r.MovePosition(r.position + new Vector3(Input.GetAxis("Horizontal") * 0.5f, 0, 0));
	}
}
