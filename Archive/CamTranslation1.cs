using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;


public class CamTranslation1 : MonoBehaviour
{

    public HeadTrackingInfo trackingInfo;
    //public GameObject eyeObject; 
    private Quaternion startRot;
    private float xEuler, yEuler, zEuler, deltaXori, deltaXori_init, xEulerPrevious, xEulerInit;
    private float xPos, yPos, zPos, deltaX, deltaX_init, xPrevious, xInit;
    private float Time_0, Time_F;
    private float time_f, timediff, time_prev;
    private float toRad, toDeg;
    private int Counter;
    private string prbsRes;
    private float pi;
    public float pertSignal;
    private int Experiment_Case;

    public Vector3 AnklePosition;
    public float SubjectHeight;


    // Magnified Motion Vars:
    private float MagnifyOri;

    // Step Response Vars:
    private float stepAmpilitude;
    private int stepStartTimeMS, stepStartTimeFrame, stepEndTimeFrame, stepStartTimeFramePrevious, stepTimeFrame, stepDurationMS, stepTimeFrameDuration, stepSteadyStateTimeFrame, stepSteadyStateTimeMS;

    // Sinosoidal Vars:
    private float Sin_Amplitude, Sin_frequency, Sin_RotationVelocity;

    //PRBS Vars:
    private float prbsAmpilitude;
    private int prbsTimeFrameDuration, prbsDurationMS;
    private float prbsDuration;
    private int iCount;
    private int PrbsElementInt, previousPrbsElementInt;
    private float PrbsElement, previousPrbsElement;
    private int PRBS_randBit;
    private float prbsCurAmp;
    private float PRBS_XOri;

    private float PRBS_dtVR;
    private int PRBS_Kt;

    // Randomized STEP
    private bool randomDirectionON;
    private int randomDirection;

    // Randomized Sinusoids
    private float RS_TimeInit;
    private bool RS_pulsedone;
    private float RS_t0, RS_ts, RS_t;
    private double RS_SteadyStateTimeSec;
    private int RS_randBit;
    private float RS_Amplitude;
    private float RS_RotationVelocity;
    private float RS_frequency;

    // Sum of Sinusoids
    private float SS_TimeInit;
    private float sinSum;
    private float SS_Amplitude;
    private float SS_fundamentalFreq;
    private float SS_maxFreq;
    private float SS_minFreq;
    private int SS_freqCount;
    private float SS_TimeTotal; //sec
    private float SS_dtVR;
    private float[] SS_result;
    private float SS_maxValue;
    private float SS_minValue;
    private float SS_P2P_Amp;
    private float SS_period;
    private int SS_NumOfPeriods;


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


    private int flagExpNum;
    private static System.Random rndGen;

    // Write to file 
    private TextWriter dataLogger;
    private TextWriter daqSignal;
    private TextWriter timeLog;
    private bool offline_Log;

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

        //Console.WriteLine("Enter Case Number: ");
        //Experiment_Case = Convert.ToInt32(Console.ReadLine());

        //Using Random Class
        rndGen = new System.Random();

        xEuler = 0.0f;
        yEuler = 0.0f;
        zEuler = 0.0f;
        deltaXori = 0.0f;
        iCount = 0;
        startRot = transform.rotation;
        Counter = 0;
        toRad = (3.14f / 180.0f);
        toDeg = (180.0f / 3.14f);
        pi = (float)Math.PI;
        daqInput = 0.0f;
        pertSignal = 0.0f;

        AnklePosition = new Vector3(transform.position.x, transform.position.y - 1.65f, transform.position.z);

        // ******************  EXPERIMENTS SETUP  ******************
        flagExpNum = 10;  // 0: Normal VR  1: Disabled Head-tracking  2: Magnify Rotation  3: Step-response (5 and 10 deg.) 4: Sinusoidal Perturbations 
                          // 5: PRBS  6: Randomized Step    7: Randomized Sinusoidal    8: Sum of Sinusoids    9:PRTS    10:TrapZ    11:SumofSin not Normalized


