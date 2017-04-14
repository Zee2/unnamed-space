using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

[ExecuteInEditMode]
public class IdentityContainer : MonoBehaviour {


    public MeshNetworkIdentity identity;
    float timeLastAssignedSubIDs = 0;

    void Update() {
        if (Application.isEditor && Application.isPlaying == false && Time.time - timeLastAssignedSubIDs > 1) {
            timeLastAssignedSubIDs = Time.time;
            byte count = 5;
            foreach(IReceivesPacket<MeshPacket> sub in gameObject.GetComponentsInChildren<IReceivesPacket<MeshPacket>>()) {
                sub.SetSubcomponentID(count);
                count++;
                Debug.Log("Setting id");
                if(count == byte.MaxValue) {
                    Debug.LogError("Too many subcomponents (scripts that implement IReceivePacket)");
                    return;
                }
            }
        }
    }

    public void PopulateComponents() {
        if(identity != null) {
            
            identity.attachedComponents = new List<IReceivesPacket<MeshPacket>>();
            identity.attachedComponents.AddRange(gameObject.GetComponentsInChildren<IReceivesPacket<MeshPacket>>(true));

            
            foreach (IReceivesPacket<MeshPacket> c in identity.attachedComponents) {
                Debug.Log("Component: " + c.GetType());
                if(c is INetworked<MeshNetworkIdentity>) {
                    INetworked<MeshNetworkIdentity> networked = c as INetworked<MeshNetworkIdentity>;
                    networked.SetIdentity(identity);
                    //Debug.Log("Setting identity with objectID = " + identity.GetObjectID());
                    
                }
                else {
                    Debug.LogError("An attached component does not support the INetworked interface!");
                }
            }
            
            
        }
        else {
            Debug.LogError("This container's MeshNetworkIdentity doesn't exist. Something very weird just happened.");
        }
    }

    public void SetIdentity(MeshNetworkIdentity id) {
        identity = id;
        PopulateComponents();
    }

    public MeshNetworkIdentity GetIdentity() {
        return identity;
    }
}
