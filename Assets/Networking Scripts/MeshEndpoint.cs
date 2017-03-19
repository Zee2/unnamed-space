﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities;
using Steamworks;

public class MeshEndpoint : MonoBehaviour {

    /*
        MeshEndpoint.cs
        Copyright 2017 Finn Sinclair

        MeshEndpoint is the main hub for incoming and outgoing P2P packets. It interfaces
        directly with the SteamNetworking libjingle system, or whichever NAT traversal
        interface is needed. It contains decision logic for parsing inncoming packets,
        including sanity checks for various factors (whether the source user exists at all,
        etc) and reroutes the validated packet to the correct networked object.

        Will intelligently respond to various state issues such as an uninitialized network
        database, missing user data, and missing object data.

        Supports sending of packets through the standard networked object model, or direct
        to a NAT identifier. (Useful when forcing packets to destinations when the
        database has not yet been created.)
    */
    
    public MeshNetwork meshnet;

    List<MeshPacket> failedPackets = new List<MeshPacket>();
    Dictionary<MeshPacket, int> packetRetries = new Dictionary<MeshPacket, int>();

    //Checks for packets from all servers.


    public void Update() {
        Receive();
    }
    public void Receive() {
        if (failedPackets.Count > 0) {
            MeshPacket p = failedPackets[0];
            failedPackets.RemoveAt(0);
            ParseData(p);
        }


        uint bufferLength = 0;
        if (SteamNetworking.IsP2PPacketAvailable(out bufferLength)) {
            Debug.Log("Receiving Packet, " + bufferLength + " bytes long");
            byte[] destBuffer = new byte[bufferLength];
            UInt32 bytesRead = 0;
            CSteamID remoteID;
            SteamNetworking.ReadP2PPacket(destBuffer, bufferLength, out bytesRead, out remoteID);

            ParseData(new MeshPacket(destBuffer));
        }
        
        
    }

    
    void ParseData(MeshPacket incomingPacket) {

        if(incomingPacket.GetSourcePlayerId() == SteamUser.GetSteamID().m_SteamID) {
            Debug.Log("Discarding packet from self");
            return;
        }


        if(incomingPacket.GetPacketType() == PacketType.PlayerJoin) {
            Debug.Log("PlayerJoin packet identified");
            if(meshnet.database == null) {
                Debug.LogError("Database not intialized yet!");
                return;
            }
            if(meshnet.database.GetAuthorized() == false) {
                Debug.Log("I'm not the provider. Discarding PlayerJoin packet");
                return;
            }
            CSteamID sID = new CSteamID(incomingPacket.GetSourcePlayerId());
            Player p = meshnet.ConstructPlayer(sID);
            meshnet.database.AddPlayer(p);
            return;

        }else if(incomingPacket.GetPacketType() == PacketType.DatabaseUpdate) {
            if(meshnet.database == null) {
                Debug.Log("Received first database update, no database to send it to.");
                Debug.Log("Rerouting to MeshNetwork.");
                meshnet.InitializeDatabaseClientside(incomingPacket);
                return;
            }
        }

        //If the packet is neither a PlayerJoin or a DatabaseUpdate
        Player source = meshnet.database.LookupPlayer(incomingPacket.GetSourcePlayerId()); //retrieve which player sent this packet
        if (source == null) { //hmmm, the NBD can't find the player
            Debug.LogError("Player from which packet originated does not exist on local NDB.");
            return;
        }

        MeshNetworkIdentity targetObject = meshnet.database.LookupObject(incomingPacket.GetTargetObjectId());
        if (targetObject == null) {
            Debug.LogError("Packet's target object doesn't exist on the database!");
            return;
        }

        targetObject.ReceivePacket(incomingPacket);


    }

    public void SendDirectToSteamID(MeshPacket packet, CSteamID id) {
        Debug.Log("Direct sending. You sure you want to do this?");
        byte[] data = packet.GetSerializedBytes();
        SteamNetworking.SendP2PPacket(id, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
    }

    public void Send(MeshPacket packet) {
        if(meshnet.database == null) {
            Debug.LogError("Trying to send packet when database does not exist.");
        }
        byte[] data = packet.GetSerializedBytes();
        Player[] allPlayers = meshnet.database.GetAllPlayers();
        if (packet.GetTargetPlayerId() == (byte)ReservedPlayerIDs.Broadcast) {
            foreach (Player p in allPlayers) {
                SteamNetworking.SendP2PPacket(new CSteamID(p.GetUniqueID()), data, (uint)data.Length, packet.qos);
            }
        }
        else {
            Player target = meshnet.database.LookupPlayer(packet.GetTargetPlayerId());
            SteamNetworking.SendP2PPacket(new CSteamID(target.GetUniqueID()), data, (uint)data.Length, packet.qos);
        }
        
        
    }

    
    

}
