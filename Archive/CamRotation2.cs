using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;

public class CamRotation2 : MonoBehaviour
{

    public HeadTrackingInfo trackingInfo;
    //public GameObject eyeObject; 
    private float xEuler, yEuler, zEuler, deltaXori, deltaXori_init, xEulerPrevious, xEulerInit;
    private float Time_0, Time_F;
    private float time_f, timediff, time_prev;
    private int Counter;
    private string prbsRes;
    private float pi;
    public float pertSignal;
    private int Experiment_Case;
    private float dtVR;
    private float T_total;

    public Vector3 AnklePosition;
    public float SubjectHeight;

    private int flagExpNum;
    private static System.Random rndGen;

    // Randomized Sinusoids
    private float RS_TimeInit;
    private bool RS_pulsedone;
    private float RS_t0, RS_ts, RS_t;
    private double RS_SteadyStateTimeSec;
    private int RS_randBit;
    private float RS_Amplitude;
    private float RS_RotationVelocity;
    private float RS_frequency;

    private float[] RS_Pos;


    // PRTS 
    private float[] PRTS_Pos;
    private float PRTS_TimeInit;
    private float PRTS_dtVR;
    private float PRTS_dt;
    private int PRTS_NumPeriod;
    private float PRTS_P2P_Amp;
    private float PRTS_Velocity;
    private int PRTS_Kt;
    private float PRTS_pos_max;
    private float PRTS_pos_min;
    private float PRTS_pos_amplitude;
    private float PRTS_TimeTotal;

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

    private float[] TrapZ_Pos;

    // TrapzV
    private float[] TrapzV_Pos;
    private float TrapzV_ti;
    private float TrapzV_ta;
    private float TrapzV_tv;
    private float TrapzV_tx;
    private float TrapzV_maxVel;
    private int TrapzV_nPulses;
    private float TrapzV_tTotal;
    private float TrapzV_dtVR;
    private float trapzV_TimeInit;
    private float TrapzV_Pos_max;
    private float TrapzV_Pos_min;
    private float TrapzV_P2P_Amp;

    // Write to file 
    private TextWriter dataLogger;
    private TextWriter daqSignal;
    private TextWriter timeLog;

    // Date and time
    private DateTime dt;
    private string dateString;
    private string logFileName;

    // daq data
    private float daqInput;