        Time_0 = 5.0f;   //Start perturbations after this time seconds
        Time_F = 315.0f;

        // ******************  3: STEP RESPONSE PARAMETERS ******************
        stepTimeFrame = 0;
        stepStartTimeFramePrevious = 0;

        // each frame = 20ms or 0.020 sec check it plz
        stepStartTimeMS = rndGen.Next(0, 4000);   // Step happens at a random time between 0 to 4000 miliseconds
        stepStartTimeFrame = stepStartTimeMS / 20; //miliseconds

        if (flagExpNum == 3)
        { print("stepStartTimeFrame is:  " + stepStartTimeFrame + "    and stepStartTimeMS is:  " + stepStartTimeMS); }
        stepAmpilitude = 10.0f; //Degrees (5 - 10 - 15 deg.)

        stepDurationMS = 1000; //miliseconds
        stepTimeFrameDuration = stepDurationMS / 20; //frames
        stepSteadyStateTimeMS = 1000; //miliseconds
        stepSteadyStateTimeFrame = stepSteadyStateTimeMS / 20;

        // ******************  4: SINUSOIDAL PARAMETERS  ********************
        Sin_Amplitude = 2.0f;
        Sin_RotationVelocity = pi;    // A.Sin(wt) w = Rad per second
        Sin_frequency = Sin_RotationVelocity / (2 * pi);      //v = w/2pi   Hz

        // ********************  5: PRBS PARAMETERS  ************************
        if (flagExpNum == 5)
        {
            prbsRes = PRBS_General();
            Time_F = 125.0f;
        }
        prbsAmpilitude = 5.0f; //Degrees (5 - 10 - 15 deg.)
        PRBS_dtVR = 0.011111f; //Time resolution CHECK PLZ
        PRBS_Kt = 40; // How many frames per each PRBS input
        prbsDuration = PRBS_dtVR * PRBS_Kt;
        if (flagExpNum == 5)
        { print("PRBS Switching Rate (Each step) =  " + prbsDuration + "seconds"); }
        //prbsDurationMS = 200;  //miliseconds
        //prbsTimeFrameDuration = prbsDurationMS / 20;
        PRBS_randBit = 1;

        // *******************  6: Randomized STEP *********************
        randomDirectionON = true;

        // *******************  7: Randomized half Sinusoids *********************
        RS_pulsedone = true;
        RS_SteadyStateTimeSec = 0.5; //Seconds default = 1.4sec;
        RS_randBit = 1;
        RS_Amplitude = 10.0f;
        RS_RotationVelocity = pi;    // A.Sin(wt) w = Rad per second;  default = pi / 2
        RS_frequency = Sin_RotationVelocity / (2 * pi);      //v = w/2pi   Hz

        // *******************  8: Sum of Sinusoids *********************        
        Experiment_Case = 26;

        SS_fundamentalFreq = 0.05f; //Hz
        SS_freqCount = 30; // 30 Fcount = 1.5Hz
        SS_minFreq = 1 * SS_fundamentalFreq;  // 0.1Hz
        SS_maxFreq = SS_freqCount * SS_fundamentalFreq; //3.0 Hz
        SS_period = 1.0f / SS_fundamentalFreq;
        SS_NumOfPeriods = 11;
        SS_TimeTotal = SS_period * SS_NumOfPeriods;

        SS_dtVR = 0.011111f; //Time resolution (time per frame) measured for VR device with FixedUpdate() method
        SS_Amplitude = 2.0f; //degrees

        //SS_maxRotationVelocity = (2 * pi) * SS_maxFreq; // w = 2pi * v

        sinSum = 0.0f;
        SS_result = new float[(int)Math.Round(SS_TimeTotal / SS_dtVR)];

        Array.Clear(SS_result, 0, SS_result.Length);
        SS_result = SumOfSin(SS_fundamentalFreq, SS_freqCount, SS_TimeTotal, SS_dtVR);

        SS_maxValue = SS_result.Max();
        SS_minValue = SS_result.Min();
        SS_P2P_Amp = Math.Abs(SS_maxValue - SS_minValue);

