using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class ZonedTransform : MonoBehaviour{

    
    public PhysicsGrid parentGrid;
    ZonedTransform proxyZT;
    public PhysicsGridManager manager;
    Transform thisTransform;
    MeshNetworkIdentity thisMNI;

    bool hasIdentityContainer = false;

    public Vector3 rigidPositionDebug;
    Rigidbody debugRigidbody;
	// Use this for initialization
	void Start () {
        thisTransform = transform;
        manager = FindObjectOfType<PhysicsGridManager>();
        if(manager == null) {
            Debug.LogError("No grid manager found in scene");
        }
        if(transform.parent != null)
            SetGrid(transform.parent.GetComponent<PhysicsGrid>(), true);
        else {
            SetGrid(manager.GetGridByID((ushort)ReservedObjectIDs.RootGrid), true);
        }
        if(parentGrid == null) {
            Debug.Log("ZonedTransform does not have a parent grid, this should happen rarely");
        }
        debugRigidbody = GetComponent<Rigidbody>();

        IdentityContainer c = GetComponent<IdentityContainer>();
        if(c != null) {
            thisMNI = c.GetIdentity();
            hasIdentityContainer = true;
        }

        manager.TriggerRootScan();
	}

    public bool GetAuthorized() {
        if(thisMNI == null || hasIdentityContainer == false) {
            Debug.Log("ThisMNI: " + thisMNI + ", hasIdentityContainer: " + hasIdentityContainer);
            
            return true;
        }
        return thisMNI.IsLocallyOwned();
    }
    
    public void SetGrid(ushort id, bool remoteOverride) {
        
        PhysicsGrid g = manager.GetGridByID(id);
        SetGrid(g, remoteOverride);
    }

    public void SetGrid(PhysicsGrid g, bool remoteOverride) {
        if (GetAuthorized() == false) {
            if(remoteOverride == false) {
                return; //not authorized, and it is not a remote update
            }
        }

        if(parentGrid != null && parentGrid.GetGridID() != (ushort)Utilities.ReservedObjectIDs.Unspecified && parentGrid.GetGridID() == g.GetGridID()) {
            //Debug.Log("No zone change needed");
            return;
        }
        //if we have no parent, do not perform hierarchical safety check
        //or if it is a remote update, also skip hierarchical safety check
        if((parentGrid == null && g != null) || remoteOverride) {
            parentGrid = g;
            transform.parent = parentGrid.transform;
            parentGrid.SendMessage("OnConfirmObjectEnter", this);
        }
        else {
            if (g != null) {

                parentGrid = g;
                while (parentGrid.Contains(this) == false) {
                    Debug.Log(parentGrid.gameObject.name + " does not contain " + gameObject.name);
                    parentGrid = manager.FindNextGrid(parentGrid);
                    if(parentGrid == null) {
                        Debug.Log("Ran out of grids!");
                    }

                }
                transform.parent = parentGrid.transform;
                parentGrid.SendMessage("OnConfirmObjectEnter", this);
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
        if(parentGrid != null && manager.IsChildOf(grid, parentGrid) == false) { //if the grid we're entering is not a child of the current grid (overlapping collider issue)
            Debug.Log("Invalid transition: " + grid.name + " not a child of " + parentGrid.name);
            return;
        }
        Debug.Log("Entering a zone!");
        SetGrid(grid, false);
        grid.SendMessage("OnConfirmObjectEnter", this);
    }

    public void OnSuggestZoneExit(PhysicsGrid grid) {
        Debug.Log(this.name + " being suggested to exit grid " + grid.gameObject.name);
        if (GetAuthorized() == false) {
            Debug.Log("Not authorized");
            return;
        }
        if(grid != parentGrid) {
            return; //we aren't leaving
        }
        Debug.Log("Finding next grid after " + grid.gameObject.name);
        SetGrid(manager.FindNextGrid(grid), false);
        grid.SendMessage("ConfirmObjectExit", this);
    }
	
    public void FixedUpdate() {
        if(debugRigidbody != null) {
            rigidPositionDebug = debugRigidbody.velocity;
        }
        
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
