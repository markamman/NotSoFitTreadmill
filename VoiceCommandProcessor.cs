//*****************************************************************************
//*****************************************************************************
// File: VoiceCommandProcessor.cs
// Hacker: M. Amman
// Description:
//   This file contains the VoiceCommandProcessor and VoiceCommandSharedData classes.
//   Through these classes, voice commands to control the treadmill can be acquired 
//   in a separate thread and then passed to the main GUI thread.
//
// Required classes:
//
// Required libraries:
//
//  NAudio 
//    Method to install: SolutionExplorer -> Right click References -> Select Manage NUGet Packages ...
//    Search for the desired library and then press Install.)
//
//  Starting with version 3.0, none of the Microsoft Speech Platform Runtime 11 installation is needed.
//  Instead, the System.Speech.dll is used. This DLL is part of the .NET Framework. See the instructions
//  in the following section for details on how to use System.Speech.
//  Software Development Kit (SDK) for the Microsoft Speech Platform Runtime 11:
//    x86_MicrosoftSpeechPlatformSDK\MicrosoftSpeechPlatformSDK.msi
//  Microsoft Speech Platform - Runtime (Version 11):
//    x86_SpeechPlatformRuntime\SpeechPlatformRuntime.msi
//  Microsoft Speech Platform - Runtime Languages (Version 11):
//    MSSpeech_SR_en-US_TELE.msi
//    MSSpeech_TTS_en-US_Helen.msi
//  Installation instructions for the above are given in the following article. 
//    https://docs.microsoft.com/en-us/archive/msdn-magazine/2014/december/voice-recognition-speech-recognition-with-net-desktop-applications
//    After the installation, add a reference to the Microsoft.Speech.dll:
//    SolutionExplorer -> Right click References -> Add Reference ... -> Browse ...
//    Select C:\ProgramFiles (x86)\Microsoft SDKs\Speech\v11.0\Assembly\Microsoft.Speech.dll
//
//  Microsoft built-in Speech Recognition engine:
//    Sarting with version 3.0 of NotSoFitTreadmill, this is used rather than the Microsoft Speech
//    Platform Runtime 11. This engine is part of the .NET Framework System.Speech.dll. To use this DLL,
//    add a reference to it: SolutionExplorer -> Right click References -> Add Reference ... -> Assemblies
//    -> Framework -> Select System.Speech.
//
// History:
//   See the notes for the individual methods below.
//   05-09-2020, MA: First version completed.
//   09-21-2025, MA: Updated to use System.Speech instead of Microsoft.Speech. Only
//     the using statements changed.
//
//*****************************************************************************
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using Microsoft.Speech.Recognition;
//using Microsoft.Speech.Synthesis;
using System.Speech.Recognition;
using System.Globalization;
using NAudio;
using NAudio.Wave;


namespace NotSoFitTreadmill
{
    /// <summary>
    /// The VoiceCommandProcessor class contains the VoiceCommandProcessor method and
    /// its startup data. Through this encapsulation, the method 
    /// can be started in its own thread and data can be passed to the thread at 
    /// startup using the class fields and voice commands to control the treadmill 
    /// can be passed to a main thread through a VoiceCommandSharedData object. 
    /// </summary>
    class VoiceCommandProcessor
    {
        //---------------------------------------------------------------------
        // Constants and public static fields.
        //---------------------------------------------------------------------

        // Speech recognition strings. 
        public static string[] TREADMILL_ALERT = new string[] { "treadmill", "tea" };
        public static string[] COMMANDS_SIMPLE = new string[] 
            { "start", "resume", "pause", "stop", "up", "down", "faster", "fast", "slower", "slow", "open", "close" };
        public static string[] COMMANDS_WITH_NUMBERS = new string[]
            {"speed", "incline", "workout" };
        public static string[] NUMBERS = new string[]
            {"one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" };
        public static string[] MODE_OPTIONS = new string[]
            {"manual", "run"};


        //---------------------------------------------------------------------
        // Fields.
        //---------------------------------------------------------------------

        /// <summary>
        /// commandSharedData is the VoiceCommandSharedData object containing the information
        /// shared between the main GUI thread and the VoiceCommandProcessor thread. 
        /// </summary>
        private VoiceCommandSharedData commandSharedData;


