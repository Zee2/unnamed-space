using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsGridManager : MonoBehaviour {

    public Transform debugTransform;
    public PhysicsGrid source;
    public PhysicsGrid target;
    Dictionary<PhysicsGrid, List<PhysicsGrid>> registry = new Dictionary<PhysicsGrid, List<PhysicsGrid>>();
	// Use this for initialization
	void Start () {
        PhysicsGrid[] grids = FindObjectsOfType<PhysicsGrid>();
        int gridcounter = 0;
        int parentCounter = 0;
        foreach(PhysicsGrid g in grids) {
            registry.Add(g, new List<PhysicsGrid>());
            Transform t = g.transform.parent;
            parentCounter = 0;
            while(t != null) {
                PhysicsGrid thisGrid = t.GetComponent<PhysicsGrid>();
                if(thisGrid == null) {
                    Debug.LogError("Invalid physics grid parentage");
                    break;
                }
                registry[g].Add(thisGrid);
                t = t.parent;
                parentCounter++;
            }
            Debug.Log("Initialized grid " + g.name + " with " + parentCounter + " parent grids.");
            gridcounter++;
            
        }
        Debug.Log("Finished initializing " + gridcounter + " grids.");
	}
	

    public void OnDrawGizmos() {
        if (debugTransform == null || source == null || target == null)
            return;
        if (registry.Count >= 2) {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetRelativePosition(source, target, debugTransform.localPosition) + target.transform.position, 0.9f);
        }
    }

    //Transforms <position> from <source> physics grid to <target> physics grid.
    //<position> is assumed to be measured relative to <source> physics grid.
	public Vector3 GetRelativePosition(PhysicsGrid source, PhysicsGrid target, Vector3 position) {
        if (target.Equals(source)) {
            return position;
        }
        if(registry.ContainsKey(source) == false || registry.ContainsKey(target) == false) {
            return Vector3.zero;
        }

        List<PhysicsGrid> targetParentage = registry[target];
        List<PhysicsGrid> sourceParentage = registry[source];
        position = position + source.transform.localPosition;
        for (int i = 0; i < sourceParentage.Count; i++) {
            
            if (sourceParentage[i] == target) {
                return position;
            }
            
            int index = targetParentage.IndexOf(sourceParentage[i]);
            if (index != -1){ //we have found a common ancestor
                
                for (int j = index-1; i >= 0; i--) { //climb back down the tree, starting at one grid below
                    position = position - targetParentage[i].transform.localPosition;
                }
                position = position - target.transform.localPosition;
                return position;
            }
            position = position + sourceParentage[i].transform.localPosition;
        }
        return Vector3.zero;
        

    }
    public Quaternion GetRelativeRotation(PhysicsGrid source, PhysicsGrid target, Quaternion rotation) {
        if (target.Equals(source)) {
            return rotation;
        }
        if (registry.ContainsKey(source) == false || registry.ContainsKey(target) == false) {
            return Quaternion.identity;
        }

        List<PhysicsGrid> targetParentage = registry[target];
        List<PhysicsGrid> sourceParentage = registry[source];

        for (int i = 0; i < sourceParentage.Count; i++) { //sourceParentage starts with the parent of the grid
            rotation = rotation * sourceParentage[i].transform.localRotation;
            int index = targetParentage.IndexOf(sourceParentage[i]);
            if (index != -1) { //we have found a common ancestor
                for (int j = index - 1; i >= 0; i--) { //climb back down the tree, starting at one grid below
                    rotation = rotation * Quaternion.Inverse(targetParentage[i].transform.localRotation);
                }
                return rotation;
            }
        }
        Debug.LogError("No shared parentage found.");
        return Quaternion.identity;


    }
}
