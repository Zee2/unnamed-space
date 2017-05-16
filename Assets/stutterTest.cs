using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stutterTest : MonoBehaviour {
    public GameObject zone;
    Rigidbody r;
    Vector3 pos;
    Transform t;
    public bool useTransform;
    public bool rotate;
    public bool move;
    // Use this for initialization
    void Start () {
        r = GetComponent<Rigidbody>();
        t = transform;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        

        pos = pos + new Vector3(Input.GetAxis("Horizontal") * 0.1f, 0, Input.GetAxis("Vertical") * 0.1f);
        if (r != null && useTransform == false && move) {
            r.MovePosition(pos);
        } else if (move) {
            t.localPosition = pos;
        }
        if (rotate) {
            t.localRotation = Quaternion.identity * Quaternion.Euler(Random.insideUnitSphere * 0.05f);
        }
	}
}
