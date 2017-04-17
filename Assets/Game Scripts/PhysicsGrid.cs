using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(ZonedTransform))]
public class PhysicsGrid : MonoBehaviour {

    public bool isRootGrid = false; //If multiple grids have this as "true", errors will happen!

    public Transform offsetSensor;
    public GameObject proxy;
    ZonedTransform proxyZT;
    MeshNetworkIdentity thisMNI;
    bool hasGridID = false;
    public Vector3 gravity;
    public float gravityStrength;
    public bool radialGravity;
    Transform gridTransform;
    ZonedTransform gridZonedTransform;
    public Vector3D currentWorldOrigin = new Vector3D();
    public bool originTest;
    
    PhysicsGridManager manager;
    Dictionary<GameObject, Rigidbody> objectsInGrid = new Dictionary<GameObject, Rigidbody>();

    ushort GridID; //copied from proxy MNI

    const int FIND_NEXT_GRID_ITERATIONS = 20;
    // Use this for initialization
    void Start() {
        manager = FindObjectOfType<PhysicsGridManager>();
        if(manager == null) {
            Debug.LogError("No physics grid manager found in scene!");
        }
        else {
            Debug.Log("Gridmanager name = " + manager.name);
        }
        manager.RebuildTree();
        gridTransform = transform;
        gridZonedTransform = GetComponent<ZonedTransform>();

        if(isRootGrid && transform.parent != null) {
            Debug.LogError("Root grid has a parent! This won't work!");
        }
        if(isRootGrid && proxy != null) {
            Debug.LogError("Root grid has a proxy! This won't work!");
        }
        if(isRootGrid && GetComponent<IdentityContainer>() != null) {
            Debug.LogError("Root grid has an identity container. Root grids should not be networked!");
        }

        ScanForGrids(); //if this is root, it will un-orphan any stray grids or objects

        if (isRootGrid == false) {
            if (proxy != null) {
                proxyZT = proxy.GetComponent<ZonedTransform>();
                if (proxy.GetComponent<IdentityContainer>() != null) {
                    hasGridID = true;
                    GridID = proxy.GetComponent<IdentityContainer>().GetIdentity().GetObjectID();
                }
            } else if (gameObject.GetComponent<IdentityContainer>() != null) {
                hasGridID = true;
                GridID = gameObject.GetComponent<IdentityContainer>().GetIdentity().GetObjectID();
            }
        }else {
            hasGridID = true;
            GridID = (ushort)ReservedObjectIDs.RootGrid;
        }
        
        

        if (hasGridID == false) {
            Debug.LogWarning("Physics grid " + name + " has no grid ID. Will not be able to be serialized across network.");
        }
    }

