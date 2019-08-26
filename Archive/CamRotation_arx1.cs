using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;


public class CamRotation1 : MonoBehaviour
{

    public HeadTrackingInfo trackingInfo;
    private Quaternion startRot;
    private float xEuler, yEuler, zEuler, deltaXori, deltaXori_init, xEulerPrevious, xEulerInit;
    private float Time_0, Time_F;
    private float toRad, toDeg;
    private int Counter;
    private string prbsRes;
    private float pi;


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
    private int iCount;
    private int PrbsElementInt, previousPrbsElementInt;
    private float PrbsElement, previousPrbsElement;
    private int PRBS_randBit;
    private float prbsCurAmp;
    private float PRBS_XOri;

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
    private float SS_Freq;
    private float SS_maxFreq;
    private float SS_maxRotationVelocity;
    private float SS_minFreq;
    private float SS_minRotationVelocity;
    private float SS_freqResolution;
    private float SS_RotationVelocityResolution;
    private int SS_freqCount;
    private float SS_randPhase;
    private float[] SS_Phase;
    private float SS_TimeTotal; //sec
    private float SS_TimeRes; //20 ms
    private float[] SS_result;
    private float SS_maxValue;

    private int flagExpNum;  // 0: Normal VR  1: Disabled Head-tracking  2: Magnify Rotation  3: Step-response (5 and 10 deg.) 4: Sinusoidal Perturbations 
                             // 5: PRBS  6: Randomized Step  7: Randomized Sinusoidal 8: Sum of Sinusoids

    private static System.Random rndGen;

    // Write to file 
    private TextWriter tw;
    private TextWriter daqSignal;
    private TextWriter timeW;

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
        iCount = 0;
        prbsRes = PRBS_General();
        startRot = transform.rotation;
        Counter = 0;
        toRad = (3.14f / 180.0f);
        toDeg = (180.0f / 3.14f);
        pi = (float)Math.PI;

        // ******************  EXPERIMENTS SETUP  ******************
        flagExpNum = 4;  // 0: Normal VR  1: Disabled Head-tracking  2: Magnify Rotation  3: Step-response (5 and 10 deg.) 4: Sinusoidal Perturbations 
                         // 5: PRBS  6: Randomized Step  7: Randomized Sinusoidal  8: Sum of Sinusoids
        Time_0 = 4.0f;   //Start perturbations after this time seconds
        Time_F = 130.0f;

        // ******************  STEP RESPONSE PARAMETERS ******************
        stepTimeFrame = 0;
        stepStartTimeFramePrevious = 0;

        // each frame = 20ms or 0.020 sec
        stepStartTimeMS = rndGen.Next(0, 4000);   // Step happens at a random time between 0 to 4000 miliseconds
        stepStartTimeFrame = stepStartTimeMS / 20; //miliseconds
        print("stepStartTimeFrame is:  " + stepStartTimeFrame + "    and stepStartTimeMS is:  " + stepStartTimeMS);
        stepAmpilitude = 10.0f; //Degrees (5 - 10 - 15 deg.)

        stepDurationMS = 1000; //miliseconds
        stepTimeFrameDuration = stepDurationMS / 20; //frames
        stepSteadyStateTimeMS = 1000; //miliseconds
        stepSteadyStateTimeFrame = stepSteadyStateTimeMS / 20;

        // ******************  SINUSOIDAL PARAMETERS  ********************
        Sin_Amplitude = 2.0f;
        Sin_RotationVelocity = pi / 2;    // A.Sin(wt) w = Rad per second
        Sin_frequency = Sin_RotationVelocity / (2 * pi);      //v = w/2pi   Hz

        // ********************  PRBS PARAMETERS  ************************
        prbsAmpilitude = 15.0f; //Degrees (5 - 10 - 15 deg.)
        prbsDurationMS = 200;  //miliseconds
        prbsTimeFrameDuration = prbsDurationMS / 20;
        PRBS_randBit = 1;

        // *******************  Randomized STEP *********************
        randomDirectionON = true;

        // *******************  Randomized Sinusoids *********************
        RS_pulsedone = true;
        RS_SteadyStateTimeSec = 0.5; //Seconds default = 1.4sec;
        RS_randBit = 1;
        RS_Amplitude = 10.0f;
        RS_RotationVelocity = pi;    // A.Sin(wt) w = Rad per second;  default = pi / 2
        RS_frequency = Sin_RotationVelocity / (2 * pi);      //v = w/2pi   Hz

        // *******************  Sum of Sinusoids *********************        
        SS_TimeTotal = 120.0f; //sec
        SS_TimeRes = 0.020f; //20 ms
        SS_Amplitude = 4.0f; //degrees
        SS_maxFreq = 3.0f; //Hz 5.0
        SS_maxRotationVelocity = (2 * pi) * SS_maxFreq; // w = 2pi * v
        SS_minFreq = 0.1f; //Hz 0.05
        SS_minRotationVelocity = (2 * pi) * SS_minFreq;
        SS_freqCount = 29;
        SS_freqResolution = (SS_maxFreq - SS_minFreq) / (SS_freqCount-1);
        SS_RotationVelocityResolution = (SS_maxRotationVelocity - SS_minRotationVelocity) / (SS_freqCount-1);
        SS_Freq = 0.0f;
        sinSum = 0.0f;

