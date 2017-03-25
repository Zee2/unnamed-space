using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(IdentityContainer))]
public class MeshNetworkTransform : MonoBehaviour, IReceivesPacket<MeshPacket>, INetworked<MeshNetworkIdentity> {


    /*

        When NetworkTransform is locally authorized, it sends out updates containing position, velocity,
        acceleration, rotation, rotational velocity, and rotational acceleration.

    */

    //not networked
    public int NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE = 4;
    public int NON_RIGIDBODY_ACCELERATION_SAMPLE_SIZE = 4;
    public int INTERP_DELAY_MILLISECONDS = 50;
    public float BROADCAST_RATE = 2;
    public float physcorrect = 20;
    public float intervalFraction = 4;
    Transform thisTransform;
    Rigidbody thisRigidbody;
    MeshNetworkIdentity thisIdentity;
    bool hasRigidbody;

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

        Vector3 accelerationAverage;
        Vector3 lastAcceleration;
        Queue<Vector3> accelerationBuffer = new Queue<Vector3>();
        Vector3[] accelerationCopyBuffer;

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
    Quaternion rotation;
    Quaternion rotationalVelocity;

    bool isKinematic;
	
    void OnEnable() {
        velocityBuffer = new Queue<Vector3>(NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE);
        for (int i = 0; i < NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE; i++) {
            velocityBuffer.Enqueue(Vector3.zero);
        }
        velocityCopyBuffer = new Vector3[NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE];
        accelerationBuffer = new Queue<Vector3>(NON_RIGIDBODY_ACCELERATION_SAMPLE_SIZE);
        for (int i = 0; i < NON_RIGIDBODY_ACCELERATION_SAMPLE_SIZE; i++) {
            accelerationBuffer.Enqueue(Vector3.zero);
        }
        accelerationCopyBuffer = new Vector3[NON_RIGIDBODY_ACCELERATION_SAMPLE_SIZE];

        rotationalVelocityBuffer = new Queue<Quaternion>(NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE);
        for (int i = 0; i < NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE; i++) {
            rotationalVelocityBuffer.Enqueue(Quaternion.identity);
        }
        rotationalVelocityCopyBuffer = new Quaternion[NON_RIGIDBODY_VELOCITY_SAMPLE_SIZE];

        if (GetComponent<Rigidbody>() == null) {
            Debug.Log("Enabling non-physics network transform");
            hasRigidbody = false;
        }
        else {
            thisRigidbody = GetComponent<Rigidbody>();
            hasRigidbody = true;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + velocity);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + acceleration);
    }

	// Update is called once per frame
	void Update () {
        if(GetIdentity() == null) {
            return;
            //Probably not set up yet.
        }
        
		if(thisTransform == null) {
            thisTransform = GetComponent<Transform>();
        }

        if (GetIdentity().IsLocallyOwned()) { //if we are the authority
            if (hasRigidbody) {
                isKinematic = thisRigidbody.isKinematic;

            }
            else {
                isKinematic = false;
            }

            position = thisTransform.localPosition; //this may need changing to work with zoning
            rotation = thisTransform.localRotation;
            /*
            if (hasRigidbody && isKinematic == false) {
                
                velocity = thisRigidbody.velocity;
                Vector3 v = thisRigidbody.angularVelocity;
                float angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                rotationalVelocity = Quaternion.AngleAxis(angle, v.normalized);
            }
            else {
            */
            if (true) {
                
                velocityBuffer.Dequeue();

                
                velocityBuffer.Enqueue((thisTransform.localPosition - lastPosition) / Time.deltaTime);

                velocityBuffer.CopyTo(velocityCopyBuffer, 0);
                velocityAverage = Vector3.zero;
                for(byte i = 0; i < velocityCopyBuffer.Length; i++) {
                    velocityAverage += velocityCopyBuffer[i];
                }
                velocityAverage /= velocityCopyBuffer.Length;
                velocity = velocityAverage;
                
                
                
                accelerationBuffer.Dequeue();
                accelerationBuffer.Enqueue((((thisTransform.localPosition - lastPosition) / Time.deltaTime) - lastVelocity) / Time.deltaTime);
                accelerationBuffer.CopyTo(accelerationCopyBuffer, 0);
                accelerationAverage = Vector3.zero;
                for (byte i = 0; i < accelerationCopyBuffer.Length; i++) {
                    accelerationAverage += accelerationCopyBuffer[i];
                }
                accelerationAverage /= accelerationCopyBuffer.Length;
                //acceleration = accelerationAverage;
                
                lastPosition = position;
                lastVelocity = velocity;
                
                rotationalVelocityBuffer.Dequeue();
                rotationalVelocityBuffer.Enqueue(Quaternion.Slerp(lastRotation, Quaternion.Inverse(lastRotation)*rotation, 1/Time.deltaTime));
                rotationalVelocityBuffer.CopyTo(rotationalVelocityCopyBuffer, 0);
                rotationalVelocityAverage = Quaternion.identity;
                for (byte i = 0; i < rotationalVelocityCopyBuffer.Length; i++) {
                    rotationalVelocityAverage *= rotationalVelocityCopyBuffer[i];
                }
                rotationalVelocity = Quaternion.Slerp(Quaternion.identity, rotationalVelocityAverage, (float)(1 / rotationalVelocityCopyBuffer.Length));
                
                lastRotation = rotation;
                

            }
            if(Time.time - lastBroadcastTime > (float)(1 / BROADCAST_RATE)) {
                lastBroadcastTime = Time.time;
                BroadcastUpdate();
                
            }
        }
        else { //if we are the shadow (2edgy4me)

            thisRigidbody.isKinematic = isKinematic;

            float timeFraction = ((Time.time - lastUpdateTime) * 1000) / INTERP_DELAY_MILLISECONDS;
            float interleavedFraction = (Time.time - lastUpdateTime) / (lastInterval / intervalFraction);

            if (hasRigidbody && isKinematic == false) { //use physics
                //physcorrect = "offset applications per second"
                currentOffset = (updatedPosition - beforeUpdatePosition) * (physcorrect) * Time.deltaTime;
                currentVelocityOffset = (updatedVelocity - beforeUpdateVelocity) * (physcorrect) * Time.deltaTime;
                currentRotationOffset = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Inverse(beforeUpdateRotation) * updatedRotation, physcorrect * Time.deltaTime);
                currentRotationalVelocityOffset = Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Inverse(beforeUpdateRotationalVelocity) * updatedRotationalVelocity, physcorrect * Time.deltaTime);
                //thus, 1/physcorrect = "amount of time it takes for a full offset"
                if (Time.time - lastUpdateTime > 1/physcorrect) {
                    currentOffset = Vector3.zero;
                    currentVelocityOffset = Vector3.zero;
                    currentRotationOffset = Quaternion.identity;
                    currentRotationalVelocityOffset = Quaternion.identity;
                }
                
                thisRigidbody.MovePosition(thisRigidbody.position + currentOffset);
                thisRigidbody.velocity = thisRigidbody.velocity + currentVelocityOffset;
                thisRigidbody.MoveRotation(thisRigidbody.rotation * currentRotationOffset);


                Vector3 v = thisRigidbody.angularVelocity; //convert angular velocity into a quaternion for easier math
                float angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                //Construct the current angular velocity quaternion, and add the current offset, and then convert back to angleAxis
                (Quaternion.AngleAxis(angle, v.normalized) * currentRotationalVelocityOffset).ToAngleAxis(out tempAngleVariable, out tempAxisVariable);
                thisRigidbody.angularVelocity = tempAxisVariable * tempAngleVariable * Mathf.Deg2Rad;

                velocity = thisRigidbody.velocity;
                position = thisRigidbody.position;
                rotation = thisRigidbody.rotation;
                v = thisRigidbody.angularVelocity;
                angle = (v.x / v.normalized.x) * Mathf.Rad2Deg;
                rotationalVelocity = Quaternion.AngleAxis(angle, v.normalized);
            }
            else { //physicsless motion
                
                
                

                


                velocity = Vector3.Lerp(beforeUpdateVelocity, updatedVelocity, TweenFunction(interleavedFraction));
                position += (velocity * Time.deltaTime) + (updatedPosition - position) * (1 - Mathf.Clamp(velocity.magnitude/5f, 0.95f, 1f));
                //position = Vector3.LerpUnclamped(beforeUpdatePosition, updatedPosition, TweenFunction(interleavedFraction));

                rotationalVelocity = Quaternion.Slerp(beforeUpdateRotationalVelocity, updatedRotationalVelocity, timeFraction);
                currentRotationOffset = Quaternion.Slerp(beforeUpdateRotation, updatedRotation, timeFraction);
                rotation = currentRotationOffset * Quaternion.SlerpUnclamped(Quaternion.identity, rotationalVelocity, Time.time - lastUpdateTime);

                if (hasRigidbody) {
                    thisRigidbody.MovePosition(position);
                    thisRigidbody.MoveRotation(rotation);
                }
                else {
                    thisTransform.position = position;
                    thisTransform.localRotation = rotation;
                }
                    
                
            }
        }
    }

    float TweenFunction(float input) {
        
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
        lastInterval = Time.time - lastUpdateTime;
        lastUpdateTime = Time.time;
        
        isKinematic = t.isKinematic;
        beforeUpdatePosition = position;
        beforeUpdateVelocity = velocity;
        beforeUpdateRotation = rotation;
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