    public void ScanForGrids() { //only the root grid should use this
        
        if (isRootGrid == true) {
            Debug.Log("Root grid is scanning!");
            ZonedTransform[] transforms = FindObjectsOfType<ZonedTransform>(); //Find all orphan zones and claim them!
            foreach (ZonedTransform z in transforms) {
                if(z.gameObject == gameObject) { //don't add ourselves
                    continue;
                }
                if(objectsInGrid.ContainsKey(z.gameObject) == false)
                    objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>()); //Everybody is the child of this grid.
                if (z.transform.parent == null) {
                    Debug.Log("Un-orphaning objct with name " + z.gameObject.name);
                    z.SetGrid(this);
                }
            }
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider c in colliders) {
                if (c.isTrigger) {
                    c.enabled = false;
                }
            }
        }
        foreach (ZonedTransform z in GetComponentsInChildren<ZonedTransform>()) {
            if (z.transform.parent == gridTransform) {
                if (objectsInGrid.ContainsKey(z.gameObject) == false) {
                    objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>());
                }
            }
        }
    }

    
    public void OnTransformParentChanged() {
        StartCoroutine(RebuildTreeRoutine());
    }

    IEnumerator RebuildTreeRoutine() {
        while (manager == null)
            yield return new WaitForEndOfFrame();
        manager.RebuildTree();
    }

    // Update is called once per frame
    void FixedUpdate() {
        foreach (KeyValuePair<GameObject, Rigidbody> entry in objectsInGrid) {
            if (entry.Value != null && entry.Key.transform.parent == gridTransform) {
                if (!entry.Value.IsSleeping()) {

                    if (radialGravity) {
                        entry.Value.AddForce(gridTransform.localToWorldMatrix * Vector3.Normalize(gravity - entry.Value.transform.localPosition) * gravityStrength, ForceMode.Acceleration);
                    } else {
                        entry.Value.AddForce(Vector3.Normalize(gridTransform.localToWorldMatrix * gravity) * gravityStrength, ForceMode.Acceleration);
                    }


                }

            }
        }


    }

    void Update() {
        if (originTest) {
            originTest = false;
            Vector3 delta = new Vector3(1, 0, 1);
            currentWorldOrigin = currentWorldOrigin + delta;
            for (int i = 0; i < gridTransform.childCount; i++) {
                gridTransform.GetChild(i).localPosition -= delta;
            }
        }
        if (offsetSensor != null && (offsetSensor.position - gridTransform.position).magnitude > 200) {
            Vector3 localDelta = gridTransform.worldToLocalMatrix * (offsetSensor.position - gridTransform.position);
            currentWorldOrigin = currentWorldOrigin + localDelta;
            for (int i = 0; i < gridTransform.childCount; i++) {
                gridTransform.GetChild(i).localPosition -= localDelta;
            }
        }

        if (thisMNI == null && hasGridID) {
            IdentityContainer c = gameObject.GetComponent<IdentityContainer>();
            if (c == null && proxy != null) {
                c = proxy.GetComponent<IdentityContainer>();
            }
            if (c == null) {
                thisMNI = null;
            }
            else {
                thisMNI = c.GetIdentity();
            }
        }
    }

    public void LateUpdate() {
        if (proxy != null && proxyZT != null) { //if we have a proxy, and that proxy has a zone transform
            gridZonedTransform.parentGrid = proxyZT.parentGrid;
            gridTransform.localPosition = proxyZT.transform.localPosition;
            gridTransform.localRotation = proxyZT.transform.localRotation;
        }
        else if (proxy != null) {
            gridTransform.position = proxy.transform.position;
            gridTransform.rotation = proxy.transform.rotation;
        }

    }

    public ushort GetGridID() {
        if(hasGridID == false) {
            Debug.LogError("No gridID available");
            return (ushort)ReservedObjectIDs.Unspecified;
        }else {
            return GridID;
        }
        
    }
    


    void OnDrawGizmos() {
        Gizmos.DrawSphere(transform.position + gravity, 0.4f);
    }


    public bool Contains(ZonedTransform z) {
        return objectsInGrid.ContainsKey(z.gameObject);
    }

    void OnTriggerEnter(Collider c) {
        Debug.Log("Grid trigger enter!");
        if(objectsInGrid.ContainsKey(c.gameObject) == false)
            objectsInGrid.Add(c.gameObject, c.GetComponent<Rigidbody>());
        c.gameObject.SendMessage("OnSuggestZoneEnter", this);
        
    }
    public void OnConfirmObjectEnter(ZonedTransform z) {
        
        if (z.GetComponent<PhysicsGrid>() != null && manager != null)
            manager.RebuildTree();
    }
    public void NotifyObjectEnter(ZonedTransform z) {
        if(objectsInGrid.ContainsKey(z.gameObject) == false) {
            objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>());
        }
        if (z.GetComponent<PhysicsGrid>() != null && manager != null)
            manager.RebuildTree();
    }


    void OnTriggerExit(Collider c) {
        if(objectsInGrid.ContainsKey(c.gameObject))
            objectsInGrid.Remove(c.gameObject);
        c.gameObject.SendMessage("OnSuggestZoneExit", this);
        
    }
    public void ConfirmObjectExit(ZonedTransform z) {
        
        if (z.GetComponent<PhysicsGrid>() != null && manager != null)
            manager.RebuildTree();
        //SendMessageUpwards("RefreshListing");
    }
    public void NotifyObjectExit(ZonedTransform z) {
        if (objectsInGrid.ContainsKey(z.gameObject)) {
            objectsInGrid.Remove(z.gameObject);
        }
        if (z.GetComponent<PhysicsGrid>() != null && manager != null)
            manager.RebuildTree();
    }

    PhysicsGrid FindHigherGrid() {
        Transform t = gridTransform.parent;
        int counter = 0;
        while (true) {
            if(counter >= FIND_NEXT_GRID_ITERATIONS) {
                return null;
            }
            if (t == null) {
                return null;
            }
            if (t.GetComponent<PhysicsGrid>() != null) {
                return t.GetComponent<PhysicsGrid>();
            }
            t = t.parent;
            
            counter++;
        }
    }
}
