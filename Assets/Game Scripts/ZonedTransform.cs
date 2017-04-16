﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class ZonedTransform : MonoBehaviour{

    
    public PhysicsGrid parentGrid;
    ZonedTransform proxyZT;
    PhysicsGridManager manager;
    Transform thisTransform;
    MeshNetworkIdentity thisMNI;

    bool hasIdentityContainer = false;

	// Use this for initialization
	void Start () {
        thisTransform = transform;
        manager = FindObjectOfType<PhysicsGridManager>();
        if(manager == null) {
            Debug.LogError("No grid manager found in scene");
        }
        if(transform.parent != null)
            parentGrid = transform.parent.GetComponent<PhysicsGrid>();
        if(parentGrid == null) {
            Debug.Log("ZonedTransform does not have a parent grid, this should happen rarely");
        }
        
	}

    public bool GetAuthorized() {
        if(thisMNI == null || hasIdentityContainer == false) {
            return true;
        }
        return thisMNI.IsLocallyOwned();
    }
    
    public void SetGrid(ushort id) {
        if (GetAuthorized() == false)
            return;

        PhysicsGrid g = manager.GetGridByID(id);
        SetGrid(g);
    }

    public void SetGrid(PhysicsGrid g) {
        if (GetAuthorized() == false)
            return;

        if(parentGrid != null && parentGrid.GetGridID() == g.GetGridID()) {
            Debug.Log("No zone change needed");
            return;
        }
        
        if(parentGrid == null && g != null) {
            parentGrid = g;
            transform.parent = parentGrid.transform;
            parentGrid.SendMessage("ConfirmObjectEnter");
        }
        else {
            if (g != null) {

                parentGrid = g;
                while (parentGrid.Contains(this) == false) {
                    parentGrid = manager.FindNextGrid(parentGrid);
                    if(parentGrid == null) {
                        Debug.Log("Ran out of grids!");
                    }

                }
                transform.parent = parentGrid.transform;
                parentGrid.SendMessage("ConfirmObjectEnter");
            }
        }

        
    }

    public void OnSuggestZoneEnter(PhysicsGrid grid) {
        Debug.Log(this.name + " being suggested to enter grid " + grid.name);
        if (GetAuthorized() == false) {
            Debug.Log("Not authorized");
            return;
        }
            
        if(grid == parentGrid) {
            return;
        }
        if(manager.IsChildOf(grid, parentGrid) == false) { //if the grid we're entering is not a child of the current grid (overlapping collider issue)
            Debug.Log("Invalid transition: " + grid.name + " not a child of " + parentGrid.name);
            return;
        }
        Debug.Log("Entering a zone!");
        SetGrid(grid);
        grid.SendMessage("OnConfirmObjectEnter", this);
    }

    public void OnSuggestZoneExit(PhysicsGrid grid) {
        Debug.Log(this.name + " being suggested to exit grid " + grid.gameObject.name);
        if (GetAuthorized() == false)
            return;
        if(grid != parentGrid) {
            return; //we aren't leaving
        }
        SetGrid(manager.FindNextGrid(grid));
        grid.SendMessage("ConfirmObjectExit", this);
    }
	
    void Update() {
        
        
    }

    public void LateUpdate() {
        
    }

    
	public void SetLargeWorldPosition(Vector3D precise) {
        if (parentGrid != null) {
            thisTransform.localPosition = precise - parentGrid.currentWorldOrigin;
        }
    }
    public Vector3D GetLargeWorldPosition() {
        if(parentGrid != null) {
            return thisTransform.localPosition + parentGrid.currentWorldOrigin;
        }
        else {
            return new Vector3D(thisTransform.localPosition);
        }
    }
}
