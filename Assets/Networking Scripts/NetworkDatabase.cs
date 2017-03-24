﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Steamworks;

[RequireComponent(typeof(IdentityContainer))]
public class NetworkDatabase : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {

    /*
        NetworkDatabase.cs
        Copyright 2017 Finn Sinclair

        NetworkDatabase is a collection of information that is a summary of the server-authoritative game state.
        The clever part of this is that the NetworkDatabase is an INetworked object just
        like everything else in the game. This means that only one of this object actually
        exists across the network! All the other NetworkDatabases on the client computers
        are "shadows" of the real NetworkDatabase, just like all of the props and objects.
        The MeshNetworkIdentity that is attached to this component contains the ownerID of the
        database, and this is the ID that determines the provider of the real database
        information. Just like a coffee cup has an owner, and that owner has the definitive
        information on that coffee cup, the database also has an owner.

        This does NOT yet support hot-swapping authorized users. Each game session must have
        one and only one provider, and it must not change throughout the session. TODO: implement
        code for SteamMatchmaking that terminates the session when the provider leaves. Otherwise,
        SteamMatchmaking will just set somebody to be the authorized user, which will break everything.
        
        playerList: hashtable between playerID and Player object
        objectList: hashtable between objectID and MeshNetworkIdentity component

    */

    public const ushort OBJECT_ID_MIN = 10;
    public const ushort OBJECT_ID_MAX = 65500;

    

    public bool UseFullUpdates = false; //Should the network database send the entire database every time something changes?
    public MeshNetworkIdentity thisObjectIdentity; //Required for INetworked
    UnityEngine.UI.Text debugText;
    GameCoordinator game;

    //Serialized below here.
    private Dictionary<ulong, Player> playerList = new Dictionary<ulong, Player>();
    private Dictionary<ushort, MeshNetworkIdentity> objectList = new Dictionary<ushort, MeshNetworkIdentity>();

    //Entirely destroy the database records.
    //For obvious reasons, try avoid doing this unless you know what you're doing.
    public void DestroyDatabase() {
        playerList = new Dictionary<ulong, Player>();
        objectList = new Dictionary<ushort, MeshNetworkIdentity>();
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisObjectIdentity;
    }
    public void SetIdentity(MeshNetworkIdentity id) {
        thisObjectIdentity = id;
    }

    //Hunt down some relevant gameobjects to keep track of
    public void OnEnable() {
        
        
        GameObject debug = GameObject.FindGameObjectWithTag("DatabaseDebug");
        if(debug != null) {
            UnityEngine.UI.Text t = debug.GetComponent<UnityEngine.UI.Text>();
            if(t != null) {
                debugText = t;
                //Debug.Log("Succesfully set debug text");
            }
            else {
                Debug.Log("Couldn't find text component");
            }
        }
        else {
            Debug.Log("Couldn't find debug text.");
        }
        
        
        
        GameObject foundGameCoord = GameObject.FindGameObjectWithTag("NetworkArchitecture");
        if (foundGameCoord != null) {
            GameCoordinator g = foundGameCoord.GetComponent<GameCoordinator>();
            if (g != null) {
                game = g;
                //Debug.Log("Successfully found game coordinator");
            } else {
                Debug.Log("Couldn't find GameCoordinator component");
            }
        } else {
            Debug.Log("Couldn't find network architecture.");
        }
        
    }

    //If we have debug readout, update the readout
    public void Update() {
        if(debugText != null) {
            string s = "";
            s += "Players: ";
            foreach(Player p in playerList.Values) {
                s += p.GetNameSanitized() + ":" + p.GetUniqueID();
                s += "\n";
            }
            s += "\nObjects: ";
            foreach (MeshNetworkIdentity i in objectList.Values) {
                s += i.GetPrefabID() + ":" + i.GetOwnerID();
                s += "\n";
            }
            debugText.text = s;
        }
    }

    //Check with MeshNetwork to see if we are the authorized user in this lobby/game/etc
    public bool GetAuthorized() {
        return GetIdentity().IsLocallyOwned();
    }
    
