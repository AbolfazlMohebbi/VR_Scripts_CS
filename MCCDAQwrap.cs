using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.VR;
using System;
using System.IO;
using System.Linq;


public class MCCDAQwrap
{
 //------------------------------------------------------------------------CONSTANTS:
 
    private const string LOG_TAG = "MCCDAQ";
    private const bool VERBOSE = false;

    // Types of configuration
    private static int GLOBALINFO = 1;
    private static int BOARDINFO = 2;

    // Types of board configuration info
    private static int BIRANGE = 6;    // Switch selectable range
    private static int BICLOCK = 5;    // Switch selectable range

    // Available ranges
    private static int BIP10VOLTS = 1;        // -10 to +10 Volts
    private static int BIP5VOLTS = 0;        // -5 to +5 Volts
    private static int UNI10VOLTS = 100;    // 0 to +10 Volts

    // Types of error reporting
    private static int DONTPRINT = 0;
    private static int PRINTALL = 3;

    // Types of error handling
    private static int DONTSTOP = 0;
    private static int STOPALL = 2;

    private static float CURRENT_REVISION_NUMBER = 6.51f;
    private static int BOARD_NUMBER = 0;
    private static int RANGE = UNI10VOLTS;

    //---------------------------------------------------------EXTERNAL METHODS IMPORTS:

    [DllImport("cbw64")]
    private static extern int cbDeclareRevision(ref float revisionNumber);

    [DllImport("cbw64")]
    private static extern int cbAOut(int boardNumber,
                                      int channel,
                                      int gain,
                                      ushort dataValue);

    [DllImport("cbw64")]
    private static extern int cbErrHandling(int reporting, int handling);

    [DllImport("cbw64")]
    private static extern int cbFlashLED(int boardNumber);

    [DllImport("cbw64")]
    private static extern int cbFromEngUnits(int boardNumber,
                                              int range,
                                              float engineeringUnits,
                                              out ushort dataValue);

    [DllImport("cbw64")]
    private static extern int cbGetConfig(int infoType,
                                           int boardNumber,
                                           int deviceNumber,
                                           int configurationItem,
                                           out int configurationValue);

    [DllImport("cbw64")]
    private static extern int cbSetConfig(int infoType,
                                           int boardNumber,
                                           int deviceNumber,
                                           int configurationItem,
                                           int configurationValue);

    [DllImport("cbw64")]
    private static extern int cbVOut(int boardNumber,
                                      int channel,
                                      int range,
                                      float dataValue,
                                      int options);

    //---------------------------------------------------------------------------FIELDS:    

    private static bool isInitialized;

    //--------------------------------------------------------------------------METHODS:

    public static void flashLED()
    {
        cbFlashLED(BOARD_NUMBER);
    }

    public static void init()
    {
        // Declare the current software version
        float revisionNum = CURRENT_REVISION_NUMBER;
        cbDeclareRevision(ref revisionNum);
        // Specify internal error handling
        cbErrHandling(PRINTALL, DONTSTOP);

        isInitialized = true;
    }

    public static void writeVolts(int channel, float volts)
    {
        ushort dataValue;
        if (VERBOSE)
        {
            //Utility.print(LOG_TAG, "Writing Volts: " + volts);
            Console.WriteLine(LOG_TAG, "Writing Volts: " + volts);
        }
        // Go from pos/neg volts to unsigned int
        cbFromEngUnits(BOARD_NUMBER, RANGE, volts, out dataValue);
        // Send signal to actuate
        cbAOut(BOARD_NUMBER, channel, RANGE, dataValue);
    }

    public static int writeWaveForm(ushort type, int channel, float Amplitude, float frequency, float time)
    {
        float volts;
        switch (type)
        {
            case 1://zero wave
                writeVolts(channel, 0.0f);
                return 0;

            case 2://ramp wave 
                return 0;

            case 3://square wave                
                return 0;

            case 4: // sine wave
                volts = (Amplitude / 2.0f) + (Amplitude * (float)(System.Math.Sin(frequency * time)));
                writeVolts(channel, volts);
                MonoBehaviour.print("volts:  " + volts);
                return 0;

            case 5: //rectangle
                return 0;

            case 6: //triangle
                return 0;
        }
        return 1;
    }

}
