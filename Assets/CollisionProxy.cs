using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionProxy : MonoBehaviour {
    public Transform target;
    Transform thisTransform;
	// Use this for initialization
	void Start () {
        thisTransform = transform;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        transform.localPosition = target.localPosition;
        transform.localRotation = target.localRotation;
	}
}
