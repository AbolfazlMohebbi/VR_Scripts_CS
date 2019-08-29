using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public class RotateCylinder : MonoBehaviour
{
    public HeadTrackingInfo trackingInfo;
    private float xEuler, yEuler, zEuler, deltaOri, xEulerPrevious, xEulerInit;
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
    private float P2P_Amp_calc;
    private float Vel_max;
    private float frequency;
    private int NumPulses;
    private float minDeltaSec;
    private float minWSec;
    private float T_total; //seconds

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
    private string dateString;
    private string timeString;
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
        deltaOri = 0.0f;
        Counter = 0;
        daqInput = 0.0f;
        pertSignal = 0.0f;

        // ******************************* Scene ***********************************
        SceneName = SceneManager.GetActiveScene().name;

        // *******************************    EXPERIMENTS SETUP   *****************************
        
        bool bVRgui = false;
        if (bVRgui)
        {
            //Load the GUI and wait until its finished
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = "VR_GUI";
            startInfo.FileName = "VR_GUI.exe";
            Process.Start(startInfo).WaitForExit();

            StreamReader csv_file =  new StreamReader(@"VR_GUI\\ExpParams.csv");
            string legends = csv_file.ReadLine();
            string Params = csv_file.ReadLine();
            csv_file.Close();
            string[] ParamsList = Params.Split(',');

            flagExpNum = (int)float.Parse(ParamsList[0]);  // 0: Normal VR  1: Randomized Half Sinusoidal  2:PRTS    3:TrapZ    4:TrapV   5: Sum of Sins   6: PRBS
            P2P_Amp = float.Parse(ParamsList[1]); //deg
            Vel_max = float.Parse(ParamsList[2]); //dps
            minDeltaSec = float.Parse(ParamsList[3]);  //Wait Time this is minDelta. delta = minDelta + rand(0, minDelta)
            minWSec = float.Parse(ParamsList[4]);  //Pulse Width, this is w. W = w + rand(0, w)
            NumPulses = (int)float.Parse(ParamsList[5]); // or Periods
            
            Time_0 = float.Parse(ParamsList[6]);   //Start perturbations after this time seconds
            T_total = float.Parse(ParamsList[7]);

            PRTS_dt = float.Parse(ParamsList[8]); //sec

            TrapV_ta = float.Parse(ParamsList[9]);
            TrapV_tv = float.Parse(ParamsList[10]);
            TrapV_tx = float.Parse(ParamsList[11]);

            SOS_minFreq = float.Parse(ParamsList[12]);
            SOS_maxFreq = float.Parse(ParamsList[13]);
            SOS_freqCount = (int)float.Parse(ParamsList[14]);
            Random_Direction = (int)float.Parse(ParamsList[15]);
            dtVR = Time.fixedDeltaTime;  //Time resolution (time per frame) measured for VR device with FixedUpdate() method
        }
        else
        {
            flagExpNum = 3;  // 0: Normal VR  1: Randomized Half Sinusoidal  2:PRTS    3:TrapZ    4:TrapV   5: Sum of Sins   6: PRBS

            Time_0 = 4.0f;   //Start perturbations after this time seconds
            dtVR = Time.fixedDeltaTime;  //Time resolution (time per frame) measured for VR device with FixedUpdate() method
            P2P_Amp = 10.0f; //deg
            Vel_max = 10.0f; //dps
            minDeltaSec = 2.5f; //Wait Time this is minDelta. delta = minDelta + rand(0, minDelta)
            minWSec = 2.5f; //Pulse Width, this is w. W = w + rand(0, w)
            NumPulses = 10; // or Periods

            PRTS_dt = 0.1f; //sec

            TrapV_ta = 0.8f;
            TrapV_tv = 1.2f;
            TrapV_tx = 2.0f;

            SOS_minFreq = 0.05f;
            SOS_maxFreq = 1.5f;
            SOS_freqCount = 15;
        }

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
                    RS_RotationVelocity = Mathf.PI / 2;    // A.Sin(wt) w = Rad per second;  default = pi / 2
                    frequency = RS_RotationVelocity / (2 * Mathf.PI);      //v = w/2pi   Hz
                    PosArray = InputFunctions.HalfSin_offline(NumPulses, P2P_Amp, RS_RotationVelocity, minDeltaSec, SceneName, out T_total, dtVR).ToArray();
                    P2P_Amp = P2P_Amp_calc;
                    break;
                }
            case 2: // ***************************  2: PRTS  ******************************** 
                {
                    logFileName = "PRTS";
                    PRTS_dt = 0.1f; //sec
                    Vel_max = P2P_Amp / (17.0f * PRTS_dt);
                    T_total = 242 * PRTS_dt * NumPulses;
                    PosArray = InputFunctions.PRTS_offline(NumPulses, PRTS_dt, Vel_max, SceneName, out T_total, dtVR).ToArray();
                    P2P_Amp = P2P_Amp_calc;
                    UnityEngine.Debug.Log(logFileName + " PRTS_dt = " + PRTS_dt + " Seconds");
                    break;
                }
            case 3: // ***************************  2: TrapZ  ******************************** 
                {
                    logFileName = "TrapZ";
                    PosArray = InputFunctions.TrapZ_offline(NumPulses, P2P_Amp, Vel_max, minDeltaSec, minWSec, SceneName, out T_total, dtVR).ToArray();
                    P2P_Amp = P2P_Amp_calc;
                    break;
                }
            case 4: // *************************   4: TrapV   ********************************
                {
                    logFileName = "TrapV";
                    TrapV_ta = 0.5f;
                    TrapV_tv = 1.5f;
                    TrapV_tx = 3.0f;
                    P2P_Amp_calc = Vel_max * (TrapV_ta + TrapV_tv);
                    PosArray = InputFunctions.TrapV_offline(NumPulses, minDeltaSec, TrapV_ta, TrapV_tv, TrapV_tx, Vel_max, SceneName, out T_total, dtVR).ToArray();
                    UnityEngine.Debug.Log( " TrapV Times: ta = " + TrapV_ta + " t_v = " + TrapV_tv + " t_x = " + TrapV_tx);
                    break;
                }
            case 5: // *************************  5: SumOfSin  *******************************
                {
                    logFileName = "SumOfSin";
                    SOS_minFreq = 0.05f;
                    SOS_maxFreq = 0.15f;
                    SOS_freqCount = 1;
                    PosArray = InputFunctions.SumOfSin_offline(NumPulses, P2P_Amp, SOS_minFreq, SOS_maxFreq, SOS_freqCount, SceneName, out T_total, dtVR).ToArray();
                    P2P_Amp = P2P_Amp_calc;
                    break;
                }
            case 6: // *************************  5: PRBS  *******************************
                {
                    logFileName = "PRBS";
                    T_total = 10.0f; //seconds
                    PosArray = InputFunctions.PRBS_offline(P2P_Amp, 0.5f, T_total, SceneName, dtVR).ToArray();
                    P2P_Amp = P2P_Amp_calc;
                    break;
                }
        }

        Time_F = Time_0 + T_total;
        Pos_max = PosArray.Max();
        Pos_min = PosArray.Min();

        UnityEngine.Debug.Log(logFileName + " P2P_Amp_calc = " + P2P_Amp_calc + " Degrees");
        UnityEngine.Debug.Log(logFileName + " Vel_max = " + Vel_max + " dps");
        UnityEngine.Debug.Log(logFileName + " Time Total = " + T_total + " Seconds");

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

                deltaOri = (xEuler - xEulerPrevious);
                if (deltaOri > 180) deltaOri = deltaOri - 360;
                if (deltaOri < -180) deltaOri = deltaOri + 360;
                transform.Rotate(new Vector3(deltaOri, yEuler, zEuler));
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