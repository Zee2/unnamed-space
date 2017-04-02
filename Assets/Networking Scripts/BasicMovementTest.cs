using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovementTest : MonoBehaviour {
    Rigidbody r;
    public float speed;
    // Use this for initialization
    void Start() {
        r = gameObject.GetComponent<Rigidbody>();
    }
	// Update is called once per frame
	void FixedUpdate() {
        //r.MovePosition(speed * Time.fixedDeltaTime * (new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"))) + transform.localPosition);
        Vector3 forceVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"));

        if (transform.parent != null)
            forceVector = transform.parent.localToWorldMatrix * forceVector;
        r.AddForce(forceVector * speed);
    }
}
