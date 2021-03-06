﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class TestObjectSpawner : MonoBehaviour {
    public ushort prefabID;
    public const float SPAWN_TIMEOUT = 5f;
    public MeshNetwork meshnet;
    StateChangeScheduler scheduler;
    bool available = true;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Insert)) {
            available = false;
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
        MeshNetworkIdentity requestedID = new MeshNetworkIdentity(0, prefabID, meshnet.GetLocalPlayerID(), false);
        IDContainer returnedObjectID = new IDContainer((ushort)ReservedObjectIDs.Unspecified);

        scheduler.ScheduleChange(requestedID, StateChange.Addition, ref returnedObjectID);

        while(returnedObjectID.id == (ushort)ReservedObjectIDs.Unspecified) {
            if(Time.time - timeStart > SPAWN_TIMEOUT) {
                Debug.LogError("Spawn timeout");
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }

        MeshNetworkIdentity newIdentity = meshnet.database.LookupObject(returnedObjectID.id);
        GameObject g = meshnet.game.GetObjectByIdentity(newIdentity.GetObjectID());
        if(prefabID != 4)
            g.AddComponent<BasicMovementTest>();
        yield break;
        yield return new WaitForSeconds(1);

        timeStart = Time.time;
        returnedObjectID = new IDContainer((ushort)ReservedObjectIDs.Unspecified);
        scheduler.ScheduleChange(newIdentity, StateChange.Removal, ref returnedObjectID);
        while (returnedObjectID.id == (ushort)ReservedObjectIDs.Unspecified) {
            if (Time.time - timeStart > SPAWN_TIMEOUT) {
                Debug.LogError("Spawn timeout");
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }
    }
}

class JitterTest{
    public void PrePreJitMe() {
        for(int i = 0; i < 1000; i++) {
            PreJitMe();
        }
    }
    public void PreJitMe() {
        List<int> l = new List<int>(1);
        ulong a = 0;
        for (int i = 1; i < 100000; i++) {
            l.Add(i);
        }
        
    }
}
