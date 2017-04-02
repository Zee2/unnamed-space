using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPhysicsManager : MonoBehaviour {

    public Transform playerHeadTransform;
    Rigidbody proxyBody;
    CapsuleCollider c;
	void Start() {
        proxyBody =  gameObject.GetComponent<Rigidbody>();
        c = gameObject.GetComponent<CapsuleCollider>();
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        //proxyBody.inertiaTensorRotation = Quaternion.identity;
        //proxyBody.inertiaTensor = Vector3.right;
        //playerRigidbody.centerOfMass = headTransform.localPosition + new Vector3(0, -0.2f, 0);
        //playerRigidbody.centerOfMass = Vector3.zero;
        c.center = playerHeadTransform.localPosition + new Vector3(0, -0.57f, -0.06f);
        proxyBody.AddForce(new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical Strafe"), Input.GetAxis("Vertical")));
    }

    public void OnCollisionEnter() {
        //Debug.Log("ahhhh");
    }

    void OnDrawGizmos() {
        if(proxyBody != null) {
            //Gizmos.DrawSphere(proxyBody.position + proxyBody.centerOfMass, 0.3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(proxyBody.position, proxyBody.position + proxyBody.inertiaTensor * 5);
            Gizmos.DrawLine(proxyBody.position, proxyBody.position + proxyBody.inertiaTensorRotation.eulerAngles);
        }
            
    }
}
