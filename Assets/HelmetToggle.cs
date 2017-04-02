using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelmetToggle : MonoBehaviour {

    public Animator helmetController;

    void OnTriggerEnter(Collider c) {
        if (c.CompareTag("Hand")) {
            helmetController.SetTrigger("HelmetTrigger");
        }
    }
}
