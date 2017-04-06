using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class ZonedTransform : MonoBehaviour {

    public Vector3D precisePosition;
    Vector3 imprecisePosition;
    public PhysicsGrid parentGrid;
    Transform thisTransform;

	// Use this for initialization
	void Start () {
        thisTransform = transform;
        imprecisePosition = transform.localPosition;
        if(transform.parent != null)
            parentGrid = transform.parent.GetComponent<PhysicsGrid>();
        if(parentGrid == null) {
            Debug.Log("ZonedTransform does not have a parent grid, this should happen rarely");
        }else {
            precisePosition = parentGrid.currentWorldOrigin + thisTransform.localPosition;
        }
	}

    void OnTransformParentChanged() {
        parentGrid = transform.parent.GetComponent<PhysicsGrid>();
        //if (parentGrid == null) {
            //Debug.Log("ZonedTransform does not have a parent grid, this should happen rarely");
        //}else {
            //parentGrid.gameObject.SendMessage("NotifyObjectEnter", this);
        //}
    }

    public void NotifyZoneEnter(PhysicsGrid grid) {
        if(parentGrid != null) {
            parentGrid.SendMessage("NotifyObjectExit", this);
        }
        parentGrid = grid;
        Vector3 delta = transform.localPosition - grid.transform.localPosition;
        precisePosition = new Vector3D(Quaternion.Inverse(grid.transform.localRotation) * delta);
        grid.SendMessage("ConfirmObjectEnter", this);
    }
    public void NotifyZoneExit(PhysicsGrid grid) {
        grid.SendMessage("ConfirmObjectExit", this);
    }
	
    void Update() {
        //precisePosition.x += Input.GetAxis("Horizontal") * 0.5f;
    }
    public void FixedUpdate() {
        thisTransform.localPosition = precisePosition - parentGrid.currentWorldOrigin;
    }
	public void LateUpdate() {
        
    }
	public void SetPrecisePosition(Vector3D precise) {
        if (parentGrid != null) {
            thisTransform.localPosition = precise - parentGrid.currentWorldOrigin;
        }
    }
}
