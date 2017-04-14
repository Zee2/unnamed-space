using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicMovementTest : MonoBehaviour {
    Rigidbody r;
    public float speed;
    PhysicsGrid g;
    ZonedTransform z;
    // Use this for initialization
    void Start() {
        r = gameObject.GetComponent<Rigidbody>();
        g = gameObject.GetComponent<PhysicsGrid>();
        z = gameObject.GetComponent<ZonedTransform>();
    }
	// Update is called once per frame
	void FixedUpdate() {
        //r.MovePosition(speed * Time.fixedDeltaTime * (new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"))) + transform.localPosition);
        //Vector3 forceVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"));

        //if (transform.parent != null)
        //forceVector = transform.parent.localToWorldMatrix * forceVector;
        //r.AddForce(forceVector * speed);
        r.AddForce(transform.parent.localToWorldMatrix * (Vector3.right * Input.GetAxis("Horizontal") * speed + Vector3.forward * Input.GetAxis("Vertical") * speed + Vector3.up * Input.GetAxis("Vertical Strafe") * speed));
        //transform.Translate(Vector3.right * Input.GetAxis("Horizontal"))
        
        //g.preciseWorldOffset += Vector3.right * Input.GetAxis("Horizontal") * speed;
        //z.SetPrecisePosition(z.precisePosition + Vector3.right * Input.GetAxis("Horizontal") * speed);
    }
}
