using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Utilities;
using System.Collections.Generic;
using System;
using Steamworks;

/*
    Utilities.cs
    Copyright 2017 Finn Sinclair

    Assorted helper classes, wrapper classes, data enumerations,
    and other useful bits and pieces.

    Data serialization and deserialization routines are included
    in MeshPacket and DatabaseUpdate. More comprehensive summaries
    of those two classes can be found next to their location in the
    source.

    The large amount of code at the bottom simply simulates a full
    database update, to verify that the serialization systems are working.
*/

namespace Utilities {


    public enum ReservedObjectIDs : ushort {
        Unspecified = 0,
        DatabaseObject = 1,
        Architecture = 2,
        RootGrid = 3
    }
    public enum ReservedPlayerIDs : ulong {
        Unspecified = 0,
        Broadcast = 1
    }
    public enum ReservedPrefabIDs : ushort {
        Unspecified = 0,
        Database = 1,
        Player = 5
    }
    public enum ReservedSubcomponentIDs : byte {
        Root = 0,
        Unspecified = 1
    }

    public enum CoordinatorStatus {
        Uninitialized,
        Idle,
        CreatingGame, //setting up game
        Hosting, //Providing a game.
        Joining,  //Joining a friend's game.
        Playing,
        PlayingAsProvider
    }

    public enum UIMode {
        Welcome,
        AskForGameInfo,
        DisplayGames,
        AskForPassword,
        Connecting
    }

    public enum PacketType : byte {
        Ping = 7,
        Generic = 0,
        VOIP = 20,
        FullUpdateRequest = 9,
        DatabaseUpdate = 10,
        PlayerJoin = 11,
        DatabaseChangeRequest = 12,
        DatabaseChangeEcho = 13,
        TransformUpdate = 20,
        KickPacket = 30,
        GenericStateUpdate = 40

    }

    public enum ConnectionStatus {
        Pending, Connected, Disconnected
    }

    public enum StateChange : byte {
        Addition = 0,
        Removal = 1,
        Change = 2,
        Override = 3,
        Unspecified = 4,
        NoChange = 5
    }

    public struct Vector3D {
        public double x;
        public double y;
        public double z;


