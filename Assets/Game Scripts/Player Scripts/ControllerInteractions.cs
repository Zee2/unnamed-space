using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
[RequireComponent(typeof(SteamVR_TrackedObject))]
[RequireComponent(typeof(Collider))]
public class ControllerInteractions : MonoBehaviour {
    public Transform playspace;
    Transform thisTransform;
    GameObject offset;
    SteamVR_Controller.Device controller;
    public InteractableObject holding;
    Collider thisCollider;
    List<InteractableObject> objectsInHand = new List<InteractableObject>();
    const int FIND_NEXT_GRID_ITERATIONS = 20;
    // Use this for initialization
    void Start () {
        if(playspace == null) {
            Debug.LogError("No playspace!");
        }
        controller = SteamVR_Controller.Input((int)GetComponent<SteamVR_TrackedObject>().index);
        thisCollider = GetComponent<Collider>();
        thisTransform = transform;
        offset = new GameObject("Attach offset");
        offset.transform.parent = thisTransform;
    }
	
	// Update is called once per frame
	void Update () {
        if (controller.GetPressDown(SteamVR_Controller.ButtonMask.Trigger)) {
            if(objectsInHand.Count > 0) {
                InteractableObject obj = objectsInHand[0];
                if (obj.IsBeingUsed() == true && obj.GetMultipleUsersAllowed() == false)
                    return;
                holding = obj;
                if (obj is Prop) {
                    Prop p = (Prop)obj;
                    p.SetBeingUsed(true);
                    offset.transform.position = p.transform.position;
                    offset.transform.rotation = p.transform.rotation;
                }
            }
        }
        if (controller.GetPressUp(SteamVR_Controller.ButtonMask.Trigger)) {
            if(holding != null) {
                StopInteracting();
            }
        }

        if(holding != null && holding is Prop) {
            HoldProp();
        }


    }

    void HoldProp() {
        if(holding == null || !(holding is Prop)) {
            return;
        }
        Prop prop = (Prop)holding;
        prop.transform.localPosition = GetTransformRelativePosition(prop.transform, thisTransform.localPosition + offset.transform.localPosition);
        prop.transform.rotation = offset.transform.rotation;
        if (prop.hasRigidbody) {
            prop.thisRigidbody.velocity = GetTransformRelativeDirection(playspace, prop.transform, controller.velocity);
            prop.thisRigidbody.angularVelocity = controller.angularVelocity;
            //AAAAAAAAAAAAAAAAAAAAAAAAAHHHHHHHHHHHH
        }
    }

    public void StopInteracting() {
        if (holding == null)
            return;
        holding.SetBeingUsed(false);
        holding = null;
    }

    void OnTriggerEnter(Collider c) {
        InteractableObject interactable = c.GetComponent<InteractableObject>();
        if (interactable == null)
            return;
        objectsInHand.Add(interactable);

    }
    void OnTriggerExit(Collider c) {
        InteractableObject interactable = c.GetComponent<InteractableObject>();
        if (interactable == null)
            return;
        objectsInHand.Remove(interactable);
    }


    Vector3 GetGridRelativePosition(Vector3 v) {
        Transform t = thisTransform;
        int counter = 0;
        while (true) {
            if (counter >= FIND_NEXT_GRID_ITERATIONS) {
                return v;
            }
            if (t == null) {
                return v;
            }
            if (t.GetComponent<PhysicsGrid>() != null) {
                return v;
            }
            v += t.transform.localPosition;
            t = t.parent;
            
            counter++;
        }
    }
    Vector3 GetTransformRelativePosition(Transform target, Vector3 local) {
        Transform t = thisTransform;
        int counter = 0;
        while (true) {
            if (counter >= FIND_NEXT_GRID_ITERATIONS) {
                return local;
            }
            if (t == null) {
                return local;
            }
            if (t.Equals(target)) {
                return local;
            }
            local += t.transform.localPosition;
            t = t.parent;
            counter++;
        }
    }
    Vector3 GetTransformRelativeDirection(Transform source, Transform target, Vector3 local) {
        Transform t = source;
        int counter = 0;
        while (true) {
            if (counter >= FIND_NEXT_GRID_ITERATIONS) {
                return local;
            }
            if (t == null) {
                return local;
            }
            if (t.Equals(target)) {
                return local;
            }
            local = Quaternion.Inverse(t.localRotation) * local;
            t = t.parent;
            counter++;
        }
    }
}
