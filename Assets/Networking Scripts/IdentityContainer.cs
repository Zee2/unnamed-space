using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

public class IdentityContainer : MonoBehaviour {

    
    public MeshNetworkIdentity identity;

    public void PopulateComponents() {
        if(identity != null) {
            
            identity.attachedComponents = new List<IReceivesPacket<MeshPacket>>();
            identity.attachedComponents.AddRange(gameObject.GetComponents<IReceivesPacket<MeshPacket>>());
            Debug.Log("Populated " + identity.attachedComponents.Count +" subcomponents on objectID " + identity.GetObjectID());

            foreach (IReceivesPacket<MeshPacket> c in identity.attachedComponents) {
                Debug.Log("Component: " + c.GetType());
                if(c is INetworked<MeshNetworkIdentity>) {
                    INetworked<MeshNetworkIdentity> networked = c as INetworked<MeshNetworkIdentity>;
                    networked.SetIdentity(identity);
                }
                else {
                    Debug.LogError("An attached component does not support the INetworked interface!");
                }
            }
        }
        else {
            Debug.Log("This container's MeshNetworkIdentity doesn't exist. Something very weird just happened.");
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
