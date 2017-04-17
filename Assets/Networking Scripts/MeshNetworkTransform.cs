using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(Transform))]
public class MeshNetworkTransform : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {


    /*

        When NetworkTransform is locally authorized, it sends out updates containing position, velocity,
        acceleration, rotation, rotational velocity, and rotational acceleration.

    */

    public byte subcomponentID;

    //not networked
    public bool useRawRigidbodyAngularVelocity = true;
    public int rotationSampleSize = 4;
    public float standardLerpDuration = 0.08f;
    public float broadcastRate = 2;
    public float physcorrect = 8;
    public float intervalFraction = 4;
    public float nudgeRatio = 0.14f;
    Transform thisTransform;
    Rigidbody thisRigidbody;
    ZonedTransform thisZonedTransform;
    bool hasZonedTransform;
    public Rigidbody proxyRigidbody; //used for non-rigidbody objects like VR controllers and heads

    Rigidbody workingRigidbody; //references whichever rigidbody we should be using (owner only)

    MeshNetworkIdentity thisIdentity;
    bool hasRigidbody;
    public bool ShouldUseRigidbody = true;
    MeshPacket outgoingPacket = new MeshPacket();
    TransformUpdate outgoingUpdate = new TransformUpdate();

    //Shadow position variables

    Vector3D beforeUpdatePosition = new Vector3D(Vector3.zero);
    Vector3D updatedPosition = new Vector3D(Vector3.zero);
    Vector3 currentOffset = Vector3.zero;

    //Shadow velocity variables

    Vector3 beforeUpdateVelocity = Vector3.zero;
    Vector3 updatedVelocity = Vector3.zero;
    Vector3 currentVelocityOffset;

    //Shadow rotation variables

    Quaternion beforeUpdateRotation;
    Quaternion updatedRotation;
    Quaternion currentRotationOffset;

    //Shadow rotation velocity variables

    Quaternion beforeUpdateRotationalVelocity;
    Quaternion updatedRotationalVelocity;
    Quaternion currentRotationalVelocityOffset;

    float lastUpdateTime = 0;
    float lastBroadcastTime = 0;

    float lastInterval = 0;

    Vector3 adjusted;

    Vector3 tempAxisVariable;
    float tempAngleVariable;

    //Master calculation variables



    Vector3 velocityAverage;
    Vector3 lastVelocity;
    Vector3D lastPosition;
    Queue<Vector3> velocityBuffer = new Queue<Vector3>();
    Vector3[] velocityCopyBuffer;

    Quaternion rotationalVelocityAverage;
    Quaternion lastRotationalVelocity;
    Quaternion lastRotation;
    Queue<Quaternion> rotationalVelocityBuffer = new Queue<Quaternion>();
    Quaternion[] rotationalVelocityCopyBuffer;

    //networked
    Vector3D position;
    public Vector3 velocity;
    public Vector3 acceleration; //only used with kinematic bodies
    public Quaternion rotation;
    public Quaternion rotationalVelocity;

    bool isKinematic;

    public void SetSubcomponentID(byte id) {
        subcomponentID = id;
    }
    public byte GetSubcomponentID() {
        return subcomponentID;
    }
	
