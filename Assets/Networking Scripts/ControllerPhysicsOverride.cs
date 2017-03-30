using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SteamVR_TrackedObject))]
[RequireComponent(typeof(MeshNetworkTransform))]
public class ControllerPhysicsOverride : MonoBehaviour {
    SteamVR_Controller.Device device;
    SteamVR_TrackedObject o;
    MeshNetworkTransform mnt;
    Vector3 angVel;
	// Use this for initialization
    void OnEnable() {
        Rigidbody r = gameObject.GetComponent<Rigidbody>();
        if(r != null) {
            Destroy(r);
        }
    }
	void Start () {
        o = GetComponent<SteamVR_TrackedObject>();
        if (o == null) {
            Debug.Log("no tracked object");
        }
        device = SteamVR_Controller.Input((int)o.index);
        mnt = GetComponent<MeshNetworkTransform>();
        
	}
	
	// Update is called once per frame
	void Update () {
        if(device == null) {
            device = SteamVR_Controller.Input((int)o.index);
        }
        mnt.velocity = device.velocity;
        angVel = device.angularVelocity;
        float angle = (angVel.x / angVel.normalized.x) * Mathf.Rad2Deg;
        mnt.rotationalVelocity = Quaternion.AngleAxis(angle, angVel.normalized);
	}

    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        //Gizmos.DrawLine(r.position, r.position+ device.velocity);
    }
}