    // Use this for initialization
    void Start()
    {
        MCCDAQwrap.flashLED();
        MCCDAQwrap.writeVolts(1, 0.0f);  // Set all voltages to zero

        dtVR = Time.fixedDeltaTime;

        //Using Random Class
        rndGen = new System.Random();

        xEuler = 0.0f;
        yEuler = 0.0f;
        zEuler = 0.0f;
        deltaXori = 0.0f;
        Counter = 0;
        pi = (float)Math.PI;
        daqInput = 0.0f;
        pertSignal = 0.0f;

        AnklePosition = new Vector3(transform.position.x, transform.position.y - 1.65f, transform.position.z);

        // ******************  EXPERIMENTS SETUP  ******************
        flagExpNum = 1;  // 0: Normal VR  1: Randomized Half Sinusoidal  2:PRTS    3:TrapZ    4:TrapzV

        Time_0 = 5.0f;   //Start perturbations after this time seconds
        Time_F = 315.0f;
       
        // *******************  1: Randomized half Sinusoids *********************
        RS_pulsedone = true;
        RS_SteadyStateTimeSec = 1.5; //Seconds default = 1.4sec;
        RS_randBit = 1;
        RS_Amplitude = 2.0f;
        RS_RotationVelocity = pi;    // A.Sin(wt) w = Rad per second;  default = pi / 2
        RS_frequency = RS_RotationVelocity / (2 * pi);      //v = w/2pi   Hz

        RS_Pos = InputFunctions.HalfSin_offline(5, RS_Amplitude, RS_RotationVelocity, (float)RS_SteadyStateTimeSec, out T_total).ToArray();

        // *******************  2: PRTS  *********************  

        PRTS_dtVR = 0.011111f; //Time resolution (time per frame) measured for VR device with FixedUpdate() method       
        PRTS_Kt = 20;// How many frames per each PRTS input
        PRTS_dt = PRTS_Kt * PRTS_dtVR;  // switching rate for PRTS inputs
        PRTS_NumPeriod = 6;
        PRTS_TimeTotal = 242 * PRTS_dt * PRTS_NumPeriod;

        //-------- with PRTS_Velocity = 1.0f  ===> we get PRTS_P2P_Amp = 17.0f * PRTS_Kt * PRTS_dtVR deg 
        //-------- with PRTS_Velocity = v     ===>        PRTS_P2P_Amp = 17.0f * PRTS_Kt * PRTS_dtVR * v
        //-------- ==> PRTS_Velocity = PRTS_P2P_Amp/(17.0f * PRTS_Kt * PRTS_dtVR)    degree per second

        // PRTS_pos_min = -4.0f *  PRTS_Kt * PRTS_dtVR * v ==> PRTS_pos_min = -4.0f/17.0f * PRTS_P2P_Amp
        // PRTS_pos_max = 13.0f *  PRTS_Kt * PRTS_dtVR * v ==> PRTS_pos_max = 13.0f/17.0f * PRTS_P2P_Amp

        PRTS_P2P_Amp = 2.0f; //degree
        PRTS_Velocity = PRTS_P2P_Amp / (17.0f * PRTS_Kt * PRTS_dtVR);

        PRTS_Pos = new float[PRTS_NumPeriod * PRTS_Kt * 242];   // ((int)Math.Pow(3, 5) - 1) = 242
        PRTS_Pos = InputFunctions.PRTS_offline(PRTS_NumPeriod, PRTS_dt, PRTS_Velocity, out T_total, PRTS_dtVR).ToArray();

        PRTS_pos_max = PRTS_Pos.Max();
        PRTS_pos_min = PRTS_Pos.Min();

        PRTS_P2P_Amp = Math.Abs(PRTS_pos_max - PRTS_pos_min);

        if (flagExpNum == 2)
        {
            Time_F = 245.0f;
            print("PRTS_pos_max = " + PRTS_pos_max);
            print("PRTS_pos_min = " + PRTS_pos_min);
            print("PRTS_dt = " + PRTS_dt);
            print("PRTS_P2P_Amp = " + PRTS_P2P_Amp);
            print("PRTS Period = " + (242 * PRTS_dt));
            print("PRTS_TimeTotal = " + PRTS_TimeTotal);
        }

        // *******************  3: TrapZ  *********************  

        TrapZ_pulsedone = true;
        TrapZ_randBit = 1;
        // TrapZ_SteadyStateTimeSec = 2.5f; //this is D. delta = D + rand(0, D)

        TrapZ_minDeltaSec = 1.5f; //Wait Time this is minDelta. delta = minDelta + rand(0, minDelta)
        TrapZ_minWSec = 1.5f; //Pulse Width, this is w. W = w + rand(0, w)

        TrapZ_P2PAmp = 10.0f; //deg                                        <<========================================
        TrapZ_Amp = TrapZ_P2PAmp / 2.0f; //degrees
        TrapZ_Vel = 10.0f; //deg per sec                                   <<========================================
        TrapZ_dt = TrapZ_Amp / TrapZ_Vel;
        //TrapZ_W = 3.0f; //Seconds  NOW IT IS RANDOM USING TrapZ_SteadyStateTimeSec

        TrapZ_Pos = InputFunctions.TrapZ_offline(6, TrapZ_P2PAmp, TrapZ_Vel, (float)TrapZ_minDeltaSec, (float)TrapZ_minWSec, out T_total).ToArray();

        if (flagExpNum == 3)
        {
            Time_F = 240.0f;
            print("TrapZ_Amp = " + TrapZ_P2PAmp);
            print("TrapZ_Vel = " + TrapZ_Vel);
            print("TrapZ_dt = " + TrapZ_dt);
        }

        // *********************** 4: TrapZ Vel **************************

        TrapzV_ti = 4.0f;
        TrapzV_ta = 0.5f;
        TrapzV_tv = 4.0f;
        TrapzV_tx = 4.0f;

        TrapzV_dtVR = 0.011111f;
        TrapzV_maxVel = 10.0f; //deg per second

        float T = TrapzV_ti + 4 * TrapzV_ta + 2 * TrapzV_tv + TrapzV_tx;
        int dt_Count = (int)Math.Round(T / TrapzV_dtVR);

        TrapzV_nPulses = 6;
        TrapzV_tTotal = T * TrapzV_nPulses;

        TrapzV_Pos = (InputFunctions.TrapV_offline(TrapzV_nPulses, TrapzV_ti, TrapzV_ta, TrapzV_tv, TrapzV_tx, TrapzV_maxVel, out T_total, TrapzV_dtVR)).ToArray();

        TrapzV_Pos_max = TrapzV_Pos.Max();
        TrapzV_Pos_min = TrapzV_Pos.Min();
        TrapzV_P2P_Amp = Math.Abs(TrapzV_Pos_max - TrapzV_Pos_min);

        if (flagExpNum == 4)
        {
            Time_F = TrapzV_tTotal;
            print("TrapzV_tTotal = " + TrapzV_tTotal);
        }

        // *******************   Write to file   *******************

        switch (flagExpNum)
        {
            case 0: // No purturbation
                { logFileName = "Normal_"; break; }
            case 1:
                { logFileName = "RandHalfSin_"; break; }
            case 2:
                { logFileName = "PRTS_"; break; }
            case 3:
                { logFileName = "TrapZ_"; break; }
            case 4:
                { logFileName = "TrapzV_"; break; }
        }

        dt = DateTime.Now;
        dateString = dt.ToString("dd-MM_hh-mm");

        dataLogger = new StreamWriter("log/VR_" + logFileName + dateString + "_Case_" + Experiment_Case.ToString() + ".txt");
        timeLog = new StreamWriter("log/Time_" + logFileName + dateString + "_Case_" + Experiment_Case.ToString() + ".txt");
        daqSignal = new StreamWriter("log/daq_" + logFileName + dateString + "_Case_" + Experiment_Case.ToString() + ".txt");
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if ((Time.time > Time_0) && (Time.time < Time_F))  //Perturbation start and finish time
        {
            print("Update time:" + Time.deltaTime);

            switch (flagExpNum)
            {
                case 0: // No purturbation
                    {
                        break;
                    }

                case 1: //Randomized Half Sinusoidal Perturbations
                    {
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            RS_TimeInit = Time.time;
                        }

                        if (RS_pulsedone == true)
                        {
                            RS_t0 = Time.time;
                            RS_ts = (float)(RS_SteadyStateTimeSec + (RS_SteadyStateTimeSec * rndGen.NextDouble()));
                            RS_randBit = InputFunctions.RandomBit();
                            RS_pulsedone = false;
                        }

                        RS_t = Time.time - RS_t0;

                        if (RS_t < RS_ts) xEuler = xEulerInit;
                        if ((RS_t >= RS_ts) && (RS_t <= RS_ts + (pi / RS_RotationVelocity)))
                        {
                            pertSignal = RS_randBit * RS_Amplitude * (float)Math.Sin(RS_RotationVelocity * (RS_t - RS_ts));
                        }
                        if (RS_t > RS_ts + (pi / RS_RotationVelocity))
                        {
                            RS_pulsedone = true;
                            pertSignal = 0;

                        }
                        xEuler = xEulerInit + pertSignal;
                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;

                        //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);

                        daqInput = 2.5f + 2.5f * (pertSignal / RS_Amplitude);
                        MCCDAQwrap.writeVolts(1, daqInput);
                        WriteLog(pertSignal, daqInput, (Time.time - RS_TimeInit));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 2: //PRTS
                    {
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            PRTS_TimeInit = Time.time;
                        }

                        if (Counter < PRTS_Pos.Length)
                        {
                            //daqInput = (float)((PRTS_Pos[Counter] / PRTS_Velocity) + 0.96f);
                            daqInput = (PRTS_Pos[Counter] - PRTS_pos_min) * (5.0f / PRTS_P2P_Amp);

                            //xEuler = xEulerInit + (PRTS_Pos[Counter]/PRTS_P2P_Amp) * PRTS_pos_amplitude;
                            xEuler = xEulerInit + PRTS_Pos[Counter];
                            MCCDAQwrap.writeVolts(1, daqInput);
                            //WriteLog((float)(PRTS_Pos[Counter]), daqInput, (Time.time - PRTS_TimeInit));
                            WriteLog(PRTS_Pos[Counter], daqInput, (Time.time - PRTS_TimeInit));

                            deltaXori = (xEuler - xEulerPrevious);
                            if (deltaXori > 180) deltaXori = deltaXori - 360;
                            if (deltaXori < -180) deltaXori = deltaXori + 360;
                            //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);
                        }
                        xEulerPrevious = xEuler;
                        Counter = Counter + 1;

                        break;
                    }

                case 3: //TrapZ - Randomized
                    {
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            TrapZ_TimeInit = Time.time;
                        }

                        if (TrapZ_pulsedone == true)
                        {
                            TrapZ_t0 = Time.time;
                            TrapZ_delta = (float)(TrapZ_minDeltaSec + (TrapZ_minDeltaSec * rndGen.NextDouble()));
                            TrapZ_W = (float)(TrapZ_minWSec + (TrapZ_minWSec * rndGen.NextDouble()));
                            TrapZ_randBit = InputFunctions.RandomBit();
                            TrapZ_pulsedone = false;
                        }

                        TrapZ_t = Time.time - TrapZ_t0;

                        if (TrapZ_t < TrapZ_delta) xEuler = xEulerInit;
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
                        xEuler = xEulerInit + pertSignal;
                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;

                        transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        //transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);

                        daqInput = 2.5f + 2.5f * (pertSignal / TrapZ_Amp);
                        MCCDAQwrap.writeVolts(1, daqInput);
                        WriteLog(pertSignal, daqInput, (Time.time - TrapZ_TimeInit));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 4: //TrapzV
                    {
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            trapzV_TimeInit = Time.time;
                        }

                        if (Counter < TrapzV_Pos.Length)
                        {
                            daqInput = (TrapzV_Pos[Counter] - TrapzV_Pos_min) * (5.0f / TrapzV_P2P_Amp);

                            xEuler = xEulerInit + TrapzV_Pos[Counter];
                            MCCDAQwrap.writeVolts(1, daqInput);
                            WriteLog(TrapzV_Pos[Counter], daqInput, (Time.time - trapzV_TimeInit));

                            deltaXori = (xEuler - xEulerPrevious);
                            if (deltaXori > 180) deltaXori = deltaXori - 360;
                            if (deltaXori < -180) deltaXori = deltaXori + 360;
                            //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);
                        }
                        xEulerPrevious = xEuler;
                        Counter = Counter + 1;
                        break;
                    }
            }

        }
    }

    void OnDestroy()
    {
        dataLogger.Close();
        timeLog.Close();
        daqSignal.Close();
    }

    public void WriteLog(float data, float daq, float time)
    {
        dataLogger.WriteLine(data + ",");
        daqSignal.WriteLine(daq + ",");
        timeLog.WriteLine(time + ",");
    }



}