    void OnEnable() {
        
        rotationalVelocityBuffer = new Queue<Quaternion>(rotationSampleSize);
        for (int i = 0; i < rotationSampleSize; i++) {
            rotationalVelocityBuffer.Enqueue(Quaternion.identity);
        }
        rotationalVelocityCopyBuffer = new Quaternion[rotationSampleSize];

        if (GetComponent<Rigidbody>() != null) {
            thisRigidbody = GetComponent<Rigidbody>();
            hasRigidbody = true;
        }
        if(GetComponent<ZonedTransform>() != null) {
            thisZonedTransform = GetComponent<ZonedTransform>();
            hasZonedTransform = true;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(CompressPosition(position), 0.6f);
        Gizmos.DrawLine(CompressPosition(position), CompressPosition(position) + velocity);
        Gizmos.DrawLine(position, position + Vector3.up * 0.5f);
        
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(CompressPosition(updatedPosition), 0.2f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(CompressPosition(beforeUpdatePosition), 0.2f);
    }

    Vector3D GetPosition() { //returns accurate position whether we have a zoned transform or not
        if (hasZonedTransform) {
            
            return thisZonedTransform.GetLargeWorldPosition();
            
            
        }else {
            
            return new Vector3D(thisTransform.localPosition);
        }
    }

    Vector3 CompressPosition(Vector3D precisePosition) { //converts from "large world" coordinates to "machine" coordinates
        return precisePosition;
        if (hasZonedTransform) {
            if (thisZonedTransform.parentGrid != null)
                return precisePosition - thisZonedTransform.parentGrid.currentWorldOrigin;
            else {
                return precisePosition;
            }
        }else {
            return precisePosition;
        }
    }
    /*
    Vector3 ConvertPointToWorldCoordinates(Vector3D precisePosition) {
        Vector3 localPosition = CompressPosition(precisePosition);
        if (hasZonedTransform) {
            if(thisZonedTransform.parentGrid != null) {
                return thisZonedTransform.parentGrid.transform.localToWorldMatrix * localPosition;
            }
            else {
                return localPosition;
            }
        }
        else {
            return localPosition;
        }
    }
    */
    


	// Update is called once per frame
	void FixedUpdate () {
        if(GetIdentity() == null) {
            //return;
            //Probably not set up yet.
        }
        
		if(thisTransform == null) {
            thisTransform = GetComponent<Transform>();
        }

        if (GetIdentity().IsLocallyOwned()) { //if we are the authority
            
            

            if(ShouldUseRigidbody == false) {
                position = GetPosition(); //returns large world coordinates if we have them! :)
                rotation = thisTransform.localRotation;
                lastRotationalVelocity = rotationalVelocity;
                lastPosition = position;
                lastVelocity = velocity;
                lastRotation = rotation;
            }else {
                if (proxyRigidbody != null) {
                    hasRigidbody = true;
                    workingRigidbody = proxyRigidbody;
                    isKinematic = proxyRigidbody.isKinematic;
                } else if (thisRigidbody != null) {
                    hasRigidbody = true;
                    workingRigidbody = thisRigidbody;
                    isKinematic = thisRigidbody.isKinematic;
                } else {
                    hasRigidbody = false;
                    isKinematic = true;
                }


                if (hasRigidbody && (isKinematic == false)) {
                    position = GetPosition(); //returns large world coordinates if we have them
                    rotation = thisTransform.localRotation;
                    velocity = thisTransform.InverseTransformDirection(workingRigidbody.velocity);
                    Vector3 v = thisTransform.InverseTransformDirection(workingRigidbody.angularVelocity);
                    float angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                    rotationalVelocity = Quaternion.AngleAxis(angle, v.normalized);
                } else if (hasRigidbody) { //we have a kinematic rigidbody
                    position = GetPosition(); //returns large world coordinates using rigidbody position
                    rotation = thisTransform.localRotation;
                    /*
                    velocityBuffer.Dequeue();


                    velocityBuffer.Enqueue((workingRigidbody.position - lastPosition) / Time.fixedDeltaTime);

                    velocityBuffer.CopyTo(velocityCopyBuffer, 0);
                    velocityAverage = Vector3.zero;
                    for(byte i = 0; i < velocityCopyBuffer.Length; i++) {
                        velocityAverage += velocityCopyBuffer[i];
                    }
                    velocityAverage /= velocityCopyBuffer.Length;

                    //velocity = velocityAverage;
                    */
                    velocity = thisTransform.InverseTransformDirection(workingRigidbody.velocity);

                    lastPosition = position;
                    lastVelocity = velocity;

                    rotationalVelocityBuffer.Dequeue();
                    rotationalVelocityBuffer.Enqueue(Quaternion.Slerp(lastRotation, Quaternion.Inverse(lastRotation) * rotation, 1 / Time.fixedDeltaTime));
                    rotationalVelocityBuffer.CopyTo(rotationalVelocityCopyBuffer, 0);
                    rotationalVelocityAverage = Quaternion.identity;
                    for (byte i = 0; i < rotationalVelocityCopyBuffer.Length; i++) {
                        rotationalVelocityAverage *= rotationalVelocityCopyBuffer[i];
                    }
                    rotationalVelocity = Quaternion.Slerp(Quaternion.identity, rotationalVelocityAverage, (float)(1 / rotationalVelocityCopyBuffer.Length));

                    lastRotation = rotation;


                } else {
                    position = GetPosition();
                    rotation = thisTransform.localRotation;
                    lastRotationalVelocity = rotationalVelocity;
                    lastPosition = position;
                    lastVelocity = velocity;
                    lastRotation = rotation;
                }
            }


            
            if(Time.fixedTime - lastBroadcastTime > (float)(1 / broadcastRate)) {
                lastBroadcastTime = Time.fixedTime;
                BroadcastUpdate();
                
            }
        }
        else { //if we are the shadow (2edgy4me)
            thisRigidbody.isKinematic = isKinematic;

            float timeFraction = (Time.fixedTime - lastUpdateTime) / standardLerpDuration;
            float interleavedFraction = (Time.fixedTime - lastUpdateTime) / (lastInterval / intervalFraction);

            if (hasRigidbody && (isKinematic == false)) {
                /*
                if (useUnitySyncing) {
                    velocity = (updatedPosition - thisRigidbody.position) * (unityInterpolateMovement / lastInterval);
                    thisRigidbody.velocity = velocity;
                    thisRigidbody.MoveRotation(Quaternion.Slerp(thisRigidbody.rotation, updatedRotation, Time.fixedDeltaTime * unityInterpolateRotation));
                    updatedPosition += (updatedVelocity * Time.fixedDeltaTime * nudgeRatio);
                    return;
                }
                */
                


                //physcorrect = "offset applications per second"
                currentOffset = (updatedPosition - beforeUpdatePosition) * (physcorrect) * Time.fixedDeltaTime;
                currentVelocityOffset = (updatedVelocity - beforeUpdateVelocity) * (physcorrect) * Time.fixedDeltaTime;
                currentRotationOffset = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Inverse(beforeUpdateRotation) * updatedRotation, physcorrect * Time.fixedDeltaTime);
                currentRotationalVelocityOffset = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Inverse(beforeUpdateRotationalVelocity) * updatedRotationalVelocity, physcorrect * Time.deltaTime);
                //thus, 1/physcorrect = "amount of time it takes for a full offset"
                if (Time.fixedTime - lastUpdateTime > 1/physcorrect) {
                    currentOffset = Vector3.zero;
                    currentVelocityOffset = Vector3.zero;
                    currentRotationOffset = Quaternion.identity;
                    currentRotationalVelocityOffset = Quaternion.identity;
                }
                
                thisRigidbody.MovePosition(thisRigidbody.position + currentOffset);
                velocity = thisRigidbody.velocity + currentVelocityOffset;
                thisRigidbody.velocity = velocity;
                thisRigidbody.MoveRotation(thisRigidbody.rotation * currentRotationOffset);


                Vector3 v = thisRigidbody.angularVelocity; //convert angular velocity into a quaternion for easier math
                float angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                //Construct the current angular velocity quaternion, and add the current offset, and then convert back to angleAxis
                (Quaternion.AngleAxis(angle, v.normalized) * currentRotationalVelocityOffset).ToAngleAxis(out tempAngleVariable, out tempAxisVariable);
                thisRigidbody.angularVelocity = tempAxisVariable * tempAngleVariable * Mathf.Deg2Rad;

                position = GetPosition();
                rotation = thisRigidbody.rotation;
                v = thisRigidbody.angularVelocity;
                angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                rotationalVelocity = Quaternion.AngleAxis(angle, v.normalized);
            }
            else { //physicsless motion
                

                /*
                    velocity = (updatedPosition - thisRigidbody.position) * (unityInterpolateMovement / lastInterval);
                    //thisRigidbody.velocity = velocity;
                    thisRigidbody.MovePosition(thisRigidbody.position + velocity * Time.fixedDeltaTime);
                    thisRigidbody.MoveRotation(Quaternion.Slerp(thisRigidbody.rotation, updatedRotation, Time.fixedDeltaTime * unityInterpolateRotation));
                    updatedPosition += (updatedVelocity * Time.fixedDeltaTime * nudgeRatio);
                */
                velocity = Vector3.Lerp(beforeUpdateVelocity, updatedVelocity, TweenFunction(interleavedFraction));
                    
                updatedPosition += updatedVelocity * Time.fixedDeltaTime * 1;
                //position += (velocity * Time.fixedDeltaTime) + (updatedPosition - position) * nudgeRatio;
                position += (updatedPosition - position) * nudgeRatio;
                //position = Vector3.LerpUnclamped(beforeUpdatePosition, updatedPosition, TweenFunction(interleavedFraction));
                    
                rotationalVelocity = Quaternion.Slerp(beforeUpdateRotationalVelocity, updatedRotationalVelocity, timeFraction);
                currentRotationOffset = Quaternion.Slerp(beforeUpdateRotation, updatedRotation, timeFraction);
                rotation = currentRotationOffset * Quaternion.SlerpUnclamped(Quaternion.identity, rotationalVelocity, Time.fixedTime - lastUpdateTime);

                if (hasRigidbody) {
                    
                    thisRigidbody.MovePosition(CompressPosition(position));
                    thisRigidbody.MoveRotation(rotation);
                } else {
                    thisTransform.position = CompressPosition(position);
                    thisTransform.localRotation = rotation;
                }
            }

                


                
                    
                
            
        }
    }

