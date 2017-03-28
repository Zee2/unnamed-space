using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(NetworkDatabase))]
public class StateChangeScheduler : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {


    public const float TRANSACTION_TIMEOUT = 10;
    ushort lastID = 0;
    MeshNetworkIdentity thisObjectIdentity;

    Dictionary<ushort, IDContainer> callbackRegistry = new Dictionary<ushort, IDContainer>();
    Dictionary<ushort, float> callbackTimers = new Dictionary<ushort, float>();
    float lastTimerCheck = 0;
    NetworkDatabase netDB;

	void OnEnable() {
        netDB = GetComponent<NetworkDatabase>();
        if(netDB == null) {
            Debug.LogError("StateChangeScheduler could not acquire database reference");
        }
    }

    void Update() {
        if(Time.time - lastTimerCheck > 5f) {
            ushort[] keys = new ushort[callbackTimers.Count];
            callbackTimers.Keys.CopyTo(keys, 0);
            for(int i = 0; i < keys.Length; i++) {
                if(Time.time - callbackTimers[keys[i]] > TRANSACTION_TIMEOUT) {
                    callbackRegistry.Remove(keys[i]);
                    callbackTimers.Remove(keys[i]);
                }
            }
        }
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisObjectIdentity;
    }
    public void SetIdentity(MeshNetworkIdentity i) {
        thisObjectIdentity = i;
    }

    public void ReceivePacket(MeshPacket p) {
        if (p.GetPacketType() == PacketType.DatabaseChangeEcho){
            if(p.GetSourcePlayerId() != netDB.GetIdentity().GetOwnerID()) {
                Debug.LogError("User that does not own the database is trying to give us echoes");
                return;
            }
            StateChangeEcho echo;
            echo = StateChangeEcho.ParseSerializedBytes(p.GetContents());
            
            if(callbackRegistry.ContainsKey(echo.GetTransactionID()) == false) {
                Debug.LogError("Echoed transaction ID is not present in transaction registry");
                return;
            }
            callbackRegistry[echo.GetTransactionID()].id = echo.GetObjectID(); //fulfill reference population, should trigger the waiting coroutine
            callbackRegistry.Remove(echo.GetTransactionID());
            callbackTimers.Remove(echo.GetTransactionID());
        }
    }

    ushort GetNextTransactionID() {
        if(lastID == ushort.MaxValue - 1) {
            lastID = 0;
            return 0;
        }
        else {
            lastID++;
            return lastID;
        }
    }

    public bool ScheduleChange(MeshNetworkIdentity id, StateChange change, ref IDContainer idReference) {
        
        if(GetIdentity().meshnetReference.database == null) {
            Debug.LogError("Can't schedule a state change without an active database");
            return false;
        }

        ushort transactionID = GetNextTransactionID();
        
        callbackRegistry.Add(transactionID, idReference);
        callbackTimers.Add(transactionID, Time.time);
        StateChangeTransaction transaction = new StateChangeTransaction(transactionID, change, id);
        MeshPacket p = new MeshPacket();
        p.SetContents(transaction.GetSerializedBytes());
        p.SetPacketType(PacketType.DatabaseChangeRequest);
        p.SetSourceObjectId(GetIdentity().GetObjectID());
        p.SetSourcePlayerId(GetIdentity().meshnetReference.GetLocalPlayerID());
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId(netDB.GetIdentity().GetOwnerID());
        GetIdentity().RoutePacket(p);
        return true;
        
        
    }

    public bool ScheduleChange(MeshNetworkIdentity id, StateChange change) {
        IDContainer dummyContainer = new IDContainer();
        return ScheduleChange(id, change, ref dummyContainer);
    }
    
}
