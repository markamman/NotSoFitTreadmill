//*****************************************************************************
//*****************************************************************************
// File: IFitCommunication.cs
// Hacker: M. Amman
// Description:
//   This file contains the IFitCommunication class, which implements communication 
//   to a treadmill through the iFit chirp audio interface. 
//
// Required classes:
//
// History:
//   See the notes for the individual methods below.
//   07-30-2019, MA: First version completed.
//   05-02-2020, MA: Added threadSpeed, threadIncline, and threadSetSpeedIncline() so that
//     the speed and incline could be set within a separate thread.
//
//*****************************************************************************
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotSoFitTreadmill
{

    //=====================================================================
    //=====================================================================
    // Class: IFitCommunication
    // Description:
    //  This is the to the class that implements communication to a treadmill 
    //  through the iFit chirp audio interface. The class provides the public  
    //  static method setSpeedIncline() for this purpose.
    //
    //=====================================================================
    //=====================================================================
    /// <summary>
    /// The IFitCommunication class implements communication to a treadmill 
    //  through the iFit chirp audio interface.
    /// </summary>
    class IFitCommunication
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

        public byte threadSpeed;
        public byte threadIncline;

        //---------------------------------------------------------------------
        // Properties  
        //---------------------------------------------------------------------


        //=====================================================================
        //=====================================================================
        // Initialization methods
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Constructor: public iFitCommunication()
        //
        // Description: iFitCommunication class constructor.
        //
        // History:
        //  07-30-2019, MA: First version completed.
        //  05-02-2020, MA: Updated for multi-threading.
        //			
        //=====================================================================
        /// <summary>
        /// iFitCommunication is the class constructor. 
        /// </summary>
        public IFitCommunication()
        {
            threadSpeed = PAUSE_TREADMILL;
            threadIncline = 10;
        }

        public IFitCommunication(byte tspeed, byte tincline)
        {
            threadSpeed = tspeed;
            threadIncline = tincline;
        }


        //=====================================================================
        //=====================================================================
        // Class methods
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Method: public static bool setSpeedIncline(byte speed, byte incline)
        //
        // Description: 
        //   This method produces an audio output on System.Media.SoundPlayer that
        //   when passed to the treadmill's iFit audio input will set the treadmill's
        //   speed and incline (after the treadmill has been placed in iFit mode by 
        //   pressing its iFit button, assuming a Nordictrack Apex 4100i treadmill). 
        //   Both speed and incline are byte variables containing 
        //   10 times the actual treadmill settings of speed in mph and incline in % grade.
        //   The development of this code was based on the following three examples.
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
        //
        //   Through testing with the Nordictrack Apex 4100i treadmill also determined 
        //   the following.
        //   1. Sending speed = 248 or 249 pauses the treadmill (speed goes to 0), incline is  
        //     ignored, and the treadmill can be restarted by sending a valid speed and incline.
        //   2. Sending an out of range speed and a valid incline causes the incline value 
        //     to be updated and the speed to remain unchanged. 
        //   3. Sending speed = 252 and incline = 252 stops the treadmill such that the iFit
        //     button on the treadmill must be cycled before computer control can continue.
        //
        // History:
        // 07-30-2019, MA: First version completed.
        // 
        //=====================================================================
        /// <summary>
        /// The method setSpeedIncline generates an audio signal on System.Media.SoundPlayer
        /// that when passed to the treadmill's iFit audio input will set the treadmill's 
        /// speed and incline.
        /// </summary>
        /// <param name="speed">
        /// speed/10 is the treadmill speed to be set in units of mph.
        /// </param>
        /// <param name="incline">
        /// incline/10 is the treadmill incline to be set in units of % grade.
        /// </param>
        public static bool setSpeedIncline(byte speed, byte incline)
        {
            int i, j;
            short samplepoint;
            int[] digitaldata = new int[32];
            int formatchunksize = 16;
            int headersize = 8;
            short formattype = 1;
            short tracks = 1;
            int samplespersecond = SAMPLERATE;
            short bitspersample = 16;
            short framesize = (short)(tracks * ((bitspersample + 7) / 8));
            int bytespersecond = samplespersecond * framesize;
            int wavesize = 4;
            int samples = (int)(384 * RATEFREQRATIO); 
            int datachunksize = samples * framesize;
            int filesize = wavesize + headersize + formatchunksize + headersize + datachunksize;

            var mstream = new MemoryStream();
            BinaryWriter bwriter = new BinaryWriter(mstream);
            SoundPlayer controlaudio;

            // Write the header.
            bwriter.Write(0x46464952); // = encoding.GetBytes("RIFF")
            bwriter.Write(filesize);
            bwriter.Write(0x45564157); // = encoding.GetBytes("WAVE")
            bwriter.Write(0x20746D66); // = encoding.GetBytes("fmt ")
            bwriter.Write(formatchunksize);
            bwriter.Write(formattype);
            bwriter.Write(tracks);
            bwriter.Write(samplespersecond);
            bwriter.Write(bytespersecond);
            bwriter.Write(framesize);
            bwriter.Write(bitspersample);
            bwriter.Write(0x61746164); // = encoding.GetBytes("data")
            bwriter.Write(datachunksize);

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

            // Write the sound sample data to the sound player.
            mstream.Seek(0, SeekOrigin.Begin);
            controlaudio = new System.Media.SoundPlayer(mstream);

            // Play() will play the sound in a new thread, whereas PlaySync() will play the sound
            // in the current thread. 
            controlaudio.Play();
 
            // Clean up.
            bwriter.Close();
            mstream.Close();
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
        // Methods: public void threadSetSpeedIncline()
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
        /// The method threadSetSpeedIncline generates an audio signal on System.Media.SoundPlayer
        /// that when passed to the treadmill's iFit audio input will set the treadmill's 
        /// speed and incline. This method is used to set the speed and incline in a separate thread. 
        /// </summary>
        public void threadSetSpeedIncline()
        {
            setSpeedIncline(threadSpeed, threadIncline);
        }

    }
}
