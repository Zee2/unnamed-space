using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(Collider))]
public class PhysicsGrid : MonoBehaviour {

    public bool enableBoundaries = true; //this should be false on the world grid, as objects should never be able to leave.

    public Transform offsetSensor;
    public Transform proxy;
    public Vector3 gravity;
    public float gravityStrength;
    public bool radialGravity;
    Transform gridTransform;
    public Vector3D currentWorldOrigin = new Vector3D();

    PhysicsGrid parentGrid;

    Dictionary<GameObject, Rigidbody> objectsInGrid = new Dictionary<GameObject, Rigidbody>();
    List<GameObject> objectsInside = new List<GameObject>();
    const int FIND_NEXT_GRID_ITERATIONS = 20;
    // Use this for initialization
    void Start() {
        gridTransform = transform;
        if (enableBoundaries == false) {
            ZonedTransform[] transforms = FindObjectsOfType<ZonedTransform>(); //Find all orphan zones and claim them!
            foreach(ZonedTransform z in transforms) {
                if(z.transform.parent == null) {
                    Debug.Log("Un-orphaning objct with name " + z.gameObject.name);
                    objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>());
                    z.gameObject.SendMessage("NotifyZoneEnter", this);
                }
            }
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider c in colliders) {
                if (c.isTrigger) {
                    c.enabled = false;
                }
            }
        }
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
        if (offsetSensor != null && offsetSensor.localPosition.magnitude > 20) {
            currentWorldOrigin = currentWorldOrigin + offsetSensor.localPosition;
            //for (int i = 0; i < gridTransform.childCount; i++) {
            //gridTransform.GetChild(i).localPosition -= offsetSensor.localPosition;
            //}
        }
    }

    


    void OnDrawGizmos() {
        Gizmos.DrawSphere(transform.position + gravity, 0.4f);
    }

    void OnTriggerEnter(Collider c) {
        if (!objectsInside.Contains(c.gameObject))
            objectsInside.Add(c.gameObject);


        if (!objectsInGrid.ContainsKey(c.gameObject)) {
            c.gameObject.SendMessage("NotifyZoneEnter", this);
        }
    }
    public void ConfirmObjectEnter(ZonedTransform z) {
        if (z.transform.parent != gridTransform) {
            z.transform.parent = gridTransform;
        }
        if(objectsInGrid.ContainsKey(z.gameObject) == false)
            objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>());
    }
    public void NotifyObjectEnter(ZonedTransform z) {
        if(objectsInGrid.ContainsKey(z.gameObject) == false) {
            objectsInGrid.Add(z.gameObject, z.GetComponent<Rigidbody>());
        }
    }


    void OnTriggerExit(Collider c) {
        
        if (objectsInside.Contains(c.gameObject))
            objectsInside.Remove(c.gameObject);
        if (objectsInGrid.ContainsKey(c.gameObject)) {
            c.gameObject.SendMessage("NotifyZoneExit", this);
            /*
            objectsInGrid.Remove(c.gameObject);
            PhysicsGrid g = FindHigherGrid();
            if(g == null) {
                c.transform.parent = null;
            }else {
                c.transform.parent = g.transform;
            }
            */
        }
    }
    public void ConfirmObjectExit(ZonedTransform z) {
        
        if (z.transform.parent == gridTransform) {
            z.transform.parent = FindHigherGrid().transform;
        }
        if (objectsInGrid.ContainsKey(z.gameObject)) {
            objectsInGrid.Remove(z.gameObject);
        }
        //SendMessageUpwards("RefreshListing");
    }
    public void NotifyObjectExit(ZonedTransform z) {
        return;
        if (objectsInGrid.ContainsKey(z.gameObject)) {
            objectsInGrid.Remove(z.gameObject);
        }
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
