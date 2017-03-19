using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class TestObjectSpawner : MonoBehaviour {
    
    public const float SPAWN_TIMEOUT = 5f;
    public MeshNetwork meshnet;
    StateChangeScheduler scheduler;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Insert)) {
            scheduler = meshnet.database.gameObject.GetComponent<StateChangeScheduler>();
            if(scheduler == null) {
                Debug.LogError("Couldn't find scheduler");
                return;
            }
            StartCoroutine(SpawnCapsule());
        }
	}

    IEnumerator SpawnCapsule() {
        float timeStart = Time.time;
        MeshNetworkIdentity requestedID = new MeshNetworkIdentity(0, 2, meshnet.GetSteamID(), false);
        ushort returnedObjectID = (ushort)ReservedObjectIDs.Unspecified;
        Debug.Log("Asking scheduler to make spawn request");
        scheduler.ScheduleChange(requestedID, StateChange.Addition, ref returnedObjectID);

        while(returnedObjectID == (ushort)ReservedObjectIDs.Unspecified) {
            if(Time.time - timeStart > SPAWN_TIMEOUT) {
                Debug.LogError("Spawn timeout");
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }

        MeshNetworkIdentity newIdentity = meshnet.database.LookupObject(returnedObjectID);
        Debug.Log("Prefab ids match: " + (requestedID.GetPrefabID() == newIdentity.GetPrefabID()));
        Debug.Log("Owner ids match: " + (requestedID.GetOwnerID() == newIdentity.GetOwnerID()));
        yield return new WaitForSeconds(1);

        timeStart = Time.time;
        returnedObjectID = (ushort)ReservedObjectIDs.Unspecified;
        scheduler.ScheduleChange(newIdentity, StateChange.Removal, ref returnedObjectID);
        while (returnedObjectID == (ushort)ReservedObjectIDs.Unspecified) {
            if (Time.time - timeStart > SPAWN_TIMEOUT) {
                Debug.LogError("Spawn timeout");
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("Removal: objectIDS match: " + (returnedObjectID == newIdentity.GetObjectID()));
    }
}