    float TweenFunction(float input) {
        return input;
        return Mathf.Pow(input, 1.1f);
    }

    public MeshNetworkIdentity GetIdentity() {
        return thisIdentity;
    }
    public void SetIdentity(MeshNetworkIdentity i) {
        thisIdentity = i;
    }

    public void ReceivePacket(MeshPacket p) {
        if(p.GetPacketType() != PacketType.TransformUpdate) {
            //Reject incompatible packets.
            return;
        }
        if(p.GetSourcePlayerId() == thisIdentity.meshnetReference.GetLocalPlayerID()) {
            //Reject packets coming from ourselves.
            return;
        }
        if(p.GetSourcePlayerId() != thisIdentity.GetOwnerID()) {
            //Reject unauhorized packets.
            return;
        }

        TransformUpdate t = TransformUpdate.ParseSerializedBytes(p.GetContents());
        ProcessUpdate(t);
    }

    void ProcessUpdate(TransformUpdate t) {
        thisRigidbody.WakeUp();
        lastInterval = Mathf.Lerp(lastInterval, Time.time - lastUpdateTime, 0.5f);
        lastUpdateTime = Time.time;
        
        isKinematic = t.isKinematic;
        beforeUpdatePosition = GetPosition(); //hmm
        beforeUpdateVelocity = thisRigidbody.velocity;
        beforeUpdateRotation = thisRigidbody.rotation;
        beforeUpdateRotationalVelocity = rotationalVelocity;

        updatedPosition = t.position; //These are large world coordinates!!
        //Debug.Log("Incoming position = " + updatedPosition.x + ", " + updatedPosition.y + ", " + updatedPosition.z);
        updatedVelocity = t.velocity;
        updatedRotation = t.rotation;
        updatedRotationalVelocity = t.rotationalVelocity;

        if(hasRigidbody && isKinematic == false) {
            
            float angle;
            Vector3 axis;
            updatedRotationalVelocity.ToAngleAxis(out angle, out axis);
            thisRigidbody.angularVelocity = axis * angle * Mathf.Deg2Rad;
        }

        if (hasZonedTransform) {
            if(t.gridID != (ushort)ReservedObjectIDs.Unspecified) {
                thisZonedTransform.SetGrid(t.gridID);
            }
            
        }

    }

