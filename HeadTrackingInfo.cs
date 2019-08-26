using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class HeadTrackingInfo : MonoBehaviour {
    public Vector3 headOri, headOriLocal;
    public Vector3 headTransl;
    public float headOriX, headOriY, headOriZ;
    public float headOriX_local, headOriY_local, headOriZ_local;
    public float headTranslX, headTranslY, headTranslZ;

    // Use this for initialization
    void Start () {
        headOriX = 0.0f;
        headOriY = 0.0f;
        headOriZ = 0.0f;
    }
	
	// Update is called once per frame
	void Update () {
        
        headOriX = transform.eulerAngles.x;
        headOriY = transform.eulerAngles.y;
        headOriZ = transform.eulerAngles.z;
        headOri = transform.eulerAngles;

        headOriX_local = transform.localEulerAngles.x;
        headOriY_local = transform.localEulerAngles.y;
        headOriZ_local = transform.localEulerAngles.z;
        headOriLocal = transform.localEulerAngles;

        headTranslX = transform.localPosition.x;
        headTranslY = transform.localPosition.y;
        headTranslZ = transform.localPosition.z;
        headTransl = transform.localPosition;

        
        //transform.rotation = Quaternion.Inverse(InputTracking.GetLocalRotation(VRNode.CenterEye));

        /*        
        headOriX = transform.rotation.eulerAngles.x;
        headOriY = transform.rotation.eulerAngles.y;
        headOriZ = transform.rotation.eulerAngles.z;

        print("HeadOriX: " + headOriX);
        print("HeadOriY: " + headOriY);
        print("HeadOriZ: " + headOriZ);
        */

    }
}