    //Player modification methods
    public DatabaseChangeResult AddPlayer(Player p, bool publishChange) {
        
        
        if (playerList.ContainsKey(p.GetUniqueID())) {
            return new DatabaseChangeResult(false, "Player already exists");
        }
        if (p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            if (!GetAuthorized()) {
                Debug.Log("Unauthorized user trying to override provider.");
                return new DatabaseChangeResult(false, "Unauthorized agent overriding provider");
            }

        }
        playerList.Add(p.GetUniqueID(), p);
        if (publishChange) {
            if (GetAuthorized()) {
                SendPlayerUpdate(p, StateChange.Addition);
                return new DatabaseChangeResult(true, "Success");
            }
            else {
                Debug.LogError("Publishing changes not authorized.");
                return new DatabaseChangeResult(false, "Publishing changes not authorized");
            }
        }
        else {
            return new DatabaseChangeResult(true, "Success");
        }
    }
    public DatabaseChangeResult RemovePlayer(Player p, bool publishChange ) {
        if (playerList.ContainsKey(p.GetUniqueID()) == false) {
            Debug.LogError("User that was requested to be removed does not exist!");
            return new DatabaseChangeResult(false, "Player does not exist");
        }
        if (p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            Debug.LogError("Trying to delete provider. This definitely isn't supposed to happen.");
            return new DatabaseChangeResult(false, "Can't delete provider");
        }
        playerList.Remove(p.GetUniqueID());
        if (publishChange) {
            if (GetAuthorized()) {
                SendPlayerUpdate(p, StateChange.Removal);
                return new DatabaseChangeResult(true, "Success");
            }
            else {
                Debug.LogError("Publishing changes not authorized.");
                return new DatabaseChangeResult(false, "Publishing changes not authorized");
            }
        }
        else {
            return new DatabaseChangeResult(true, "Success");
        }
    }
    public DatabaseChangeResult ChangePlayer(Player p, bool publishChange) {
        
        if (playerList.ContainsKey(p.GetUniqueID()) == false) {
            Debug.LogError("Trying to modify player object that doesn't exist here!");
            return new DatabaseChangeResult(false, "Player doesn't exist");
        }
        if (p.GetUniqueID() == GetIdentity().GetOwnerID() && p.GetUniqueID() != (ulong)ReservedPlayerIDs.Unspecified) {
            Debug.LogError("Trying to modify provider. This isn't supposed to happen.");
            return new DatabaseChangeResult(false, "Can't modify provider");
        }
        playerList[p.GetUniqueID()].DeepCopyAndApply(p);
        if (publishChange) {
            if (GetAuthorized()) {
                SendPlayerUpdate(p, StateChange.Change);
                return new DatabaseChangeResult(true, "Success");
            }
            else {
                Debug.LogError("Publishing changes not authorized.");
                return new DatabaseChangeResult(false, "Publishing changes not authorized");
            }
        }
        else {
            return new DatabaseChangeResult(true, "Success");
        }
    }

    //Object modification methods
    public DatabaseChangeResult AddObject(MeshNetworkIdentity i, bool publishChange) {
        if (objectList.ContainsKey(i.GetObjectID())) {
            return new DatabaseChangeResult(false, "Object already exists");
        }
        if(i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
        }
        
        //If the object isn't the database, we need to assign an available objectID
        if(i.GetObjectID() != (ushort)ReservedObjectIDs.DatabaseObject) {
            IDAssignmentResult idresult = GetAvailableObjectID(); //Look for an available object id
            if (idresult.success) {
                i.SetObjectID(idresult.id);
            }else {
                return new DatabaseChangeResult(false, "No object id available for use. Too many objects?");
            }
        }
        objectList.Add(i.GetObjectID(), i);
        if (publishChange) {
            if (GetAuthorized()) {
                SendObjectUpdate(i, StateChange.Addition);
                return new DatabaseChangeResult(true, i.GetObjectID());
            }
            else {
                Debug.LogError("Publishing changes not authorized.");
                return new DatabaseChangeResult(false, "Publishing changes not authorized");
            }
        }
        else {
            return new DatabaseChangeResult(true, i.GetObjectID());
        }
    }
    public DatabaseChangeResult RemoveObject(MeshNetworkIdentity i, bool publishChange) {
        
        if (objectList.ContainsKey(i.GetObjectID()) == false) {
            return new DatabaseChangeResult(false, "Object does not exist");
        }
        if(i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.LogError("Tried to remove database. Bad idea.");
            return new DatabaseChangeResult(false, "Can't remove database.");
        }
        objectList.Remove(i.GetObjectID());
        if (publishChange) {
            if (GetAuthorized()) {
                SendObjectUpdate(i, StateChange.Removal);
                return new DatabaseChangeResult(true, i.GetObjectID());
            }
            else {
                Debug.LogError("Publishing changes not authorized.");
                return new DatabaseChangeResult(false, "Publishing changes not authorized");
            }
        }
        else {
            return new DatabaseChangeResult(true, i.GetObjectID());
        }
        
    }
    public DatabaseChangeResult ChangeObject(MeshNetworkIdentity i, bool publishChange) {
        
        if (objectList.ContainsKey(i.GetObjectID()) == false) {
            Debug.LogError("Object that was requested to be changed does not exist!");
            return new DatabaseChangeResult(false, "Object does not exist");
        }
        if (i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
            Debug.LogError("Tried to change database. This action is prohibited."); //maybe not in the future...
            return new DatabaseChangeResult(false, "Can't change database");
        }
        objectList[i.GetObjectID()].DeepCopyAndApply(i);
        if (publishChange) {
            if (GetAuthorized()) {
                SendObjectUpdate(i, StateChange.Change);
                return new DatabaseChangeResult(true, i.GetObjectID());
            }
            else {
                Debug.LogError("Publishing changes not authorized.");
                return new DatabaseChangeResult(false, "Publishing changes not authorized");
            }
        }
        else {
            return new DatabaseChangeResult(true, i.GetObjectID());
        }
    }

