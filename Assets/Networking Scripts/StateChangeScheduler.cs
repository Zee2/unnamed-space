using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(NetworkDatabase))]
public class StateChangeScheduler : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {


    public const float TRANSACTION_TIMEOUT = 10;

    MeshNetworkIdentity thisObjectIdentity;

    Dictionary<ushort, ushort> callbackRegistry = new Dictionary<ushort, ushort>();
    Dictionary<float, ushort> callbackTimers = new Dictionary<float, ushort>();
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
            float[] timers = new float[callbackTimers.Count];
            callbackTimers.Keys.CopyTo(timers, 0);
            foreach(float timer in timers) {
                if(Time.time - timer > TRANSACTION_TIMEOUT) {
                    callbackRegistry.Remove(callbackTimers[timer]);
                    callbackTimers.Remove(timer);
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
            callbackRegistry[echo.GetTransactionID()] = echo.GetObjectID(); //fulfill reference population, should trigger the waiting coroutine
            callbackRegistry.Remove(echo.GetTransactionID());
            callbackTimers.Remove(echo.GetTransactionID());
        }
    }

    ushort GetAvailableTransactionID() {
        for(ushort i = 1; i < ushort.MaxValue; i++) {
            if(callbackRegistry.ContainsKey(i) == false) {
                
                return i;
            }
        }
        return 0;
    }

    public bool ScheduleChange(MeshNetworkIdentity id, StateChange change, ref ushort callbackObjectID) {
        ushort transactionID = GetAvailableTransactionID();
        if(transactionID == 0) {
            Debug.LogError("State change scheduler ran out of available transaction IDs");
            return false;
        }
        callbackRegistry.Add(transactionID, callbackObjectID);
        callbackTimers.Add(Time.time, transactionID);
        StateChangeTransaction transaction = new StateChangeTransaction(transactionID, change, id);
        MeshPacket p = new MeshPacket();
        p.SetContents(transaction.GetSerializedBytes());
        p.SetPacketType(PacketType.DatabaseChangeRequest);
        p.SetSourceObjectId(GetIdentity().GetObjectID());
        p.SetSourcePlayerId(GetIdentity().meshnetReference.GetSteamID());
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId(netDB.GetIdentity().GetOwnerID());
        Debug.Log("Scheduler sending packet: target player ID = " + p.GetTargetPlayerId() + ", target object ID = " + p.GetTargetObjectId());
        GetIdentity().RoutePacket(p);
        return true;
    }
}