        SS_result = new float[(int)Math.Round(60.0f/0.020f)];
        Array.Clear(SS_result, 0, SS_result.Length);
        SS_result = SumOfSin(SS_Amplitude, SS_minFreq, SS_maxFreq, SS_freqCount, SS_TimeTotal, SS_TimeRes);

        if (SS_result.Max() <= Math.Abs(SS_result.Min())) { SS_maxValue = Math.Abs(SS_result.Min()); }
        else { SS_maxValue = SS_result.Max(); }

        // *******************   Write to file   *******************
        tw = new StreamWriter("VR_Results.txt");
        timeW = new StreamWriter("Time.txt");
        daqSignal = new StreamWriter("daq_Signals.txt");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //time_f = Time.time;
        //timediff = time_f - time_prev;
        //print("timediff:  " + timediff);
        //time_prev = time_f;

        if ((Time.time > Time_0) && (Time.time < Time_F) )  //Perturbation start and finish time
        {
            //MCCDAQwrap.writeVolts(1, 5.0f);   // Send start vision perturbation signal of 5v to channel 1.            

            switch (flagExpNum)
            {
                case 0: // No purturbation
                    {
                        break;
                    }

                case 1:   //head tracking disabled
                    {
                        transform.rotation = startRot * Quaternion.Inverse(InputTracking.GetLocalRotation(VRNode.CenterEye));
                        //transform.rotation = startRot * Quaternion.Inverse(UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.CenterEye));
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
                            transform.Rotate(new Vector3(-stepAmpilitude, yEuler, zEuler));
                            MCCDAQwrap.writeVolts(1, 5.0f);
                        }

                        if (stepTimeFrame == stepStartTimeFrame + stepTimeFrameDuration)
                        {
                            transform.Rotate(new Vector3(stepAmpilitude, yEuler, zEuler));
                            MCCDAQwrap.writeVolts(1, 0.0f);
                        }
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

                        xEuler = xEulerInit + (Sin_Amplitude * (float)Math.Sin(Sin_RotationVelocity * (Time.time - RS_TimeInit)));
                        tw.WriteLine(Sin_Amplitude * (float)Math.Sin(Sin_RotationVelocity * (Time.time - RS_TimeInit)) + ",");
                        timeW.WriteLine((Time.time - Time_0) + ",");
                        MCCDAQwrap.writeVolts(1, 2.5f + 2.5f * (float)Math.Sin(Sin_RotationVelocity * (Time.time - RS_TimeInit)));

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;
                        transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 5: //PRBS
                    {
                        Counter = Counter + 1;

                        if (Counter % prbsTimeFrameDuration == 0)  //done at each prbsTimeFrameDuration
                        {
                            if (iCount < prbsRes.Length)
                            {
                                char prbsElementChar = prbsRes[iCount];
                                PrbsElementInt = Convert.ToInt32(new string(prbsElementChar, 1));
                                PrbsElement = (float)Convert.ToDouble(new string(prbsElementChar, 1)) - 0.5f;
                                print("PrbsElement:  " + PrbsElement);

                                prbsCurAmp = (PrbsElement - previousPrbsElement) * prbsAmpilitude;
                                transform.Rotate(new Vector3(prbsCurAmp, yEuler, zEuler));
                                MCCDAQwrap.writeVolts(1, PrbsElementInt*5.0f);
                                previousPrbsElement = PrbsElement;
                                iCount = iCount + 1;
                            }
                        }
                        else
                        {
                            PrbsElement = previousPrbsElement; // For the sake of logging!
                        }
                        tw.WriteLine((float)PrbsElementInt - 0.5f + ",");
                        timeW.WriteLine((Time.time - Time_0) + ",");
                        break;
                    }

                case 6: // Randomized Step-response (5 - 10 - 15 deg.)
                    {
                        // each frame = 16ms or 0.016 sec
                        stepTimeFrame = stepTimeFrame + 1;

                        if (stepTimeFrame == stepStartTimeFrame)
                        {
                            stepAmpilitude = RandomBit() * stepAmpilitude;
                            transform.Rotate(new Vector3(-stepAmpilitude, yEuler, zEuler));
                            MCCDAQwrap.writeVolts(1, 5.0f);
                            stepStartTimeFrame = stepStartTimeFramePrevious + stepTimeFrameDuration + stepSteadyStateTimeFrame + rndGen.Next(0, stepSteadyStateTimeFrame);
                            print("stepStartTimeFrame" + stepStartTimeFrame);
                        }

                        if (stepTimeFrame == stepStartTimeFramePrevious + stepTimeFrameDuration)
                        {
                            transform.Rotate(new Vector3(stepAmpilitude, yEuler, zEuler));
                            MCCDAQwrap.writeVolts(1, 0.0f);
                            stepStartTimeFramePrevious = stepStartTimeFrame;
                        }
                        break;
                    }

                case 7: //Randomized Sinusoidal Perturbations
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

                            print("RS_t0: " + RS_t0);
                            print("ts: " + RS_ts);
                            print("t: " + (Time.time - RS_t0));
                        }

                        RS_t = Time.time - RS_t0;

                        if (RS_t < RS_ts) xEuler = xEulerInit;
                        if ((RS_t >= RS_ts) && (RS_t <= RS_ts + (pi / RS_RotationVelocity)))
                        {
                            xEuler = xEulerInit + (RS_randBit * RS_Amplitude * (float)Math.Sin(RS_RotationVelocity * (RS_t - RS_ts)));
                            MCCDAQwrap.writeVolts(1, 5.0f * (float)Math.Sin(RS_RotationVelocity * (RS_t - RS_ts)));
                        }
                        if (RS_t > RS_ts + (pi / RS_RotationVelocity))
                        {
                            RS_pulsedone = true;
                            xEuler = xEulerInit;
                        }

                        tw.WriteLine(xEuler + ",");
                        timeW.WriteLine((Time.time - RS_TimeInit) + ",");

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;
                        transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }


