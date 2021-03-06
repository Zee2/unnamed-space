﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Utilities;

public class PhysicsGridManager : MonoBehaviour {

    public ZonedTransform debugTransform;
    public PhysicsGrid source;
    public PhysicsGrid target;
    Dictionary<PhysicsGrid, List<PhysicsGrid>> registry = new Dictionary<PhysicsGrid, List<PhysicsGrid>>();

    public Text debug;
    public PhysicsGrid testChild;
    public PhysicsGrid testParent;

    // Use this for initialization
    void Start() {
        BuildTree();
    }

    void Update() {
        
        string s = "Grid Registry: \n";
        foreach(KeyValuePair<PhysicsGrid, List<PhysicsGrid>> k in registry) {
            s += k.Key.name + " :{";
            foreach(PhysicsGrid g in k.Value) {
                s += g.name + ", ";
            }
            s += "} \n";
        }

        if(debug != null) {
            debug.text = s;
        }
    }

    void BuildTree() {
        bool foundRootGridYet = false;
        PhysicsGrid[] grids = FindObjectsOfType<PhysicsGrid>();
        int gridcounter = 0;
        int parentCounter = 0;
        foreach (PhysicsGrid g in grids) {
            if (registry.ContainsKey(g))
                registry.Remove(g);
            registry.Add(g, new List<PhysicsGrid>());
            Transform t = g.transform.parent; //starts with parent!
            parentCounter = 0;
            while (t != null) {
                PhysicsGrid thisGrid = t.GetComponent<PhysicsGrid>();
                if (thisGrid == null) {
                    Debug.LogError("Invalid physics grid parentage");
                    break;
                }
                registry[g].Add(thisGrid);
                t = t.parent;
                parentCounter++;
            }
            if (g.isRootGrid) {
                if (foundRootGridYet) {
                    Debug.LogError("MORE THAN ONE ROOT GRID!");

                } else {
                    g.ScanForGrids();
                    foundRootGridYet = true;
                }
                
            }
            //Debug.Log("Initialized grid " + g.name + " with " + parentCounter + " parent grids.");
            gridcounter++;

        }
        Debug.Log("Finished initializing " + gridcounter + " grids.");
    }


    public void RebuildTree() {
        BuildTree();
    }
    public void TriggerRootScan() {
        GetGridByID((ushort)ReservedObjectIDs.RootGrid).ScanForGrids();
    }
    public void OnDrawGizmos() {
        if (debugTransform == null || debugTransform.parentGrid == null || target == null)
            return;
        if (registry.Count >= 2) {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(debugTransform.transform.position, debugTransform.transform.position + GetRelativeRotation(debugTransform.parentGrid, target, debugTransform.transform.localRotation) * (Vector3.up * 2));
            Gizmos.DrawSphere(target.transform.position + GetRelativePosition(debugTransform.parentGrid, target, debugTransform.transform.localPosition), 0.7f);
        }
    }

    public PhysicsGrid GetGridByID(ushort id) {
        foreach(PhysicsGrid g in registry.Keys) {
            if(g.GetGridID() == id) {
                return g;
            }
        }
        return null;
    }

    //Transforms <position> from <source> physics grid to <target> physics grid.
    //<position> is assumed to be measured relative to <source> physics grid.
    //GetRelativePosition uses machine-space coordinates, not floating-origin-transformed coordinates.
    public Vector3 GetRelativePosition(PhysicsGrid source, PhysicsGrid target, Vector3 position) {

        //FLAW HERE! Doesn't work for source being the common ancestor, and target being a child.
        //It doesn't check for common ancestry BEFORE jumping up a level. Bad!
        //FIX FIX FIX!


        if (source == null || target == null) {
            Debug.LogError("Null grid");
            return position;
        }

        if (target.Equals(source)) {
            
            return position;
        }
        if (registry.ContainsKey(source) == false || registry.ContainsKey(target) == false) {
            Debug.LogError("Zone not found");
            return Vector3.zero;
        }

        List<PhysicsGrid> targetParentageInclusive = registry[target];
        targetParentageInclusive.Insert(0, target);
        List<PhysicsGrid> sourceParentageInclusive = registry[source];
        sourceParentageInclusive.Insert(0, source);
        //position = (source.transform.localRotation * position) + source.transform.localPosition; //"position" is now one level up
        for (int i = 0; i < sourceParentageInclusive.Count; i++) {

            if (sourceParentageInclusive[i] == target) { //when i = 0, sourceParentage[i] = source
                return position;
            }

            int index = targetParentageInclusive.IndexOf(sourceParentageInclusive[i]);
            if (index != -1) { //we have found a common ancestor

                for (int j = index - 1; i >= 0; i--) { //climb back down the tree, starting at one grid below
                    position = (Quaternion.Inverse(targetParentageInclusive[i].transform.localRotation) * position) - targetParentageInclusive[i].transform.localPosition;
                }
                return position;
            }
            //going up one level now
            position = (sourceParentageInclusive[i].transform.localRotation * position) + sourceParentageInclusive[i].transform.localPosition;
        }
        return Vector3.zero;


    }

