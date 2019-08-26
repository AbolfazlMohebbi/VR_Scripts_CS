using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;

static class InputFunctions
{
    public static float RemoveOriFlickering()
    {
        return 0.0f;
    }

    public static List<float> PRTS_offline(int NumOfPeriods, float SwitchingRate, float Velocity, string SceneName, out float T_total, float dtVR = 0.011111f)
    {
        System.Random rndGen = new System.Random();

        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/PRTS/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);

        float P2P_Amp = Velocity * (17.0f * SwitchingRate);        
        string spec = P2P_Amp.ToString() + "deg" + Velocity.ToString() + "dps";

        TextWriter posFile = new StreamWriter(logPath + "/PRTS_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 
        TextWriter velFile = new StreamWriter(logPath + "/PRTS_Vel_" + spec + "_" + timeString + ".txt");  // Write to file 

        int repeatCount = (int)Math.Round(SwitchingRate / dtVR);
        int[] ShiftRegisters = { 2, 0, 2, 0, 2 };
        int m = 5;

        int[] Velocity_ShiftRegisters = new int[NumOfPeriods * ((int)Math.Pow(3, m) - 1)]; // T = (3^m - 1).dt
        var VelocitySequence = new List<float>();
        var VelocitySequenceExtended = new List<float>();
        var PositionSequence = new List<float>();
        PositionSequence.Insert(0, 0.0f);

        var VelocityProfile = new List<float>();
        var PositionProfile = new List<float>();

        for (int k = 1; k <= Velocity_ShiftRegisters.Length; k++)
        {
            Velocity_ShiftRegisters[k - 1] = cmod((ShiftRegisters[2] - ShiftRegisters[3] - ShiftRegisters[4]), 3);
            ShiftRegisters[4] = ShiftRegisters[3];
            ShiftRegisters[3] = ShiftRegisters[2];
            ShiftRegisters[2] = ShiftRegisters[1];
            ShiftRegisters[1] = ShiftRegisters[0];
            ShiftRegisters[0] = Velocity_ShiftRegisters[k - 1];
            if (k % ((int)Math.Pow(3, m) - 1) == 1)
            {
                ShiftRegisters[0] = 0;
                ShiftRegisters[1] = 2;
                ShiftRegisters[2] = 0;
                ShiftRegisters[3] = 2;
                ShiftRegisters[4] = 0;
            }
        }

        // scale to -v and v:   0-->0  1-->v  2-->-v
        for (int k = 0; k < Velocity_ShiftRegisters.Length; k++)
        {
            if (Velocity_ShiftRegisters[k] == 1) { VelocitySequence.Insert(k, Velocity);}
            if (Velocity_ShiftRegisters[k] == 2) { VelocitySequence.Insert(k, -Velocity); } 
            if (Velocity_ShiftRegisters[k] == 0) { VelocitySequence.Insert(k, 0.0f); }
        }

        // repeat elements based on switching rate and update rate
        for (int k = 1; k <= VelocitySequence.Count; k++)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                VelocitySequenceExtended.Insert(repeatCount * (k - 1) + i, VelocitySequence[k - 1]);
                velFile.WriteLine(VelocitySequenceExtended[repeatCount * (k - 1) + i] + ",");
            }
        }

        // Integral of velocity = position
        for (int i = 1; i < (NumOfPeriods * repeatCount * ((int)Math.Pow(3, m) - 1)); i++)
        {
            PositionSequence.Insert(i, (VelocitySequenceExtended[i] * dtVR) + PositionSequence[i - 1]);
            posFile.WriteLine(PositionSequence[i] + ",");
        }

        T_total = 242 * SwitchingRate * NumOfPeriods;
        posFile.Close();
        velFile.Close();
        return PositionSequence;
    }

    public static string PRBS()
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

        if (period == Math.Pow(2, polynomial.Length) - 1)
        {
            Console.WriteLine("polynomial is maximal length");
        }
        else
        {
            Console.WriteLine("polynomial is not maximal length");
        }

        prbs_buf = prbs;
        for (int i = 1; i < 10; i++)
        {
            prbs = prbs + prbs_buf;
        }

        Console.WriteLine("period = " + period);
        Console.WriteLine("prbs = " + prbs);
        return prbs;
    }


    public static List<float> PRBS_offline(float Amplitude, float SwitchingRate, float T_total, string SceneName, float dtVR = 0.011111f, bool randomDirection = false)
    {
        System.Random rndGen = new System.Random();
        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/PRBS/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);

        string spec = Amplitude.ToString() + "deg";
        TextWriter posFile = new StreamWriter(logPath + "/PRBS_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 

        var PositionSequence = new List<float>();
        var PositionProfile = new List<float>();

        int dt_Count_total = 0;
        int dt_Count = (int)Math.Round(SwitchingRate / dtVR);
        char prbsElementChar;
        float PrbsElement, previousPrbsElement;
        int randBit;
        float T;

        if (randomDirection) { Amplitude = Amplitude/2; }

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

        if (period == Math.Pow(2, polynomial.Length) - 1)
        { Debug.Log("polynomial is maximal length");}
        else
        { Debug.Log("polynomial is not maximal length");}

        T = SwitchingRate * prbs.Length;
        prbs_buf = prbs;
        for (int i = 0; i < (int)Math.Round(T_total/T); i++)
        {
            prbs = prbs + prbs_buf;
        }

        for (int i = 0; i < (int)Math.Round(T_total / SwitchingRate); i++) //number of pulses
        {
            prbsElementChar = prbs[i];
            PrbsElement = (float)Convert.ToDouble(new string(prbsElementChar, 1));
            if (randomDirection) { randBit = (3 - 2 * rndGen.Next(1, 3)); }
            else { randBit = 1; }

            for (int k = 0; k < dt_Count; k++)
            {
                PositionProfile.Insert(dt_Count_total + k, randBit * PrbsElement * Amplitude);
                posFile.WriteLine(PositionProfile[dt_Count_total + k] + ",");
            }
            dt_Count_total = dt_Count_total + dt_Count;
        }
        posFile.Close();
        return PositionProfile;
    }

    public static List<float> SumOfSin_offline(int numPeriods, float P2P_Amp, float minFreq, float maxFreq, int freqCount, string SceneName, out float T_total, float dtVR = 0.011111f, bool randomPhase = true)
    {
        System.Random rndGen = new System.Random();
        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/SumOfSin/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);

        string spec = P2P_Amp.ToString() + "deg_" + maxFreq.ToString() + "Hz";
        TextWriter posFile = new StreamWriter(logPath + "/SumOfSin_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 

        int dt_Count;
        int dt_Count_total = 0;
        float T, t;


        float fundFreq;
        if (freqCount == 1)
        { fundFreq = minFreq; }
        else { fundFreq = (maxFreq - minFreq) / (freqCount - 1); }

        float Freq = 0.0f;
        float result = 0.0f;
        T = 1.0f / minFreq;
        dt_Count = (int)Math.Round(T / dtVR);

        var PositionProfile = new List<float>();
        var PositionProfileAmped = new List<float>();

        float[] randPhase = new float[freqCount];
        for (int i = 0; i < freqCount; i++)
        {
            if (randomPhase)
            { randPhase[i] = (float)(2.0 * (float)Math.PI * rndGen.NextDouble()); }
            else
            { randPhase[i] = 0.0f; }// in case we don't want random phases
        }

        for (int i = 0; i < numPeriods; i++)
        {
            for (int k = 0; k < dt_Count; k++)
            {
                t = k * dtVR;
                for (int j = 0; j < freqCount; j++)
                {
                    Freq = minFreq + (j * fundFreq);
                    result = result + (float)Math.Sin(Freq * 2.0f * (float)Math.PI * t + randPhase[j]);
                }
                PositionProfile.Insert(dt_Count_total + k, result);
                //posFile.WriteLine(PositionProfile[dt_Count_total + k] + ",");
                result = 0.0f;
            }
            dt_Count_total = dt_Count_total + dt_Count;
        }

        for (int j = 0; j < numPeriods * dt_Count; j++)
        {
            PositionProfileAmped.Insert(j, PositionProfile[j] * P2P_Amp / (PositionProfile.Max() - PositionProfile.Min()));
            posFile.WriteLine(PositionProfileAmped[j] + ",");
        }

        posFile.Close();
        T_total = numPeriods * T;
        return PositionProfile;
    }

    public static List<float> HalfSin_offline(int NumOfPulses, float RS_Amplitude, float RS_RotationVelocity, float minDeltaSec, string SceneName, out float T_total, float dtVR = 0.011111f, bool RandomDirection = true)
    {
        System.Random rndGen = new System.Random();
        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/HalfSin/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);

        string spec = RS_Amplitude.ToString() + "deg" + RS_RotationVelocity.ToString() + "dps";
        TextWriter posFile = new StreamWriter(logPath + "/HalfSin_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 
        TextWriter velFile = new StreamWriter(logPath + "/HalfSin_Vel_" + spec + "_" + timeString + ".txt");  // Write to file 

        int dt_Count;
        int dt_Count_total = 0;
        int randBit;
        float T, ti, ts, t;
        ts = Mathf.PI / RS_RotationVelocity;

        // RS_RotationVelocity   // A.Sin(wt) w = Rad per second;  default = pi / 2
        float RS_frequency = RS_RotationVelocity / (2 * Mathf.PI);      //v = w/2pi   Hz

        var VelocitySequence = new List<float>();
        var PositionSequence = new List<float>();
        var VelocityProfile = new List<float>();
        var PositionProfile = new List<float>();

        for (int i = 0; i < NumOfPulses; i++)
        {
            randBit = (3 - 2 * rndGen.Next(1, 3));
            if (!RandomDirection) { ti = minDeltaSec; } else { ti = minDeltaSec * (1.0f + (float)rndGen.NextDouble()); }
            T = ti + ts;
            dt_Count = (int)Math.Round(T / dtVR);

            for (int k = 0; k < dt_Count; k++)
            {
                t = k * dtVR;

                if (t >= 0 && t < ti)
                {
                    PositionSequence.Insert(k, 0.0f);
                    VelocitySequence.Insert(k, 0.0f);
                }

                if (t >= ti && t <= ti + ts)
                {
                    PositionSequence.Insert(k, RS_Amplitude * Mathf.Sin(RS_RotationVelocity * (t - ti)));
                    VelocitySequence.Insert(k, RS_Amplitude * RS_RotationVelocity * Mathf.Cos(RS_RotationVelocity * (t - ti)));
                }

                PositionProfile.Insert(dt_Count_total + k, randBit * PositionSequence[k]);
                VelocityProfile.Insert(dt_Count_total + k, randBit * VelocitySequence[k]);

                posFile.WriteLine(PositionProfile[dt_Count_total + k] + ",");
                velFile.WriteLine(VelocityProfile[dt_Count_total + k] + ",");
            }
            dt_Count_total = dt_Count_total + dt_Count;
        }

        posFile.Close();
        velFile.Close();

        T_total = dt_Count_total * dtVR;
        return PositionProfile;
    }

    public static List<float> TrapZ_offline(int NumOfPulses, float TrapZ_Amp, float TrapZ_Vel, float minDeltaSec, float minWSec, string SceneName, out float T_total, float dtVR = 0.011111f, bool RandomDirection = true)
    {
        System.Random rndGen = new System.Random();
        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/TrapZ/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);

        string spec = TrapZ_Amp.ToString() + "deg" + TrapZ_Vel.ToString() + "dps";

        TextWriter posFile = new StreamWriter(logPath + "/TrapZ_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 
        TextWriter velFile = new StreamWriter(logPath + "/TrapZ_Vel_" + spec + "_" + timeString + ".txt");  // Write to file 

        int dt_Count;
        int dt_Count_total = 0;
        int randBit;
        float T, td, tw, t;

        //float TrapZ_Amp = TrapZ_P2PAmp / 2.0f; //degrees
        float dt = TrapZ_Amp / TrapZ_Vel;

        var VelocitySequence = new List<float>();
        var PositionSequence = new List<float>();
        var VelocityProfile = new List<float>();
        var PositionProfile = new List<float>();

        for (int i = 0; i < NumOfPulses; i++)
        {
            randBit = (3 - 2 * rndGen.Next(1, 3));
            if (!RandomDirection)
            {
                td = minDeltaSec;
                tw = minWSec;
            }
            else
            {
                td = minDeltaSec * (1.0f + (float)rndGen.NextDouble());
                tw = minWSec * (1.0f + (float)rndGen.NextDouble());
            }

            T = td + tw + 2.0f * dt;
            dt_Count = (int)Math.Round(T / dtVR);

            for (int k = 0; k < dt_Count; k++)
            {
                t = k * dtVR;

                if (t >= 0 && t < td)
                {
                    PositionSequence.Insert(k, 0.0f);
                    VelocitySequence.Insert(k, 0.0f);
                }

                if (t >= td && t <= td + dt)
                {
                    PositionSequence.Insert(k, TrapZ_Vel * (t - td));
                    VelocitySequence.Insert(k, TrapZ_Vel);
                }

                if (t >= td+dt && t <= td + dt + tw)
                {
                    PositionSequence.Insert(k, TrapZ_Amp);
                    VelocitySequence.Insert(k, 0);
                }

                if (t >= td + dt + tw && t <= td + dt + tw + dt)
                {
                    PositionSequence.Insert(k, TrapZ_Amp - TrapZ_Vel * (t - (td + dt + tw) ));
                    VelocitySequence.Insert(k, -TrapZ_Vel);
                }

                PositionProfile.Insert(dt_Count_total + k, randBit * PositionSequence[k]);
                VelocityProfile.Insert(dt_Count_total + k, randBit * VelocitySequence[k]);

                posFile.WriteLine(PositionProfile[dt_Count_total + k] + ",");
                velFile.WriteLine(VelocityProfile[dt_Count_total + k] + ",");
            }
            dt_Count_total = dt_Count_total + dt_Count;
        }
        posFile.Close();
        velFile.Close();

        T_total = dt_Count_total * dtVR;
        return PositionProfile;
    }

    public static float[] TrapV_offline_array(int NumOfPulses, float ti, float ta, float tv, float tx, float VelocityAmp, string SceneName, out float T_total, float dtVR = 0.011111f)
    {
        System.Random rndGen = new System.Random();
        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/TrapV/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);

        float P2P_Amp = VelocityAmp * (ta + tv);
        string spec = P2P_Amp.ToString() + "deg" + VelocityAmp.ToString() + "dps";

        TextWriter posFile = new StreamWriter(logPath + "/TrapV_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 
        TextWriter velFile = new StreamWriter(logPath + "/TrapV_Vel_" + spec + "_" + timeString + ".txt");  // Write to file 

        float Acc = VelocityAmp / ta;
        T_total = ti + 4 * ta + 2 * tv + tx;
        float t, tL;
        float x0;
        int dt_Count = (int)Math.Round(T_total / dtVR);

        int randBit;
        float[] VelocitySequence = new float[dt_Count];
        float[] PositionSequence = new float[dt_Count];
        float[] VelocityProfile = new float[dt_Count * NumOfPulses];
        float[] PositionProfile = new float[dt_Count * NumOfPulses];

        for (int k = 0; k < dt_Count; k++)
        {
            t = k * dtVR;

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
                VelocitySequence[k] = VelocityAmp - Acc * tL;
                x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv;
                PositionSequence[k] = x0 - 0.5f * Acc * tL * tL + VelocityAmp * tL;
            }

            if (t >= ti + ta + tv + ta && t < ti + ta + tv + ta + tx)
            {
                tL = t - (ti + ta + tv + ta);
                VelocitySequence[k] = 0.0f;
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
                VelocitySequence[k] = -VelocityAmp + Acc * tL;
                PositionSequence[k] = x0 + 0.5f * Acc * tL * tL - VelocityAmp * tL;
            }
        }

        for (int i = 0; i < NumOfPulses; i++)
        {
            randBit = (3 - 2 * rndGen.Next(1, 3));
            for (int k = 0; k < dt_Count; k++)
            {
                PositionProfile[i * dt_Count + k] = randBit * PositionSequence[k];
                VelocityProfile[i * dt_Count + k] = randBit * VelocitySequence[k];

                posFile.WriteLine(PositionProfile[i * dt_Count + k] + ",");
                velFile.WriteLine(VelocityProfile[i * dt_Count + k] + ",");
            }
        }
        posFile.Close();
        velFile.Close();

        return PositionProfile;
    }

    public static List<float> TrapV_offline(int NumOfPulses, float ti_fix, float ta, float tv, float tx, float VelocityAmp, string SceneName, out float T_total, float dtVR = 0.011111f, bool RandomDirection = true)
    {
        DateTime datetime = DateTime.Now;
        string dateString = datetime.ToString("dd-MM-yyyy");
        string timeString = datetime.ToString("HH-mm");
        string logDir = "log/Offline/" + dateString + "/" + SceneName + "/TrapV/";
        string currentDir = Directory.GetCurrentDirectory();
        string logPath = Path.Combine(currentDir, logDir);
        Directory.CreateDirectory(logPath);
        System.Random rndGen = new System.Random();

        float P2P_Amp = VelocityAmp * (ta + tv);
        string spec = P2P_Amp.ToString() + "deg" + VelocityAmp.ToString() + "dps";

        TextWriter posFile = new StreamWriter(logPath + "/TrapV_Pos_" + spec + "_" + timeString + ".txt");  // Write to file 
        TextWriter velFile = new StreamWriter(logPath + "/TrapV_Vel_" + spec + "_" + timeString + ".txt");  // Write to file 

        float Acc;
        float T;
        float t, tL;
        float ti = 0.0f;
        float x0;
        int dt_Count;
        int dt_Count_total = 0;
        int randBit;

        var VelocitySequence = new List<float>();
        var PositionSequence = new List<float>();
        var VelocityProfile = new List<float>();
        var PositionProfile = new List<float>();

        for (int i = 0; i < NumOfPulses; i++)
        {
            //randBit = RandomBit();
            randBit = (3 - 2 * rndGen.Next(1, 3));

            if (!RandomDirection) { ti = ti_fix; } else { ti = ti_fix * (1.0f + (float)rndGen.NextDouble()); }
            T = ti + 4 * ta + 2 * tv + tx;
            dt_Count = (int)Math.Round(T / dtVR);
            Acc = VelocityAmp / ta;

            for (int k = 0; k < dt_Count; k++)
            {
                t = k * dtVR;

                if (t >= 0 && t < ti)
                {
                    tL = t;
                    VelocitySequence.Insert(k, 0.0f);
                    PositionSequence.Insert(k, 0.0f);
                }

                if (t >= ti && t < ti + ta)
                {
                    tL = t - ti;
                    VelocitySequence.Insert(k, tL * Acc);
                    PositionSequence.Insert(k, 0.5f * Acc * tL * tL);
                }

                if (t >= ti + ta && t < ti + ta + tv)
                {
                    tL = t - ti - ta;
                    x0 = 0.5f * Acc * ta * ta;
                    VelocitySequence.Insert(k, VelocityAmp);
                    PositionSequence.Insert(k, (x0 + VelocityAmp * tL));
                }

                if (t >= ti + ta + tv && t < ti + ta + tv + ta)
                {
                    tL = t - ti - ta - tv;
                    x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv;
                    VelocitySequence.Insert(k, VelocityAmp - Acc * tL);
                    PositionSequence.Insert(k, x0 - 0.5f * Acc * tL * tL + VelocityAmp * tL);
                }

                if (t >= ti + ta + tv + ta && t < ti + ta + tv + ta + tx)
                {
                    tL = t - (ti + ta + tv + ta);
                    VelocitySequence.Insert(k, 0.0f);
                    PositionSequence.Insert(k, 0.5f * Acc * ta * ta + VelocityAmp * tv - 0.5f * Acc * ta * ta + VelocityAmp * ta);
                }

                if (t >= ti + ta + tv + ta + tx && t < ti + ta + tv + ta + tx + ta)
                {
                    tL = t - (ti + ta + tv + ta + tx);
                    x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv - 0.5f * Acc * ta * ta + VelocityAmp * ta;
                    VelocitySequence.Insert(k, -Acc * tL);
                    PositionSequence.Insert(k, x0 - 0.5f * Acc * tL * tL);
                }

                if (t >= ti + ta + tv + ta + tx + ta && t < ti + ta + tv + ta + tx + ta + tv)
                {
                    tL = t - (ti + ta + tv + ta + tx + ta);
                    x0 = 0.5f * Acc * ta * ta + VelocityAmp * tv;
                    VelocitySequence.Insert(k, -VelocityAmp);
                    PositionSequence.Insert(k, x0 - VelocityAmp * tL);
                }

                if (t >= ti + ta + tv + ta + tx + ta + tv && t <= ti + ta + tv + ta + tx + ta + tv + ta)
                {
                    tL = t - (ti + ta + tv + ta + tx + ta + tv);
                    x0 = 0.5f * Acc * ta * ta;
                    VelocitySequence.Insert(k, -VelocityAmp + Acc * tL);
                    PositionSequence.Insert(k, x0 + 0.5f * Acc * tL * tL - VelocityAmp * tL);
                }

                PositionProfile.Insert(dt_Count_total + k, randBit * PositionSequence[k]);
                VelocityProfile.Insert(dt_Count_total + k, randBit * VelocitySequence[k]);

                posFile.WriteLine(PositionProfile[dt_Count_total + k] + ",");
                velFile.WriteLine(VelocityProfile[dt_Count_total + k] + ",");
            }
            dt_Count_total = dt_Count_total + dt_Count;
        }

        posFile.Close();
        velFile.Close();

        T_total = dt_Count_total * dtVR;
        return PositionProfile;
    }

    public static int RandomBit()
    {
        System.Random rndGenerator = new System.Random();
        return (3 - 2 * rndGenerator.Next(1, 3));
    }

    public static float ToDegree(float theta)
    {
        float x = theta * (180.0f / 3.14f);
        return x;
    }

    public static float ToRadian(float xx)
    {
        float theta = xx * (3.14f / 180.0f);
        return theta;
    }

    public static int cmod(int a, int n)
    {
        int result = a % n;
        if ((result < 0 && n > 0) || (result > 0 && n < 0))
        {
            result += n;
        }
        return result;
    }

}