        //=====================================================================
        // Constructor: public VoiceCommandProcessor()
        //
        // History:
        //  05-09-2020, MA: First version completed.
        //
        //=====================================================================
        /// <summary>
        /// The VoiceCommandProcessor class constructor initializes the class fields
        /// with the startup information contained in the passed parameters.
        /// </summary>
        /// <param name="vcshareddata">
        /// vcshareddata is the VoiceCommandSharedData object that 
        /// contains the initial data and is to be used to hold the newly 
        /// acquired data.
        /// </param>
        public VoiceCommandProcessor(VoiceCommandSharedData vcshareddata)
        {
            // Set  commandSharedData to refer to the vcshareddata object.  vcshareddata is the
            // object that will be updated within getVoiceCommands().  
            commandSharedData = vcshareddata;
        }


        //=====================================================================
        // Method:	public void getVoiceCommands()
        //
        // Description:
        //  This is the routine that reads and processes the user's voice commands.
        //  This method runs in its own thread.
        //  The starting point for the development of this method was the following article.
        //  https://docs.microsoft.com/en-us/archive/msdn-magazine/2014/december/voice-recognition-speech-recognition-with-net-desktop-applications
        //
        // History:
        //  05-11-2020, MA: First version completed.
        //=====================================================================
        /// <summary>
        /// getVoiceCommands is the routine that reads and processes the user's voice commands.
        /// This method is intended to run in its own thread.
        /// </summary>
        public void getVoiceCommands()
        {
            bool acquirecommands = true;
            //CultureInfo ci;
            //SpeechRecognitionEngine sre;

            // Check that an input device exists.
            int inputdevices = WaveIn.DeviceCount;
            if (inputdevices < 1)
            {
                // Send an error back to the GUI thread.
                commandSharedData.setVoiceCommand(-1, true);
                return;
            }

            using (SpeechRecognitionEngine sre =
                new SpeechRecognitionEngine(new CultureInfo("en-us")))
            {
                try
                {
                    //ci = new CultureInfo("en-us");
                    //sre = new SpeechRecognitionEngine(ci);

                    // Set up the speech recognition system.
                    //sre.SetInputToAudioStream();
                    sre.SetInputToDefaultAudioDevice();
                    sre.SpeechRecognized += sre_SpeechRecognized;

                    // Build the grammar.

                    // Create the choices.
                    Choices chtreadmillalert = new Choices(TREADMILL_ALERT);
                    Choices chcommandssimple = new Choices(COMMANDS_SIMPLE);
                    Choices chcommandswithnumbers = new Choices(COMMANDS_WITH_NUMBERS);
                    Choices chnumbers = new Choices(NUMBERS);
                    Choices chmodes = new Choices(MODE_OPTIONS);

                    // Assemble the word sequences.
                    GrammarBuilder gbcommandssimple = new GrammarBuilder();
                    gbcommandssimple.Append(chtreadmillalert);
                    gbcommandssimple.Append(chcommandssimple);
                    GrammarBuilder gbcommandswithnumbers = new GrammarBuilder();
                    gbcommandswithnumbers.Append(chtreadmillalert);
                    gbcommandswithnumbers.Append(chcommandswithnumbers);
                    gbcommandswithnumbers.Append(chnumbers);
                    GrammarBuilder gbcommandmode = new GrammarBuilder();
                    gbcommandmode.Append(chtreadmillalert);
                    gbcommandmode.Append("mode");
                    gbcommandmode.Append(chmodes);

                    // Load the grammar.
                    sre.LoadGrammarAsync(new Grammar(gbcommandssimple));
                    sre.LoadGrammarAsync(new Grammar(gbcommandswithnumbers));
                    sre.LoadGrammarAsync(new Grammar(gbcommandmode));
                    sre.RecognizeAsync(RecognizeMode.Multiple);
                }
                catch
                {
                    // Send an error back to the GUI thread.
                    commandSharedData.setVoiceCommand(-2, true);
                    return;
                }

                // Loop until the main GUI thread sets commandSharedData.acquireCommandState to false. 
                while (acquirecommands == true)
                {
                    // Update the acquisition state flag for the next loop condition check.
                    commandSharedData.getAcquireCommandState(ref acquirecommands, 100);

                    // Pause for a while.
                    Thread.Sleep(100);
                }
                //sre.Dispose();
            }
        }

        public void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result == null)
                return;

            string voicetext = e.Result.Text;
            float confidence = e.Result.Confidence;
            bool pass;
            int command;
            int number;

            // Check that there was good confidence in the recognition.
            if (confidence < 0.6)
                return;

            // Make sure the treadmill alert was given.
            pass = false;
            foreach (string itemtext in TREADMILL_ALERT)
            {
                if(voicetext.IndexOf(itemtext) >= 0)
                {
                    pass = true;
                    break;
                }
            }
            if (!pass)
                return;