    //Transforms <position> from <source> physics grid to <target> physics grid.
    //<position> is assumed to be measured relative to <source> physics grid.

    //This override uses large-world coordinates.
    public Vector3D GetRelativePosition(PhysicsGrid source, PhysicsGrid target, Vector3D position) {
        if (source == null || target == null) {
            Debug.LogError("Null grid");
            return position;
        }

        if (target.Equals(source)) {

            return position;
        }
        if (registry.ContainsKey(source) == false || registry.ContainsKey(target) == false) {
            Debug.LogError("Zone not found");
            return new Vector3D(Vector3.zero);
        }

        PhysicsGrid[] targetParentageInclusive = new PhysicsGrid[registry[target].Count + 1];
        registry[target].CopyTo(targetParentageInclusive, 1);
        targetParentageInclusive[0] = target;
        PhysicsGrid[] sourceParentageInclusive = new PhysicsGrid[registry[source].Count + 1];
        registry[source].CopyTo(sourceParentageInclusive, 1);
        sourceParentageInclusive[0] = source;
        for (int i = 0; i < sourceParentageInclusive.Length; i++) { //starts with source grid

            if (sourceParentageInclusive[i] == target) { //when i = 0, sourceParentage[i] = source
                return position;
            }
            
            int index = Array.IndexOf(targetParentageInclusive, sourceParentageInclusive[i]);
            if (index != -1) { //we have found a common ancestor

                for (int j = index - 1; i >= 0; i--) { //climb back down the tree, starting at one grid below
                    position = new Vector3D(Quaternion.Inverse(targetParentageInclusive[i].transform.localRotation) * (position - targetParentageInclusive[i].GetGridZonedTransform().GetLargeWorldPosition()));
                }
                return position;
            }
            //going up one level now
            position = (sourceParentageInclusive[i].transform.localRotation * position) + sourceParentageInclusive[i].GetGridZonedTransform().GetLargeWorldPosition();
        }
        Debug.LogError("Fallback");
        return new Vector3D(Vector3.zero);


    }
    


    public Quaternion GetRelativeRotation(PhysicsGrid source, PhysicsGrid target, Quaternion rotation) {

        if (source == null || target == null) {
            Debug.LogError("Null zone");
            return rotation;
        }

        if (target.Equals(source)) {
            return rotation;
        }
        if (registry.ContainsKey(source) == false || registry.ContainsKey(target) == false) {
            Debug.LogError("Not in registry");
            return Quaternion.identity;
        }

        List<PhysicsGrid> targetParentage = registry[target];
        List<PhysicsGrid> sourceParentage = registry[source];
        rotation = source.transform.localRotation * rotation;
        for (int i = 0; i < sourceParentage.Count; i++) {

            if (sourceParentage[i] == target) {
                return rotation;
            }

            int index = targetParentage.IndexOf(sourceParentage[i]);
            if (index != -1) { //we have found a common ancestor

                for (int j = index - 1; i >= 0; i--) { //climb back down the tree, starting at one grid below
                    rotation = Quaternion.Inverse(targetParentage[i].transform.localRotation) * rotation;
                }
                rotation = Quaternion.Inverse(target.transform.localRotation) * rotation;
                return rotation;
            }
            rotation = sourceParentage[i].transform.localRotation * rotation;
        }
        return Quaternion.identity;


    }

    public PhysicsGrid FindNextGrid(PhysicsGrid grid) {
        if (registry.ContainsKey(grid) == false) {
            Debug.LogError("Registry can't find grid " + grid.gameObject.name);
            return null;
        }
            
        if(registry[grid].Count == 0) {
            Debug.LogError("Grid " + grid.gameObject.name + " has no parents.");
            return null;
        }
        return registry[grid][0];
    }

    //Returns true if <child> is a subgrid of <parent>
    //Returns false if either grid is not registered
    public bool IsChildOf(PhysicsGrid child, PhysicsGrid parent) {
        if (registry.ContainsKey(child) == false || registry.ContainsKey(parent) == false) {
            Debug.LogError("Child check includes non-registered grid");
            return false;
        }
            
        return registry[child].Contains(parent);

    }
    
}