                case 8: //Sum of Sinusoids Perturbations - Normalized
                    {
                        sinSum = 0.0f;
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            SS_TimeInit = Time.time;
                        }

                        sinSum = (SS_result[Counter] / SS_maxValue) * SS_Amplitude * 0.5f;

                        tw.WriteLine(sinSum + ",");
                        timeW.WriteLine((Time.time - SS_TimeInit) + ",");
                        daqSignal.WriteLine((((SS_result[Counter] / SS_maxValue) + 1.0f) * 2.5f) + ",");

                        xEuler = xEulerInit + sinSum;
                        MCCDAQwrap.writeVolts(1, (((SS_result[Counter] / SS_maxValue) + 1.0f) * 2.5f) );

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;
                        transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }

                case 9: //Sum of Sinusoids Perturbations - Not Normalized
                    {
                        sinSum = 0.0f;
                        if (Counter == 0)
                        {
                            xEulerInit = trackingInfo.headOriX;
                            xEulerPrevious = xEulerInit;
                            SS_TimeInit = Time.time;
                        }

                        for (int fi = 0; fi < SS_freqCount; fi++)
                        {
                            SS_Freq = SS_minFreq + fi*SS_freqResolution;
                            //print("SS_Phase[i] = " + SS_Phase[fi]);
                            sinSum = sinSum + (SS_Amplitude * (float)Math.Sin(SS_Freq * 2.0f * pi * (Time.time - SS_TimeInit) + SS_Phase[fi]));
                        }

                        tw.WriteLine(sinSum + ",");
                        timeW.WriteLine((Time.time - SS_TimeInit) + ",");

                        sinSum = sinSum / 11.0f * SS_Amplitude;
                        xEuler = xEulerInit + sinSum;
                       
                        MCCDAQwrap.writeVolts(1, 2.5f + (2.5f * sinSum * 0.02f));

                        deltaXori = (xEuler - xEulerPrevious);
                        if (deltaXori > 180) deltaXori = deltaXori - 360;
                        if (deltaXori < -180) deltaXori = deltaXori + 360;
                        transform.Rotate(new Vector3(deltaXori, yEuler, zEuler));
                        xEulerPrevious = xEuler;

                        Counter = Counter + 1;
                        break;
                    }


            }

        }
    }

    void OnDestroy()
    {
        tw.Close();
        timeW.Close();
        daqSignal.Close();
    }

    public int RandomBit()
    {
        if (randomDirectionON == true)
        {
            randomDirection = 3 - 2 * rndGen.Next(1, 3);
            print("randomDirection = " + randomDirection);
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

    public float[] SumOfSin(float Amplitude, float minFreq, float maxFreq, int freqCount, float Ttotal, float frameT)
    {

        float[] randPhase = new float[freqCount];
        float[] result = new float[(int)Math.Round(Ttotal/frameT)];
        Array.Clear(result, 0, result.Length);
        float freqResolution = (maxFreq - minFreq) / (freqCount - 1);
        float Freq = 0.0f;
        TextWriter SOS_file = new StreamWriter("VR_SumOfSin.txt");  // Write to file 
        SOS_file.WriteLine("SumOfSinRes = [");

        for (int i = 0; i < freqCount; i++)
        {
            randPhase[i] = (float)(2.0 * pi * rndGen.NextDouble());
        }

        for (int j = 0; j < result.Length; j++)
        {
            for (int i = 0; i < freqCount; i++)
            {
                Freq = minFreq + (i * freqResolution);
                //print("Freq: " + Freq);
                result[j] = result[j] + (Amplitude * (float)Math.Sin(Freq * 2.0f * pi * (j* frameT) + randPhase[i]));
            }
            SOS_file.WriteLine(result[j] + ",");
            //print("result[" + j + "] = " + result[j]);
        }

        SOS_file.WriteLine("];");
        SOS_file.Close();
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

}