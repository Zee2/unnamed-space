using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;

public class BasicMovementTest : MonoBehaviour {
    public Rigidbody r;
    public float speed = 4;
    PhysicsGrid g;
    ZonedTransform z;
    // Use this for initialization
    void Start() {
        
        r = gameObject.GetComponent<Rigidbody>();
        g = gameObject.GetComponent<PhysicsGrid>();
        z = gameObject.GetComponent<ZonedTransform>();
    }
    void OnEnable() {
        r = gameObject.GetComponent<Rigidbody>();
        g = gameObject.GetComponent<PhysicsGrid>();
        z = gameObject.GetComponent<ZonedTransform>();
    }
    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.K)) {
            r.isKinematic = !r.isKinematic;
        }

        if(z.parentGrid != null)
            r.MovePosition(z.parentGrid.transform.TransformPoint(transform.localPosition + speed * Time.deltaTime * (new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical")))));
        else
            r.MovePosition(transform.localPosition + speed * Time.deltaTime * (new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"))));
        //Vector3 forceVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical"));

        //if (transform.parent != null)
        //forceVector = transform.parent.localToWorldMatrix * forceVector;
        //r.AddForce(forceVector * speed);
        //if(transform.parent == null) {
        //r.AddForce((Vector3.right * Input.GetAxis("Horizontal") * speed + Vector3.forward * Input.GetAxis("Vertical") * speed + Vector3.up * Input.GetAxis("Vertical Strafe") * speed));

        //}else {
        //r.AddForce(transform.parent.localToWorldMatrix * (Vector3.right * Input.GetAxis("Horizontal") * speed + Vector3.forward * Input.GetAxis("Vertical") * speed + Vector3.up * Input.GetAxis("Vertical Strafe") * speed));

        //}
        //transform.Translate(Vector3.right * Input.GetAxis("Horizontal"))

        //g.preciseWorldOffset += Vector3.right * Input.GetAxis("Horizontal") * speed;
        //z.SetPrecisePosition(z.precisePosition + Vector3.right * Input.GetAxis("Horizontal") * speed);
    }
}
