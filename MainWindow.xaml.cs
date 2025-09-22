//*****************************************************************************
//*****************************************************************************
// File: MainWindow.xaml.cs
// Hacker: M. Amman
//
// Description:
//  This file is the main source code file for the Not-So-Fit Treadmill application.
//
//  The file contains the following classes:
//    MainWindow: generates the main user interface.
//    
//  Additional files/classes used by this class and contained in other files are:
//    IFitCommunication: implements communication to the treadmill through an iFit chirp audio
//      interface. This class is no longer used and has been replaced by NAudioIFitCommunications.
//    NAudioIFitCommunications: implements communication to the treadmill through an iFit chirp audio
//      interface using the NAudio library.
//    NotificationClient: class that in combination with an NAudio.CoreAudioApi.MMDeviceEnumerator
//      object enables the handling of audio device event changes.
//    TreadmillWorkout: class that contains the treadmill workout data and associated methods.
//    WorkoutDisplay: class that contains the methods for displaying the treadmill speed and 
//      incline workout data.
//    VoiceCommandProcessor: class implementing a  voice command processor that allows the treadmill 
//      to be controlled through voice commands. The voice command processor operates in a separate thread
//      and passes commands to the GUI thread through a VoiceCommandSharedData object.
//    WorkoutEditor: class that enables the graphical creation, editing, and saving of treadmill workouts.
//
// Development environment:
//   Microsoft Visual Studio Community 2022 (64-bit) Version 17.14.5
//   C# Tools 4.14.0
//
// Required libraries:
//
//   NAudio 
//     Method to install: SolutionExplorer -> Right click References -> Select Manage NUGet Packages...
//     Search for the desired library and then press Install.
//
//   Starting with version 3.0, none of the Microsoft Speech Platform Runtime 11 installation is needed.
//   Instead, the System.Speech.dll is used. This DLL is part of the .NET Framework. See the instructions
//   in the following section for details on how to use System.Speech.
//   Software Development Kit (SDK) for the Microsoft Speech Platform Runtime 11:
//     x86_MicrosoftSpeechPlatformSDK\MicrosoftSpeechPlatformSDK.msi
//   Microsoft Speech Platform - Runtime (Version 11):
//     x86_SpeechPlatformRuntime\SpeechPlatformRuntime.msi
//   Microsoft Speech Platform - Runtime Languages (Version 11):
//     MSSpeech_SR_en-US_TELE.msi
//     MSSpeech_TTS_en-US_Helen.msi
//   Installation instructions for the above are given in the following article. 
//     https://docs.microsoft.com/en-us/archive/msdn-magazine/2014/december/voice-recognition-speech-recognition-with-net-desktop-applications
//     After the installation, add a reference to the Microsoft.Speech.dll:
//     SolutionExplorer -> Right click References -> Add Reference ... -> Browse...
//     Select C:\ProgramFiles (x86)\Microsoft SDKs\Speech\v11.0\Assembly\Microsoft.Speech.dll
//
//   Microsoft built-in Speech Recognition engine:
//     Sarting with version 3.0 of NotSoFitTreadmill, this is used rather than the Microsoft Speech
//     Platform Runtime 11. This engine is part of the .NET Framework System.Speech.dll. To use this DLL,
//     add a reference to it: SolutionExplorer -> Right click References -> Add Reference ... -> Assemblies
//     -> Framework -> Select System.Speech.
//
//   Microsoft Visual Studio Installer Projects (MVSIP):
//     To create the software installer NotSoFitTreadmillInstaller, the MVSIP extension must be added 
//     to Visual Studio. To do this:
//       Extensions -> Manage Extensions -> Search for "installer" in the "Visual Studio Marketplace" 
//       Select "Microsoft Visual Studio Installer Projects 2022" and install (for Visual Studio 2022).
//
//   Xceed Extended WPF Toolkit
//     WPF does not have numeric updown controls. This toolkit adds them as well as other controls.
//     The WorkoutEditor portion of Not-So-Fit Treadmill uses an IntegerUpDown control.
//     Method to install: SolutionExplorer -> Right click References -> Select Manage NUGet Packages...
//       Search for and select Extended.Wpf.Toolkit then press Install.
//       Right click in Toolbox -> Select Add Tab -> Name it WPF Toolkit.
//       Right click the new WPF Toolkit -> Select Choose Items... -> Select the DLLs
//       ...\packages\Extended.Wpf.Toolkit*\lib\*\*.dll
//
// Last modified: 09-22-2025
//   
// Version history:
//   See the notes for the individual methods below.
//   1.0.0, 04-22-2020, MA: 
//     First fully functional version completed.
//   1.1.0, 05-14-2020, MA:
//     Added user selection of audio output device. Added voice command treadmill control.
//   1.2.2, 05-19-2020, MA:
//     Installer added.
//   1.3, 10-28-2020, MA:
//     Added a startup configuration file NotSoFitTreadmill.cfg.
//   2.0, 06-21-2021, MA:
//     Fixed a bug in MainWindow().
//     Added workout editor capability.
//   2.1, 06-23-2021, MA:
//     Made minor changes and additions to WorkoutEditor.xaml.cs.
//   2.2, 08-09-2024, MA:
//     Updated to Visual Studio 2022. The Microsoft Visual Studio Installer Projects extension
//     was changed from Microsoft Visual Studio Installer Projects to Microsoft Visual Studio
//     Installer Projects 2022. Basic functionality tested. No code changes made.
//   3.0, 09-22-2025, MA:
//     Changed the voice command processing from using the Microsoft Speech Platform Runtime 11
//     to using the built-in Microsoft Speech Recognition engine. This change eliminated the
//     need to install the Microsoft Speech Platform Runtime 11 and associated language files.
//     Started using GitHub for version control. The files are located in the cloud and in a local
//     repository at c:\Users\marka\source\repos\markamman\NotSoFitTreadmill.
//     Recreated the installer NotSoFitTreadmillInstaller and placed the installer folder within
//     the NotSoFitTreadmill folder.
//     Used Google Jules to write a README.md file for the GitHub repository. Made the repository
//     public rather than private.
//     Added the NotSoFitTreadmillWorkouts folder along with workouts and the
//     NotSoFitTreadmill.cfg to the repository.
//
//*****************************************************************************
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NotSoFitTreadmill
{
    //=====================================================================
    //=====================================================================
    // Class: MainWindow
    // Description:
    //  This is the class that generates the main user interface.
    //
    //=====================================================================
    //=====================================================================
    public partial class MainWindow : Window
    {
        //---------------------------------------------------------------------
        // Variable types definitions.
        //---------------------------------------------------------------------
        enum TreadmillState { Stopped, Running, Paused }
        
        enum WorkoutState { Start, Pause, Resume, Stop, SegmentDone}

        enum WindowScale { P100, P150, P200 }

        //---------------------------------------------------------------------
        // Constants and public static fields.
        //---------------------------------------------------------------------

        // Default directory containing workouts.
        const string DEFAULT_DIRECTORY = @"c:\NotSoFitTreadmillWorkouts\";

        // Number of segments in the manual workout. The treadmill will stop when in manual 
        // mode after this many segments have been run.
        const int MANUAL_WORKOUT_SEGMENTS = 240;

        // Current display segement number at which the display should start shifting.
        //const int MANUAL_WORKOUT_SEGMENT_SHIFT = 100;

        // Window width and height at 100 % scale.
        const int WINDOW_WIDTH = 400;
        const int WINDOW_HEIGHT = 520;

        // Number of times to repetitively send an audio command.
        const int COMMAND_REPEAT_NUMBER = 2;

        // Wait between treadmill commands in milliseconds.
        //const int COMMAND_WAIT = 300;

        //---------------------------------------------------------------------
        // Fields.
        //---------------------------------------------------------------------

        // Fields containing the treadmill status.
        private byte treadmillIncline;
        private byte treadmillSetSpeed;
        private TreadmillState currentTreadmillState;

        // Fields containing the run statistics.
        private TimeSpan treadmillRunTime;
        private double treadmillRunDistance;

        // Workout list.
        private List<TreadmillWorkout> treadmillWorkouts;
        private ObservableCollection<string> treadmillWorkoutNames;

        // Workout display.
        private WorkoutDisplay treadmillWorkoutDisplay;

        // Workout segment currently running (0 based).
        private int currentSegment;

        // Manual workout fields.
        private TreadmillWorkout manualTreadmillWorkout;
        private int manualCurrentSegment;

        // Timers and fields used to update the GUI and run statistics.
        private DispatcherTimer updateTimer;
        private Stopwatch statsStopwatch;
        private long statsPreviousTime;
        private bool statsFirstTimeSpan;
        private bool firstRun;

        // Timer and fields for status text updates.
        private Stopwatch statusTextStopwatch;
        private long statusTextTimeDuration;
        private string statusText;

        // Timers used for the workout segments.
        private DispatcherTimer segmentTimer;
        private Stopwatch segmentStopwatch;

        // Default directory containing workout files.
        private string defaultDirectory;

        // Scale for the main window and its contents.
        private WindowScale windowScale;

        // Audio device event detection objects.
        private MMDeviceEnumerator audioDeviceEnumerator;
        private NotificationClient audioDeviceNotificationClient;

        // Selected audio device.
        private int selectedAudioDeviceNumber;
        private string selectedAudioDeviceName;

        // State of voice command processor.
        //private bool voiceCommandActive;

        // Thread used to acquire voice commands.
        Thread voiceCommandThread;

        // Shared data object used for the voice command acquisition.
        public VoiceCommandSharedData voiceCommandData;


        //---------------------------------------------------------------------------
        // Delegates.  
        //---------------------------------------------------------------------------


        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------


        //=====================================================================
        //=====================================================================
        // Initialization methods.
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Constructor: public MainWindow()
        //
        // Description:  MainWindow class constructor.
        //
        // History:
        //  07-30-2019, MA: First version completed.
        //  06-13-2021, MA: Fixed initialize the treadmill state section.
        //
        //=====================================================================
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the workout display.
            treadmillWorkoutDisplay = new WorkoutDisplay(workoutDisplayCanvas);

            // Hide/show debug button.
            buttonDebug.Visibility = Visibility.Hidden;
            // buttonDebug.Visibility = Visibility.Visible;

            // Inititialize the treadmill state fields.
            treadmillIncline = 10;
            treadmillSetSpeed = 10;
            currentTreadmillState = TreadmillState.Stopped;
            buttonStartPause.Content = "Start";

            // Initialize the run statistics fields.
            treadmillRunTime = new TimeSpan(0);
            treadmillRunDistance = 0.0;

            // Initialize the treadmill workout list, add a default workout, and load
            // all valid workouts from the default directory.
            treadmillWorkouts = new List<TreadmillWorkout>();
            treadmillWorkouts.Add(new TreadmillWorkout());
            if (Directory.Exists(DEFAULT_DIRECTORY))
            {
                defaultDirectory = DEFAULT_DIRECTORY;
            }
            else
            {
                defaultDirectory = Directory.GetCurrentDirectory();
            }
            readTreadmillWorkouts();
            currentSegment = 0;

            // Set up the manual treadmill workout to be used for the workout display when
            // the treadmill is in the manual mode.
            manualTreadmillWorkout = new TreadmillWorkout();
            manualTreadmillWorkout.workoutName = "Manual Workout";
            manualTreadmillWorkout.workoutSegments = new List<TreadmillWorkout.WORKOUT_SEGMENT>();
            for(int i = 0; i < MANUAL_WORKOUT_SEGMENTS; i++)
            {
                manualTreadmillWorkout.workoutSegments.Add(new TreadmillWorkout.WORKOUT_SEGMENT(treadmillSetSpeed, treadmillIncline, 30, 30));
            }
            manualTreadmillWorkout.setTotalDurations();
            manualCurrentSegment = 0;

            // Setup the selected workout combobox.
            comboBoxSelectedWorkout.Items.Clear();
            treadmillWorkoutNames = new ObservableCollection<string>();
            comboBoxSelectedWorkout.ItemsSource = treadmillWorkoutNames;
            updateSelectedWorkoutComboBox();

            // Audio device output initialization.
            selectedAudioDeviceNumber = 0;
            selectedAudioDeviceName = "";
            updateAudioDeviceList();

            // Set up the audio device enumerator and event detection.
            audioDeviceEnumerator = new MMDeviceEnumerator();
            audioDeviceNotificationClient = new NotificationClient();
            audioDeviceEnumerator.RegisterEndpointNotificationCallback(audioDeviceNotificationClient);

            // Voice command processing initialization.
            menuItemVoiceEnabled.IsChecked = false;
            menuItemVoiceDisabled.IsChecked = true;
            //voiceCommandActive = false;
            voiceCommandData = new VoiceCommandSharedData();

            // Set the control focus.
            defaultFocus();

            // Initialize the scale for the main window and its contents.
            windowScale = WindowScale.P100;

            // Read the initiallization file NotSoFitTreadmill.cfg if it exists and set inital the values
            // of the GUI controls based on the contents of this file.
            readControlStateFile();
            setWindowScale();
            updateWorkoutDisplay();

            // Initialize the treadmill state.
            //IFitCommunication.setSpeedIncline(NAudioiFitCommunication.PAUSE_TREADMILL, treadmillIncline);
            //Thread.Sleep(COMMAND_WAIT);
            //IFitCommunication.setSpeedIncline(NAudioiFitCommunication.IGNORE_SPEED, treadmillIncline);
            //Thread.Sleep(COMMAND_WAIT);
            NAudioiFitCommunication.setSpeedIncline(NAudioiFitCommunication.PAUSE_TREADMILL, treadmillIncline, selectedAudioDeviceNumber - 1);
            NAudioiFitCommunication.setSpeedIncline(NAudioiFitCommunication.IGNORE_SPEED, treadmillIncline, selectedAudioDeviceNumber - 1);

            // Initialize the GUI update and statistics timers and associated fields.
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(100.0);
            updateTimer.Tick += updateTimer_Tick;
            updateTimer.Start();
            statsStopwatch = new Stopwatch();
            statsPreviousTime = 0;
            statsFirstTimeSpan = true;
            statusTextStopwatch = new Stopwatch();
            statusTextStopwatch.Reset();
            statusTextStopwatch.Start();

            // Update the status text and set it to remain for 3 s.
            statusText = "";
            firstRun = false;
            writeStatusText("Remember to press the iFit button on the treadmill.", 3000);

            // Initialize the workout segment timers.
            segmentTimer = new DispatcherTimer();
            segmentTimer.Interval = TimeSpan.FromMilliseconds(100.0);
            segmentTimer.Tick += segmentTimer_Tick;
            segmentTimer.Stop();
            segmentStopwatch = new Stopwatch();
        }


        //=====================================================================
        //=====================================================================
        // Button click event handlers.
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Methods: private void buttonStartPause_Click(object sender, RoutedEventArgs e)
        //          private void startPauseTreadmillToggle()
        //
        // Description:
        //  Run/pause button click event handler. This handler toggles the run state of the 
        //  treadmill and the run/pause button.
        //
        // History:
        //  07-31-2019, MA: First versions completed.
        //  04-20-2020, MA: Added workout segment processing for the manual mode.
        //
        //=====================================================================
        private void buttonStartPause_Click(object sender, RoutedEventArgs e)
        {
            /*
            // Handle "Run workout" mode.

            if (comboBoxMode.SelectedIndex == 1)
            {
                if (currentTreadmillState == TreadmillState.Running)
                {
                    updateGUIControls(TreadmillState.Paused);
                    workoutSegmentProcessing(WorkoutState.Pause);
                }
                else if (currentTreadmillState == TreadmillState.Paused)
                {
                    updateGUIControls(TreadmillState.Running);
                    workoutSegmentProcessing(WorkoutState.Resume);
                }
                else if (currentTreadmillState == TreadmillState.Stopped)
                {
                    updateGUIControls(TreadmillState.Running);
                    workoutSegmentProcessing(WorkoutState.Start);
                }
            }
            else

            // Handle "Manual" workout mode.

            {
                //setWindowScale();
                if (currentTreadmillState == TreadmillState.Running)
                {
                    updateGUIControls(TreadmillState.Paused);
                    workoutSegmentProcessing(WorkoutState.Pause);
                }
                else if (currentTreadmillState == TreadmillState.Paused)
                {
                    updateGUIControls(TreadmillState.Running);
                    workoutSegmentProcessing(WorkoutState.Resume);
                }
                else if (currentTreadmillState == TreadmillState.Stopped)
                {
                    updateGUIControls(TreadmillState.Running);
                    workoutSegmentProcessing(WorkoutState.Start);
                }
            }

            */

            startPauseTreadmillToggle();
            defaultFocus();
        }

        private void startPauseTreadmillToggle()
        {
            if (currentTreadmillState == TreadmillState.Running)
            {
                updateGUIControls(TreadmillState.Paused);
                workoutSegmentProcessing(WorkoutState.Pause);
            }
            else if (currentTreadmillState == TreadmillState.Paused)
            {
                updateGUIControls(TreadmillState.Running);
                workoutSegmentProcessing(WorkoutState.Resume);
            }
            else if (currentTreadmillState == TreadmillState.Stopped)
            {
                updateGUIControls(TreadmillState.Running);
                workoutSegmentProcessing(WorkoutState.Start);
            }
        }


        //=====================================================================
        // Methods: private void buttonStop_Click(object sender, RoutedEventArgs e)
        //          private void stopTreadmill()
        //
        // Description:
        //  Stop button click event handler. 
        //
        // History:
        //  08-10-2019, MA: First versions completed.
        //  04-20-2020, MA: Added workout segment processing for the manual mode.
        //
        //=====================================================================
        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (comboBoxMode.SelectedIndex == 1)
            {
                updateGUIControls(TreadmillState.Stopped);
                workoutSegmentProcessing(WorkoutState.Stop);
            }
            else
            {
                updateGUIControls(TreadmillState.Stopped);
                workoutSegmentProcessing(WorkoutState.Stop);
            }
            treadmillSetSpeed = 10;
            */

            stopTreadmill();
            defaultFocus();
        }


        private void stopTreadmill()
        {
            updateGUIControls(TreadmillState.Stopped);
            workoutSegmentProcessing(WorkoutState.Stop);
            treadmillSetSpeed = 10;
        }


        //=====================================================================
        // Method: private void ButtonResetStats_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Reset statistics button click event handler. This handler zeros the run time
        //  and distance display numbers.
        //
        // History:
        //  08-02-2019, MA: First versions completed.
        //
        //=====================================================================
        private void ButtonResetStats_Click(object sender, RoutedEventArgs e)
        {
            // Stop the update timer.
            updateTimer.Stop();

            // Zero the statistics fields.
            treadmillRunTime = TimeSpan.Zero;
            treadmillRunDistance = 0.0;

            // Restart the update timer.
            updateTimer.Start();

            defaultFocus();
        }


        //=====================================================================
        // Methods:	private void buttonIncline_Click(object sender, RoutedEventArgs e)
        //          private void setIncline(byte tincline)
        //
        // Description:
        //  Incline numbered buttons click event handler.  This handler is called
        //  when any one of the incline numbered buttons is clicked.  The method
        //  determines which button was clicked and sets the incline appropriately.
        //
        // History:
        //  07-31-2019, MA: First version completed.
        // 
        //=====================================================================
        private void buttonIncline_Click(object sender, RoutedEventArgs e)
        {
            byte buttonincline;

            // Determine the incline associated with the button pressed.
            try
            {
                buttonincline = (byte)(10*Int32.Parse((((Button)sender).Content).ToString()));
            }
            catch
            {
                defaultFocus();
                return;
            }

            // Update the treadmill incline.
            setIncline(buttonincline);
            defaultFocus();
        }

        private void setIncline(byte tincline)
        {
            // Range check tincline.
            if ((tincline < 10) || (tincline > 120))
                return;

            // Update the treadmill incline if needed.
            if (tincline != treadmillIncline)
            {
                treadmillIncline = tincline;
                updateManualTreadmillWorkout();
                updateWorkoutDisplay();
                updateTreadmillState();
            }
        }


        //=====================================================================
        // Methods:	private void ButtonInclineDown_Click(object sender, RoutedEventArgs e)
        //          private void inclineDown()
        //
        // Description:
        //  Incline down button click event handler. If the incline is >= 15
        //  (1.5 % grade), this method lowers the incline by 5 (0.5 % grade).
        //
        // History:
        //  08-01-2019, MA: First version completed.
        // 
        //=====================================================================
        private void ButtonInclineDown_Click(object sender, RoutedEventArgs e)
        {
            inclineDown();
            defaultFocus();
        }

        private void inclineDown()
        {
            if (treadmillIncline >= 15)
            {
                treadmillIncline = (byte)(treadmillIncline - 5);
                updateManualTreadmillWorkout();
                updateWorkoutDisplay();
                updateTreadmillState();
            }
        }


        //=====================================================================
        // Methods:	private void ButtonInclineUp_Click(object sender, RoutedEventArgs e)
        //          private void inclineUp()
        //
        // Description:
        //  Incline up button click event handler. If the incline is <= 115
        //  (11.5 % grade), this method increases the incline by 5 (0.5 % grade).
        //
        // History:
        //  08-01-2019, MA: First version completed.
        // 
        //=====================================================================
        private void ButtonInclineUp_Click(object sender, RoutedEventArgs e)
        {
            inclineUp();
            defaultFocus();
        }

        private void inclineUp()
        {
            if (treadmillIncline <= 115)
            {
                treadmillIncline = (byte)(treadmillIncline + 5);
                updateManualTreadmillWorkout();
                updateWorkoutDisplay();
                updateTreadmillState();
            }
        }


        //=====================================================================
        // Method:	private void buttonSpeed_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Speed numbered buttons click event handler.  This handler is called
        //  when any one of the speed numbered buttons is clicked.  The method
        //  determines which button was clicked and sets the speed appropriately.
        //
        // History:
        //  08-01-2019, MA: First version completed.
        // 
        //=====================================================================
        private void buttonSpeed_Click(object sender, RoutedEventArgs e)
        {
            byte buttonspeed;

            // Determine the speed associated with the button pressed.
            try
            {
                buttonspeed = (byte)(10 * Int32.Parse((((Button)sender).Content).ToString()));
            }
            catch
            {
                defaultFocus();
                return;
            }

            // Set the speed.
            setSpeed(buttonspeed);
            defaultFocus();
        }

        private void setSpeed(byte tspeed)
        {
            // Change the speed only if the treadmill is running.
            if (currentTreadmillState == TreadmillState.Running)
            {
                // Range check tspeed.
                if ((tspeed < 10) || (tspeed > 100))
                    return;

                // Update the treadmill set speed only if needed.
                if (tspeed != treadmillSetSpeed)
                {
                    treadmillSetSpeed = tspeed;
                    updateManualTreadmillWorkout();
                    updateWorkoutDisplay();
                    updateTreadmillState();
                }
            }
        }


        //=====================================================================
        // Methods:	private void ButtonSpeedDown_Click(object sender, RoutedEventArgs e)
        //          private void speedDown()
        //
        // Description:
        //  Speed down button click event handler. If the speed is >= 11
        //  (1.1 mph), this method lowers the speed by 1 (0.1 mph).
        //
        // History:
        //  08-01-2019, MA: First version completed.
        // 
        //=====================================================================
        private void ButtonSpeedDown_Click(object sender, RoutedEventArgs e)
        {
            speedDown();
            defaultFocus();
        }

        private void speedDown()
        {
            // If the treadmill is not running, do nothing other than resetting the set speed.
            if (currentTreadmillState != TreadmillState.Running)
            {
                //treadmillSetSpeed = 10;
            }

            // Adjust the set speed if it is not at a limit.
            else if (treadmillSetSpeed >= 11)
            {
                treadmillSetSpeed = (byte)(treadmillSetSpeed - 1);
                updateManualTreadmillWorkout();
                updateWorkoutDisplay();
                updateTreadmillState();
            }
        }


        //=====================================================================
        // Methods:	private void ButtonSpeedUp_Click(object sender, RoutedEventArgs e)
        //          private void speedUp()
        //
        // Description:
        //  Speed up button click event handler. If the set speed is <= 99
        //  (9.9 mph), this method increases the speed by 1 (0.1 mph).
        //
        // History:
        //  08-01-2019, MA: First version completed.
        // 
        //=====================================================================
        private void ButtonSpeedUp_Click(object sender, RoutedEventArgs e)
        {
            speedUp();
            defaultFocus();
        }

        private void speedUp()
        {
            // If the treadmill is not running, do nothing other than resetting the set speed.
            if (currentTreadmillState != TreadmillState.Running)
            {
                //treadmillSetSpeed = 10;
            }

            // Adjust the set speed if it is not at a limit.
            else if (treadmillSetSpeed <= 99)
            {
                treadmillSetSpeed = (byte)(treadmillSetSpeed + 1);
                updateManualTreadmillWorkout();
                updateWorkoutDisplay();
                updateTreadmillState();
            }
        }


        //=====================================================================
        // Method:	private void ButtonDebug_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Opens a debug window.
        //
        // History:
        //  08-06-2019, MA: First version completed.
        // 
        //=====================================================================
        private void ButtonDebug_Click(object sender, RoutedEventArgs e)
        {
            if (DebugWindow.DebugWindowInstance == null)
            {
                DebugWindow debugWindow1 = new DebugWindow();
                debugWindow1.Owner = this;
                debugWindow1.Show();
            }
            else if(DebugWindow.DebugWindowInstance.WindowState == WindowState.Minimized)
            {
                DebugWindow.DebugWindowInstance.WindowState = WindowState.Normal;
            }
            else
            {
                DebugWindow.DebugWindowInstance.Focus();
            }
        }


        //=====================================================================
        //=====================================================================
        // Timer event handlers.
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Methods:	void updateTimer_Tick(object sender, EventArgs e)
        //
        // Description:
        //  GUI timer tick event handler and associated methods.  
        //
        // History:
        //  07-31-2019, MA: First version completed.
        //
        //=====================================================================
        void updateTimer_Tick(object sender, EventArgs e)
        {
            // Update the statistics display if needed. 

            if(currentTreadmillState == TreadmillState.Running)
            {
                long timeinterval;
                long currenttime;
                TimeSpan timeSpanInterval;

                if(statsFirstTimeSpan == true)
                {
                    statsStopwatch.Start();
                    statsFirstTimeSpan = false;
                }
                else
                {
                    currenttime = statsStopwatch.ElapsedMilliseconds;
                    if (currenttime > statsPreviousTime)
                    {
                        timeinterval = currenttime - statsPreviousTime;
                    }
                    else
                    {
                        timeinterval = 0;
                    }

                    timeSpanInterval = TimeSpan.FromMilliseconds(timeinterval);
                    treadmillRunTime = treadmillRunTime + timeSpanInterval;

                    // Denominator conversion factor in the following consists of 10 to convert treadmillSetSpeed to
                    // mph and 1000*60*60 to convert timeinterval from ms to h.
                    treadmillRunDistance = treadmillRunDistance + timeinterval * treadmillSetSpeed / 36000000.0;
                }
                statsPreviousTime = statsStopwatch.ElapsedMilliseconds;
            }
            else
            {
                statsFirstTimeSpan = true;
            }
            updateStatisticsDisplay();

            // Check for and handle any voice commands.

            bool voicecmdactive = false;
            voiceCommandData.getAcquireCommandState(ref voicecmdactive);
            while (voicecmdactive)
            {
                // Make sure the voice processor thread is active. If it is not, update the GUI
                // and shared data, and display an error message.
                if ((voiceCommandThread == null) || (!voiceCommandThread.IsAlive))
                {
                    menuItemVoiceEnabled.IsChecked = false;
                    menuItemVoiceDisabled.IsChecked = true;
                    voiceCommandData.setAcquireCommandState(false);
                    writeStatusText("The voice command processing thread is dead, voice commands disabled.", 3000);
                    break;
                }
                
                // Check for no input devices.
                int inputdevices = WaveIn.DeviceCount;
                if (inputdevices < 1)
                {
                    // Shutdown the voice command processor since no input devices exist. Display an error message.
                    menuItemVoiceEnabled.IsChecked = false;
                    menuItemVoiceDisabled.IsChecked = true;
                    voiceCommandData.setAcquireCommandState(false);
                    writeStatusText("No microphone present, voice commands disabled.", 3000);
                    break;
                }

                // Check for a new command from the voice command processor.
                int command = 0;
                bool newcommand = false;
                voiceCommandData.getVoiceCommand(ref command, ref newcommand);
                if (!newcommand)
                {
                    break;
                }

                // Check for an error from the voice command processor. If an error has been sent,
                // shutdown the voice command processor and display an error message.
                if (command < 0)
                {
                    menuItemVoiceEnabled.IsChecked = false;
                    menuItemVoiceDisabled.IsChecked = true;
                    voiceCommandData.setAcquireCommandState(false);
                    writeStatusText("The voice command processing thread passed an error, voice commands disabled.", 3000);
                    break;
                }

                // Handle the command.
                handleVoiceCommand(command);
                //string outtext = "Command: " + (command & 0x00FF).ToString() + " Parameter: " + (command >> 8).ToString();
                //writeStatusText(outtext, 2000);

                // Clear the shared data.
                voiceCommandData.setVoiceCommand(0, false);
                break;
            }

            // Check for added or removed audio devices.
            if(audioDeviceNotificationClient.deviceAnyChange)
            {
                writeStatusText("Audio device change detected.", 3000);
                audioDeviceNotificationClient.deviceAnyChange = false;
                updateAudioDeviceList();
            }
        }


        //=====================================================================
        // Methods:	void segmentTimer_Tick(object sender, EventArgs e)
        //
        // Description:
        //  Workout segment tick event handler and associated methods.  
        //
        // History:
        //  08-11-2019, MA: First version completed.
        //  04-20-2020, MA: Modified for manual mode handling.
        //
        //=====================================================================
        void segmentTimer_Tick(object sender, EventArgs e)
        {
            // Handle the "Manual" mode.

            if (comboBoxMode.SelectedIndex == 0)
            {
                // Check if the segment stopwatch indicates that the current segment time
                // has been reached or exceeded. If so, make a call to workoutSegmentProcessing()
                // so that the next segment can be started. For the manual mode, each segment is
                // 30,000 ms long.
                if (segmentStopwatch.ElapsedMilliseconds > manualTreadmillWorkout.workoutSegments[manualCurrentSegment].totalduration*1000)
                {
                    workoutSegmentProcessing(WorkoutState.SegmentDone);
                }
            }

            // Handle the "Run workout" mode.

            else if (comboBoxMode.SelectedIndex == 1)
            {
                // Check if the segment stopwatch indicates that the current segment time
                // has been reached or exceeded. If so, make a call to workoutSegmentProcessing()
                // so that the next segment can be started.
                if(segmentStopwatch.ElapsedMilliseconds > 
                    treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments[currentSegment].totalduration * 1000)
                {
                    workoutSegmentProcessing(WorkoutState.SegmentDone);
                }
            }
            else
            {
                segmentTimer.Stop();
                segmentStopwatch.Stop();
                segmentStopwatch.Reset();
            }
        }


        //=====================================================================
        //=====================================================================
        // Menu click event handlers.
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private void commandsMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Event handler for a commands menu item mouse click.
        //
        // History:
        //  04-17-2020, MA: First version completed.
        // 
        //=====================================================================
        private void commandsMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }


        //=====================================================================
        // Method:	private void openMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Event handler for an open workout menu item mouse click.
        //
        // History:
        //  04-17-2020, MA: First version completed.
        // 
        //=====================================================================
        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int filereaderror;
            string returnedfilespec = "";
            string outtext;
            TreadmillWorkout tempworkout = new TreadmillWorkout();

            // Read the workout into tempworkout.
            filereaderror = tempworkout.loadWorkoutDialog(ref returnedfilespec);

            // Set firstRun flag so that labelStatus text will not be immediately overwritten.
            firstRun = true;

            // Error check and add workout to workout list.
            if ((filereaderror == 0) && (tempworkout.workoutSegments.Count > 0))
            {
                treadmillWorkouts.Add(tempworkout);
                updateSelectedWorkoutComboBox();
                outtext = "Added workout: " + tempworkout.workoutName;
                writeStatusText(outtext, 0);
            }
            else
            {
                writeStatusText("No workout added.", 0);
            }
        }


        //=====================================================================
        // Method:	private void scaleMenuItem_Click(object sender, RoutedEventArgs e)
        //          private void setWindowScale()
        //
        // Description:
        //  Event handler for a Window scale menu item mouse click.
        //
        // History:
        //  04-23-2020, MA: First version completed.
        // 
        //=====================================================================
        private void scaleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string menuitemtext;

            menuitemtext = ((MenuItem)sender).Header.ToString();

            if(menuitemtext == "150 %")
            {
                windowScale = WindowScale.P150;
                setWindowScale();
            }
            else if (menuitemtext == "200 %")
            {
                windowScale = WindowScale.P200;
                setWindowScale();
            }
            else
            {
                windowScale = WindowScale.P100;
                setWindowScale();
            }
        }


        private void setWindowScale()
        {
            switch (windowScale)
            {
                case WindowScale.P100:
                    menuItemScale100.IsChecked = true;
                    menuItemScale150.IsChecked = false;
                    menuItemScale200.IsChecked = false;
                    windowMain.Width = WINDOW_WIDTH;
                    windowMain.Height = WINDOW_HEIGHT;
                    gridMainWindow.LayoutTransform = new ScaleTransform(1.0, 1.0);
                    break;
                case WindowScale.P150:
                    menuItemScale100.IsChecked = false;
                    menuItemScale150.IsChecked = true;
                    menuItemScale200.IsChecked = false;
                    windowMain.Width = 1.5 * WINDOW_WIDTH;
                    windowMain.Height = 1.5 * WINDOW_HEIGHT - 18;
                    gridMainWindow.LayoutTransform = new ScaleTransform(1.5, 1.5);
                    break;
                case WindowScale.P200:
                    menuItemScale100.IsChecked = false;
                    menuItemScale150.IsChecked = false;
                    menuItemScale200.IsChecked = true;
                    windowMain.Width = 2 * WINDOW_WIDTH;
                    windowMain.Height = 2 * WINDOW_HEIGHT - 36;
                    gridMainWindow.LayoutTransform = new ScaleTransform(2.0, 2.0);
                    break;
                default:
                    menuItemScale100.IsChecked = true;
                    menuItemScale150.IsChecked = false;
                    menuItemScale200.IsChecked = false;
                    windowMain.Width = WINDOW_WIDTH;
                    windowMain.Height = WINDOW_HEIGHT;
                    gridMainWindow.LayoutTransform = new ScaleTransform(1.0, 1.0);
                    break;
            }
        }


        //=====================================================================
        // Method: private void deviceMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description: 
        //   This is the device menu click event handler.
        //
        // History:
        // 05-08-2020, MA: First version completed.
        // 
        //=====================================================================
        private void deviceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem smi = (MenuItem)e.OriginalSource;
            if (smi != null)
            {
                int i = 0;
                foreach (MenuItem mi in menuItemDeviceSelect.Items)
                {
                    if (mi.Equals(smi))
                    {
                        mi.IsChecked = true;
                        selectedAudioDeviceNumber = i;
                        selectedAudioDeviceName = (string)mi.Header;
                        //Console.WriteLine("selectedAudioDeviceName = {0}\n", selectedAudioDeviceName);
                    }
                    else
                    {
                        mi.IsChecked = false;
                    }
                    i = i + 1;
                }
            }
        }


        //=====================================================================
        // Method:	private void editMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  This is the edit workout menu click event handler. This opens a window for 
        //  creating, opening, editing, and saving workouts.
        //
        // History:
        //  06-14-2020, MA: First version completed.
        // 
        //=====================================================================
        private void editMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (WorkoutEditor.workoutEditorInstance == null)
            {
                WorkoutEditor WorkoutEditor1 = new WorkoutEditor(defaultDirectory);
                WorkoutEditor1.Left = this.Left + this.Width;
                WorkoutEditor1.Top = this.Top;
                WorkoutEditor1.Show();
            }
            else if (WorkoutEditor.workoutEditorInstance.WindowState == WindowState.Minimized)
            {
                WorkoutEditor.workoutEditorInstance.WindowState = WindowState.Normal;
            }
            else
            {
                WorkoutEditor.workoutEditorInstance.Focus();
            }
        }


        //=====================================================================
        // Method: private void voiceMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description: 
        //   This is the voice control menu click event handler.
        //
        // History:
        // 05-09-2020, MA: First version completed.
        // 
        //=====================================================================
        private void voiceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem smi = (MenuItem)e.OriginalSource;
            if (smi != null)
            {
                // Enable voice commands if an audio input device is present.
                int inputdevices = WaveIn.DeviceCount;
                if (((string)smi.Header == "Enabled") && (inputdevices >= 1))
                {
                    // Update menu item checks.
                    menuItemVoiceEnabled.IsChecked = true;
                    menuItemVoiceDisabled.IsChecked = false;

                    // Start the VoiceCommandProcessor thread.
                    startVoiceCommandProcessor();

                    // Update status text.
                    writeStatusText("Voice commands enabled.", 3000);
                }
                else

                // Disable voice commands.

                {
                    // Update menu item checks.
                    menuItemVoiceEnabled.IsChecked = false;
                    menuItemVoiceDisabled.IsChecked = true;

                    // Set the shared data flag to indicate that the VoiceCommandProcessor thread
                    // should not be active.
                    voiceCommandData.setAcquireCommandState(false);

                    // Update status text.
                    if ((string)smi.Header == "Enabled")
                    {
                        writeStatusText("No microphone present, voice commands disabled.", 3000);
                    }
                    else
                    {
                        writeStatusText("Voice commands disabled.", 3000);
                    }
                }
            }
        }


        //=====================================================================
        // Method:	private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        //          private void windowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //          private void shutdown()
        //
        // Description:
        //  Event handlers and methods for ending execution.
        //
        // History:
        //  04-17-2020, MA: First version completed.
        // 
        //=====================================================================
        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            shutdown();
        }

        private void windowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            shutdown();
        }

        private void shutdown()
        {
            // Make sure the voice processing thread is not running.
            stopVoiceCommandProcessor(0);

            // Stop the treadmill if it is running.
            updateGUIControls(TreadmillState.Stopped);
            workoutSegmentProcessing(WorkoutState.Stop);

            // Update the startup configuration file.
            saveControlStateFile();

            Application.Current.Shutdown();
            //Environment.Exit(0);
        }


        //=====================================================================
        //=====================================================================
        // ComboBox changed event handlers.
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private void comboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //
        // Description:
        //  Event handler for a selection change of the treadmill operational mode.
        //
        // History:
        //  04-19-2020, MA: First version completed.
        // 
        //=====================================================================
        private void comboBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Make sure the treadmill is stopped since this control should only be accessible
            // in the stopped state.
            if (currentTreadmillState != TreadmillState.Stopped)
                return;

            updateWorkoutDisplay();
        }

        //=====================================================================
        // Method:	private void comboBoxSelectedWorkout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //
        // Description:
        //  Event handler for a selection change of the selected workout.
        //
        // History:
        //  04-19-2020, MA: First version completed.
        // 
        //=====================================================================
        private void comboBoxSelectedWorkout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Make sure the treadmill is stopped since this control should only be accessible
            // in the stopped state.
            if (currentTreadmillState != TreadmillState.Stopped)
                return;

            updateWorkoutDisplay();
        }


        //=====================================================================
        //=====================================================================
        // General methods.
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private void workoutSegmentProcessing(WorkoutState wstate)
        //
        // Description:
        //  Depending on the wstate parameter and global state variables, updates the 
        //  workout segment processing.
        //
        // History:
        //  08-10-2019, MA: First version completed.
        //  04-20-2020, MA: Modified for manual mode handling.
        //
        //=====================================================================
        private void workoutSegmentProcessing(WorkoutState wstate)
        {
            // Handle the "Manual" mode.
            
            if (comboBoxMode.SelectedIndex == 0)
            {
                // Handle the case of a workout segment having been just completed. 
                if (wstate == WorkoutState.SegmentDone)
                {
                    // Stop the reset timer.
                    segmentTimer.Stop();

                    // If that was the last segment in the manual workout, the workout is finished and the
                    // treadmill should be signaled to stop.
                    manualCurrentSegment = manualCurrentSegment + 1;
                    if (manualCurrentSegment >= manualTreadmillWorkout.workoutSegments.Count)
                    {
                        manualCurrentSegment = 0;
                        segmentStopwatch.Stop();
                        segmentStopwatch.Reset();
                        currentTreadmillState = TreadmillState.Stopped;
                        treadmillSetSpeed = 10;
                        updateGUIControls(currentTreadmillState);
                        updateTreadmillState();
                    }

                    // Otherwise, the next workout segment should be setup and started. There is no need to update the speed and incline.
                    else
                    {
                        segmentTimer.Start();
                    }
                }

                // Handle the case of a start request.
                if (wstate == WorkoutState.Start)
                {
                    // Stop and reset the timers.
                    segmentTimer.Stop();
                    segmentStopwatch.Stop();
                    segmentStopwatch.Reset();

                    // Setup and start running the first workout segment.
                    // The speed and incline of the segment are given by the current treadmillSetSpeed
                    // and treadmillIncline values.
                    manualCurrentSegment = 0;
                    updateManualTreadmillWorkout();
                    currentTreadmillState = TreadmillState.Running;
                    updateTreadmillState();
                    segmentTimer.Start();
                    segmentStopwatch.Start();
                }

                // Handle the case of a stop request.
                if (wstate == WorkoutState.Stop)
                {
                    // Stop and reset the timers.
                    segmentTimer.Stop();
                    segmentStopwatch.Stop();
                    segmentStopwatch.Reset();

                    // Signal that the treadmill should be stopped.
                    manualCurrentSegment = 0;
                    currentTreadmillState = TreadmillState.Stopped;
                    treadmillSetSpeed = 10;
                    updateTreadmillState();
                }

                // Handle the case of a pause request.
                if (wstate == WorkoutState.Pause)
                {
                    // Stop but do not reset the timers.
                    segmentTimer.Stop();
                    segmentStopwatch.Stop();

                    // Signal that the treadmill should be paused.
                    currentTreadmillState = TreadmillState.Paused;
                    updateTreadmillState();
                }

                // Handle the case of a resume request.
                if (wstate == WorkoutState.Resume)
                {
                    // Resume the timers.
                    segmentTimer.Start();
                    segmentStopwatch.Start();

                    // Signal that the treadmill operation should be resumed.
                    currentTreadmillState = TreadmillState.Running;
                    updateTreadmillState();
                }

                updateWorkoutDisplay();

                return;
            }

            // Handle the "Run workout" mode.

            // Error check. Do nothing if the selected workout is not valid.
            if (treadmillWorkouts.Count == 0)
            {
                return;
            }
            try
            {
                if(treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments.Count == 0)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            // Handle the case of a workout segment having been just completed. 
            if (wstate == WorkoutState.SegmentDone)
            {
                // Stop and reset the timers.
                segmentTimer.Stop();

                // If that was the last segment in the workout, the workout is finished and the
                // treadmill should be signaled to stop.
                currentSegment = currentSegment + 1;
                if(currentSegment >= treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments.Count)
                {
                    currentSegment = 0;
                    segmentStopwatch.Stop();
                    segmentStopwatch.Reset();
                    currentTreadmillState = TreadmillState.Stopped;
                    treadmillSetSpeed = 10;
                    updateGUIControls(currentTreadmillState);
                    updateTreadmillState();
                }

                // Otherwise, the next workout segment should be setup and started.
                else
                {
                    treadmillSetSpeed = treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments[currentSegment].speed;
                    treadmillIncline = treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments[currentSegment].incline;
                    currentTreadmillState = TreadmillState.Running;
                    updateTreadmillState();
                    segmentTimer.Start();
                }
            }

            // Handle the case of a start request.
            if (wstate == WorkoutState.Start)
            {
                // Stop and reset the timers.
                segmentTimer.Stop();
                segmentStopwatch.Stop();
                segmentStopwatch.Reset();

                // Setup and start running the first workout segment.
                currentSegment = 0;
                treadmillSetSpeed = treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments[currentSegment].speed;
                treadmillIncline = treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments[currentSegment].incline;
                currentTreadmillState = TreadmillState.Running;
                updateTreadmillState();
                segmentTimer.Start();
                segmentStopwatch.Start();
            }

            // Handle the case of a stop request.
            if (wstate == WorkoutState.Stop)
            {
                // Stop and reset the timers.
                segmentTimer.Stop();
                segmentStopwatch.Stop();
                segmentStopwatch.Reset();

                // Signal that the treadmill should be stopped.
                currentSegment = 0;
                currentTreadmillState = TreadmillState.Stopped;
                treadmillSetSpeed = 10;
                updateTreadmillState();
            }

            // Handle the case of a pause request.
            if (wstate == WorkoutState.Pause)
            {
                // Stop but do not reset the timers.
                segmentTimer.Stop();
                segmentStopwatch.Stop();

                // Signal that the treadmill should be paused.
                currentTreadmillState = TreadmillState.Paused;
                updateTreadmillState();
            }

            // Handle the case of a resume request.
            if (wstate == WorkoutState.Resume)
            {
                // Resume the timers.
                segmentTimer.Start();
                segmentStopwatch.Start();

                // Signal that the treadmill operation should be resumed.
                currentTreadmillState = TreadmillState.Running;
                updateTreadmillState();
            }

            updateWorkoutDisplay();
        }


        //=====================================================================
        // Method:	private void handleVoiceCommand(int command)
        //
        // Description:
        //  Performs the operation specified by the passed voice command.
        //
        // History:
        //  05-15-2020, MA: First version completed.
        //
        //=====================================================================
        private void handleVoiceCommand(int command)
        {
            int basecommand;
            int commandparameter;

            // Do nothing for a negative (error) or zero command.
            if(command <= 0)
            {
                return;
            }

            // Parse the command.
            basecommand = command & 0x00FF;
            commandparameter = command >> 8;

            // Perform the operation specified by the command.

            switch (basecommand)
            {
                // Simple command: start and resume.
                case 1:
                case 2:
                    if (currentTreadmillState != TreadmillState.Running)
                    {
                        startPauseTreadmillToggle();
                    }
                    break;

                // Simple command: pause.
                case 3:
                    if (currentTreadmillState == TreadmillState.Running)
                    {
                        startPauseTreadmillToggle();
                    }
                    break;

                // Simple command: stop.
                case 4:
                    if ((currentTreadmillState == TreadmillState.Running) || (currentTreadmillState == TreadmillState.Paused))
                    {
                        stopTreadmill();
                    }
                    break;

                // Simple command: increase incline.
                case 5:
                    inclineUp();
                    break;

                // Simple command: decrease incline.
                case 6:
                    inclineDown();
                    break;

                // Simple command: increase speed.
                case 7:
                case 8:
                    speedUp();
                    break;

                // Simple command: decrease speed.
                case 9:
                case 10:
                    speedDown();
                    break;

                // Simple command: open the treadmill window.
                case 11:
                    windowMain.WindowState = WindowState.Normal;
                    windowMain.Activate();
                    break;

                // Simple command: minimize the treadmill window.
                case 12:
                    windowMain.WindowState = WindowState.Minimized;
                    break;

                // Command with a number: set speed.
                case 16:
                    if((commandparameter > 0) && (commandparameter < 6))
                    {
                        setSpeed((byte)(commandparameter * 10));
                    }
                    break;

                // Command with a number: set incline.
                case 17:
                    if ((commandparameter > 0) && (commandparameter < 13))
                    {
                        setIncline((byte)(commandparameter * 10));
                    }
                    break;

                // Command with a number: set workout number.
                case 18:
                    if ((commandparameter >= 0) && (commandparameter < treadmillWorkouts.Count)
                        && (currentTreadmillState == TreadmillState.Stopped))
                    {
                        comboBoxSelectedWorkout.SelectedIndex = commandparameter - 1;
                        updateWorkoutDisplay();
                    }
                    break;

                // Command to set manual mode.
                case 32:
                    if ((currentTreadmillState == TreadmillState.Stopped) &&
                        (comboBoxMode.SelectedIndex == 1))
                    {
                        comboBoxMode.SelectedIndex = 0;
                        updateWorkoutDisplay();
                    }
                    break;

                // Command to set run workout mode.
                case 33:
                    if ((currentTreadmillState == TreadmillState.Stopped) &&
                        (comboBoxMode.SelectedIndex == 0))
                    {
                        comboBoxMode.SelectedIndex = 1;
                        updateWorkoutDisplay();
                    }
                    break;

                default:
                    break;
            }

        }


        //=====================================================================
        // Method:	private void updateTreadmillState()
        //
        // Description:
        //  Based on the values of the treadmill state fields (treadmillRunning, 
        //  treadmillSetSpeed, and treadmillIncline), the running state of the treadmill
        //  is changed through a call to IFitCommunication.setSpeedIncline().
        //
        // History:
        //  07-31-2019, MA: First version completed.
        //  05-02-2020, MA: Added sleeps so that the command transmission would end before
        //    the execution continues.
        //  05-14-2020, MA: Changed from IFitCommunication to NAudioIFitCommunication.
        //
        //=====================================================================
        private void updateTreadmillState()
        {
            // Stop the update timer.
            //updateTimer.Stop();

            // Send the appropriate audio command to the treadmill.
            if(currentTreadmillState == TreadmillState.Running)
            {
                for (int i = 0; i < COMMAND_REPEAT_NUMBER; i++)
                {
                    //IFitCommunication.setSpeedIncline(treadmillSetSpeed, treadmillIncline);
                    //Thread.Sleep(COMMAND_WAIT);
                    NAudioiFitCommunication.setSpeedIncline(treadmillSetSpeed, treadmillIncline, selectedAudioDeviceNumber - 1);
                    labelTimeValue.Refresh();
                }
            }
            else
            {
                //IFitCommunication.setSpeedIncline(IFitCommunication.IGNORE_SPEED, treadmillIncline);
                //Thread.Sleep(COMMAND_WAIT);
                //IFitCommunication.setSpeedIncline(IFitCommunication.PAUSE_TREADMILL, treadmillIncline);
                //Thread.Sleep(COMMAND_WAIT);
                NAudioiFitCommunication.setSpeedIncline(NAudioiFitCommunication.IGNORE_SPEED, treadmillIncline, selectedAudioDeviceNumber - 1); 
                NAudioiFitCommunication.setSpeedIncline(NAudioiFitCommunication.PAUSE_TREADMILL, treadmillIncline, selectedAudioDeviceNumber - 1);
            }

            // Start the update timer.
            //updateTimer.Start();
        }


        //=====================================================================
        // Method: private void startVoiceCommandProcessor()
        //         private void stopVoiceCommandProcessor()
        //
        // Description: 
        //   These methods start and stop the voice command processing thread. 
        //
        // History:
        // 05-10-2020, MA: First version completed.
        // 
        //=====================================================================
        private void startVoiceCommandProcessor()
        {
            // If the voice command processor thread is already running, signal for it to
            // stop before starting it again.
            stopVoiceCommandProcessor(1000);

            // Start the voice command processor thread.

            voiceCommandData.setAcquireCommandState(true);
            voiceCommandData.setVoiceCommand(0, false);
            VoiceCommandProcessor vcp = new VoiceCommandProcessor(voiceCommandData);
            voiceCommandThread = new Thread(new ThreadStart(vcp.getVoiceCommands));
            voiceCommandThread.Start();
            voiceCommandThread.Priority = ThreadPriority.Normal;
        }

        private void stopVoiceCommandProcessor(int delay)
        {
            if (voiceCommandThread != null)
            {
                if (voiceCommandThread.IsAlive)
                {
                    voiceCommandData.setAcquireCommandState(false);
                    Thread.Sleep(delay);
                    if (voiceCommandThread.IsAlive)
                    {
                        voiceCommandThread.Abort();
                    }
                }
            }
        }


        //=====================================================================
        // Method:	private void updateManualTreadmillWorkout()
        //
        // Description:
        //  Updates manualTreadmillWorkout to reflect the current speed and incline
        //  settings.
        //
        // History:
        //  04-20-2020, MA: First version completed.
        //
        //=====================================================================

        private void updateManualTreadmillWorkout()
        {
            // Handle the "Manual" mode.

            if (comboBoxMode.SelectedIndex == 0)
            {
                for (int i = manualCurrentSegment; i < MANUAL_WORKOUT_SEGMENTS; i++)
                {
                    manualTreadmillWorkout.workoutSegments[i] = new TreadmillWorkout.WORKOUT_SEGMENT(treadmillSetSpeed, treadmillIncline, 30, 30);
                }
                manualTreadmillWorkout.setTotalDurations();
            }

            // Handle the "Run workout" mode.

            if (comboBoxMode.SelectedIndex == 1)
            {

            }
        }


        //=====================================================================
        // Method:	private void updateStatisticsDisplay()
        //
        // Description:
        //  This method updates the run statistics displayed on the GUI.  
        //
        // History:
        //  08-01-2019, MA: First version completed.
        //  04-22-2020, MA: Added labelStatus updates.
        //
        //=====================================================================
        private void updateStatisticsDisplay()
        {
            string speedtext;
            string stext;
            string outtext;
            string[] running = { @".    ", @" .   ", @"  .  ", @"   . ", @"    ." }; 

            if (currentTreadmillState == TreadmillState.Running)
            {
                speedtext = String.Format("{0:F1}", treadmillSetSpeed / 10.0);

                if (comboBoxMode.SelectedIndex == 0)
                {
                    stext = "Treadmill is running in manual mode.";
                }
                else
                {
                    stext = "Treadmill is running workout segment " + currentSegment.ToString() + running[(int)(treadmillRunTime.Milliseconds/200) % 5];
                }
                firstRun = false;
            }
            else if(currentTreadmillState == TreadmillState.Paused)
            {
                speedtext = "0.0";
                stext = "Treadmill is paused.";
            }
            else if(currentTreadmillState == TreadmillState.Stopped)
            {
                speedtext = "0.0";
                stext = "Treadmill is stopped.";
            }
            else
            {
                speedtext = "0.0";
                stext = "Treadmill is stopped.";
            }

            if((firstRun) || (statusTextStopwatch.ElapsedMilliseconds < statusTextTimeDuration))
            {
                labelStatus.Content = statusText;
            }
            else
            {
                labelStatus.Content = stext;
            }

            labelSpeedValue.Content = speedtext;

            outtext = String.Format("{0:F1}", treadmillIncline / 10.0);
            labelInclineValue.Content = outtext;

            outtext = String.Format("{0:F2}", treadmillRunDistance);
            labelDistanceValue.Content = outtext;

            outtext = string.Format("{0:D2}:{1:D2}:{2:D2}",
                treadmillRunTime.Hours, treadmillRunTime.Minutes, treadmillRunTime.Seconds);
            labelTimeValue.Content = outtext;
        }


        //=====================================================================
        // Method:	void writeStatusText(string text, long duration)
        //
        // Description:
        //  This method displays status text that will remain in the status bar for
        //  at least duration ms before it will be overwritten by the status messages
        //  generated within the updateStatisticsDisplay method. If duration is zero or
        //  negative, firstRun is set to true so that the text will remain until the treadmill
        //  is changed to the running state or a different message is set with this 
        //  method.
        //
        // History:
        //  05-14-2020, MA: First version completed.
        //
        //=====================================================================
        void writeStatusText(string text, long duration)
        {
            statusTextStopwatch.Restart();
            if (duration <= 0)
            {
                firstRun = true;
            }
            else
            {
                firstRun = false;
                statusTextTimeDuration = duration;
            }
            statusText = text; 
        }


        //=====================================================================
        // Method: private void updateGUIControls(TreadmillState tstate)
        //
        // Description:
        //  This method updates the GUI controls.  
        //
        // History:
        //  08-11-2019, MA: First version completed.
        //
        //=====================================================================
        private void updateGUIControls(TreadmillState tstate)
        {
            if(tstate == TreadmillState.Stopped)
            {
                buttonStartPause.Content = "Start";
                comboBoxMode.IsEnabled = true;
                comboBoxSelectedWorkout.IsEnabled = true;
                menuItemOpenWorkout.IsEnabled = true;
                menuItemDeviceSelect.IsEnabled = true;
            }
            else if(tstate == TreadmillState.Running)
            {
                buttonStartPause.Content = "Pause";
                comboBoxMode.IsEnabled = false;
                comboBoxSelectedWorkout.IsEnabled = false;
                menuItemOpenWorkout.IsEnabled = false;
                menuItemDeviceSelect.IsEnabled = false;
            }
            else if(tstate == TreadmillState.Paused)
            {
                buttonStartPause.Content = "Resume";
                comboBoxMode.IsEnabled = false;
                comboBoxSelectedWorkout.IsEnabled = false;
                menuItemOpenWorkout.IsEnabled = false;
                menuItemDeviceSelect.IsEnabled = false;
            }
        }

        
        //=====================================================================
        // Method:	private void updateSelectedWorkoutComboBox()
        //
        // Description:
        //  This method updates the comboBoxSelectedWorkout collection with the
        //  workout names contained in treadmillWorkouts.  
        //
        // History:
        //  08-07-2019, MA: First version completed.
        //  04-24-2020, MA: Added the removal and addition of the event handler.
        //
        //=====================================================================
        private void updateSelectedWorkoutComboBox()
        {

            // Remove the selectedWorkoutComboBox event handler while the combobox is being updated. 
            comboBoxSelectedWorkout.SelectionChanged -= new System.Windows.Controls.SelectionChangedEventHandler(comboBoxSelectedWorkout_SelectionChanged);

            // Update the combobox items.
            if (treadmillWorkouts.Count > 0)
            {
                treadmillWorkoutNames.Clear();
                foreach (TreadmillWorkout twitem in treadmillWorkouts)
                {
                    treadmillWorkoutNames.Add(twitem.workoutName);
                }
                comboBoxSelectedWorkout.SelectedIndex = 0;
            }

            // Add back the selectedWorkoutComboBox event handler. 
            comboBoxSelectedWorkout.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(comboBoxSelectedWorkout_SelectionChanged);
        }


        //=====================================================================
        // Method:	private void updateWorkoutDisplay()
        //
        // Description:
        //  This method updates the workout display based on the operating state
        //  of the treadmill.
        //
        // History:
        //  04-19-2020, MA: First version completed.
        // 
        //=====================================================================
        private void updateWorkoutDisplay()
        {
            // Do nothing if the treadmillWorkoutDisplay is NULL. This check is required since this method
            // will be called during initialization and prior to the setting of treadmillWorkoutDisplay.

            if (treadmillWorkoutDisplay != null)
            {
                // Handle the "Manual" mode.

                if (comboBoxMode.SelectedIndex == 0)
                {
                    if (manualCurrentSegment > WorkoutDisplay.WORKOUT_SEGMENT_SHIFT)
                    {
                        treadmillWorkoutDisplay.plotWorkout(manualTreadmillWorkout, manualCurrentSegment - WorkoutDisplay.WORKOUT_SEGMENT_SHIFT,
                            manualTreadmillWorkout.workoutSegments.Count, manualCurrentSegment, manualCurrentSegment);
                    }
                    else
                    {
                        treadmillWorkoutDisplay.plotWorkout(manualTreadmillWorkout, 0,
                            manualTreadmillWorkout.workoutSegments.Count, manualCurrentSegment, manualCurrentSegment);
                    }
                }

                // Handle the "Run workout" mode.

                if (comboBoxMode.SelectedIndex == 1)
                {
                    if(currentSegment > WorkoutDisplay.WORKOUT_SEGMENT_SHIFT)
                    {
                        treadmillWorkoutDisplay.plotWorkout(treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex], currentSegment - WorkoutDisplay.WORKOUT_SEGMENT_SHIFT, 
                            treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments.Count, currentSegment, currentSegment);
                    }
                    else
                    {
                        treadmillWorkoutDisplay.plotWorkout(treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex], 0, 
                            treadmillWorkouts[comboBoxSelectedWorkout.SelectedIndex].workoutSegments.Count, currentSegment, currentSegment);
                    }
                }
            }
        }


        //=====================================================================
        // Method: private void updateAudioDeviceList()
        //
        // Description: 
        //   This method enumurates the available audio output devices and updates
        //   the devices menu items.
        //
        // History:
        // 05-08-2020, MA: First version completed.
        // 05-19-2020, MA: Added ability to find previously selected device in the new list.
        // 
        //=====================================================================
        private void updateAudioDeviceList()
        {
            List<string> devicenames = new List<string>();

            selectedAudioDeviceNumber = 0;
            menuItemDeviceSelect.Items.Clear();

            devicenames = NAudioiFitCommunication.enumerateDevices();
            if (devicenames.Count() > 0)
            {
                for (int dn = 0; dn < devicenames.Count(); dn++)
                {
                    MenuItem menudev = new MenuItem();
                    menudev.Header = devicenames[dn];
                    menudev.Background = new SolidColorBrush(Color.FromArgb(255, 204, 213, 240));
                    menudev.IsChecked = false;
                    menudev.Click += new RoutedEventHandler(deviceMenuItem_Click);
                    menuItemDeviceSelect.Items.Add(menudev);
                    if (devicenames[dn] == selectedAudioDeviceName)
                    {
                        selectedAudioDeviceNumber = dn;
                    }
                }
                selectedAudioDeviceName = devicenames[selectedAudioDeviceNumber];
                ((MenuItem)menuItemDeviceSelect.Items[selectedAudioDeviceNumber]).IsChecked = true;
            }
        }


        //=====================================================================
        // Method:	private void defaultFocus()
        //
        // Description:
        //  Sets the focus of this object to a specific control.
        //
        // History:
        //  07-31-2019, MA: First version completed.
        // 
        //=====================================================================
        private void defaultFocus()
        {
            buttonStartPause.Focus();
        }


        //=====================================================================
        // Method: private void readTreadmillWorkouts()
        //
        // Description:
        //  This method loads all workouts from files contained in the default directory.  
        //
        // History:
        //  04-16-2020, MA: First version completed.
        //
        //=====================================================================
        private void readTreadmillWorkouts()
        {
            int filereaderror;
            TreadmillWorkout tempworkout;
            string[] fileEntries = { "" };

            // Get a list of files from the default directory.
            try
            {
                fileEntries = Directory.GetFiles(defaultDirectory);
            }
            catch
            {
                return;
            }

            // Process the list of files.
            foreach(string filespec in fileEntries)
            {
                // Process only *.wrk files.
                string capsfilespec = filespec.ToUpper();
                if(!capsfilespec.Contains(".WRK"))
                {
                    continue;
                }

                // Read the workout into tempworkout.
                tempworkout = new TreadmillWorkout();
                filereaderror = tempworkout.loadWorkout(filespec);

                // Error check and add workout to workout list.
                if((filereaderror == 0) && (tempworkout.workoutSegments.Count > 0))
                {
                    treadmillWorkouts.Add(tempworkout);
                }
            }
        }


        //=====================================================================
        // Method: private bool saveControlStateFile()
        //
        // Description:
        //  This method saves the current values of various controls to a text file.
        //  These values can be later read and set using readControlStateFile().
        //
        // History:
        //  10-28-2020, MA: First version completed.
        //
        //=====================================================================
        private bool saveControlStateFile()
        {
            string configfilespec = "";
            StreamWriter outstream;

            // Construct the filespec.
            configfilespec = defaultDirectory + @"\NotSoFitTreadmill.cfg";

            // Open the configuration file.
            try
            {
                outstream = File.CreateText(configfilespec);
            }
            catch
            {
                return false;
            }

            // Write the formatted ASCII configuration file.
            try
            {
                outstream.WriteLine(((int)windowScale).ToString("0"));
                outstream.WriteLine(selectedAudioDeviceNumber.ToString("0"));
                outstream.WriteLine(selectedAudioDeviceName);
                outstream.WriteLine(comboBoxMode.SelectedIndex.ToString("0"));
                outstream.WriteLine(comboBoxSelectedWorkout.SelectedIndex.ToString("0"));
                outstream.WriteLine(comboBoxSelectedWorkout.SelectedItem.ToString());
            }
            catch
            {
                outstream.Close();
                return false;
            }

            outstream.Close();
            return true;
        }


        //=====================================================================
        // Method: private bool readControlStateFile()
        //
        // Description:
        //  This method reads a text file containing values of various controls previously saved
        //  using saveControlStateFile(). The GUI is then updated to these control values.
        //
        // History:
        //  10-28-2020, MA: First version completed.
        //
        //=====================================================================
        private bool readControlStateFile()
        {
            string readstring = "";
            string configfilespec = "";
            StreamReader instream;
            int index;

            // Construct the filespec.
            configfilespec = defaultDirectory + @"\NotSoFitTreadmill.cfg";

            // Open the configuration file.
            try
            {
                instream = File.OpenText(configfilespec);
            }
            catch
            {
                return false;
            }

            // Read the formatted ASCII configuration file.
            // Set the GUI control states as specified by the configuration file.
            try
            {
                // Read and set the window scale.
                readstring = instream.ReadLine();
                windowScale = (WindowScale)Int32.Parse(readstring);

                // Read and set the audio output device. 
                readstring = instream.ReadLine();
                index = Int32.Parse(readstring);
                readstring = instream.ReadLine();
                if (menuItemDeviceSelect.Items.Count > index)
                {
                    for (int i  = 0; i < menuItemDeviceSelect.Items.Count; i++)
                    {
                        if (i == index)
                        {
                            ((MenuItem)menuItemDeviceSelect.Items[i]).IsChecked = true;
                            selectedAudioDeviceNumber = i;
                            selectedAudioDeviceName = ((MenuItem)menuItemDeviceSelect.Items[i]).ToString();  
                        }
                        else
                        {
                            ((MenuItem)menuItemDeviceSelect.Items[i]).IsChecked = false;
                        }
                    }
                }

                // Read and set the comboBoxMode. 
                readstring = instream.ReadLine();
                index = Int32.Parse(readstring);
                comboBoxMode.SelectedIndex = index;

                // Read and set the comboBoxSelectedWorkout. 
                readstring = instream.ReadLine();
                index = Int32.Parse(readstring);
                if (comboBoxSelectedWorkout.Items.Count > index)
                {
                    comboBoxSelectedWorkout.SelectedIndex = index;
                }
            }
            catch
            {
                instream.Close();
                return false;
            }

            instream.Close();
            return true;
        }
    }




    //=====================================================================
    //=====================================================================
    // Class: NotificationClient
    // Description:
    //  This is the class that in combination with an NAudio.CoreAudioApi.MMDeviceEnumerator
    //  object enables the handling of audio device event changes.
    //
    //=====================================================================
    //=====================================================================
    class NotificationClient : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
    {
        public string stateChangedDeviceID;
        public DeviceState stateChangedNewState;
        public string deviceAddedDeviceID;
        public string deviceRemovededDeviceID;
        public string defaultDeviceChangedDeviceID;
        public bool deviceStateChanged;
        public bool deviceAdded;
        public bool deviceRemoved;
        public bool deviceDefaultChanged;
        public bool deviceAnyChange;

        public NotificationClient()
        {
            stateChangedDeviceID = "";
            stateChangedNewState = new DeviceState();
            deviceAddedDeviceID = "";
            deviceRemovededDeviceID = "";
            defaultDeviceChangedDeviceID = "";
            deviceStateChanged = false;
            deviceAdded = false;
            deviceRemoved = false;
            deviceDefaultChanged = false;
            deviceAnyChange = false;
    }

    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            //Console.WriteLine("OnDeviceStateChanged, device ID = {0}, device state = {1}\n", deviceId, newState);
            stateChangedDeviceID = deviceId;
            stateChangedNewState = newState;
            deviceStateChanged = true;
            deviceAnyChange = true;
        }

        void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) 
        {
            //Console.WriteLine("OnDeviceAdded, device ID = {0}\n", pwstrDeviceId);
            deviceAddedDeviceID = pwstrDeviceId;
            deviceAdded = true;
            deviceAnyChange = true;
        }

        void IMMNotificationClient.OnDeviceRemoved(string deviceId) 
        {
            //Console.WriteLine("OnDeviceRemoved, device ID = {0}\n", deviceId);
            deviceRemovededDeviceID = deviceId;
            deviceRemoved = true;
            deviceAnyChange = true;
        }

        void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) 
        {
            //Console.WriteLine("OnDefaultDeviceChanged, device ID = {0}\n", defaultDeviceId);
            defaultDeviceChangedDeviceID = defaultDeviceId;
            deviceDefaultChanged = true;
            deviceAnyChange = true;
        }

        void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) 
        { 
        }
    }
}