        if (flagExpNum == 8)
        {
            Time_F = 230.0f;
            print("SS_maxValue = " + SS_maxValue);
            print("SS_minValue = " + SS_minValue);
            print("SS_TimeTotal = " + SS_TimeTotal);
            print("SS_Amplitude = " + SS_Amplitude);
            print("SS_fundamentalFreq = " + SS_fundamentalFreq);
            print("SS_minFreq = " + SS_minFreq);
            print("SS_maxFreq = " + SS_maxFreq);
            print("SS_P2P_Amp = " + SS_P2P_Amp);

            print("SS_result.Length = " + SS_result.Length);

        }

        // *******************  9: PRTS  *********************  

        PRTS_dtVR = 0.011111f; //Time resolution (time per frame) measured for VR device with FixedUpdate() method       
        PRTS_Kt = 20;// How many frames per each PRTS input                       <<=======================================
        PRTS_dt = PRTS_Kt * PRTS_dtVR;  // switching rate for PRTS inputs
        PRTS_NumPeriod = 6;
        PRTS_TimeTotal = 242 * PRTS_dt * PRTS_NumPeriod;

        //-------- with PRTS_Velocity = 1.0f  ===> we get PRTS_P2P_Amp = 17.0f * PRTS_Kt * PRTS_dtVR deg 
        //-------- with PRTS_Velocity = v     ===>        PRTS_P2P_Amp = 17.0f * PRTS_Kt * PRTS_dtVR * v
        //-------- ==> PRTS_Velocity = PRTS_P2P_Amp/(17.0f * PRTS_Kt * PRTS_dtVR)    degree per second

        // PRTS_pos_min = -4.0f *  PRTS_Kt * PRTS_dtVR * v ==> PRTS_pos_min = -4.0f/17.0f * PRTS_P2P_Amp
        // PRTS_pos_max = 13.0f *  PRTS_Kt * PRTS_dtVR * v ==> PRTS_pos_max = 13.0f/17.0f * PRTS_P2P_Amp


        PRTS_P2P_Amp = 2.0f; //degree                                                <=====================================
        PRTS_Velocity = PRTS_P2P_Amp / (17.0f * PRTS_Kt * PRTS_dtVR);

        PRTS_Pos = new float[PRTS_NumPeriod * PRTS_Kt * 242];   // ((int)Math.Pow(3, 5) - 1) = 242
        PRTS_Pos = PRTS(PRTS_NumPeriod, PRTS_dt, PRTS_dtVR, PRTS_Velocity);

        //PRTS_pos_amplitude = 4.0f; //deg  NOT USED NOW

        PRTS_pos_max = PRTS_Pos.Max();
        //PRTS_pos_max = 13.0f / 17.0f * PRTS_P2P_Amp;
        PRTS_pos_min = PRTS_Pos.Min();
        //PRTS_pos_min = -4.0f / 17.0f * PRTS_P2P_Amp;

        PRTS_P2P_Amp = Math.Abs(PRTS_pos_max - PRTS_pos_min);

        if (flagExpNum == 9)
        {

            Time_F = 245.0f;
            print("PRTS_pos_max = " + PRTS_pos_max);
            print("PRTS_pos_min = " + PRTS_pos_min);
            print("PRTS_dt = " + PRTS_dt);
            print("PRTS_P2P_Amp = " + PRTS_P2P_Amp);
            print("PRTS Period = " + (242 * PRTS_dt));
            print("PRTS_TimeTotal = " + PRTS_TimeTotal);
        }

        // *******************  10: TrapZ  *********************  

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

        if (flagExpNum == 10)
        {
            Time_F = 240.0f;
            print("TrapZ_Amp = " + TrapZ_P2PAmp);
            print("TrapZ_Vel = " + TrapZ_Vel);
            print("TrapZ_dt = " + TrapZ_dt);
        }


        // *******************   Write to file   *******************

        offline_Log = true;