        public Vector3D(double x, double y, double z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3D(float x, float y, float z) {
            this.x = (double)x;
            this.y = (double)y;
            this.z = (double)z;
        }
        public Vector3D(Vector3 v) {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public static Vector3D operator +(Vector3D v1, Vector3D v2){
            return new Vector3D(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        public static Vector3D operator +(Vector3D v1, Vector3 v2) {
            return new Vector3D(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }
        public static Vector3D operator +(Vector3 v1, Vector3D v2) {
            return new Vector3D(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector3D operator -(Vector3D v1, Vector3D v2) {
            return new Vector3D(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public static Vector3D operator -(Vector3D v1, Vector3 v2) {
            return new Vector3D(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public static Vector3D operator -(Vector3 v1, Vector3D v2) {
            return new Vector3D(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }
        public static Vector3D operator *(Vector3D v1, float f) {
            return new Vector3D(v1.x * f, v1.y * f, v1.z * f);
        }

        public static implicit operator Vector3(Vector3D v) {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

    }

    public struct GamePublishingInfo {
        public string name;
        public string password;
    }

    public struct GameMatchmakingInfo {
        public string name;
        public ulong id;
        public Action<CSteamID> callback;
    }

    public struct IDAssignmentResult {
        public bool success;
        public ushort id;

        public IDAssignmentResult(bool s, ushort i) {
            success = s;
            id = i;
        }
    }

    public class IDContainer {
        public ushort id;

        public IDContainer(ushort i) {
            id = i;
        }
        public IDContainer() {
            id = (ushort)ReservedObjectIDs.Unspecified;
        }
    }



    public struct DatabaseChangeResult {
        public bool success;
        public string error;
        public ushort data;

        public DatabaseChangeResult(bool s, string e) {
            success = s;
            error = e;
            data = 0;
        }
        public DatabaseChangeResult(bool s, ushort data) {
            success = s;
            this.data = data;
            error = "";
        }
    }

    /*
        Utilities.Player

        Data structure for player information in the networked object model.
        Has self-contained serialization and deserialization methods, which
        use byte arrays for transmission across the network.
    */
    public class Player {
        private static readonly Encoding encoding = new ASCIIEncoding();
        private string displayName;
        private ulong uniqueID;
        private string privateKey;

        public Player() {
            displayName = "DefaultPlayerName";
            uniqueID = 0;
            privateKey = "DefaultPrivateKey";

        }

        public Player(string name,
            ulong id,
            string privateKey) {

            SetName(name);
            SetUniqueID(id);
            SetPrivateKey(privateKey);

        }

        public void DeepCopyAndApply(Player p) {
            SetName(p.GetNameDesanitized());
            SetUniqueID(p.GetUniqueID());
            SetPrivateKey(p.GetPrivateKey());
        }

        public void SetName(string n) {
            displayName = n.Replace(":", "$COLON").Substring(0, Mathf.Min(20, n.Length));
        }
        public string GetNameSanitized() {
            return displayName;
        }
        public string GetNameDesanitized() {
            return displayName.Replace("$COLON", ":");
        }

        public void SetUniqueID(ulong id) {
            uniqueID = id;
        }
        public ulong GetUniqueID() {
            return uniqueID;
        }


        public void SetPrivateKey(string k) {
            privateKey = k;
        }
        public string GetPrivateKey() {
            return privateKey;
        }

        public byte[] SerializeFull() {
            string s = String.Join(":", new string[] { GetNameSanitized(), uniqueID.ToString(), privateKey });
            
            return encoding.GetBytes(s);
        }

        public static Player DeserializeFull(byte[] bytes) {
            string s = encoding.GetString(bytes);
            string[] parts = s.Split(':');
            
            return new Player(parts[0], ulong.Parse(parts[1]), parts[2]);
        }

        

    }

    /*
        Utilities.MeshPacket

        Data structure which contains raw byte data along with metadata
        concerning the source, destination, and type of contents. This is
        the main currency of the distributed network object model. Each
        packet is designed to be self-sufficient, meaning that each packet
        is unique, identifiable, self-representative, and serializable (both ways).
    */

    public class MeshPacket {

        public static readonly List<PacketType> RELIABLE_TYPES = new List<PacketType>(new PacketType[] {
            PacketType.DatabaseUpdate,
            PacketType.FullUpdateRequest,
            PacketType.PlayerJoin,
            PacketType.DatabaseChangeEcho,
            PacketType.DatabaseChangeRequest,
            PacketType.KickPacket,
            PacketType.GenericStateUpdate });
        public static readonly List<PacketType> NO_DELAY_TYPES = new List<PacketType>(new PacketType[]{
            PacketType.VOIP,
            PacketType.TransformUpdate });


        private byte[] contents;
        private PacketType type;
        private ulong srcPlayerId;
        private ulong targetPlayerId;
        private ushort srcObjectId;
        private ushort targetObjectId;
        private byte subcomponentID;
        public EP2PSend qos;


        public MeshPacket() { //if no data supplied, generate empty packet with generic typebyte
            contents = new byte[1];
            type = PacketType.Generic;
            srcPlayerId = 0;
            targetPlayerId = 0;
            srcObjectId = 0;
            targetObjectId = 0;
        }
        public MeshPacket(byte[] serializedData) { //Deserialize packet.
            int pointer = 0;
            type = (PacketType)serializedData[pointer];
            pointer++;
            subcomponentID = serializedData[pointer];
            pointer++;
            srcPlayerId = BitConverter.ToUInt64(serializedData, pointer);
            pointer += 8;
            targetPlayerId = BitConverter.ToUInt64(serializedData, pointer);
            pointer += 8;
            srcObjectId = BitConverter.ToUInt16(serializedData, pointer);
            pointer += 2;
            targetObjectId = BitConverter.ToUInt16(serializedData, pointer);
            pointer += 2;
            contents = new byte[serializedData.Length - pointer];
            Buffer.BlockCopy(serializedData, pointer, contents, 0, contents.Length);
        }
        public MeshPacket(byte[] contents, PacketType type, ulong srcPlayer, ulong targetPlayer, ushort srcObject, ushort targetObject, byte subComponentID) {
            this.contents = contents;
            this.type = type;
            srcPlayerId = srcPlayer;
            targetPlayerId = targetPlayer;
            srcObjectId = srcObject;
            targetObjectId = targetObject;
        }
        public byte[] GetContents() {
            return contents;
        }

        public void SetPacketType(PacketType p) {
            type = p;
            if(RELIABLE_TYPES.Contains(p)){
                qos = EP2PSend.k_EP2PSendReliable;
            }else if (NO_DELAY_TYPES.Contains(p)) {
                qos = EP2PSend.k_EP2PSendUnreliableNoDelay;
            }
            else {
                qos = EP2PSend.k_EP2PSendUnreliable;
            }
        }
        public PacketType GetPacketType() {
            return type;
        }
        public void SetContents(byte[] contents) {
            this.contents = contents;
        }

        public byte GetSubcomponentID() {
            return subcomponentID;
        }
        public void SetSubcomponentID(byte id) {
            subcomponentID = id;
        }
        public ulong GetSourcePlayerId() {
            return srcPlayerId;
        }
        public ulong GetTargetPlayerId() {
            return targetPlayerId;
        }
        public void SetSourcePlayerId(ulong i) {
            srcPlayerId = i;
        }
        public void SetTargetPlayerId(ulong i) {
            targetPlayerId = i;
        }

        public ushort GetSourceObjectId() {
            return srcObjectId;
        }
        public ushort GetTargetObjectId() {
            return targetObjectId;
        }
        public void SetSourceObjectId(ushort i) {
            srcObjectId = i;
        }
        public void SetTargetObjectId(ushort i) {
            targetObjectId = i;
        }

        public byte[] GetSerializedBytes() {
            int pointer = 0;
            byte[] output = new byte[22 + contents.Length];
            output[pointer] = (byte)type;
            pointer++;
            output[pointer] = subcomponentID;
            pointer++;
            Buffer.BlockCopy(BitConverter.GetBytes(srcPlayerId), 0, output, pointer, 8);
            pointer += 8;
            Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, output, pointer, 8);
            pointer += 8;
            Buffer.BlockCopy(BitConverter.GetBytes(srcObjectId), 0, output, pointer, 2);
            pointer += 2;
            Buffer.BlockCopy(BitConverter.GetBytes(targetObjectId), 0, output, pointer, 2);
            pointer += 2;
            Buffer.BlockCopy(contents, 0, output, pointer, contents.Length);

            return output;
        }



    }


    /*
        Utilities.DatabaseUpdate

        Delta updates that drive the distributed network databases. Only the data
        that is modified is sent. This usually happens one change at a time. However,
        in some cases, multiple changes can be sent in the same DatabaseUpdate. (This
        usually occurs when a mass quantity of game state information needs to be sent.)

        The usual sequence of events is as follows:

        - Master database executes update
        - Master database creates checksum of entire database
        - Master database compiles DatabaseUpdate containing the necessary
            (usually just one) change
        - Master database broadcasts DatabaseUpdate to all peers
        - Each peer applies the update, generates a checksum of their own local database
        - If checksums don't match, the peer requests a full (non-delta) update from the master
    */


    public class DatabaseUpdate : IMeshSerializable{

        //These dictionaries are treated as deltas (why send the entire database?)
        public Dictionary<Player, StateChange> playerDelta = new Dictionary<Player, StateChange>();
        public Dictionary<MeshNetworkIdentity, StateChange> objectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
        public ushort fullHash;
        public bool isFullUpdate;

        public DatabaseUpdate() {
            playerDelta = new Dictionary<Player, StateChange>();
            objectDelta = new Dictionary<MeshNetworkIdentity, StateChange>();
            fullHash = 0;
            isFullUpdate = false;
        }

        public DatabaseUpdate(Dictionary<Player, StateChange> players,
            Dictionary<MeshNetworkIdentity, StateChange> objects,
            ushort databaseHash, bool fullUpdate) {

            playerDelta = players;
            objectDelta = objects;
            fullHash = databaseHash;
            isFullUpdate = fullUpdate;
        }


        public void DeserializeAndApply(byte[] serializedData) {
            DatabaseUpdate decoded = DatabaseUpdate.ParseContentAsDatabaseUpdate(serializedData);
            this.playerDelta = decoded.playerDelta;
            this.objectDelta = decoded.objectDelta;
            this.fullHash = decoded.fullHash;
            this.isFullUpdate = decoded.isFullUpdate;
        }


        //Serialize the update into a bytestream, recursively serializing all
        //contained objects and players.
        public byte[] GetSerializedBytes() {
            if (isFullUpdate) {
                //Debug.Log("!!!! Just serialized a full update!");

            }

            
            List<byte> output = new List<byte>(100);

            byte numPlayers = (byte)playerDelta.Keys.Count;
            output.Add(numPlayers);
            
            foreach (KeyValuePair<Player, StateChange> entry in playerDelta) {
                
                byte[] serializedPlayer = entry.Key.SerializeFull();
                output.Add((byte)serializedPlayer.Length);
                output.AddRange(serializedPlayer);
                output.Add((byte)entry.Value);
                
            }
            
            byte numObjects = (byte)objectDelta.Keys.Count;
            output.Add(numObjects);
            
            foreach (KeyValuePair<MeshNetworkIdentity, StateChange> entry in objectDelta) {
                
                byte[] serializedObject = entry.Key.GetSerializedBytes();
                

                output.AddRange(serializedObject);
                output.Add((byte)entry.Value);
            }
            
            output.AddRange(BitConverter.GetBytes(fullHash));
            output.AddRange(BitConverter.GetBytes(isFullUpdate));
            output.TrimExcess();
            
            
            return output.ToArray();
        }

        //Deserialize incoming byte data and construct a deserialized DatabaseUpdate
        //Note, this is static
        public static DatabaseUpdate ParseContentAsDatabaseUpdate(byte[] serializedData) {
            
            Dictionary<Player, StateChange> playerList = new Dictionary<Player, StateChange>();
            Dictionary<MeshNetworkIdentity, StateChange> networkObjects = new Dictionary<MeshNetworkIdentity, StateChange>();
            
            byte[] rawData = serializedData;
            byte numOfNewPlayers = rawData[0];
            int pointer = 1;
            byte i = 0;
            while (i < numOfNewPlayers) {
                int blobLength = rawData[pointer];

                pointer++; //pointer is now at the beginning of the player data blob

                byte[] playerData = new byte[blobLength];
                Buffer.BlockCopy(rawData, pointer, playerData, 0, blobLength);
                Player p = Player.DeserializeFull(playerData);
                pointer += blobLength;
                StateChange s = (StateChange)rawData[pointer];
                playerList.Add(p, s);
                pointer++;
                i++;
            }
            byte numOfObjects = rawData[pointer];
            pointer++; //pointer now at the beginning of the first MeshNetworkIdentity data
            byte j = 0;
            while (j < numOfObjects) {
                MeshNetworkIdentity netid = new MeshNetworkIdentity();
                byte[] trimmed = new byte[MeshNetworkIdentity.NETWORK_IDENTITY_BYTE_SIZE];
                Buffer.BlockCopy(rawData, pointer, trimmed, 0, trimmed.Length);
                netid.DeserializeAndApply(trimmed);
                pointer += MeshNetworkIdentity.NETWORK_IDENTITY_BYTE_SIZE;
                StateChange s = (StateChange)rawData[pointer];
                networkObjects.Add(netid, s);
                pointer++;
                j++;
            }
            ushort hash = BitConverter.ToUInt16(rawData, pointer);
            pointer += 2;
            bool fullUpdateFlag = BitConverter.ToBoolean(rawData, pointer);
            
            
            return new DatabaseUpdate(playerList, networkObjects, hash, fullUpdateFlag);
        }


    }

    public class StateChangeTransaction : IMeshSerializable{
        ushort transactionID;
        StateChange changeType;
        MeshNetworkIdentity obj;

        public ushort GetTransactionID() {
            return transactionID;
        }
        public void SetTransactionID(ushort input) {
            transactionID = input;
        }
        public StateChange GetChangeType() {
            return changeType;
        }
        public void SetChangeType(StateChange c) {
            changeType = c;
        }
        public MeshNetworkIdentity GetObjectData() {
            return obj;
        }
        public void SetObjectData(MeshNetworkIdentity i) {
            obj = i;
        }

        public StateChangeTransaction() {
            transactionID = 0;
            changeType = StateChange.Unspecified;
            obj = null;
        }
        public StateChangeTransaction(ushort id, StateChange type, MeshNetworkIdentity i) {
            transactionID = id;
            changeType = type;
            obj = i;
        }

        public byte[] GetSerializedBytes() {
            List<byte> output = new List<byte>();
            output.AddRange(BitConverter.GetBytes(transactionID));
            output.Add((byte)changeType);
            if(obj == null) {
                output.Add(0);
            }
            else {
                output.Add(1);
                output.AddRange(obj.GetSerializedBytes());
            }
            return output.ToArray();
        }
        
        public static StateChangeTransaction ParseSerializedBytes(byte[] data) {
            StateChangeTransaction transaction = new StateChangeTransaction();
            transaction.SetTransactionID(BitConverter.ToUInt16(data, 0));
            transaction.SetChangeType((StateChange)data[2]);
            
            if(data[3] == 1) {
                byte[] dataEndCap = new byte[data.Length - 4];
                Buffer.BlockCopy(data, 4, dataEndCap, 0, dataEndCap.Length); //hmmm
                MeshNetworkIdentity i = new MeshNetworkIdentity();
                i.DeserializeAndApply(dataEndCap);
                transaction.SetObjectData(i);
            }
            return transaction;
        }
    }

    public class StateChangeEcho : IMeshSerializable {
        ushort transactionID;
        ushort objectID;

        public StateChangeEcho() {
            transactionID = 0;
            objectID = 0;
        }
        public StateChangeEcho(ushort transactionID, ushort objectID) {
            this.transactionID = transactionID;
            this.objectID = objectID;
        }
        public ushort GetTransactionID() {
            return transactionID;
        }
        public void SetTransactionID(ushort id) {
            transactionID = id;
        }
        public ushort GetObjectID() {
            return objectID;
        }
        public void SetObjectID(ushort id) {
            objectID = id;
        }

        public byte[] GetSerializedBytes() {
            List<byte> output = new List<byte>();
            output.AddRange(BitConverter.GetBytes(transactionID));
            output.AddRange(BitConverter.GetBytes(objectID));
            return output.ToArray();
        }

        public static StateChangeEcho ParseSerializedBytes(byte[] data) {
            StateChangeEcho echo = new StateChangeEcho();
            echo.transactionID = BitConverter.ToUInt16(data, 0);
            echo.objectID = BitConverter.ToUInt16(data, 2);
            return echo;
        }
    }

    public class TransformUpdate : IMeshSerializable {
        public Vector3D position;
        public Vector3 velocity;
        public Quaternion rotation;
        public Quaternion rotationalVelocity;
        public bool isKinematic;
        public ushort gridID;

        public byte[] GetSerializedBytes() {
            byte[] output = new byte[3*3*4 + 2*4*4 + 1 + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, output, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, output, 8, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, output, 16, 8);


            Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, output, 24, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, output, 28, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, output, 32, 4);
            
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, output, 36, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, output, 40, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, output, 44, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, output, 48, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(rotationalVelocity.w), 0, output, 52, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotationalVelocity.x), 0, output, 56, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotationalVelocity.y), 0, output, 60, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(rotationalVelocity.z), 0, output, 64, 4);

            if (isKinematic) {
                output[68] = 1;
            }
            else {
                output[68] = 0;
            }
            Buffer.BlockCopy(BitConverter.GetBytes(gridID), 0, output, 69, 2);
            return output;

        }

        
        public static TransformUpdate ParseSerializedBytes(byte[] data) {
            TransformUpdate t = new TransformUpdate();
            t.position.x = BitConverter.ToDouble(data, 0);
            t.position.y = BitConverter.ToDouble(data, 8);
            t.position.z = BitConverter.ToDouble(data, 16);

            t.velocity.x = BitConverter.ToSingle(data, 24);
            t.velocity.y = BitConverter.ToSingle(data, 28);
            t.velocity.z = BitConverter.ToSingle(data, 32);
            
            t.rotation.w = BitConverter.ToSingle(data, 36);
            t.rotation.x = BitConverter.ToSingle(data, 40);
            t.rotation.y = BitConverter.ToSingle(data, 44);
            t.rotation.z = BitConverter.ToSingle(data, 48);

            t.rotationalVelocity.w = BitConverter.ToSingle(data, 52);
            t.rotationalVelocity.x = BitConverter.ToSingle(data, 56);
            t.rotationalVelocity.y = BitConverter.ToSingle(data, 60);
            t.rotationalVelocity.z = BitConverter.ToSingle(data, 64);

            t.isKinematic = (data[68] == 1);
            t.gridID = BitConverter.ToUInt16(data, 69);
            return t;
        }
    }

    public class PropUpdate : IMeshSerializable {
        
        public bool isUsed;

        public PropUpdate(bool used) {
            isUsed = used;
        }
        public PropUpdate() {
            isUsed = false;
        }

        public byte[] GetSerializedBytes() {
            byte[] output = new byte[1];
            if (isUsed)
                output[0] = 1;
            else
                output[0] = 0;
            return output;

        }


        public static PropUpdate ParseSerializedBytes(byte[] data) {
            PropUpdate p = new PropUpdate();
            p.isUsed = (data[0] == 1);
            return p;
        }
    }
    //All networked components must implement this.
    public interface IReceivesPacket<MeshPacket> {
        void ReceivePacket(MeshPacket p);
        void SetSubcomponentID(byte id);
        byte GetSubcomponentID();
    }

    public interface IMeshSerializable {
        byte[] GetSerializedBytes();
    }
    public interface INetworked<MeshNetworkIdentity> {
        MeshNetworkIdentity GetIdentity();
        void SetIdentity(MeshNetworkIdentity id);
    }

    public class Testing {

        //Runs some checks to make sure that the serialization
        //systems are running and correctly translating the data.
        //TODO automate checking
        public static void DebugDatabaseSerialization() {
            Debug.Log("Creating player named Mary Jane.");
            Player p1 = new Player("Mary Janee", 2233443, "abcde");
            Debug.Log("Creating player named John Smith");
            Player p2 = new Player("John Smith", 52342342, "12345");

            DatabaseUpdate db = new DatabaseUpdate();
            db.playerDelta.Add(p1, StateChange.Addition);
            db.playerDelta.Add(p2, StateChange.Removal);
            db.isFullUpdate = true;

            MeshNetworkIdentity dummy1 = new MeshNetworkIdentity();
            MeshNetworkIdentity dummy2 = new MeshNetworkIdentity();
            dummy1.SetObjectID(1337);
            dummy1.SetOwnerID(1234);
            dummy1.SetLocked(true);
            dummy2.SetObjectID(4200);
            dummy2.SetOwnerID(4321);
            dummy2.SetLocked(false);

            db.objectDelta.Add(dummy1, StateChange.Change);
            db.objectDelta.Add(dummy2, StateChange.Addition);
            db.fullHash = 4321;

            Debug.Log("Total payload length: " + db.GetSerializedBytes().Length);
            //Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerDelta, db.objectDelta));
            MeshPacket p = new MeshPacket();
            p.SetPacketType(PacketType.DatabaseUpdate);
            p.SetSourceObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetSourcePlayerId(120);
            p.SetTargetObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);
            p.SetContents(db.GetSerializedBytes());
            
            byte[] transmitData = p.GetSerializedBytes();






            //THIS WOULD GET SENT ACROSS THE NETWORK

            MeshPacket received = new MeshPacket(transmitData);
            Debug.Log("Received packet:");
            Debug.Log("packetType: " + received.GetPacketType());
            Debug.Log("sourceObjectID: " + received.GetSourceObjectId());
            Debug.Log("sourcePlayerID: " + received.GetSourcePlayerId());
            Debug.Log("targetObjectID: " + received.GetTargetObjectId());
            Debug.Log("targetPlayerID: " + received.GetTargetPlayerId());
            Debug.Log("Payload length: " + received.GetContents().Length);
            

            DatabaseUpdate receivedDB = DatabaseUpdate.ParseContentAsDatabaseUpdate(received.GetContents());
            Debug.Log("Received DatabaseUpdate: isfullupdate = " + receivedDB.isFullUpdate);
            Debug.Log("Hash = " + receivedDB.fullHash);
            //Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerDelta, db.objectDelta));
            Debug.Log("Total number of objects: " + receivedDB.objectDelta.Count);
            int i = 1;
            foreach(MeshNetworkIdentity id in receivedDB.objectDelta.Keys) {
                Debug.Log("Object " + i + ": ");
                Debug.Log("objectID: " + id.GetObjectID());
                Debug.Log("prefabID: " + id.GetPrefabID());
                Debug.Log("ownerID : " + id.GetOwnerID());
                Debug.Log("Locked: " + id.GetLocked());
                i++;
            }
            Debug.Log("Total number of players: " + receivedDB.playerDelta.Count);
            i = 1;
            foreach (Player player in receivedDB.playerDelta.Keys) {
                Debug.Log("Player " + i + ": ");
                Debug.Log("Desanitized Name: " + player.GetNameDesanitized());
                Debug.Log("Sanitized Name: " + player.GetNameSanitized());
                Debug.Log("uniqueID: " + player.GetUniqueID());
                Debug.Log("privateKey: " + player.GetPrivateKey());
                i++;
            }
        }

        public static void DebugPackets() {
            MeshPacket p = new MeshPacket();
            p.SetSourceObjectId(5);
            p.SetTargetObjectId(10);
            p.SetSourcePlayerId(1234);
            p.SetTargetPlayerId(4321);
            p.SetSubcomponentID(2);
            p.SetPacketType(PacketType.DatabaseUpdate);
            p.SetContents(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 });
            byte[] buffer = p.GetSerializedBytes();
            MeshPacket newPacket = new MeshPacket(buffer);
            Debug.Log(newPacket.GetTargetObjectId());
            Debug.Log(newPacket.GetPacketType());
        }

        public static void ProfileSerialization() {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            
            
            DatabaseUpdate db = new DatabaseUpdate();
            for(int i = 0; i < 1; i++) {
                db.playerDelta.Add(new Player("PLAYERNAME", (ulong)i, "key"), StateChange.Addition);
            }
            db.isFullUpdate = true;

           
            for(int i = 0; i < 1; i++) {
                db.objectDelta.Add(new MeshNetworkIdentity((ushort)i, 20, 40, true), StateChange.Addition);
            }
            
            //Debug.Log("Database hash: " + NetworkDatabase.GenerateDatabaseChecksum(db.playerDelta, db.objectDelta));
            MeshPacket p = new MeshPacket();
            p.SetPacketType(PacketType.DatabaseUpdate);
            p.SetSourceObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetSourcePlayerId(120);
            p.SetTargetObjectId((byte)ReservedObjectIDs.DatabaseObject);
            p.SetTargetPlayerId((byte)ReservedPlayerIDs.Broadcast);

            stopwatch.Start();
            p.SetContents(db.GetSerializedBytes());
            stopwatch.Stop();
            byte[] transmitData = p.GetSerializedBytes();


            //THIS WOULD GET SENT ACROSS THE NETWORK
            
            MeshPacket received = new MeshPacket(transmitData);
            
            DatabaseUpdate receivedDB = DatabaseUpdate.ParseContentAsDatabaseUpdate(received.GetContents());
            Debug.logger.logEnabled = true;
            Debug.Log("Testing: " + stopwatch.Elapsed.TotalMilliseconds);
        }

        public static void BitTesting() {
            Debug.Log("Making int of 50");
            int a = 250;
            byte fromCasting = (byte)a;
            Debug.Log("Casted: " + fromCasting);
        }

        public static void TransactionTesting() {
            StateChangeTransaction t = new StateChangeTransaction(1234, StateChange.Addition, new MeshNetworkIdentity(101, 2, 1234, true));
            byte[] bytes = t.GetSerializedBytes();
            MeshPacket p = new MeshPacket(bytes, PacketType.DatabaseChangeRequest, 4325, 911, 45, 45, (byte)ReservedSubcomponentIDs.Unspecified);
            byte[] bytesToSend = p.GetSerializedBytes();

            //SENDDDD

            MeshPacket destPacket = new MeshPacket(bytesToSend);
            Debug.Log("Source player: " + destPacket.GetSourcePlayerId());
            Debug.Log("Source object: " + destPacket.GetSourceObjectId());
            Debug.Log("Target player: " + destPacket.GetTargetPlayerId());
            Debug.Log("Target object: " + destPacket.GetTargetObjectId());
            Debug.Log("Packet type: " + destPacket.GetPacketType());
            Debug.Log("<<<<<<  Transaction:  >>>>>>");
            StateChangeTransaction destTransaction = StateChangeTransaction.ParseSerializedBytes(destPacket.GetContents());
            Debug.Log("Change type: " + destTransaction.GetChangeType());
            Debug.Log("Transaction id: " + destTransaction.GetTransactionID());
            Debug.Log("Object id: " + destTransaction.GetObjectData().GetObjectID());
            Debug.Log("Owner id: " + destTransaction.GetObjectData().GetOwnerID());

        }

        
    }


}

