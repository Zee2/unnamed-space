﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;


public class Server {

    

    public bool isSetup = false;
    public bool isListening = false;

    public string shout;

    int port;
    byte reliableChannel;
    byte unreliableChannel;
    int socketID;

    List<PeerInfo> peers = new List<PeerInfo>();

	// Use this for initialization
	public Server() {
	    
	}

    public int getSocketID() {
        return socketID;
    }
    public int getPort() {
        return port;
    }

    public void SetupServer(int listenPort) {
        port = listenPort;
        Debug.Log("Starting up host at port " + listenPort);
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();

        reliableChannel = config.AddChannel(QosType.Reliable);
        unreliableChannel = config.AddChannel(QosType.Unreliable);
        
        HostTopology topology = new HostTopology(config, 10);

        socketID = NetworkTransport.AddHost(topology, listenPort);
        isSetup = true;
    }

    public bool Connect(string address, int port) {
        byte err;
        int connId;
        connId = NetworkTransport.Connect(socketID, address, port, 0, out err);
        NetworkError error = (NetworkError)err;
        Debug.Log("Server with socketID " + socketID + "tried to connect to peer " + address + ":" + port + " with error code " + error.ToString());
        if (error.Equals(NetworkError.Ok)) {
            Debug.Log("Server successfully made connection with peer, awaiting connect confirmation.");
            return true;
        }else {
            Debug.Log("NetworkTransport.Connect() had an error. Did not connect to peer.");
            return false;
        }
        
    }
	
	
	public void Update () {
	}

    public bool AcceptPeer(int id) {
        bool isIdUnique = true;
        foreach(PeerInfo ci in peers) {
            if(ci.connectionId == id) {
                isIdUnique = false;
            }
        }
        if (!isIdUnique) {
            return false;
        }

        PeerInfo newConnection = new PeerInfo(id);
        peers.Add(newConnection);
        return true;
    }

    public bool BroadcastAll(byte[] packet) {
        Debug.Log("BroadcastAll");
        bool successflag = true;
        byte error;
        foreach(PeerInfo peer in peers) {
            byte err;
            bool success = NetworkTransport.Send(socketID, peer.connectionId, unreliableChannel, packet, packet.Length, out err);
            if (!success) {
                successflag = false;
                error = err;
            }
                
        }
        return successflag;
        
    }
    

    
}