    void BroadcastUpdate() {
        if(GetIdentity().IsLocallyOwned() == false) {
            Debug.LogError("Not authorized to broadcast updates");
        }

        outgoingPacket.SetPacketType(PacketType.TransformUpdate);
        outgoingPacket.SetSourceObjectId(GetIdentity().GetObjectID());
        outgoingPacket.SetTargetObjectId(GetIdentity().GetObjectID());
        outgoingPacket.SetSourcePlayerId(GetIdentity().meshnetReference.GetLocalPlayerID());
        outgoingPacket.SetTargetPlayerId((ulong)ReservedPlayerIDs.Broadcast);
        outgoingPacket.SetSubcomponentID(GetSubcomponentID());

        outgoingUpdate.isKinematic = isKinematic;
        outgoingUpdate.position = position; //Again, these are large world coordinates!
        //Debug.Log("Outgoing position = " + position.x + ", " + position.y + ", " + position.z);
        outgoingUpdate.velocity = velocity;
        outgoingUpdate.rotation = rotation;
        outgoingUpdate.rotationalVelocity = rotationalVelocity;
        if (hasZonedTransform) {
            if (thisZonedTransform.parentGrid != null)
                outgoingUpdate.gridID = thisZonedTransform.parentGrid.GetGridID();
            else
                outgoingUpdate.gridID = (ushort)ReservedObjectIDs.Unspecified;
        }
        else {
            outgoingUpdate.gridID = (ushort)ReservedObjectIDs.Unspecified;
        }
        

        outgoingPacket.SetContents(outgoingUpdate.GetSerializedBytes());
        thisIdentity.RoutePacket(outgoingPacket);
    }
}