        switch (flagExpNum)
        {
            case 0: // No purturbation
                { logFileName = "Normal_"; break; }
            case 1:
                { logFileName = "NoHeadTrack_"; break; }
            case 2:
                { logFileName = "Magnified_"; break; }
            case 3:
                { logFileName = "Step_"; break; }
            case 4:
                { logFileName = "Sinusoidal_"; break; }
            case 5:
                { logFileName = "PRBS_"; break; }
            case 6:
                { logFileName = "RandStep_"; break; }
            case 7:
                { logFileName = "RandHalfSin_"; break; }
            case 8:
                { logFileName = "SumOfSin_"; break; }
            case 9:
                { logFileName = "PRTS_"; break; }
            case 10:
                { logFileName = "TrapZ_"; break; }
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
        //time_f = Time.time;
        //timediff = time_f - time_prev;
        //print("timediff for " + logFileName + " is: " + timediff);
        //time_prev = time_f;

        if ((Time.time > Time_0) && (Time.time < Time_F))  //Perturbation start and finish time
        {
            //MCCDAQwrap.writeVolts(1, 5.0f);   // Send start vision perturbation signal of 5v to channel 1.
            print("Update time:" + Time.deltaTime);
            Debug.Log("Update time Debug:" + Time.deltaTime);

            switch (flagExpNum)
            {
                case 0: // No purturbation
                    {
                        break;
                    }

                case 1:   //head tracking disabled
                    {
                        transform.rotation = startRot * Quaternion.Inverse(UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.CenterEye));
                        break;
                    }

                case 2:  // Magnify Motion
                    {
                        MagnifyOri = 1.5f;     // Magnifying Factor. When MagnifyOri = 1.0f, No Magnification. 

                        xEuler = trackingInfo.headOriX;
                        if (Counter == 0)
                        {
                            xEulerPrevious = trackingInfo.headOriX;
                            deltaXori_init = xEuler - xEulerPrevious;
                            if (deltaXori_init > 180) deltaXori_init = deltaXori_init - 360;
                            if (deltaXori_init < -180) deltaXori_init = deltaXori_init + 360;
                        }

                        deltaXori = xEuler - xEulerPrevious;
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;
                        transform.Rotate(new Vector3((MagnifyOri - 1.0f) * deltaXori, (MagnifyOri - 1.0f) * yEuler, (MagnifyOri - 1.0f) * zEuler));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 3: // Step-response (5 - 10 - 15 deg.)
                    {
                        // each frame = 16ms or 0.016 sec
                        stepTimeFrame = stepTimeFrame + 1;

                        if (stepTimeFrame == stepStartTimeFrame)
                        {
                            pertSignal = -stepAmpilitude;
                            //transform.Rotate(new Vector3(pertSignal, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), pertSignal);
                            daqInput = 5.0f;
                            MCCDAQwrap.writeVolts(1, daqInput);
                        }

                        if (stepTimeFrame == stepStartTimeFrame + stepTimeFrameDuration)
                        {
                            pertSignal = stepAmpilitude;
                            //transform.Rotate(new Vector3(pertSignal, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), pertSignal);
                            daqInput = 0.0f;
                            MCCDAQwrap.writeVolts(1, daqInput);
                        }
                        WriteLog(daqInput, daqInput, (Time.time - Time_0));
                        break;
                    }

                case 4: //Sinusoidal Perturbations
                    {
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            RS_TimeInit = Time.time;
                        }
                        pertSignal = Sin_Amplitude * (float)Math.Sin(Sin_RotationVelocity * (Time.time - RS_TimeInit));
                        xEuler = xEulerInit + pertSignal;

                        daqInput = 2.5f + (2.5f / Sin_Amplitude) * pertSignal;
                        MCCDAQwrap.writeVolts(1, daqInput);
                        WriteLog(pertSignal, daqInput, (Time.time - Time_0));

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;

                        //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);

                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 5: //PRBS
                    {
                        if (Counter % PRBS_Kt == 0)  //done at each prbsTimeFrameDuration
                        {
                            if (iCount < prbsRes.Length)
                            {
                                char prbsElementChar = prbsRes[iCount];
                                PrbsElementInt = Convert.ToInt32(new string(prbsElementChar, 1));
                                PrbsElement = (float)Convert.ToDouble(new string(prbsElementChar, 1));
                                //print("PrbsElement:  " + PrbsElement);

                                prbsCurAmp = (PrbsElement - previousPrbsElement) * prbsAmpilitude;
                                //transform.Rotate(new Vector3(prbsCurAmp, yEuler, zEuler));
                                transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), prbsCurAmp);

                                //daqInput = (float)(PrbsElementInt * 5.0f);
                                daqInput = 2.5f + 2.5f * (prbsCurAmp / prbsAmpilitude);
                                MCCDAQwrap.writeVolts(1, daqInput);
                                previousPrbsElement = PrbsElement;
                                iCount = iCount + 1;
                            }
                        }
                        else
                        {
                            PrbsElement = previousPrbsElement; // For the sake of logging!
                        }
                        WriteLog(prbsCurAmp, daqInput, (Time.time - Time_0));
                        Counter = Counter + 1;
                        break;
                    }

                case 6: // Randomized Step-response (5 - 10 - 15 deg.)
                    {
                        // each frame = 16ms or 0.016 sec
                        stepTimeFrame = stepTimeFrame + 1;

                        if (stepTimeFrame == stepStartTimeFrame)
                        {
                            stepAmpilitude = RandomBit() * stepAmpilitude;
                            //transform.Rotate(new Vector3(-stepAmpilitude, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), -stepAmpilitude);
                            MCCDAQwrap.writeVolts(1, 5.0f);
                            stepStartTimeFrame = stepStartTimeFramePrevious + stepTimeFrameDuration + stepSteadyStateTimeFrame + rndGen.Next(0, stepSteadyStateTimeFrame);
                            //print("stepStartTimeFrame" + stepStartTimeFrame);
                        }

                        if (stepTimeFrame == stepStartTimeFramePrevious + stepTimeFrameDuration)
                        {
                            //transform.Rotate(new Vector3(stepAmpilitude, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), stepAmpilitude);
                            MCCDAQwrap.writeVolts(1, 0.0f);
                            stepStartTimeFramePrevious = stepStartTimeFrame;
                        }
                        break;
                    }

                case 7: //Randomized Half Sinusoidal Perturbations
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
                            RS_randBit = RandomBit();
                            RS_pulsedone = false;

                            //print("RS_t0: " + RS_t0);
                            //print("ts: " + RS_ts);
                            //print("t: " + (Time.time - RS_t0));
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

                case 8: //Sum of Sinusoids Perturbations - Normalized
                    {

                        sinSum = 0.0f;
                        daqInput = 0.0f;
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            SS_TimeInit = Time.time;
                        }

                        if (Counter < SS_result.Length)
                        {
                            sinSum = (SS_result[Counter] / SS_P2P_Amp) * SS_Amplitude;
                            xEuler = xEulerInit + sinSum;

                            daqInput = (SS_result[Counter] - SS_minValue) * (5.0f / SS_P2P_Amp);

                            deltaXori = (xEuler - xEulerPrevious);
                            if (deltaXori > 180) deltaXori = deltaXori - 360;
                            if (deltaXori < -180) deltaXori = deltaXori + 360;
                            //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                            transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);

                            MCCDAQwrap.writeVolts(1, daqInput);
                            WriteLog(sinSum, daqInput, (Time.time - SS_TimeInit));
                        }

                        xEulerPrevious = xEuler;
                        Counter = Counter + 1;
                        break;
                    }

