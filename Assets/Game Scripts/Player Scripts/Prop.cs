using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;


public class Prop : InteractableObject, INetworked<MeshNetworkIdentity>, IReceivesPacket<MeshPacket> {

    public bool hasRigidbody;
    public Rigidbody thisRigidbody;

    public Vector3 currentPositionOffset;
    public Quaternion currentRotationOffset;

    MeshNetworkIdentity thisIdentity;
    byte subcomponentID;
    // Use this for initialization
    void Start() {
        thisRigidbody = GetComponent<Rigidbody>();
        if(thisRigidbody == null) {
            hasRigidbody = false;
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        hasRigidbody = (thisRigidbody == null);
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisIdentity;
    }
    public void SetIdentity(MeshNetworkIdentity id) {
        thisIdentity = id;
    }

    public byte GetSubcomponentID() {
        return subcomponentID;
    }
    public void SetSubcomponentID(byte id) {
        subcomponentID = id;
    }

    public void ReceivePacket(MeshPacket p) {
        return;
    }

    public override void SetBeingUsed(bool used) {
        if(IsBeingUsed() != used) {
            if(GetIdentity() != null) {
                if (GetIdentity().IsLocallyOwned() == false)
                    return;
                base.SetBeingUsed(used);
                MeshPacket p = new MeshPacket();
                p.SetContents(new PropUpdate(used).GetSerializedBytes());
                p.SetTargetObjectId(GetIdentity().GetObjectID());
                p.SetTargetPlayerId((ulong)ReservedPlayerIDs.Broadcast);
                p.SetSourceObjectId(GetIdentity().GetObjectID());
                p.SetSourcePlayerId(GetIdentity().GetLocalPlayerID());

                p.SetPacketType(PacketType.GenericStateUpdate);
                GetIdentity().RoutePacket(p);
            }
            
        }

        

    }

}
