using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stutterTest : MonoBehaviour {
    public GameObject zone;
    Rigidbody r;
    Vector3 pos;
    Transform t;
    public bool UpdateMovePosition;
    public bool SinMove;
    // Use this for initialization
    void Start () {
        r = GetComponent<Rigidbody>();
        t = transform;
	}
	
    void Update()
    {
        
        if (SinMove)
        {
            r.MovePosition(Vector3.up * -3 + Vector3.forward * Mathf.Sin(Time.time));
        }
    }

	// Update is called once per frame
	void FixedUpdate () {
        //r.MoveRotation(Quaternion.identity);
        if (UpdateMovePosition)
            r.MovePosition(t.parent.TransformPoint(Vector3.up * 5 + Vector3.forward * Input.GetAxis("Horizontal")));
        /*
        pos = pos + new Vector3(Input.GetAxis("Horizontal") * 0.1f, 0, Input.GetAxis("Vertical") * 0.1f);
        if (r != null && useTransform == false && move) {
            r.MovePosition(pos);
        } else if (move) {
            t.localPosition = pos;
        }
        if (rotate) {
            t.localRotation = Quaternion.identity * Quaternion.Euler(Random.insideUnitSphere * 0.05f);
        }
        */
    }

    private void OnDrawGizmos()
    {
        if(r != null)
        {
            Gizmos.DrawLine(r.position, r.position + r.velocity);
        }
    }
}
