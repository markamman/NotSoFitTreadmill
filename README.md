# Not-So-Fit Treadmill

This is a Windows Presentation Foundation (WPF) desktop application for controlling an iFit-compatible treadmill using audio commands. It allows for manual control, running pre-defined workouts, and voice command operation.

## Features

*   **Manual and Workout-Driven Control:** Operate the treadmill manually or run pre-defined workout programs.
*   **Workout Editor:** A graphical interface to create, open, edit, and save custom workout files (`.wrk`).
*   **Voice Commands:** Hands-free control for starting, stopping, pausing, and adjusting speed and incline.
*   **Workout Profile Display:** A real-time graphical display of the current workout's speed and incline profile.
*   **Run Statistics:** Tracks and displays run time and distance.
*   **Audio Device Selection:** Allows the user to select the correct audio output device for communicating with the treadmill.
*   **UI Scaling:** The main window can be scaled for better visibility on different screen resolutions.

## Requirements

To build this project, you will need:
*   Microsoft Visual Studio 2022 (Community Edition is sufficient).
*   .NET Framework (the project targets net472 and uses the built-in `System.Speech` library).
*   **[Optional]** [Microsoft Visual Studio Installer Projects 2022](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2022InstallerProjects) extension. This is only required if you want to build the `NotSoFitTreadmillInstaller` project.

## Dependencies

The project relies on the following NuGet packages, which should be restored automatically by Visual Studio:

*   **NAudio:** Used to generate the audio "chirps" that send commands to the treadmill.
*   **Extended.Wpf.Toolkit:** Provides additional UI controls used in the Workout Editor.

## How to Build

1.  Clone this repository to your local machine.
2.  Open the `NotSoFitTreadmill.sln` file in Visual Studio 2022.
3.  Right-click on the solution in the Solution Explorer and select "Restore NuGet Packages".
4.  Select `Build > Build Solution` from the main menu.
5.  The compiled application (`NotSoFitTreadmill.exe`) will be located in the `bin/Debug/` or `bin/Release/` directory.

## How to Run

After building the project, you can run the `NotSoFitTreadmill.exe` executable from the output directory.

**Important:** For the application to control your treadmill, you must connect your computer's audio output to the treadmill's audio input and press the "iFit" button on the treadmill console.

### Workouts

The application loads workout files (`.wrk`) from a default directory: `c:\NotSoFitTreadmillWorkouts\`. You can create your own workouts using the built-in editor, which is accessible from the `File > Edit Workout` menu.

### Voice Commands

Voice commands can be enabled from the `Voice` menu. Once enabled, you can use commands such as:
*   "Start" / "Resume"
*   "Pause" / "Stop"
*   "Increase speed" / "Decrease speed"
*   "Increase incline" / "Decrease incline"
*   "Set speed to 5"
*   "Set incline to 8"

## Author

*   M. Amman

## Version History

*   **3.0, 09-21-2025:** Changed voice command processing to use the built-in Microsoft Speech Recognition engine, removing the need for the Microsoft Speech Platform Runtime 11. Started using GitHub for version control.
*   **2.2, 08-09-2024:** Updated to Visual Studio 2022.
*   **2.1, 06-23-2021:** Minor changes and additions to WorkoutEditor.
*   **2.0, 06-21-2021:** Added workout editor capability.
*   **1.3, 10-28-2020:** Added a startup configuration file `NotSoFitTreadmill.cfg`.
*   **1.2.2, 05-19-2020:** Installer added.
*   **1.1.0, 05-14-2020:** Added user selection of audio output device and voice command control.
*   **1.0.0, 04-22-2020:** First fully functional version completed.
