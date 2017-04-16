using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(ZonedTransform))]
public class PhysicsGrid : MonoBehaviour {

    public bool enableBoundaries = true; //this should be false on the world grid, as objects should never be able to leave.

    public Transform offsetSensor;
    public GameObject proxy;
    ZonedTransform proxyZT;
    MeshNetworkIdentity thisMNI;
    bool hasIdentityContainer = false;
    public Vector3 gravity;
    public float gravityStrength;
    public bool radialGravity;
    Transform gridTransform;
    ZonedTransform gridZonedTransform;
    public Vector3D currentWorldOrigin = new Vector3D();
    
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
        gridTransform = transform;
        gridZonedTransform = GetComponent<ZonedTransform>();
        if (enableBoundaries == false) {
            ZonedTransform[] transforms = FindObjectsOfType<ZonedTransform>(); //Find all orphan zones and claim them!
            foreach(ZonedTransform z in transforms) {
                objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>()); //Everybody is the child of this grid.
                if(z.transform.parent == null) {
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
        foreach(ZonedTransform z in GetComponentsInChildren<ZonedTransform>()) {
            if(z.transform.parent == gridTransform) {
                if(objectsInGrid.ContainsKey(z.gameObject) == false) {
                    objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>());
                }
            }
        }

        if (proxy != null)
            proxyZT = proxy.GetComponent<ZonedTransform>();
        if (gameObject.GetComponent<IdentityContainer>() != null) {
            hasIdentityContainer = true;
            GridID = gameObject.GetComponent<IdentityContainer>().GetIdentity().GetObjectID();
        }
        else {
            if (proxy != null) {
                if (proxy.GetComponent<IdentityContainer>() != null) {
                    hasIdentityContainer = true;
                    GridID = proxy.GetComponent<IdentityContainer>().GetIdentity().GetObjectID();
                }
            }
        }

        if (hasIdentityContainer == false) {
            Debug.LogWarning("Physics grid " + name + " has no grid ID. Will not be able to be serialized across network.");
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
        if (offsetSensor != null && (offsetSensor.position - gridTransform.position).magnitude > 200) {
            Vector3 localDelta = gridTransform.worldToLocalMatrix * (offsetSensor.position - gridTransform.position);
            currentWorldOrigin = currentWorldOrigin + localDelta;
            for (int i = 0; i < gridTransform.childCount; i++) {
                gridTransform.GetChild(i).localPosition -= localDelta;
            }
        }

        if (thisMNI == null && hasIdentityContainer) {
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
        if(proxy == null) {
            Debug.LogError("Grid has no proxy, cannot retrieve GridID");
            return (ushort)ReservedObjectIDs.Unspecified;
        }
        else {
            IdentityContainer c = proxy.gameObject.GetComponent<IdentityContainer>();
            if(c != null) {
                return c.GetIdentity().GetObjectID();
            }
            else {
                Debug.Log("Proxy has no MeshNetworkIdentity");
                return (ushort)ReservedObjectIDs.Unspecified;
            }
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
