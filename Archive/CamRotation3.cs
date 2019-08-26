using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;

public class CamRotation3 : MonoBehaviour
{
    public HeadTrackingInfo trackingInfo;
    private float xEuler, yEuler, zEuler, deltaXori, deltaXori_init, xEulerPrevious, xEulerInit;
    private float Time_0, Time_F;
    private int Counter;
    public float pertSignal;
    private float daqInput;
    private float dtVR;
    private float[] PosArray;
    private float TimeInit;
    private float Pos_min;
    private float Pos_max;
    private float P2P_Amp;
    private float Vel_max;
    private float frequency;
    private int NumPulses;
    private float minDeltaSec;
    private float minWSec;
    private float T_total; //seconds

    public Vector3 AnklePosition;
    private int flagExpNum;
    private static System.Random rndGen;

    // Randomized Sinusoids
    private float RS_RotationVelocity;
    // PRTS 
    private float PRTS_dt;
    // TrapV
    private float TrapV_ta;
    private float TrapV_tv;
    private float TrapV_tx;

    // Write to file 
    private TextWriter dataLogger;
    private TextWriter daqSignal;
    private TextWriter timeLog;
    // Date and time
    private DateTime dt;
    private string dateString;
    private string logFileName;

    // Use this for initialization
    void Start()
    {
        MCCDAQwrap.flashLED();
        MCCDAQwrap.writeVolts(1, 0.0f);  // Set all voltages to zero
        //Using Random Class
        rndGen = new System.Random();

        xEuler = 0.0f;
        yEuler = 0.0f;
        zEuler = 0.0f;
        deltaXori = 0.0f;
        Counter = 0;
        daqInput = 0.0f;
        pertSignal = 0.0f;
        AnklePosition = new Vector3(transform.position.x, transform.position.y - 1.65f, transform.position.z);

        // *******************************    EXPERIMENTS SETUP   *****************************
        flagExpNum = 4;  // 0: Normal VR  1: Randomized Half Sinusoidal  2:PRTS    3:TrapZ    4:TrapV
        Time_0 = 5.0f;   //Start perturbations after this time seconds
        dtVR = Time.fixedDeltaTime;  //Time resolution (time per frame) measured for VR device with FixedUpdate() method
        P2P_Amp = 5.0f; //deg
        Vel_max = 5.0f; //dps
        minDeltaSec = 1.5f; //Wait Time this is minDelta. delta = minDelta + rand(0, minDelta)
        minWSec = 1.5f; //Pulse Width, this is w. W = w + rand(0, w)
        NumPulses = 4; // or Periods

        switch (flagExpNum)
        {
            case 0: // No purturbation
                {
                    logFileName = "Normal";
                    PosArray = new float[(int)Math.Round(T_total / dtVR)];
                    Array.Clear(PosArray, 0, PosArray.Length);
                    T_total = 200.0f;//sec
                    break;
                }
            case 1: // *******************  1: Randomized half Sinusoids *********************
                {
                    logFileName = "RandHalfSin"; 
                    RS_RotationVelocity = Mathf.PI;    // A.Sin(wt) w = Rad per second;  default = pi / 2
                    frequency = RS_RotationVelocity / (2 * Mathf.PI);      //v = w/2pi   Hz
                    PosArray = InputFunctions.HalfSin_offline(NumPulses, P2P_Amp, RS_RotationVelocity, minDeltaSec, out T_total, dtVR).ToArray();
                    break;
                }
            case 2: // ***************************  2: PRTS  ******************************** 
                {
                    logFileName = "PRTS";
                    PRTS_dt = 0.1f; //sec
                    Vel_max = P2P_Amp / (17.0f * PRTS_dt);
                    T_total = 242 * PRTS_dt * NumPulses;
                    PosArray = InputFunctions.PRTS_offline(NumPulses, PRTS_dt, Vel_max, out T_total, dtVR).ToArray();
                    break;
                }
            case 3: // ***************************  2: TrapZ  ******************************** 
                {
                    logFileName = "TrapZ";
                    PosArray = InputFunctions.TrapZ_offline(NumPulses, P2P_Amp, Vel_max, minDeltaSec, minWSec, out T_total, dtVR).ToArray();
                    break;
                }
            case 4: // *************************   4: TrapZ Vel   ****************************
                {
                    logFileName = "TrapV";
                    TrapV_ta = 0.5f;
                    TrapV_tv = 4.0f;
                    TrapV_tx = 4.0f;
                    PosArray = (InputFunctions.TrapV_offline(NumPulses, minDeltaSec, TrapV_ta, TrapV_tv, TrapV_tx, Vel_max, out T_total, dtVR)).ToArray();
                    break;
                }
        }

        Time_F = Time_0 + T_total;
        Pos_max = PosArray.Max();
        Pos_min = PosArray.Min();

        Debug.Log("T_total = " + T_total + " Seconds");
        Debug.Log("Time Final = " + Time_F + " Seconds");

        // *******************   Write to file   *******************
        dt = DateTime.Now;
        dateString = dt.ToString("dd-MM_hh-mm");
        string logDir = "log/Online/" + logFileName;
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);
        dataLogger = new StreamWriter(logPath + "/VR_" + logFileName + dateString + ".txt");
        timeLog = new StreamWriter(logPath + "/Time_" + logFileName + dateString + ".txt");
        daqSignal = new StreamWriter(logPath + "/daq_" + logFileName + dateString + ".txt");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if ((Time.time > Time_0) && (Time.time < Time_F))  //Perturbation start and finish time
        {
            //print("Update time:" + Time.deltaTime);
            if (Counter == 0)
            {
                xEulerInit = trackingInfo.headOriX;
                xEulerPrevious = xEulerInit;
                TimeInit = Time.time;
            }

            if (Counter < PosArray.Length)
            {
                daqInput = (PosArray[Counter] - Pos_min) * (5.0f / (Pos_max - Pos_min));
                xEuler = xEulerInit + PosArray[Counter];
                MCCDAQwrap.writeVolts(1, daqInput);
                WriteLog(PosArray[Counter], daqInput, (Time.time - TimeInit));

                deltaXori = (xEuler - xEulerPrevious);
                if (deltaXori > 180) deltaXori = deltaXori - 360;
                if (deltaXori < -180) deltaXori = deltaXori + 360;
                //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                transform.RotateAround(AnklePosition, new Vector3(0, 0, 1), deltaXori);
            }
            xEulerPrevious = xEuler;
            Counter = Counter + 1;
        }
        else if (Time.time > Time_F)  { Debug.Log("VR Input Finished!!!");}
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