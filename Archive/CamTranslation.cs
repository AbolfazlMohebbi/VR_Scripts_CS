using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;

public class CamTranslation : MonoBehaviour
{
    public HeadTrackingInfo trackingInfo;
    private float xPos, yPos, zPos, deltaX, deltaX_init, xPrevious, xInit;
    private int Counter;

    // TrapZ
    private float TrapZ_Amp;
    private float TrapZ_P2PAmp;
    private float TrapZ_Vel;
    private float TrapZ_dt;
    private float TrapZ_W;
    private float TrapZ_TimeInit;
    private bool TrapZ_pulsedone;
    private float TrapZ_t0, TrapZ_delta, TrapZ_t;
    private double TrapZ_SteadyStateTimeSec;
    private double TrapZ_minDeltaSec; //this is minDelta. delta = minDelta + rand(0, minDelta)
    private double TrapZ_minWSec; //this is D. delta = D + rand(0, D)
    private int TrapZ_randBit;
    private float TrapZ_frequency;

    public float pertSignal;
    private static System.Random rndGen;


    // Start is called before the first frame update
    void Start()
    {
        rndGen = new System.Random();
        Counter = 0;

        TrapZ_pulsedone = true;
        TrapZ_randBit = 1;

        TrapZ_minDeltaSec = 1.5f; //Wait Time this is minDelta. delta = minDelta + rand(0, minDelta)
        TrapZ_minWSec = 1.5f; //Pulse Width, this is w. W = w + rand(0, w)

        TrapZ_P2PAmp = 2.0f; //meters                                     <<========================================
        TrapZ_Amp = TrapZ_P2PAmp / 2.0f; //meters
        TrapZ_Vel = 2.0f; // m/sec                                        <<========================================
        TrapZ_dt = TrapZ_Amp / TrapZ_Vel;
    }

    // Update is called once per frame
    void Update()
    {
        if (Counter == 0)
        {
            xInit = trackingInfo.headTranslX;
            xPrevious = xInit;
            TrapZ_TimeInit = Time.time;
        }

        if (TrapZ_pulsedone == true)
        {
            TrapZ_t0 = Time.time;
            TrapZ_delta = (float)(TrapZ_minDeltaSec + (TrapZ_minDeltaSec * rndGen.NextDouble()));
            TrapZ_W = (float)(TrapZ_minWSec + (TrapZ_minWSec * rndGen.NextDouble()));
            TrapZ_randBit = RandomBit();
            TrapZ_pulsedone = false;
        }

        TrapZ_t = Time.time - TrapZ_t0;

        if (TrapZ_t < TrapZ_delta) xPos = xInit;
        if ((TrapZ_t >= TrapZ_delta) && (TrapZ_t < TrapZ_delta + TrapZ_dt))
        {
            pertSignal = TrapZ_randBit * TrapZ_Vel * (TrapZ_t - TrapZ_delta);
        }
        if ((TrapZ_t >= TrapZ_delta + TrapZ_dt) && (TrapZ_t < TrapZ_W + TrapZ_delta + TrapZ_dt))
        {
            pertSignal = TrapZ_randBit * TrapZ_Amp;
        }
        if ((TrapZ_t >= TrapZ_W + TrapZ_delta + TrapZ_dt) && (TrapZ_t <= TrapZ_W + TrapZ_delta + 2 * TrapZ_dt))
        {
            pertSignal = TrapZ_randBit * (TrapZ_Amp - TrapZ_Vel * (TrapZ_t - TrapZ_delta - TrapZ_W - TrapZ_dt));
        }

        if (TrapZ_t > TrapZ_W + TrapZ_delta + 2 * TrapZ_dt)
        {
            TrapZ_pulsedone = true;
            pertSignal = 0;

        }
        xPos = xInit + pertSignal;
        deltaX = (xPos - xPrevious);
        transform.Translate(new Vector3(deltaX, yPos, zPos));
        xPrevious = xPos;
        Counter = Counter + 1;
    }

    public int RandomBit()
    {
        int randomDirection = 3 - 2 * rndGen.Next(1, 3);
        return randomDirection;
    }

}
