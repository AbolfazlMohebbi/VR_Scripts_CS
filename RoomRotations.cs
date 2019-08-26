using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public class RoomRotations : MonoBehaviour
{
    public Camera cameraObject;
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
    private string SceneName;

    // Randomized Sinusoids
    private float RS_RotationVelocity;
    // PRTS 
    private float PRTS_dt;
    // TrapV
    private float TrapV_ta;
    private float TrapV_tv;
    private float TrapV_tx;
    // Sum of Sins
    private float SOS_maxFreq;
    private float SOS_minFreq;
    private int SOS_freqCount;

    // Write to file 
    private TextWriter dataLogger;
    private TextWriter daqSignal;
    private TextWriter timeLog;
    private TextWriter headMotionLog;

    // Date and time
    private DateTime dt;
    private string logFileName;
    private string dateString;
    private string timeString;

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
        AnklePosition = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y - 1.65f, cameraObject.transform.position.z);

        // ******************************* Scene ***********************************
        SceneName = SceneManager.GetActiveScene().name;

        // *******************************    EXPERIMENTS SETUP   *****************************
        flagExpNum = 4;  // 0: Normal VR  1: Randomized Half Sinusoidal  2:PRTS    3:TrapZ    4:TrapV   5: Sum of Sins   6: PRBS

        Time_0 = 4.0f;   //Start perturbations after this time seconds
        dtVR = Time.fixedDeltaTime;  //Time resolution (time per frame) measured for VR device with FixedUpdate() method
        P2P_Amp = 5.0f; //deg
        Vel_max = 5.0f; //dps
        minDeltaSec = 2.5f; //Wait Time this is minDelta. delta = minDelta + rand(0, minDelta)
        minWSec = 1.5f; //Pulse Width, this is w. W = w + rand(0, w)
        NumPulses = 10; // or Periods

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
                    RS_RotationVelocity = Mathf.PI/2;    // A.Sin(wt) w = Rad per second;  default = pi / 2
                    frequency = RS_RotationVelocity / (2 * Mathf.PI);      //v = w/2pi   Hz
                    PosArray = InputFunctions.HalfSin_offline(NumPulses, P2P_Amp, RS_RotationVelocity, minDeltaSec, SceneName, out T_total, dtVR).ToArray();
                    break;
                }
            case 2: // ***************************  2: PRTS  ******************************** 
                {
                    logFileName = "PRTS";
                    PRTS_dt = 0.1f; //sec
                    Vel_max = P2P_Amp / (17.0f * PRTS_dt);
                    T_total = 242 * PRTS_dt * NumPulses;
                    PosArray = InputFunctions.PRTS_offline(NumPulses, PRTS_dt, Vel_max, SceneName, out T_total, dtVR).ToArray();
                    break;
                }
            case 3: // ***************************  2: TrapZ  ******************************** 
                {
                    logFileName = "TrapZ";
                    PosArray = InputFunctions.TrapZ_offline(NumPulses, P2P_Amp, Vel_max, minDeltaSec, minWSec, SceneName, out T_total, dtVR).ToArray();
                    break;
                }
            case 4: // *************************   4: TrapV   ********************************
                {
                    logFileName = "TrapV";
                    TrapV_ta = 0.5f;
                    TrapV_tv = 1.5f;
                    TrapV_tx = 3.0f;
                    P2P_Amp = Vel_max * (TrapV_ta + TrapV_tv);
                    PosArray = InputFunctions.TrapV_offline(NumPulses, minDeltaSec, TrapV_ta, TrapV_tv, TrapV_tx, Vel_max, SceneName, out T_total, dtVR).ToArray();
                    break;
                }
            case 5: // *************************  5: SumOfSin  *******************************
                {
                    logFileName = "SumOfSin";
                    SOS_minFreq = 0.05f;
                    SOS_maxFreq = 0.15f;
                    SOS_freqCount = 1;
                    PosArray = InputFunctions.SumOfSin_offline(NumPulses, P2P_Amp, SOS_minFreq, SOS_maxFreq, SOS_freqCount, SceneName, out T_total, dtVR).ToArray();
                    break;
                }
            case 6: // *************************  5: PRBS  *******************************
                {
                    logFileName = "PRBS";
                    T_total = 10.0f; //seconds
                    PosArray = InputFunctions.PRBS_offline(P2P_Amp, 0.5f, T_total, SceneName, dtVR).ToArray();
                    break;
                }
        }

        Time_F = Time_0 + T_total;
        Pos_max = PosArray.Max();
        Pos_min = PosArray.Min();

        Debug.Log(logFileName + " T_total = " + T_total + " Seconds");
        Debug.Log(logFileName + " Time Final = " + Time_F + " Seconds");
        Debug.Log(logFileName + " P2P_Amp = " + P2P_Amp + " Degrees"); 
        Debug.Log(logFileName + " Vel_max = " + Vel_max + " dps");

        // *******************   Write to file   *******************
        dt = DateTime.Now;
        dateString = dt.ToString("dd-MM-yyyy");
        timeString = dt.ToString("HH-mm");
        string logDir = "log/Online/" + dateString + "/" + SceneName + "/" + logFileName;
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);
        dataLogger = new StreamWriter(logPath + "/VR_" + logFileName + '_' + timeString + ".txt");
        timeLog = new StreamWriter(logPath + "/Time_" + logFileName + '_' + timeString + ".txt");
        daqSignal = new StreamWriter(logPath + "/daq_" + logFileName + '_' + timeString + ".txt");
        headMotionLog = new StreamWriter(logPath + "/HeadMotion_" + logFileName + '_' + timeString + ".txt");

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if ((Time.time > Time_0) && (Time.time < Time_F))  //Perturbation start and finish time
        {
            //print("Update time:" + Time.deltaTime);
            if (Counter == 0)
            {
                xEulerInit = transform.eulerAngles.x;
                xEulerPrevious = xEulerInit;
                TimeInit = Time.time;
            }

            if (Counter < PosArray.Length)
            {
                pertSignal = PosArray[Counter];
                daqInput = (PosArray[Counter] - Pos_min) * (5.0f / (Pos_max - Pos_min));
                xEuler = xEulerInit + PosArray[Counter];
                MCCDAQwrap.writeVolts(1, daqInput);
                WriteLog(PosArray[Counter], daqInput, (Time.time - TimeInit));
                WriteLogOri(trackingInfo.headTransl);

                deltaXori = (xEuler - xEulerPrevious);
                if (deltaXori > 180) deltaXori = deltaXori - 360;
                if (deltaXori < -180) deltaXori = deltaXori + 360;
                //transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                transform.RotateAround(AnklePosition, new Vector3(1, 0, 0), deltaXori);
            }
            xEulerPrevious = xEuler;
            Counter = Counter + 1;
        }
        else if (Time.time > Time_F)
        {
            Debug.Log("VR Input Finished!!!");
            MCCDAQwrap.writeVolts(1, 2.5f);
        }
    }

    void OnDestroy()
    {
        dataLogger.Close();
        timeLog.Close();
        daqSignal.Close();
        headMotionLog.Close();
    }

    public void WriteLog(float data, float daq, float time)
    {
        dataLogger.WriteLine(data + ",");
        daqSignal.WriteLine(daq + ",");
        timeLog.WriteLine(time + ",");
    }

    public void WriteLogOri(Vector3 Ori)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Ori[i] > 180) Ori[i] = Ori[i] - 360;
            if (Ori[i] < -180) Ori[i] = Ori[i] + 360;
        }

        headMotionLog.WriteLine();
        headMotionLog.Write(Ori[0] + ",");
        headMotionLog.Write(Ori[1] + ",");
        headMotionLog.Write(Ori[2]);
    }


}