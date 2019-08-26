using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CamRotationTest : MonoBehaviour
{
    
    public HeadTrackingInfo trackingInfo;
    private float xEuler, yEuler, zEuler, xEulerPrevious, xEulerInit;
    private float deltaXori, deltaYori, deltaZori;
    private float rot, tr;
    private Quaternion camQuat;
    
    
    // Start is called before the first frame update
    void Start()
    {
        xEuler = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(tr, 0.0f, 0.0f));
        tr = - 0.1f * (float)(Math.Sin(Time.time));
    }
}