    //Checks if there is an object ID available for use, and returns it if there is one
    public IDAssignmentResult GetAvailableObjectID() {
        for(ushort u = OBJECT_ID_MIN; u <= OBJECT_ID_MAX; u++) {
            if(objectList.ContainsKey(u) == false) {
                IDAssignmentResult positiveResult;
                positiveResult.id = u;
                positiveResult.success = true;
                return positiveResult;
            }
        }
        IDAssignmentResult result;
        result.id = (ushort)ReservedObjectIDs.Unspecified;
        result.success = false;
        return result;
    }
    
    //Lookup a player in an error-safe way
    public Player LookupPlayer(ulong id) {
        if (playerList.ContainsKey(id)) {
            return playerList[id]; //Hash table enables very fast lookup
        }
        else {
            Debug.LogError("LookupPlayer() cannot find indicated playerID" + id);
            return null;
        }
    }

    //Lookup an object in an error-safe way
    public MeshNetworkIdentity LookupObject(ushort objectID) {
        if (objectList.ContainsKey(objectID)) {
            return objectList[objectID]; //Hash table enables very fast lookup
        }
        else {
            Debug.LogError("LookupObject() cannot find indicated playerID" + objectID);
            return null;
        }
    }

    //Retrieve array of all players on database
    public Player[] GetAllPlayers() {
        
        Player[] output = new Player[playerList.Count];
        playerList.Values.CopyTo(output, 0);
        return output;
        
    }
    
    //Send delta containing a player
    private void SendPlayerUpdate(Player p, StateChange s) {
        Dictionary<Player, StateChange> playerListDelta = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectListDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        playerListDelta.Add(p, s);
        SendDelta(playerListDelta, objectListDelta, (ulong)ReservedPlayerIDs.Broadcast, false);
    }
    
    //Send delta containing an object
    private void SendObjectUpdate(MeshNetworkIdentity id, StateChange s) {
        Dictionary<Player, StateChange> playerListDelta = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectListDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        objectListDelta.Add(id, s);
        SendDelta(playerListDelta, objectListDelta, (ulong)ReservedPlayerIDs.Broadcast, false);
    }

    
    
