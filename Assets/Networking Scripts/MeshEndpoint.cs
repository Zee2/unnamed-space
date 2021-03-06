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
    float msAllowedForPackets = 5;
    //Checks for packets from all servers.


    public void Update() {
        
        Receive();
        
    }
    public void Receive() {
        System.Diagnostics.Stopwatch timeSpentProcessingPackets = new System.Diagnostics.Stopwatch();
        timeSpentProcessingPackets.Start();
        /*
        if (failedPackets.Count > 0) {
            MeshPacket p = failedPackets[0];
            failedPackets.RemoveAt(0);
            ParseData(p);
        }
        */

        uint bufferLength = 0;
        while (timeSpentProcessingPackets.Elapsed.TotalMilliseconds < msAllowedForPackets) {
            bufferLength = 0;
            if (SteamNetworking.IsP2PPacketAvailable(out bufferLength)) {
                //Debug.Log("Receiving Packet, " + bufferLength + " bytes long");
                byte[] destBuffer = new byte[bufferLength];
                UInt32 bytesRead = 0;
                CSteamID remoteID;
                SteamNetworking.ReadP2PPacket(destBuffer, bufferLength, out bytesRead, out remoteID);
                //Debug.Log("CSteamID remoteID = " + remoteID.m_SteamID);
                ParseData(new MeshPacket(destBuffer));
            }else {
                break;
            }
        }
        
        
        
    }

    
    void ParseData(MeshPacket incomingPacket) {
        
        //Debug.Log("Packet parsing: type = " + incomingPacket.GetPacketType() + ", source playerID = " + incomingPacket.GetSourcePlayerId() + ", target objectID = " + incomingPacket.GetTargetObjectId());
        if (incomingPacket.GetSourcePlayerId() == meshnet.GetLocalPlayerID()) {
            //Debug.Log("Discarding packet from self");
            //return;
        }

        
        if (incomingPacket.GetPacketType() == PacketType.PlayerJoin) {
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
            meshnet.database.AddPlayer(p, true);
            return;

        }else if(incomingPacket.GetPacketType() == PacketType.DatabaseUpdate) {
            
            
            if (meshnet.database == null) {
                Debug.Log("Received first database update, no database to send it to.");
                Debug.Log("Rerouting to MeshNetwork.");
                meshnet.InitializeDatabaseClientside(incomingPacket);
                return;
            }
        }else if(incomingPacket.GetPacketType() == PacketType.KickPacket) {
            meshnet.initiateDisconnect();
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
            //Debug.LogError("type = " + incomingPacket.GetPacketType() + ", sourceObject = " + incomingPacket.GetSourceObjectId() + ", source player = " + incomingPacket.GetSourcePlayerId() + ", target object = " + incomingPacket.GetTargetObjectId());
            return;
        }

        targetObject.ReceivePacket(incomingPacket);
        
    }

    public void SendDirectToSteamID(MeshPacket packet, ulong id) {
        CSteamID steamID = new CSteamID(id);
        Debug.Log("Direct sending. You sure you want to do this?");
        //Debug.Log("Dest. object: " + packet.GetTargetObjectId());
        byte[] data = packet.GetSerializedBytes();
        SteamNetworking.SendP2PPacket(steamID, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
    }

    public void Send(MeshPacket packet) {
        //Debug.Log("Send(): Dest. object: " + packet.GetTargetObjectId());
        if (meshnet.database == null) {
            Debug.LogError("Trying to send packet when database does not exist.");
        }

        if(packet.GetTargetPlayerId() == meshnet.GetLocalPlayerID()) {
            ParseData(packet);
            return;
        }
        
        byte[] data = packet.GetSerializedBytes();
        Player[] allPlayers = meshnet.database.GetAllPlayers();
        if (packet.GetTargetPlayerId() == (byte)ReservedPlayerIDs.Broadcast) {
            foreach (Player p in allPlayers) {
                if(p.GetUniqueID() == meshnet.GetLocalPlayerID() && packet.GetPacketType() == PacketType.TransformUpdate) {
                    continue;
                }

                SteamNetworking.SendP2PPacket(new CSteamID(p.GetUniqueID()), data, (uint)data.Length, packet.qos);
            }
        }
        else {
            
            SteamNetworking.SendP2PPacket(new CSteamID(packet.GetTargetPlayerId()), data, (uint)data.Length, packet.qos);
        }
        
        
    }

    
    

}
