using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsDebugger : MonoBehaviour {
    Rigidbody r;
    public Vector3 physicsPosition;
    public Vector3 physicsVelocity;
    public Vector3 physicsAngVel;
    bool shouldSleep = true;
    Material mat;
	// Use this for initialization
	void Start () {
        r = GetComponent<Rigidbody>();
        mat = GetComponent<MeshRenderer>().material;
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space)) {
            shouldSleep = !shouldSleep;
        }
        if (r.IsSleeping() != shouldSleep) {
            if (shouldSleep)
                r.Sleep();
            else
                r.WakeUp();
        }
        
        physicsPosition = r.position;
        physicsVelocity = r.velocity;
        physicsAngVel = r.angularVelocity;
        if (r.IsSleeping()) {
            mat.color = Color.red;
        }else {
            mat.color = Color.green;
        }
	}

    void FixedUpdate() {
        if (r.IsSleeping() != shouldSleep) {
            if (shouldSleep)
                r.Sleep();
            else
                r.WakeUp();
        }
    }

    void OnDrawGizmos() {
        //Gizmos.DrawLine(physicsPosition, physicsPosition + physicsVelocity);
    }
}
