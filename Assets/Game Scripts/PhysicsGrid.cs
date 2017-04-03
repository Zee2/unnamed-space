using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PhysicsGrid : MonoBehaviour {




    public Transform proxy;
    public Vector3 gravity;
    public float gravityStrength;
    public bool radialGravity;
    public Transform gridTransform;
    Dictionary<GameObject, Rigidbody> objectsInGrid = new Dictionary<GameObject, Rigidbody>();
    List<GameObject> objectsToRemove = new List<GameObject>();
    const int FIND_NEXT_GRID_ITERATIONS = 20;
	// Use this for initialization
	void Start () {
        gridTransform = transform;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		foreach(KeyValuePair<GameObject, Rigidbody> entry in objectsInGrid) {
            if(entry.Value != null && entry.Key.transform.parent == gridTransform) {
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
        /*
        foreach(GameObject g in objectsInGrid.Keys) {
            if(g.transform.parent != gridTransform) {
                objectsToRemove.Add(g);
            }
        }
        for(int i = 0; i < objectsToRemove.Count; i++) {
            objectsInGrid.Remove(objectsToRemove[i]);
        }
        */
        if(proxy != null) {
            gridTransform.position = proxy.position;
        }
    }

    void OnDrawGizmos() {
        Gizmos.DrawSphere(transform.position + gravity, 0.4f);
    }

    void OnTriggerEnter(Collider c) {
        if (!objectsInGrid.ContainsKey(c.gameObject)) {
            if(c.transform.parent != gridTransform) {
                c.transform.parent = gridTransform;
            }
            objectsInGrid.Add(c.gameObject, c.GetComponent<Rigidbody>());
        }
    }
    void OnTriggerExit(Collider c) {
        if (objectsInGrid.ContainsKey(c.gameObject)) {
            objectsInGrid.Remove(c.gameObject);
            PhysicsGrid g = FindHigherGrid();
            if(g == null) {
                c.transform.parent = null;
            }else {
                c.transform.parent = g.transform;
            }
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
