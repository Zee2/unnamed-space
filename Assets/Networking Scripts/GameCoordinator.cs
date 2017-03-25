using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Utilities;

[RequireComponent(typeof(MeshNetwork))]
public class GameCoordinator : MonoBehaviour {

    /// <summary>
    ///     
    ///     GameCoordinator.cs
    ///     Copyright 2017 Finn Sinclair
    ///     
    ///     High-level coordinator for game status. Creates and destroys actual
    ///     gameobjects.
    /// 
    /// </summary>

    public MeshNetwork meshnet;

    //Network Prefab Registry
    Dictionary<ushort, GameObject> networkPrefabs = new Dictionary<ushort, GameObject>();
    Dictionary<ushort, GameObject> activeObjects = new Dictionary<ushort, GameObject>();

    public void Start() {
        DontDestroyOnLoad(gameObject);
        meshnet = gameObject.GetComponent<MeshNetwork>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("NetworkPrefabs");

        foreach (GameObject prefab in prefabs) {
            if (prefab.GetComponent<IdentityContainer>() == null) {
                Debug.LogError("A NetworkPrefab is missing an IdentityContainer.");
            }
            else {
                string prefabID = prefab.name.Substring(prefab.name.LastIndexOf('_') + 1);
                networkPrefabs.Add(ushort.Parse(prefabID), prefab);

            }

        }
        Debug.Log("GameCoordinator tried to register " + prefabs.Length + " network prefabs, succeeded with " + networkPrefabs.Count + ".");
        
    }

    public void EnterGame(CSteamID lobbyID) {
        return;
    }

    public GameObject GetObjectByIdentity(ushort id) {
        GameObject g;
        activeObjects.TryGetValue(id, out g);
        return g;
    }

    public GameObject SpawnDatabase(MeshNetworkIdentity i) {

        if (meshnet == null) {
            Debug.LogError("Trying to spawn object when underlying mesh network not intialized.");
            return null;
        }
        if(i.GetObjectID() != (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.LogError("Trying to use database spawning method for non-database object");
            return null;
        }
        i.SetMeshnetReference(meshnet); //set a reference to the mesh network
        if (networkPrefabs.ContainsKey(i.GetPrefabID()) == false) {
            Debug.LogError("NetworkPrefab registry error: Requested prefab ID does not exist.");
            return null;
        }
        GameObject g = Instantiate(networkPrefabs[i.GetPrefabID()]);
        IdentityContainer c = g.GetComponent<IdentityContainer>();
        if (c == null) {
            Debug.LogError("NetworkPrefab error: spawned prefab does not contain IdentityContainer");
            return null;
        }
        c.SetIdentity(i);
        activeObjects.Add(i.GetObjectID(), g);
        return g;
    }

    
    //This simply instantiates a network prefab.
    //The objct should already exist on the database.
    public GameObject SpawnObject(ushort objectID) {

        if(meshnet == null) {
            Debug.LogError("Trying to spawn object when underlying mesh network not intialized.");
            return null;
        }
        if(meshnet.database.LookupObject(objectID) == null) {
            Debug.LogError("Trying to spawn network object without presence on the database");
            return null;
        }
        MeshNetworkIdentity localIdentity = meshnet.database.LookupObject(objectID);

        localIdentity.SetMeshnetReference(meshnet); //set a reference to the mesh network
        if (networkPrefabs.ContainsKey(localIdentity.GetPrefabID()) == false) {
            Debug.LogError("NetworkPrefab registry error: Requested prefab ID does not exist.");
            return null;
        }
        GameObject g = Instantiate(networkPrefabs[localIdentity.GetPrefabID()]);
        IdentityContainer c = g.GetComponent<IdentityContainer>();
        if (c == null) {
            Debug.LogError("NetworkPrefab error: spawned prefab does not contain IdentityContainer");
            return null;
        }
        c.SetIdentity(localIdentity);
        activeObjects.Add(objectID, g);
        return g;
    }

    
    public bool RemoveObject(ushort objectID) {
        
        if(activeObjects.ContainsKey(objectID) == false) {
            Debug.LogError("GameCoordinator has no record of object intended for removall");
            return false;
        }
        GameObject.Destroy(activeObjects[objectID]);
        activeObjects.Remove(objectID);
        return true;
    }
}
