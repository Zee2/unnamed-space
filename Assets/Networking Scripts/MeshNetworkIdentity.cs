using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System;

public class MeshNetworkIdentity : IReceivesPacket<MeshPacket>, IMeshSerializable {

    /// <summary>
    /// 
    ///     MeshNetworkIdentity.cs
    ///     Copyright 2017 Finn Sinclair
    ///     
    ///     MeshNetworkIdentity is a script that allows objects to be 
    ///     synchronized across the mesh network. It receives
    ///     packets from the ServerManager based on its objectID, and routes them
    ///     to the correct component on the object (MeshNetworkTransform, etc). It
    ///     is packet-unaware, meaning that it will take any MeshPacket and route it
    ///     to the assigned component. MeshNetworkIdentity also is the dispatch point
    ///     for outgoing packets, where a component on the object (MeshNetworkTransform, etc)
    ///     will use the Identity to send the packets.
    ///     
    ///     MeshNetworkIdentity lives inside a container component on a networked prefab.
    ///     It contains a list attachedComponents, which keeps track of all of the networked
    ///     components living on the prefab. It will broadcast the incoming packet
    ///     indiscriminately to all components in this list. Each component must be
    ///     responsible for sanity-checking the incoming packet. The vast majority
    ///     of usage-cases will involve only one networked component attached to this
    ///     MeshNetworkIdentity. However, if there are multiple, you should use the
    ///     Utilities.PacketType enumeration to differentiate your data types.
    ///
    /// 
    /// </summary>

    public const int NETWORK_IDENTITY_BYTE_SIZE = 2+2+8+1;

    public MeshNetwork meshnetReference;
    ushort objectID;
    ushort prefabID;
    ulong ownerID;
    bool locked;

    //Not serialized across the network! This gets initialized and populated
    //when the container component is enabled. All IReceivesPacket components
    //attached to the relevant object will wind up in this List<>.
    public List<IReceivesPacket<MeshPacket>> attachedComponents;

    public byte GetSubcomponentID() {
        return (byte)ReservedSubcomponentIDs.Root;
    }
    public void SetSubcomponentID(byte id) {
        return;
    }

    public MeshNetworkIdentity(ushort objectID, ushort prefabID, ulong ownerID, bool locked) {
        this.objectID = objectID;
        this.prefabID = prefabID;
        this.ownerID = ownerID;
        this.locked = locked;
        
    }
    public MeshNetworkIdentity() {
        this.objectID = (ushort)ReservedObjectIDs.Unspecified;
        this.prefabID = (ushort)ReservedPrefabIDs.Unspecified;
        this.ownerID = (ulong)ReservedPlayerIDs.Unspecified;
        this.locked = false;
    }
    
    public void ReceivePacket(MeshPacket p) {
        if(attachedComponents.Count == 0) {
            Debug.Log("This MeshNetworkIdentity has no associated components! Forgot to populate it?");
        }
        foreach(IReceivesPacket<MeshPacket> component in attachedComponents) {
            component.ReceivePacket(p);
        }
    }

    public void RoutePacket(MeshPacket p) {
        //Debug.Log("Routing packet through objectID = " + GetObjectID() + ", destinationObject = " + p.GetTargetObjectId());
        if (meshnetReference == null) {
            Debug.LogError("Identity trying to route packet without meshnet reference");
            return;
        }
        if(p.GetSourceObjectId() != GetObjectID()) {
            Debug.LogError("Wrong MNI used to route packet: srcID = " + p.GetSourceObjectId() + ", thisID = " + GetObjectID());
            return;
        }
        meshnetReference.RoutePacket(p);
    }

    public bool IsLocallyOwned() {
        if (meshnetReference == null) {
            Debug.LogError("Meshnet reference missing");
            return false;
        }
        if (meshnetReference.GetLocalPlayerID() == GetOwnerID()) {
            return true;
        }else {
            return false;
        }
    }
    //Performs a deep copy of the given MNI and applies it to the existing object, preserving active pointers
    //Preserves the existing MeshNetwork reference and the existing list of attached components
    //These are not serialized across the network (obviously) and thus should not be deepcopied
    public void DeepCopyAndApply(MeshNetworkIdentity i) {
        SetLocked(i.GetLocked());
        SetObjectID(i.GetObjectID());
        SetOwnerID(i.GetOwnerID());
        SetPrefabID(i.GetPrefabID());
    }


    public byte[] GetSerializedBytes() {
        byte[] output = new byte[NETWORK_IDENTITY_BYTE_SIZE];
        Buffer.BlockCopy(BitConverter.GetBytes(objectID), 0, output, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(prefabID), 0, output, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(ownerID), 0, output, 4, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(locked), 0, output, 12, 1);
        
        return output;
    }
    public void DeserializeAndApply(byte[] data) {
        objectID = BitConverter.ToUInt16(data, 0);
        prefabID = BitConverter.ToUInt16(data, 2);
        ownerID = BitConverter.ToUInt64(data, 4);
        locked = BitConverter.ToBoolean(data, 12);
    }

    public ushort GetObjectID() {
        return objectID;
    }
    public void SetObjectID(ushort id) {
        objectID = id;
    }
    public ushort GetPrefabID() {
        return prefabID;
    }
    public void SetPrefabID(ushort id) {
        prefabID = id;
    }
    public ulong GetOwnerID() {
        return ownerID;
    }
    public void SetOwnerID(ulong id) {
        ownerID = id;
    }
    public bool GetLocked() {
        return locked;
    }
    public void SetLocked(bool l) {
        locked = l;
    }

    public void SetMeshnetReference(MeshNetwork net) {
        meshnetReference = net;
    }
}