    //Generates order-agnostic commutative checksum of given players and objects
    //Can generate checksums of individual objects if needed
    public static ushort GenerateDatabaseChecksum(Dictionary<ulong, Player> players,
        Dictionary<ushort, MeshNetworkIdentity> objects) {

        ushort hash = 0x0;

        foreach(KeyValuePair<ulong, Player> entry in players) {
            Dictionary<Player, StateChange> fakePlayerDelta = new Dictionary<Player, StateChange>();
            Dictionary<MeshNetworkIdentity, StateChange> fakeObjectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
            fakePlayerDelta.Add(entry.Value, StateChange.Change);
            DatabaseUpdate fakeUpdate = new DatabaseUpdate(fakePlayerDelta, fakeObjectDelta, 0, false);
            byte[] data = fakeUpdate.GetSerializedBytes();
            ushort checksum = 0;
            for (int j = 0; j < data.Length; j++) {
                checksum = (ushort)(((checksum & 0xFFFF) >> 1) + ((checksum & 0x1) << 15)); // Rotate the accumulator
                checksum = (ushort)((checksum + data[j]) & 0xFFFF);                        // Add the next chunk
            }

            hash = (ushort)(hash ^ checksum);
        }
        foreach (KeyValuePair<ushort, MeshNetworkIdentity> entry in objects) {
            Dictionary<Player, StateChange> fakePlayerDelta = new Dictionary<Player, StateChange>();
            Dictionary<MeshNetworkIdentity, StateChange> fakeObjectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
            fakeObjectDelta.Add(entry.Value, StateChange.Change);
            DatabaseUpdate fakeUpdate = new DatabaseUpdate(fakePlayerDelta, fakeObjectDelta, 0, false);
            byte[] data = fakeUpdate.GetSerializedBytes();
            ushort checksum = 0;
            for (int j = 0; j < data.Length; j++) {
                checksum = (ushort)(((checksum & 0xFFFF) >> 1) + ((checksum & 0x1) << 15)); // Rotate the accumulator
                checksum = (ushort)((checksum + data[j]) & 0xFFFF);                        // Add the next chunk
            }

            hash = (ushort)(hash ^ checksum);
        }

        return hash;


    }

    //Send delta update containing given player and object delta information
    //Formats packet to be sent to specified target ID (use ReservedPlayerIDs.Broadcast if necessary)
    //Will always include the database object and the database owner, even if not specified
    private void SendDelta(Dictionary<Player, StateChange> playerUpdate, Dictionary<MeshNetworkIdentity, StateChange> objectUpdate, ulong targetPlayerID, bool isFullUpdate) {
        if(objectList.ContainsKey(GetIdentity().GetObjectID()) == false ||
            playerList.ContainsKey(GetIdentity().GetOwnerID()) == false){
            Debug.Log("Trying to send delta when database is not yet fully set up. Skipping");
            return;
        }


        MeshPacket p = new MeshPacket();
        p.SetPacketType(PacketType.DatabaseUpdate);
        p.qos = EP2PSend.k_EP2PSendReliable;
        p.SetSourcePlayerId(GetIdentity().GetOwnerID());
        p.SetSourceObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        p.SetTargetPlayerId(targetPlayerID);
        p.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        
        //Check if the database is included in the delta
        bool flag = false;
        
        foreach(MeshNetworkIdentity i in objectUpdate.Keys) {
            if(i.GetPrefabID() == GetIdentity().GetPrefabID()) {
                flag = true;
            }
        }
        //If not, add database to delta. (The database should always be included.)
        if (flag == false) {
            objectUpdate.Add(GetIdentity(), StateChange.Addition);
        }
        //Check if the owner is included in the delta
        flag = false;
        foreach (Player pl in playerUpdate.Keys) {
            if (pl.GetUniqueID() == GetIdentity().GetOwnerID()) {
                flag = true;
            }
        }
        //If not, add owner to delta. (The owner should always be included.)
        if (flag == false) {
            
            playerUpdate.Add(playerList[GetIdentity().GetOwnerID()], StateChange.Addition);
        }
        ushort hash = GenerateDatabaseChecksum(playerList, objectList);
        DatabaseUpdate update = new DatabaseUpdate(playerUpdate, objectUpdate, hash, isFullUpdate);
        p.SetContents(update.GetSerializedBytes());
        GetIdentity().RoutePacket(p);
    }

    public void ReceivePacket(MeshPacket p) {
        if(p.GetPacketType() == PacketType.DatabaseUpdate) {
            if(p.GetSourcePlayerId() == GetIdentity().GetOwnerID()) { //if the sender is authorized to make changes
                DatabaseUpdate dbup = DatabaseUpdate.ParseContentAsDatabaseUpdate(p.GetContents());
                
                if (dbup.isFullUpdate == false) {
                    ReceiveDeltaUpdate(dbup);
                }
                else {
                    ReceiveFullUpdate(dbup);
                }
            }
            else {
                Debug.LogError("Got a DatabaseUpdate from somebody not authorized! Weird!");
            }
        }else if(p.GetPacketType() == PacketType.FullUpdateRequest){
            SendFullUpdate(p.GetSourcePlayerId());
        } else if(p.GetPacketType() == PacketType.DatabaseChangeRequest) {
            ConsiderChangeRequest(p);
        }

    }
    
