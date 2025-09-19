//*****************************************************************************
//*****************************************************************************
// File: NAudioiFitCommunication.cs
// Hacker: M. Amman
// Description:
//   This file contains the NAudioiFitCommunication class, which implements communication 
//   to a treadmill through the iFit chirp audio interface. 
//
// Required classes:
//
// Required libraries:
//
//  NAudio 
//    Method to install: SolutionExplorer -> Right click References -> Select Manage NUGet Packages ...
//    Search for the desired library and then press Install.)
//
//
// History:
//   See the notes for the individual methods below.
//   07-30-2019, MA: First version completed.
//   05-02-2020, MA: Added threadSpeed, threadIncline, and threadSetSpeedIncline() so that
//     the speed and incline could be set within a separate thread.
//   05-08-2020, MA: Modified to use the NAudio library and added the parameter devicenumber.
//     Changed the name from IFitCommunication to NAudioiFitCommunication.
//
//*****************************************************************************
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NotSoFitTreadmill
{

    //=====================================================================
    //=====================================================================
    // Class: NAudioiFitCommunication
    // Description:
    //  This is the to the class that implements communication to a treadmill 
    //  through the iFit chirp audio interface. The class provides the public  
    //  static method setSpeedIncline() for this purpose.
    //
    //=====================================================================
    //=====================================================================
    /// <summary>
    /// The NAudioiFitCommunication class implements communication to a treadmill 
    //  through the iFit chirp audio interface.
    /// </summary>
    class NAudioiFitCommunication
    {
        //---------------------------------------------------------------------
        // Constants
        //---------------------------------------------------------------------

        private const int AMPLITUDE = 32767; 
        private const int FREQUENCY = 2000;
        private const int SAMPLERATE = 44100;
        public const byte PAUSE_TREADMILL = (byte)248;
        public const byte IGNORE_SPEED = (byte)200;

        //---------------------------------------------------------------------
        // Static fields
        //---------------------------------------------------------------------

        private static int RATEFREQRATIO = (int)((double)SAMPLERATE / (double)FREQUENCY);

        //---------------------------------------------------------------------
        // Fields
        //---------------------------------------------------------------------

        // Fields needed for multi-threading.
        public byte threadSpeed;
        public byte threadIncline;
        public int threadDevice;

        //---------------------------------------------------------------------
        // Properties  
        //---------------------------------------------------------------------


        //=====================================================================
        //=====================================================================
        // Initialization methods
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Constructor: public NAudioiFitCommunication()
        //
        // Description: NAudioiFitCommunication class constructor.
        //
        // History:
        //  07-30-2019, MA: First version completed.
        //  05-02-2020, MA: Updated for multi-threading.
        //			
        //=====================================================================
        /// <summary>
        /// NAudioiFitCommunication is the class constructor. 
        /// </summary>
        public NAudioiFitCommunication()
        {
            threadSpeed = PAUSE_TREADMILL;
            threadIncline = 10;
            threadDevice = -1;
        }

        public NAudioiFitCommunication(byte tspeed, byte tincline, int tdevice)
        {
            threadSpeed = tspeed;
            threadIncline = tincline;
            threadDevice = tdevice;
        }


        //=====================================================================
        //=====================================================================
        // Class methods
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Method: public static bool setSpeedIncline(byte speed, byte incline, int devicenumber)
        //
        // Description: 
        //   This method produces an audio output on audio device devicenumber that
        //   when passed to the treadmill's iFit audio input will set the treadmill's
        //   speed and incline (after the treadmill has been placed in iFit mode by 
        //   pressing its iFit button, assuming a Nordictrack Apex 4100i treadmill). 
        //   Both speed and incline are byte variables containing 
        //   10 times the actual treadmill settings of speed in mph and incline in % grade.
        //   The development of this code was based on the following resources.
        //   1. iFitJava by Doug Bradbury, found at the site 
        //     https://github.com/dougbradbury/iFitJava.
        //     Doug's example provided the information needed to encode the speed 
        //     and incline in the signal.
        //   2. ChirpMaker.bas by Mike Curiale and Ron Clarke, found at the sites
        //     http://web.archive.org/web/20031018125030/http://members.ync.net/mcuriale/imfit/index.html
        //     http://web.archive.org/web/20050123055507/http://members.ync.net/mcuriale/imfit/chirpmaker.zip
        //     This code also demonstrated how to generate the appropriate audio
        //     signal.
        //   3. A response to "Creating sine or square wave in C#" on stackoverflow.com 
        //     provided by Edward, found at 
        //     https://stackoverflow.com/questions/203890/creating-sine-or-square-wave-in-c-sharp.
        //     Edward's example showed how to produce an audio WAV file in memory using 
        //     MemoryStream and then play the MemoryStream with System.Media.SoundPlayer.
        //   4. NAudio documentation and examples found at
        //     https://github.com/naudio/NAudio.
        //
        //   Through testing with the Nordictrack Apex 4100i treadmill the following was 
        //   also determined. 
        //   1. Sending speed = 248 or 249 pauses the treadmill (speed goes to 0), incline is  
        //     ignored, and the treadmill can be restarted by sending a valid speed and incline.
        //   2. Sending an out of range speed and a valid incline causes the incline value 
        //     to be updated and the speed to remain unchanged. 
        //   3. Sending speed = 252 and incline = 252 stops the treadmill such that the iFit
        //     button on the treadmill must be cycled before computer control can continue.
        //
        // History:
        // 07-30-2019, MA: First version completed.
        // 05-08-2020, MA: Modified to use the NAudio library and added the parameter devicenumber.
        // 
        //=====================================================================
        /// <summary>
        /// The method setSpeedIncline generates an audio signal 
        /// that when passed to the treadmill's iFit audio input will set the treadmill's 
        /// speed and incline.
        /// </summary>
        /// <param name="speed">
        /// speed/10 is the treadmill speed to be set in units of mph.
        /// </param>
        /// <param name="incline">
        /// incline/10 is the treadmill incline to be set in units of % grade.
        /// </param>
        /// <param name="devicenumber">
        /// devicenumber is the interger number of the audio device that will be used for
        /// the audio. A value of -1 indicates that the default device should be used.
        /// </param>
        public static bool setSpeedIncline(byte speed, byte incline, int devicenumber)
        {
            int i, j;
            short samplepoint;
            int[] digitaldata = new int[32];

            // Error checks.
            if((devicenumber < -1)||(devicenumber >= WaveOut.DeviceCount))
            {
                return false;
            }

            // Create the binary memory stream.
            var mstream = new MemoryStream();
            BinaryWriter bwriter = new BinaryWriter(mstream);

            // Generate the digital data array from the speed and incline values.
            digitaldata = generateDigitalData(speed, incline);

            // Write 32*RATEFREQRATIO number of zeros.
            for(i=0; i < 32 * RATEFREQRATIO; i++)
            {
                bwriter.Write((short)0);
            }

            // Write the data. Number of samples written = 32*4*RATEFREQRATIO.
            for (i = 0; i < 32; i++)
            {
                for (j = 0; j < 4 * RATEFREQRATIO; j++)
                {
                    samplepoint = (short)(digitaldata[i] * AMPLITUDE * Math.Sin((2.0 * Math.PI * (double)j) / ((double)RATEFREQRATIO)));
                    bwriter.Write(samplepoint);
                }
            }

            // Write 64*RATEFREQRATIO number of zeros.
            for (i = 0; i < 64 * RATEFREQRATIO; i++)
            {
                bwriter.Write((short)0);
            }

            // Write the data again. Number of samples written = 32*4*RATEFREQRATIO.
            for (i = 0; i < 32; i++)
            {
                for (j = 0; j < 4 * RATEFREQRATIO; j++)
                {
                    samplepoint = (short)(digitaldata[i] * AMPLITUDE * Math.Sin((2.0 * Math.PI * (double)j) / ((double)RATEFREQRATIO)));
                    bwriter.Write(samplepoint);
                }
            }

            // Write 32*RATEFREQRATIO number of zeros.
            for (i = 0; i < 32 * RATEFREQRATIO; i++)
            {
                bwriter.Write((short)0);
            }

            // Create a raw wave stream out of the data and then play the wave.
            // The thread execution is prevented from continuing beyond this block until the playing of the wave
            // has finished.
            mstream.Seek(0, SeekOrigin.Begin);
            var rawstream = new RawSourceWaveStream(mstream, new WaveFormat(SAMPLERATE, 16, 1));
            var wo = new WaveOutEvent();
            wo.DeviceNumber = devicenumber;
            wo.Init(rawstream);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(50);
            }

            // Clean up.
            wo.Dispose();
            rawstream.Dispose();
            bwriter.Close();
            mstream.Dispose();
            return true;
        }


        //=====================================================================
        // Methods: private static int[] generateDigitalData(byte speed, byte incline)
        //   private static void loadBits(byte inbyte, ref int [] outarray, int startindex)
        //
        // Description: 
        //  This method returns an int array (data) whose elements contain only 0 or 1 as determined
        //  by the bit values of speed, incline, and checksum, where checksum = speed + incline.
        //  Each of the three data elements (speed, incline, and checksum) consist of the 2 bits 01
        //  followed by the 8 bits of data element byte.
        //  Both speed and incline are byte variables containing 10 times the actual treadmill settings
        //  of speed in mph and incline in % grade.
        //  The structure of data is as follows.
        //   index  data[index]
        //   0      0
        //   1      0
        //   2      1
        //   3      speed.0
        //   ...
        //   10     speed.7
        //   11     0
        //   12     1
        //   13     incline.0
        //   ...
        //   20     incline.7
        //   21     0
        //   22     1
        //   23     checksum.0
        //   ...
        //   30     checksum.7
        //   31     0
        //
        // History:
        // 07-31-2019, MA: First version completed.
        // 
        //=====================================================================
        private static int[] generateDigitalData(byte speed, byte incline)
        {
            int[] data = new int[32];
            byte checksum;
            int dataindex;

            dataindex = 0;
            checksum = (byte)(speed + incline);
            data[dataindex] = 0;
            dataindex = dataindex + 1;
            data[dataindex] = 0;
            dataindex = dataindex + 1;
            data[dataindex] = 1;
            dataindex = dataindex + 1;
            loadBits(speed, ref data, dataindex);
            dataindex = dataindex + 8;
            data[dataindex] = 0;
            dataindex = dataindex + 1;
            data[dataindex] = 1;
            dataindex = dataindex + 1;
            loadBits(incline, ref data, dataindex);
            dataindex = dataindex + 8;
            data[dataindex] = 0;
            dataindex = dataindex + 1;
            data[dataindex] = 1;
            dataindex = dataindex + 1;
            loadBits(checksum, ref data, dataindex);
            dataindex = dataindex + 8;
            data[dataindex] = 0;

            return data;
        }

        private static void loadBits(byte inbyte, ref int [] outarray, int startindex)
        {
            int outarrayindex = startindex;

            for(int i = 0; i < 8; i++)
            {
                outarray[outarrayindex] = ((inbyte >> i) & 1);
                outarrayindex = outarrayindex + 1;
            }
        }


        //=====================================================================
        // Method: public void threadSetSpeedIncline()
        //
        // Description: 
        //  This method is the multi-threading thread method that is used when it is desired
        //  to set the treadmill speed and incline using a separate thread. This follows the
        //  "Passing data to threads" example found at
        //  https://docs.microsoft.com/en-us/dotnet/standard/threading/creating-threads-and-passing-data-at-start-time
        //
        // History:
        // 05-02-2020, MA: First version completed.
        // 
        //=====================================================================
        /// <summary>
        /// The method threadSetSpeedIncline generates an audio signal 
        /// that when passed to the treadmill's iFit audio input will set the treadmill's 
        /// speed and incline. This method is used to set the speed and incline in a separate thread. 
        /// </summary>
        public void threadSetSpeedIncline()
        {
            setSpeedIncline(threadSpeed, threadIncline, threadDevice);
        }


        //=====================================================================
        // Method: public static List<string> enumerateDevices()
        //
        // Description: 
        //   This method returns a List<string> containing the product names of all
        //   the available audio output devices.
        //
        // History:
        // 05-08-2020, MA: First version completed.
        // 
        //=====================================================================
        /// <summary>
        /// The method enumerateDevices returns a List<string> containing the product names of all
        /// the available audio output devices.
        /// </summary>
        public static List<string> enumerateDevices()
        {
            List<string> devicenames = new List<string>();

            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                devicenames.Add(caps.ProductName);
            }

            return devicenames;
        }
    }

}
