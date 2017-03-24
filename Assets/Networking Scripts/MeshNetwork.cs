using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime;
using System;
using System.Reflection;
using Steamworks;
using Utilities;

[RequireComponent(typeof(UIController))]
[RequireComponent(typeof(GameCoordinator))]
public class MeshNetwork : MonoBehaviour {

    /// <summary>
    /// 
    ///     MeshNetwork.cs
    ///     Copyright 2017 Finn Sinclair
    ///     
    ///     The main manager of all network-related actions. Interfaces with the Steam
    ///     matchmaking system, for both provider and peer activities. Manages the lobby,
    ///     manages joining and leaving events (needs work), and has the ability to
    ///     route packets to the mesh endpoint.
    ///      
    ///     Check the #regions for delineated provider and client code. MeshNetwork can
    ///     do both, but not at once. Right now, MeshNetwork is definitely state-sensitive,
    ///     and will break if treated incorrectly. More robust implementations and state-insensitive
    ///     methods are in the works.
    ///     
    /// </summary>


    UIController networkUIController;
    public NetworkDatabase database;
    public GameCoordinator game;
    MeshEndpoint endpoint;
    //Current lobby
    CSteamID lobby = CSteamID.Nil;

    //Steamworks callbacks/callresults
    CallResult<LobbyCreated_t> m_LobbyCreated;
    CallResult<LobbyEnter_t> m_JoinedLobby;
    CallResult<LobbyMatchList_t> m_GotLobbyList;
    Callback<P2PSessionRequest_t> m_NewUserSession;
    Callback<LobbyChatUpdate_t> m_ChatUpdate;
    void Start() {
        Debug.logger.logEnabled = true;
        DontDestroyOnLoad(gameObject);
        
        Debug.logger.logEnabled = true;
        
        foreach (var method in typeof(DatabaseUpdate).GetMethods()) {
            //Debug.Log("Method: " + method.Name);
            method.MethodHandle.GetFunctionPointer();
        }
        foreach (var method in typeof(MeshNetworkIdentity).GetMethods()) {
            //Debug.Log("Method: " + method.Name);
            method.MethodHandle.GetFunctionPointer();
        }
        foreach (var method in typeof(Player).GetMethods()) {
            //Debug.Log("Method: " + method.Name);
            method.MethodHandle.GetFunctionPointer();
        }
        foreach (var method in typeof(String).GetMethods()) {
            //Debug.Log("Method: " + method.Name);
            method.MethodHandle.GetFunctionPointer();
        }
        foreach (var method in typeof(MeshPacket).GetMethods()) {
            //Debug.Log("Method: " + method.Name);
            method.MethodHandle.GetFunctionPointer();
        }

        typeof(NetworkDatabase).GetMethod("GenerateDatabaseChecksum").MethodHandle.GetFunctionPointer();
        Player p = new Player("hello", 123, "abc");
        //System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(System.RuntimeMethodHandle
        Testing.DebugDatabaseSerialization();
        //Testing.BitTesting();
        //Testing.TransactionTesting();

        p.SerializeFull();
        
        networkUIController = gameObject.GetComponent<UIController>();

        
        endpoint = gameObject.AddComponent<MeshEndpoint>();
        endpoint.meshnet = this;
        game = gameObject.GetComponent<GameCoordinator>();
        
        if (SteamManager.Initialized) {
            m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnCreateLobby);
            m_JoinedLobby = CallResult<LobbyEnter_t>.Create(OnJoinedLobby);
            m_GotLobbyList = CallResult<LobbyMatchList_t>.Create(OnGotLobbyList);
            m_NewUserSession = Callback<P2PSessionRequest_t>.Create(OnSessionRequest);
            m_ChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyUpdate);
        }
        else {
            Debug.LogError("SteamManager not initialized!");
        }
        //WarmupHosting();

    }

    protected void OnSessionRequest(P2PSessionRequest_t pCallback) {
        if (lobby.Equals(CSteamID.Nil)) {
            Debug.Log("User trying to send packet to us, and our lobby doesn't exist!");
            return;
        }
        bool flag = false;
        for(int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(lobby); i++) {
            if(SteamMatchmaking.GetLobbyMemberByIndex(lobby, i) == pCallback.m_steamIDRemote) {
                flag = true;
            }
        }

        if (flag) {
            SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
        }
    }
    
    //Create a networked player given SteamID information.
    //Pass in SteamUser.GetSteamID() for <id> if you want to
    //construct your own player object.
    public Player ConstructPlayer(CSteamID id) {
        Player p = new Player();
        string name = SteamFriends.GetFriendPersonaName(id);
        if (name.Equals("")) {
            Debug.LogError("Name request returned blank, (probably) lobby not ready");
            return null;
        }
        p.SetName(name);
        p.SetUniqueID(id.m_SteamID);
        p.SetPrivateKey("key");
        return p;
    }

    public ulong GetSteamID() {
        return SteamUser.GetSteamID().m_SteamID;
    }

    public void RoutePacket(MeshPacket p) {
        endpoint.Send(p);
    }

    public void RoutePacketDirect(MeshPacket p, CSteamID id) {
        endpoint.SendDirectToSteamID(p, id);
    }

    public void OnApplicationExit() {
        if(!lobby.Equals(CSteamID.Nil))
            SteamMatchmaking.LeaveLobby(lobby);
    }
    


    #region Provider-oriented code

    public void WarmupHosting() {
        MeshNetworkIdentity databaseID = new MeshNetworkIdentity((ushort)ReservedObjectIDs.DatabaseObject,
            (ushort)ReservedPrefabIDs.Database,
            (ulong)GetSteamID(), true);

        NetworkDatabase database2 = game.SpawnDatabase(databaseID).GetComponent<NetworkDatabase>(); //Spawns the database prefab.
        Debug.Log("Registering database.");
        database2.AddObject(databaseID, true); //Tells the database that it itself exists (funny)

        //First, we get our own player object, and we make ourselves the provider.
        Player me = ConstructPlayer(SteamUser.GetSteamID());
        Debug.Log("Registering provider.");
        database.AddPlayer(me, true);
    }

    public void HostGame() {
        
        if(lobby.Equals(CSteamID.Nil) == false) {
            Debug.LogError("Lobby already created. Probably already hosting. Must shut down hosting before doing it again.");
            return;
        }

        //Construct the network database. Very important!
        MeshNetworkIdentity databaseID = new MeshNetworkIdentity((ushort)ReservedObjectIDs.DatabaseObject, 
            (ushort)ReservedPrefabIDs.Database, 
            (ulong)GetSteamID(), true);
        
        database = game.SpawnDatabase(databaseID).GetComponent<NetworkDatabase>(); //Spawns the database prefab.
        Debug.Log("Registering database.");
        database.AddObject(databaseID, true); //Tells the database that it itself exists (funny)
        
        //First, we get our own player object, and we make ourselves the provider.
        Player me = ConstructPlayer(SteamUser.GetSteamID());
        Debug.Log("Registering provider.");
        database.AddPlayer(me, true);

        //Actually create the lobby. Password info, etc, will be set after this.
        Debug.Log("Creating Lobby");
        m_LobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 4));
        
    }
    public void OnCreateLobby(LobbyCreated_t pCallback, bool bIOFailure) {
        if(pCallback.m_eResult != EResult.k_EResultOK) {
            Debug.LogError("Lobby creation didn't work.");
            Debug.LogError(pCallback.m_eResult);
            return;
        }
        Debug.Log("Successfully created lobby.");
        lobby = new CSteamID(pCallback.m_ulSteamIDLobby);
        //Then, we switch the server UI so that the player can enter the information.
        //The UI has buttons that contain references to various callbacks here.
        networkUIController.RequestHostingInfo(OnGetHostingInfo);
    }
    public void OnGetHostingInfo(GamePublishingInfo info) {
        //Set basic info
        SteamMatchmaking.SetLobbyData(lobby, "name", info.name);
        SteamMatchmaking.SetLobbyData(lobby, "pwd", info.password);

        //Now that we have the password in place, we can make it public
        SteamMatchmaking.SetLobbyType(lobby, ELobbyType.k_ELobbyTypePublic);
        
        game.EnterGame(lobby);
    }

    public void OnLobbyUpdate(LobbyChatUpdate_t pCallback) {
        if(pCallback.m_rgfChatMemberStateChange == (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) {
            Debug.Log("Player disconnected");
        }else if(pCallback.m_rgfChatMemberStateChange == (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) {
            Debug.Log("Player left");
        }
    }

    #endregion

    #region Client-oriented code

    public void JoinGame() {
        if(lobby.Equals(CSteamID.Nil) == false) {
            Debug.LogError("Trying to join a game when already connected! This won't work.");
            return;
        }
        //First, we need the UI to show the player the available lobbies.
        Debug.Log("Requesting lobbies...");
        m_GotLobbyList.Set(SteamMatchmaking.RequestLobbyList());
        
    }

    public void SearchForLobbies() {
        Debug.Log("Searching for lobbies...");
        m_GotLobbyList.Set(SteamMatchmaking.RequestLobbyList());
    }

    protected void OnGotLobbyList(LobbyMatchList_t pCallback, bool bIOfailure) {
        uint numLobbies = pCallback.m_nLobbiesMatching;
        Debug.Log(numLobbies + " lobbies found.");
        GameMatchmakingInfo[] lobbies = new GameMatchmakingInfo[numLobbies];
        for(int i = 0; i < numLobbies; i++) {
            lobbies[i].id = SteamMatchmaking.GetLobbyByIndex(i).m_SteamID;
            lobbies[i].name = SteamMatchmaking.GetLobbyData(new CSteamID(lobbies[i].id), "name");
            
            lobbies[i].callback = OnGetLobbySelection;
        }
        networkUIController.RequestLobbySelection(OnGetLobbySelection, lobbies);
    }
    

    public void OnGetLobbySelection(CSteamID selectedLobby) {
        lobby = selectedLobby;
        networkUIController.RequestPassword(OnGetPassword);

        
    }

    protected void OnGetPassword(string pwd) {
        if (SteamMatchmaking.GetLobbyData(lobby, "pwd").Equals(pwd)) {
            m_JoinedLobby.Set(SteamMatchmaking.JoinLobby(lobby));
        }
        else {
            Debug.Log("Password doesn't match!");
            networkUIController.AlertPasswordMismatch();
        }
    }

    protected void OnJoinedLobby(LobbyEnter_t pCallback, bool bIOfailure) {
        if(pCallback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess) {
            Debug.LogError("Lobby joining failed.");
            Debug.LogError(pCallback.m_EChatRoomEnterResponse);
            Reset();
            return;
        }
        else {
            RegisterWithProvider();
        }
        
    }

    

    protected void RegisterWithProvider() {
        networkUIController.SetUIMode(UIMode.Connecting);

        //Create a PlayerJoin packet, which the provider will use as a trigger to
        //register a new player. It will update its internal database, and will
        //distribute this info as a normal DatabaseUpdate.
        MeshPacket p = new MeshPacket(new byte[0],
            PacketType.PlayerJoin,
            SteamUser.GetSteamID().m_SteamID,
            SteamMatchmaking.GetLobbyOwner(lobby).m_SteamID,
            (byte)ReservedObjectIDs.Unspecified,
            (byte)ReservedObjectIDs.DatabaseObject);

        p.qos = EP2PSend.k_EP2PSendReliable;
        RoutePacketDirect(p, new CSteamID(p.GetTargetPlayerId()));
        //Soon, we will receive a DatabaseUpdate with all of the up to date database information,
        //including our own player object!

    }

    public void InitializeDatabaseClientside(MeshPacket p) {

        if(database != null) {
            Debug.LogError("Database already exists. InitializeDatabaseClientside prohibited.");
            return;
        }

        DatabaseUpdate u = DatabaseUpdate.ParseContentAsDatabaseUpdate(p.GetContents());
        //Here, we construct the database shadow using the database update.
        bool flagHasFoundDatabase = false;
        MeshNetworkIdentity databaseID = null;
        foreach(MeshNetworkIdentity i in u.objectDelta.Keys) {
            if(i.GetObjectID() == (ushort)ReservedObjectIDs.DatabaseObject) {
                flagHasFoundDatabase = true;
                database = game.SpawnDatabase(i).GetComponent<NetworkDatabase>(); //Spawns the database prefab.
                databaseID = i;
                break;
            }
        }
        if(flagHasFoundDatabase == false || database == null) {
            Debug.LogError("Database intialization failed.");
            return;
        }
        database.ReceivePacket(p);

    }

    #endregion


    protected void Reset() {
        lobby = CSteamID.Nil;
        networkUIController.SetUIMode(UIMode.Welcome);
    }
}