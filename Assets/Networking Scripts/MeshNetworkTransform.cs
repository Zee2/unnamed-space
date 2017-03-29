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
    public Rigidbody proxyRigidbody; //used for non-rigidbody objects like VR controllers and heads

    Rigidbody workingRigidbody; //references whichever rigidbody we should be using (owner only)

    MeshNetworkIdentity thisIdentity;
    public bool hasRigidbody;

    MeshPacket outgoingPacket = new MeshPacket();
    TransformUpdate outgoingUpdate = new TransformUpdate();

    //Shadow position variables

    Vector3 beforeUpdatePosition = Vector3.zero;
    Vector3 updatedPosition = Vector3.zero;
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
    Vector3 lastPosition;
    Queue<Vector3> velocityBuffer = new Queue<Vector3>();
    Vector3[] velocityCopyBuffer;

    Quaternion rotationalVelocityAverage;
    Quaternion lastRotationalVelocity;
    Quaternion lastRotation;
    Queue<Quaternion> rotationalVelocityBuffer = new Queue<Quaternion>();
    Quaternion[] rotationalVelocityCopyBuffer;

    //networked
    Vector3 position;
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
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + velocity);
        Gizmos.DrawLine(position, position + Vector3.up * 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + acceleration);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(updatedPosition, 0.2f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(beforeUpdatePosition, 0.2f);
    }

	// Update is called once per frame
	void FixedUpdate () {
        if(GetIdentity() == null) {
            //return;
            //Probably not set up yet.
        }
        
		if(thisTransform == null) {
            thisTransform = GetComponent<Transform>();
        }

        if (true || GetIdentity().IsLocallyOwned()) { //if we are the authority

            if(proxyRigidbody != null) {
                hasRigidbody = true;
                workingRigidbody = proxyRigidbody;
                isKinematic = proxyRigidbody.isKinematic;
            }else if(thisRigidbody != null) {
                hasRigidbody = true;
                workingRigidbody = thisRigidbody;
                isKinematic = thisRigidbody.isKinematic;
            }else {
                hasRigidbody = false;
                isKinematic = true;
            }
            
            
            if (hasRigidbody && isKinematic == false) {
                position = thisTransform.localPosition; //this may need changing to work with zoning
                rotation = thisTransform.localRotation;
                velocity = workingRigidbody.velocity;
                Vector3 v = workingRigidbody.angularVelocity;
                float angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                rotationalVelocity = Quaternion.AngleAxis(angle, v.normalized);
            }
            else if(hasRigidbody) {
                position = workingRigidbody.position; //this may need changing to work with zoning
                rotation = workingRigidbody.rotation;
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
                velocity = workingRigidbody.velocity;
                
                lastPosition = position;
                lastVelocity = velocity;
                
                rotationalVelocityBuffer.Dequeue();
                rotationalVelocityBuffer.Enqueue(Quaternion.Slerp(lastRotation, Quaternion.Inverse(lastRotation)*rotation, 1/Time.fixedDeltaTime));
                rotationalVelocityBuffer.CopyTo(rotationalVelocityCopyBuffer, 0);
                rotationalVelocityAverage = Quaternion.identity;
                for (byte i = 0; i < rotationalVelocityCopyBuffer.Length; i++) {
                    rotationalVelocityAverage *= rotationalVelocityCopyBuffer[i];
                }
                rotationalVelocity = Quaternion.Slerp(Quaternion.identity, rotationalVelocityAverage, (float)(1 / rotationalVelocityCopyBuffer.Length));
                
                lastRotation = rotation;


            }else {
                position = thisTransform.localPosition;
                rotation = thisTransform.localRotation;
                lastRotationalVelocity = rotationalVelocity;
                lastPosition = position;
                lastVelocity = velocity;
                lastRotation = rotation;
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

            if (hasRigidbody && isKinematic == false) { //use physics
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
                
                position = thisRigidbody.position;
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
                    thisRigidbody.MovePosition(position);
                    thisRigidbody.MoveRotation(rotation);
                } else {
                    thisTransform.position = position;
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
        beforeUpdatePosition = thisRigidbody.position; //hmm
        beforeUpdateVelocity = thisRigidbody.velocity;
        beforeUpdateRotation = thisRigidbody.rotation;
        beforeUpdateRotationalVelocity = rotationalVelocity;

        updatedPosition = t.position;
        updatedVelocity = t.velocity;
        updatedRotation = t.rotation;
        updatedRotationalVelocity = t.rotationalVelocity;

        if(hasRigidbody && isKinematic == false) {
            
            float angle;
            Vector3 axis;
            updatedRotationalVelocity.ToAngleAxis(out angle, out axis);
            thisRigidbody.angularVelocity = axis * angle * Mathf.Deg2Rad;
        }

    }

    void BroadcastUpdate() {
        return;
        if(GetIdentity().IsLocallyOwned() == false) {
            Debug.LogError("Not authorized to broadcast updates");
        }

        outgoingPacket.SetPacketType(PacketType.TransformUpdate);
        outgoingPacket.SetSourceObjectId(GetIdentity().GetObjectID());
        outgoingPacket.SetTargetObjectId(GetIdentity().GetObjectID());
        outgoingPacket.SetSourcePlayerId(GetIdentity().meshnetReference.GetLocalPlayerID());
        outgoingPacket.SetTargetPlayerId((ulong)ReservedPlayerIDs.Broadcast);

        outgoingUpdate.isKinematic = isKinematic;
        outgoingUpdate.position = position;
        outgoingUpdate.velocity = velocity;
        outgoingUpdate.acceleration = acceleration;
        outgoingUpdate.rotation = rotation;
        outgoingUpdate.rotationalVelocity = rotationalVelocity;

        outgoingPacket.SetContents(outgoingUpdate.GetSerializedBytes());
        thisIdentity.RoutePacket(outgoingPacket);
    }
}