    public void SendFullUpdate(ulong sourceID) {
        Debug.Log("Sending full update.");
        Dictionary<Player, StateChange> playerUpdate = new Dictionary<Player, StateChange>();
        Dictionary<MeshNetworkIdentity, StateChange> objectUpdate = new Dictionary<MeshNetworkIdentity, StateChange>();

        foreach(Player p in playerList.Values) {
            playerUpdate[p] = StateChange.Override;
        }
        foreach(MeshNetworkIdentity i in objectList.Values) {
            objectUpdate[i] = StateChange.Override;
        }
        SendDelta(playerUpdate, objectUpdate, sourceID, true); //This should contain the database, so the delta algorithm shouldn't add it in
    }

    
    //This is called when the authorized database sends an update to this database.
    //If this object is the authorized database, this should never be called.
    public void ReceiveDeltaUpdate(DatabaseUpdate dbup) {
        //TODO: Write safe methods for local addition/deletion etc
        //TODO: Make sure that network and local references are not confused (StateChange.Change)
        //TODO: Refactor "override" full update system so that omissions are meaningful
        foreach (Player p in dbup.playerDelta.Keys) {
            if (dbup.playerDelta[p] == StateChange.Addition) {
                AddPlayer(p, false);
            } else if (dbup.playerDelta[p] == StateChange.Removal) {
                RemovePlayer(p, false);
            } else if (dbup.playerDelta[p] == StateChange.Change) {
                playerList[p.GetUniqueID()].DeepCopyAndApply(p);
            } else if (dbup.playerDelta[p] == StateChange.Override) { //Probably coming from a FullUpdate
                Debug.LogError("Received an override update inside a delta update");
            }
        }
        foreach (MeshNetworkIdentity i in dbup.objectDelta.Keys) {
            if (dbup.objectDelta[i] == StateChange.Addition) {

                //If the incoming object is the database, we don't want to create it
                //(because this script is running off of the already created database!)
                //We only want to create a recursive link.
                if(i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
                    DatabaseChangeResult result = AddObject(GetIdentity(), false);
                    if (result.success) {
                        Debug.Log("Successfully added database to database: recursive link achieved");
                    }
                }else {
                    DatabaseChangeResult result = AddObject(i, false);
                    if (result.success) {
                        game.SpawnObject(i.GetObjectID());
                    }
                }
            }
            else if (dbup.objectDelta[i] == StateChange.Removal) {
                DatabaseChangeResult result = RemoveObject(i, false);
                if (result.success) {
                    game.RemoveObject(i.GetObjectID());
                }
            }
            else if (dbup.objectDelta[i] == StateChange.Change) {
                objectList[i.GetObjectID()].DeepCopyAndApply(i);
            } else if (dbup.objectDelta[i] == StateChange.Override) { //Probably coming from a FullUpdate
                Debug.LogError("Received an override update inside a delta update");
            }
        }

        ushort check = GenerateDatabaseChecksum(playerList, objectList);
        if(check != dbup.fullHash) {
            Debug.Log("Database checksum doesn't match: " + check + " vs " + dbup.fullHash + ". Requesting full update.");
            MeshPacket p = new MeshPacket(new byte[0], PacketType.FullUpdateRequest,
                GetIdentity().meshnetReference.GetSteamID(),
                GetIdentity().GetOwnerID(),
                GetIdentity().GetObjectID(),
                GetIdentity().GetObjectID());
            GetIdentity().RoutePacket(p);
        }else {
            //Debug.Log("Delta successful, hash matches");
        }
    }

