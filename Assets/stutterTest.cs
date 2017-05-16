using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stutterTest : MonoBehaviour {
    Rigidbody r;
    Vector3 pos;
    Transform t;
	// Use this for initialization
	void Start () {
        r = GetComponent<Rigidbody>();
        t = transform;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        pos = pos + new Vector3(Input.GetAxis("Horizontal") * 0.1f, 0, Input.GetAxis("Vertical") * 0.1f);
        if(r!=null)
            r.MovePosition(t.parent.TransformPoint(pos));
        //t.position = t.parent.TransformPoint(pos);
	}
}
