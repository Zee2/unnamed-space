using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovementTest : MonoBehaviour {
    Rigidbody r;
    // Use this for initialization
    void Start() {
        r = gameObject.GetComponent<Rigidbody>();
    }
	// Update is called once per frame
	void Update() {
        r.MovePosition(1 * Time.deltaTime * (new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"))) + r.position);
        
    }
}