                case 9: //PRTS
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

                case 10: //TrapZ - Randomized
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
                            TrapZ_randBit = RandomBit();
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

                case 11: //Sum of Sinusoids Perturbations - Not Normalized
                    {
                        sinSum = 0.0f;
                        daqInput = 0.0f;
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            SS_TimeInit = Time.time;
                        }

                        sinSum = SS_result[Counter] * SS_Amplitude * 0.5f;
                        xEuler = xEulerInit + sinSum;
                        daqInput = (SS_result[Counter] + 1.0f) * 2.5f;

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;

                        //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);

                        MCCDAQwrap.writeVolts(1, daqInput);
                        WriteLog(sinSum, daqInput, (Time.time - SS_TimeInit));

                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 12: //Randomized Half Sinusoidal Perturbations wrt ankle
                    {
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            RS_TimeInit = Time.time;
                        }
                        pertSignal = Sin_Amplitude * (float)Math.Sin(Sin_RotationVelocity * (Time.time - RS_TimeInit));
                        xEuler = xEulerInit + pertSignal;

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;


                        //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));

                        transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);

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

    public int RandomBit()
    {
        if (randomDirectionON == true)
        {
            randomDirection = 3 - 2 * rndGen.Next(1, 3);
            //print("randomDirection = " + randomDirection);
        }
        else
        {
            randomDirection = 1;
        }
        return randomDirection;
    }

    public float ToDegree(float theta)
    {
        float x = theta * (180.0f / 3.14f);
        return x;
    }

    public float ToRadian(float xx)
    {
        float theta = xx * (3.14f / 180.0f);
        return theta;
    }

    public float[] SumOfSin(float fundFreq, int freqCount, float Ttotal, float VR_timeFrame)
    {
        dt = DateTime.Now;
        dateString = dt.ToString("dd-MM-yyyy_hh-mm");
        TextWriter SOS_pos = new StreamWriter("log/Offline/SumOfSin/SumOfSin_Pos_" + dateString + ".txt");  // Write to file 

        float[] randPhase = new float[freqCount];
        float[] result = new float[(int)Math.Round(Ttotal / VR_timeFrame)];
        Array.Clear(result, 0, result.Length);
        float Freq = 0.0f;

        for (int i = 0; i < freqCount; i++)
        {
            randPhase[i] = (float)(2.0 * pi * rndGen.NextDouble());
            //randPhase[i] = 0.0f; // in case we don't want random phases
        }

        for (int j = 0; j < result.Length; j++)
        {
            for (int i = 0; i < freqCount; i++)
            {
                Freq = fundFreq + (i * fundFreq);
                result[j] = result[j] + (float)Math.Sin(Freq * 2.0f * pi * (j * VR_timeFrame) + randPhase[i]);
            }
            SOS_pos.WriteLine(result[j] + ",");
        }
        SOS_pos.Close();
        return result;
    }

    public string PRBS_General()
    {
        var polynomial = "1100000";
        //"1100000" = x^7+x^6+1
        //"10100" = x^5+x^3+1
        //"110" = x^3+x^2+1
        var start_state = 0x1;  /* Any nonzero start state will work. */

        var taps = Convert.ToInt32(polynomial, 2);
        var lfsr = start_state;
        var period = 0;
        var prbs = "";
        var prbs_buf = "";

        do
        {
            var lsb = lfsr & 1;  /* Get LSB (i.e., the output bit). */
            prbs = prbs + lsb;
            lfsr >>= 1;          /* Shift register */
            if (lsb == 1)
            {      /* Only apply toggle mask if output bit is 1. */
                lfsr ^= taps;      /* Apply toggle mask, value has 1 at bits corresponding to taps, 0 elsewhere. */
            }
            ++period;
        } while (lfsr != start_state);

        //var prbsInt = Convert.ToInt32(prbs, 2);
        //print("prbsInt =   " + prbsInt);

        if (period == Math.Pow(2, polynomial.Length) - 1)
        {
            print("polynomial is maximal length");
        }
        else
        {
            print("polynomial is not maximal length");
        }

        prbs_buf = prbs;
        for (int i = 1; i < 10; i++)
        {
            prbs = prbs + prbs_buf;
        }

        print("period = " + period);
        print("prbs = " + prbs);
        return prbs;
    }

    public int cmod(int a, int n)
    {
        int result = a % n;
        if ((result < 0 && n > 0) || (result > 0 && n < 0))
        {
            result += n;
        }
        return result;
    }

    public void WriteLog(float data, float daq, float time)
    {
        dataLogger.WriteLine(data + ",");
        daqSignal.WriteLine(daq + ",");
        timeLog.WriteLine(time + ",");
    }

    public float[] PRTS(int NumOfPeriods, float SwitchingRate, float timeRes, float VelocityAmp)
    {
        dt = DateTime.Now;
        dateString = dt.ToString("dd-MM-yyyy_hh-mm");

        TextWriter PRTS_posFile = new StreamWriter("log/Offline/PRTS/PRTS_Pos_" + dateString + ".txt");  // Write to file 
        TextWriter PRTS_velFile = new StreamWriter("log/Offline/PRTS/PRTS_Vel_" + dateString + ".txt");  // Write to file 
        int repeatCount = (int)Math.Round(SwitchingRate / timeRes);

        int[] ShiftRegisters = { 2, 0, 2, 0, 2 };
        int m = 5;
        int[] VelocitySequence = new int[NumOfPeriods * ((int)Math.Pow(3, m) - 1)]; // T = (3^m - 1).dt
        float[] PositionSequence = new float[NumOfPeriods * repeatCount * ((int)Math.Pow(3, m) - 1)];
        float[] VelocityVector = new float[NumOfPeriods * ((int)Math.Pow(3, m) - 1)];
        float[] VelocityVectorExtended = new float[NumOfPeriods * repeatCount * ((int)Math.Pow(3, m) - 1)];
        PositionSequence[0] = 0.0f;
        PRTS_posFile.WriteLine(PositionSequence[0] + ",");

        for (int k = 1; k <= VelocitySequence.Length; k++)
        {
            VelocitySequence[k - 1] = cmod((ShiftRegisters[2] - ShiftRegisters[3] - ShiftRegisters[4]), 3);
            ShiftRegisters[4] = ShiftRegisters[3];
            ShiftRegisters[3] = ShiftRegisters[2];
            ShiftRegisters[2] = ShiftRegisters[1];
            ShiftRegisters[1] = ShiftRegisters[0];
            ShiftRegisters[0] = VelocitySequence[k - 1];
            if (k % ((int)Math.Pow(3, m) - 1) == 1)
            {
                ShiftRegisters[0] = 0;
                ShiftRegisters[1] = 2;
                ShiftRegisters[2] = 0;
                ShiftRegisters[3] = 2;
                ShiftRegisters[4] = 0;
            }
            //print("VelocitySequence[" + k + "] = " + VelocitySequence[k-1]);
            //PRTS_velFile.WriteLine(VelocitySequence[k - 1] + ",");
        }

        // scale to -v and v:   0-->0  1-->v  2-->-v
        for (int k = 0; k < VelocitySequence.Length; k++)
        {
            if (VelocitySequence[k] == 1) { VelocityVector[k] = VelocityAmp; }
            if (VelocitySequence[k] == 2) { VelocityVector[k] = -VelocityAmp; }
            if (VelocitySequence[k] == 0) { VelocityVector[k] = 0; }
        }

        // repeat elements based on switching rate and update rate
        for (int k = 1; k <= VelocityVector.Length; k++)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                VelocityVectorExtended[repeatCount * (k - 1) + i] = VelocityVector[k - 1];
                PRTS_velFile.WriteLine(VelocityVectorExtended[repeatCount * (k - 1) + i] + ",");
            }
        }

        // Integral of velocity = position
        for (int i = 1; i < PositionSequence.Length; i++)
        {
            PositionSequence[i] = (VelocityVectorExtended[i] * timeRes) + PositionSequence[i - 1];
            PRTS_posFile.WriteLine(PositionSequence[i] + ",");
        }

        PRTS_posFile.Close();
        PRTS_velFile.Close();
        return PositionSequence;
    }

    public float[] TrapZVel(int NumOfPulses, float ti, float ta, float tv, float tx, float timeRes, float VelocityAmp)
    {
        DateTime datetime = DateTime.Now;
        dateString = datetime.ToString("dd-MM-yyyy_hh-mm");

        float Acc = VelocityAmp / ta;

        TextWriter TrapzV_posFile = new StreamWriter("log/Offline/TrapzV/TrapZV_Pos_" + dateString + ".txt");  // Write to file 
        TextWriter TrapzV_velFile = new StreamWriter("log/Offline/TrapzV/TrapzV_Vel_" + dateString + ".txt");  // Write to file 

        float T = ti + 4 * ta + 2 * tv + tx;
        int dt_Count = (int)Math.Round(T / timeRes);

        float[] VelocitySequence = new float[NumOfPulses * dt_Count];
        float[] PositionSequence = new float[NumOfPulses * dt_Count];

        PositionSequence[0] = 0.0f;
        TrapzV_posFile.WriteLine(PositionSequence[0] + ",");

        float t, tL;
        float x0;

        for (int k = 1; k <= PositionSequence.Length; k++)
        {
            t = k * timeRes;

            if (t >= 0 && t < ti)
            {
                tL = t;
                VelocitySequence[k] = 0.0f;
                PositionSequence[k] = 0.0f;
            }

            if (t >= ti && t < ti + ta)
            {
                tL = t - ti;
                VelocitySequence[k] = tL * Acc;
                PositionSequence[k] = 0.5f * Acc * tL * tL;
            }

            if (t >= ti + ta && t < ti + ta + tv)
            {
                tL = t - ti - ta;
                VelocitySequence[k] = VelocityAmp;
                x0 = 0.5f * Acc * ta * ta;
                PositionSequence[k] = x0 + VelocityAmp * tL;
            }

            if (t >= ti + ta + tv && t < ti + ta + tv + ta)
            {
                tL = t - ti - ta - tv;
                VelocitySequence[k] = -Acc * tL;
                x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv;
                PositionSequence[k] = x0 - 0.5f * Acc * tL * tL + VelocityAmp * tL;
            }

            if (t >= ti + ta + tv + ta && t < ti + ta + tv + ta + tx)
            {
                tL = t - (ti + ta + tv + ta);
                VelocitySequence[k] = 0.0f;
                //PositionSequence[k] = 0.5f * Acc * ta * ta + VelocityAmp * tv - 0.5f * Acc * (t - ti - ta - tv) * (t - ti - ta - tv) + VelocityAmp * (t - ti - ta - tv);
                PositionSequence[k] = 0.5f * Acc * ta * ta + VelocityAmp * tv - 0.5f * Acc * ta * ta + VelocityAmp * ta;
            }

            if (t >= ti + ta + tv + ta + tx && t < ti + ta + tv + ta + tx + ta)
            {
                tL = t - (ti + ta + tv + ta + tx);
                x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv - 0.5f * Acc * ta * ta + VelocityAmp * ta;
                VelocitySequence[k] = -Acc * tL;
                PositionSequence[k] = x0 - 0.5f * Acc * tL * tL;
            }

            if (t >= ti + ta + tv + ta + tx + ta && t < ti + ta + tv + ta + tx + ta + tv)
            {
                tL = t - (ti + ta + tv + ta + tx + ta);
                VelocitySequence[k] = -VelocityAmp;
                x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv;
                PositionSequence[k] = x0 - VelocityAmp * tL;
            }

            if (t >= ti + ta + tv + ta + tx + ta + tv && t <= ti + ta + tv + ta + tx + ta + tv + ta)
            {
                tL = t - (ti + ta + tv + ta + tx + ta + tv);
                x0 = 0.5f * Acc * ta * ta;
                VelocitySequence[k] = VelocityAmp * tL;
                PositionSequence[k] = x0 + 0.5f * Acc * tL * tL - VelocityAmp * tL;
            }

            TrapzV_posFile.WriteLine(PositionSequence[k] + ",");
            TrapzV_velFile.WriteLine(VelocitySequence[k] + ",");
        }

        TrapzV_posFile.Close();
        TrapzV_velFile.Close();
        return PositionSequence;
    }

}