            // Handle the simple commands.
            command = 0;
            foreach (string itemtext in COMMANDS_SIMPLE)
            {
                command++;
                if(voicetext.IndexOf(itemtext) >= 0)
                {
                    commandSharedData.setVoiceCommand(command, true);
                    return;
                }
            }

            // Handle the commands with a number parameter.
            command = 15;
            pass = false;
            foreach (string itemtext in COMMANDS_WITH_NUMBERS)
            {
                command++;
                if (voicetext.IndexOf(itemtext) >= 0)
                {
                    pass = true;
                    break;
                }
            }
            if(pass)
            {
                number = 0;
                foreach (string itemtext in NUMBERS)
                {
                    number++;
                    if (voicetext.IndexOf(itemtext) >= 0)
                    {
                        command = command | (number << 8);
                        commandSharedData.setVoiceCommand(command, true);
                        return;
                    }
                }
                return;
            }

            // Handle the mode command.
            if (voicetext.IndexOf("mode") >= 0)
            {
                if (voicetext.IndexOf(MODE_OPTIONS[0]) >= 0)
                {
                    commandSharedData.setVoiceCommand(32, true);
                    return;
                }
                if (voicetext.IndexOf(MODE_OPTIONS[1]) >= 0)
                {
                    commandSharedData.setVoiceCommand(33, true);
                    return;
                }
            }
        }
    }




        /// <summary>
        /// The VoiceCommandSharedData class contains the data shared between a VoiceCommandProcessor
        /// thread and a GUI thread.
        /// Access to this shared data is only possible through the class methods
        /// that make use of the Monitor class methods in order to restrict access
        /// to the data to a single program thread.  This prevents multiple threads
        /// from simultaneously manipulating the shared data.
        /// </summary>
        public class VoiceCommandSharedData
    {
        /// <summary>
        /// locker is an object used to lock the data inside the 
        /// VoiceCommandSharedDat class.  The locking ensures that only one thread 
        /// can access the data at any given time.
        /// </summary>
        private object locker = new object();

        /// <summary>
        /// acquireCommandsState is a flag indicating if the command acquisition is active.
        /// This flag is private so that it can only be accessed through the class 
        /// methods which rely on obtaining an exclusive lock before gaining access. 
        /// </summary>
        private bool acquireCommandsState;

        /// <summary>
        /// newCommandState is a flag indicating if voiceCommand has just been updated.
        /// This flag is private so that it can only be accessed through the class 
        /// methods which rely on obtaining an exclusive lock before gaining access. 
        /// </summary>
        private bool newCommandState;

        /// <summary>
        /// voiceCommand is an encoded treadmill operation command. 
        /// </summary>
        private int voiceCommand;


        //=====================================================================
        // Constructor: public VoiceCommandSharedData()
        //
        // Description: VoiceCommandSharedData class constructor.
        //
        // History:
        //   05-09-2020, MA: First version completed.
        //
        //=====================================================================
        /// <summary>
        /// The VoiceCommandSharedData class constructor initializes the class data
        /// to default values.
        /// </summary>
        public VoiceCommandSharedData()
        {
            acquireCommandsState = false;
            newCommandState = false;
            voiceCommand = 0;
        }


        //=====================================================================
        // Methods: public void setAcquireCommandState(bool state)
        //          public bool setAcquireCommandState(bool state, int waittime)
        //          public void getAcquireCommandState(ref bool state)
        //          public bool getAcquireCommandState(ref bool state, int waittime)
        //          ...
        //
        // Description:
        //  Methods to get and set the fields. These methods ensure 
        //  that only one thread can access the fields at a time.  The methods 
        //  without a waittime are blocking in that code execution will stop on the calling
        //  thread until the object is no longer locked by a different thread.
        //  The second method will only block and wait for the specified waittime
        //  (in ms).  After waiting for this period without successfully gaining
        //  access, the method will return false and will not have updated any
        //  field.  By calling this method with waittime = 0, nonblocking
        //  access to the fields is achieved. 
        //
        // History:
        //  05-09-2020, MA: Added.
        // 
        //=====================================================================

        /// <summary>
        /// setAcquireCommandState sets the value of the acquisition state stored in the 
        /// VoiceCommandSharedData object to the value contained in the parameter state.
        /// This method ensures that only one thread can access the object data at a time.  
        /// This method is blocking in that code execution will stop on the calling 
        /// thread until the VoiceCommandSharedData object is no longer locked by any other 
        /// thread.
        /// </summary>
        /// <param name="state">
        /// state contains the acquisition state value that will be written to the 
        /// VoiceCommandSharedData object.
        /// </param>
        public void setAcquireCommandState(bool state)
        {
            Monitor.Enter(locker);
            try
            {
                acquireCommandsState = state;
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }


        /// <summary>
        /// setAcquireCommandState sets the value of the acquisition state stored in the 
        /// VoiceCommandSharedData object to the value contained in the parameter state.
        /// This method ensures that only one thread can access the object data at a time.  
        /// This method will at most block the code execution on the calling thread  
        /// for the specified waittime (in ms). If after waiting for this period without 
        /// successfully gaining access, the method will return false and will not have 
        /// made any changes to the VoiceCommandSharedData object.  If access is gained during 
        /// this wait time, the object will be updated and a value of true will be returned.  
        /// By calling this method with waittime = 0, nonblocking access is achieved. 
        /// </summary>
        /// <param name="state">
        /// state contains the acquisition state value that will be written to the 
        /// VoiceCommandSharedData object.
        /// </param>
        /// <param name="waittime">
        /// waittime is the maximum time in ms that the method will wait to obtain access to
        /// the VoiceCommandSharedData object.
        /// </param>
        /// <returns>
        /// A return value of true indicates that the VoiceCommandSharedData object was updated.
        /// A return value of false indicates that access was not gained within the specified wait 
        /// time so no update was made.
        /// </returns>
        public bool setAcquireCommandState(bool state, int waittime)
        {
            if (!Monitor.TryEnter(locker, waittime))
            {
                return false;
            }
            try
            {
                acquireCommandsState = state;
            }
            finally
            {
                Monitor.Exit(locker);
            }
            return true;
        }


        /// <summary>
        /// getAcquireCommandState reads the acquisition state stored in the VoiceCommandSharedData object 
        /// and returns this value through the parameter state.  
        /// This method ensures that only one thread can access the object data 
        /// at a time.  This method is blocking in that code execution will stop 
        /// on the calling thread until the VoiceCommandSharedData object is no longer 
        /// locked by any other thread.
        /// </summary>
        /// <param name="state">
        /// state is the parameter that will be used to return the acquisition state currently 
        /// stored in the VoiceCommandSharedData object.
        /// </param>
        public void getAcquireCommandState(ref bool state)
        {
            Monitor.Enter(locker);
            try
            {
                state = acquireCommandsState;
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }


        /// <summary>
        /// getAcquireCommandState reads the acquisition state stored in the VoiceCommandSharedData object 
        /// and returns this value through the parameter state.  
        /// This method ensures that only one thread can access the object data at a time.  
        /// This method will at most block the code execution on the calling thread  
        /// for the specified waittime (in ms). If after waiting for this period without 
        /// successfully gaining access, the method will return false and will not have 
        /// made any changes to the return parameter.  If access is gained during 
        /// this wait time, the return parameter will be updated and a value of true will be returned.  
        /// By calling this method with waittime = 0, nonblocking access is achieved. 
        /// </summary>
        /// <param name="state">
        /// state is the parameter that will be used to return the acquisition state currently 
        /// stored in the VoiceCommandSharedData object.
        /// </param>
        /// <param name="waittime">
        /// waittime is the maximum time in ms that the method will wait to obtain access to
        /// the VoiceCommandSharedData object.
        /// </param>
        /// <returns>
        /// A return value of true indicates that the returned parameter was updated.
        /// A return value of false indicates that access was not gained within the specified wait 
        /// time so no update was made.
        /// </returns>
        public bool getAcquireCommandState(ref bool state, int waittime)
        {
            if (!Monitor.TryEnter(locker, waittime))
            {
                return false;
            }
            try
            {
                state = acquireCommandsState;
            }
            finally
            {
                Monitor.Exit(locker);
            }
            return true;
        }


        /// <summary>
        /// setVoiceCommand sets the values of voiceCommand and newCommandState stored in the 
        /// VoiceCommandSharedData object to the values contained in the passed parameters.
        /// This method ensures that only one thread can access the object data at a time.  
        /// This method is blocking in that code execution will stop on the calling 
        /// thread until the VoiceCommandSharedData object is no longer locked by any other 
        /// thread.
        /// </summary>
        /// <param name="command">
        /// command contains the voiceCommand value that will be written to the 
        /// VoiceCommandSharedData object.
        /// </param>
        /// <param name="state">
        /// state contains the newCommandState value that will be written to the 
        /// VoiceCommandSharedData object.
        /// </param>
        public void setVoiceCommand(int command, bool state)
        {
            Monitor.Enter(locker);
            try
            {
                voiceCommand = command;
                newCommandState = state;
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }


        /// <summary>
        /// setVoiceCommand sets the values of voiceCommand and newCommandState stored in the 
        /// VoiceCommandSharedData object to the values contained in the passed parameters.
        /// This method ensures that only one thread can access the object data at a time.  
        /// This method will at most block the code execution on the calling thread  
        /// for the specified waittime (in ms). If after waiting for this period without 
        /// successfully gaining access, the method will return false and will not have 
        /// made any changes to the VoiceCommandSharedData object.  If access is gained during 
        /// this wait time, the object will be updated and a value of true will be returned.  
        /// By calling this method with waittime = 0, nonblocking access is achieved. 
        /// </summary>
        /// <param name="command">
        /// command contains the voiceCommand value that will be written to the 
        /// VoiceCommandSharedData object.
        /// </param>
        /// <param name="state">
        /// state contains the newCommandState value that will be written to the 
        /// VoiceCommandSharedData object.
        /// </param>
        /// <param name="waittime">
        /// waittime is the maximum time in ms that the method will wait to obtain access to
        /// the VoiceCommandSharedData object.
        /// </param>
        /// <returns>
        /// A return value of true indicates that the VoiceCommandSharedData object was updated.
        /// A return value of false indicates that access was not gained within the specified wait 
        /// time so no update was made.
        /// </returns>
        public bool setVoiceCommand(int command, bool state, int waittime)
        {
            if (!Monitor.TryEnter(locker, waittime))
            {
                return false;
            }
            try
            {
                voiceCommand = command;
                newCommandState = state;
            }
            finally
            {
                Monitor.Exit(locker);
            }
            return true;
        }


        /// <summary>
        /// getVoiceCommand reads the values of voiceCommand and newCommandState 
        /// stored in the VoiceCommandSharedData object and returns these values through the method parameters.  
        /// This method ensures that only one thread can access the object data 
        /// at a time.  This method is blocking in that code execution will stop 
        /// on the calling thread until the VoiceCommandSharedData object is no longer 
        /// locked by any other thread.
        /// </summary>
        /// <param name="command">
        /// command is the parameter that will be used to return the voiceCommand value currently 
        /// stored in the VoiceCommandSharedData object.
        /// </param>
        /// <param name="state">
        /// state is the parameter that will be used to return the newCommandState value currently 
        /// stored in the VoiceCommandSharedData object.
        /// </param>
        public void getVoiceCommand(ref int command, ref bool state)
        {
            Monitor.Enter(locker);
            try
            {
                command = voiceCommand;
                state = newCommandState;
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }


        /// <summary>
        /// getVoiceCommand reads the values of voiceCommand and newCommandState 
        /// stored in the VoiceCommandSharedData object and returns these values through the method parameters.  
        /// This method ensures that only one thread can access the object data at a time.  
        /// This method will at most block the code execution on the calling thread  
        /// for the specified waittime (in ms). If after waiting for this period without 
        /// successfully gaining access, the method will return false and will not have 
        /// made any changes to the return parameters.  If access is gained during 
        /// this wait time, the return parameters will be updated and a value of true will be returned.  
        /// By calling this method with waittime = 0, nonblocking access is achieved. 
        /// </summary>
        /// <param name="command">
        /// command is the parameter that will be used to return the voiceCommand value currently 
        /// stored in the VoiceCommandSharedData object.
        /// <param name="state">
        /// state is the parameter that will be used to return the newCommandState value currently 
        /// stored in the VoiceCommandSharedData object.
        /// </param>
        /// <param name="waittime">
        /// waittime is the maximum time in ms that the method will wait to obtain access to
        /// the VoiceCommandSharedData object.
        /// </param>
        /// <returns>
        /// A return value of true indicates that the returned parameters were updated.
        /// A return value of false indicates that access was not gained within the specified wait 
        /// time so no update was made.
        /// </returns>
        public bool getVoiceCommand(ref int command, ref bool state, int waittime)
        {
            if (!Monitor.TryEnter(locker, waittime))
            {
                return false;
            }
            try
            {
                command = voiceCommand;
                state = newCommandState;
            }
            finally
            {
                Monitor.Exit(locker);
            }
            return true;
        }

    }

}