    public void ReceiveFullUpdate(DatabaseUpdate dbup) {
        if(dbup.isFullUpdate == false) {
            Debug.LogError("Trying to parse delta update as full update");
            return;
        }
        Dictionary<ushort, MeshNetworkIdentity> dbupObjectHashTable = new Dictionary<ushort, MeshNetworkIdentity>();
        foreach(MeshNetworkIdentity i in dbup.objectDelta.Keys) {
            dbupObjectHashTable.Add(i.GetObjectID(), i);
        }
        foreach(ushort localObjectID in objectList.Keys) { //iterate through all local objects
            if (dbupObjectHashTable.ContainsKey(localObjectID)) {



                //if they are the same prefab, keep the object, but update the info
                if(dbupObjectHashTable[localObjectID].GetPrefabID() == LookupObject(localObjectID).GetPrefabID()) {
                    LookupObject(localObjectID).DeepCopyAndApply(dbupObjectHashTable[localObjectID]);
                }
                else { //if they are different prefabs, something seriously screwy happened and we need to nuke the local object
                    RemoveObject(LookupObject(localObjectID), false); //the order of these commands is very important
                    game.RemoveObject(localObjectID);
                    AddObject(dbupObjectHashTable[localObjectID], false);
                    game.SpawnObject(localObjectID);
                    //this will probably break some gameplay stuff
                    //so this probably shouldn't happen very often
                }

                dbupObjectHashTable.Remove(localObjectID); //check this one off our list
            }
            else {
                RemoveObject(LookupObject(localObjectID), false); //If we have it and the fullUpdate doesn't, nuke it!
            }
            //Now, only the objects that we don't have yet are left in the databaseupdate
            //Debug.Log(dbupObjectHashTable.Keys.Count + " items included in fullUpdate that we don't currently have. Now adding them");
            foreach(MeshNetworkIdentity i in dbupObjectHashTable.Values) {
                AddObject(i, false); //add the ones we should have
            }

        }
    }

    /*
        Users SHOULD be able to:
            Request object creation and assign the object to themselves
            Request object ownership change of others' objects if the object is unlocked
            Request object deletion of their own objects


        Users SHOULD NOT be able to:
            Request object deletion of others' objects
            Create objects and make other players the owner
            Request object ownership change of others' objects if the object is locked
            Request changing objects' objectID, prefabID, or any other 

    */
    public void ConsiderChangeRequest(MeshPacket p) {
        if(GetAuthorized() == false) {
            Debug.LogError("Being asked to change the database when not authorized!");
            return;
        }
        if(LookupPlayer(p.GetSourcePlayerId()) == null) {
            Debug.LogError("Change request source player does not exist on the database");
            return;
        }

        StateChangeTransaction transaction = StateChangeTransaction.ParseSerializedBytes(p.GetContents());
        if(transaction.GetChangeType() == StateChange.Addition) {
            //Check if the desired owner is the packet sender
            if(p.GetSourcePlayerId() != transaction.GetObjectData().GetOwnerID()) {
                Debug.LogError("Requested object creation tries to assign to new player: prohibited");
                return;
            }
            DatabaseChangeResult result = AddObject(transaction.GetObjectData(), true);
            if(result.success == false) {
                Debug.LogError("Requested object addition failed: " + result.error);
                return;
            }
            else {
                game.SpawnObject(result.data);
                EchoChangeRequest(transaction.GetTransactionID(), result.data, p.GetSourcePlayerId());
            }
            
        }else if(transaction.GetChangeType() == StateChange.Removal) {
            //Check if requesting user owns the object
            if(p.GetSourcePlayerId() != transaction.GetObjectData().GetOwnerID()) {
                Debug.LogError("User trying to remove an object that doesn't belong to them");
                return;
            }
            MeshNetworkIdentity localObjectToRemove = LookupObject(transaction.GetObjectData().GetObjectID());
            if(localObjectToRemove == null) {
                Debug.LogError("Couldn't find the object requested to be removed.");
            }
            DatabaseChangeResult result = RemoveObject(LookupObject(transaction.GetObjectData().GetObjectID()), true);
            if(result.success == false) {
                Debug.LogError("Requested object removal failed: " + result.error);
                return;
            }
            else {
                game.RemoveObject(result.data);
                EchoChangeRequest(transaction.GetTransactionID(), localObjectToRemove.GetObjectID(), p.GetSourcePlayerId());
            }
        }
    }

    public void EchoChangeRequest(ushort transactionID, ushort objectID, ulong destinationPlayer) {

        if (!GetAuthorized()) {
            Debug.LogError("Not authorized to echo");
            return;
        }
        
        StateChangeEcho echo = new StateChangeEcho(transactionID, objectID);
        MeshPacket packet = new MeshPacket();
        packet.SetPacketType(PacketType.DatabaseChangeEcho);
        packet.SetContents(echo.GetSerializedBytes());
        packet.SetTargetPlayerId(destinationPlayer);
        packet.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        packet.SetSourcePlayerId(GetIdentity().GetOwnerID());
        packet.SetTargetObjectId((ushort)ReservedObjectIDs.DatabaseObject);
        GetIdentity().RoutePacket(packet);
    }
    
}
