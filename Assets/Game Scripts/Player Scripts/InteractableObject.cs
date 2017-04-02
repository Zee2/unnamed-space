using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour {

    public bool supportsMultipleUsers = false;


    //Networked interactable objects should broadcast this across the network
    bool beingUsed;

    
    public bool GetMultipleUsersAllowed() {
        return supportsMultipleUsers;
    }

    public bool IsBeingUsed() {
        return beingUsed;
    }

    public virtual void SetBeingUsed(bool used) {
        beingUsed = used;
    }


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
