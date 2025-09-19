//*****************************************************************************
//*****************************************************************************
// File: TreadmillWorkout.cs
// Hacker: M. Amman
// Description:
//   This file contains the TreadmillWorkout class.
//
// Required classes:
//
// History:
//   See the notes for the individual methods below.
//   08-04-2019, MA: First version completed.
//   04-16-2020, MA: Set the segment duration to be 30 s for all segments. This was done
//     so that the WorkoutDisplay class could be written assuming a fixed segment duration.
//   06-17-2020, MA: Added statistics methods.
//   06-21-2021, MA: Added methods for saving and modifying workouts.
//
//*****************************************************************************
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace NotSoFitTreadmill
{
    //=====================================================================
    // Class: TreadmillWorkout
    // Description:
    //  See below.
    //
    //=====================================================================
    //=====================================================================
    /// <summary>
    /// The TreadmillWorkout class contains the treadmill workout data and associated methods.
    /// The workout data consists of an array of workout segments with each segment containing 
    /// a treadmill speed, treadmill incline, and segment duration.
    /// </summary>
    public class TreadmillWorkout
    {
        //---------------------------------------------------------------------------
        // Constants
        //---------------------------------------------------------------------------

        /// <summary>
        /// MAXIMUM_WORKOUT_SEGMENTS is the maximum number of segments allowed in a workout.
        /// This is currently not used.
        /// </summary>
        public const int MAXIMUM_WORKOUT_SEGMENTS = 1000;

        /// <summary>
        /// MINIMUM_SEGMENT_SPEED is the minimum speed in miles per hour times 10 that a workout 
        /// segment can be.
        /// </summary>
        public const byte MINIMUM_SEGMENT_SPEED = (byte)5;

        /// <summary>
        /// MAXIMUM_SEGMENT_SPEED is the maximum speed in miles per hour times 10 that a workout 
        /// segment can be.
        /// </summary>
        public const byte MAXIMUM_SEGMENT_SPEED = (byte)100;

        /// <summary>
        /// SPEED_INCREMENT is the allowable speed step size in miles per hour times 10.
        /// </summary>
        public const byte SPEED_INCREMENT = (byte)1;

        /// <summary>
        /// MINIMUM_SEGMENT_INCLINE is the minimum incline in percent grade times 10 that a workout 
        /// segment can be.
        /// </summary>
        public const byte MINIMUM_SEGMENT_INCLINE = (byte)0;

        /// <summary>
        /// MAXIMUM_SEGMENT_INCLINE is the maximum incline in percent grade times 10 that a workout 
        /// segment can be.
        /// </summary>
        public const byte MAXIMUM_SEGMENT_INCLINE = (byte)120;

        /// <summary>
        /// INCLINE_INCREMENT is the allowable incline step size in percent grade times 10.
        /// </summary>
        public const byte INCLINE_INCREMENT = (byte)5;

        /// <summary>
        /// MINIMUM_SEGMENT_DURATION is the minimum time duration in seconds that a workout 
        /// segment can be.
        /// </summary>
        public const int MINIMUM_SEGMENT_DURATION = 30;

        /// <summary>
        /// MAXIMUM_SEGMENT_DURATION is the maximum time duration in seconds that a workout 
        /// segment can be.
        /// </summary>
        public const int MAXIMUM_SEGMENT_DURATION = 30;

        //---------------------------------------------------------------------------
        // Workout segment structure definition
        //---------------------------------------------------------------------------

        /// <summary>
        /// WORKOUT_SEGMENT is a structure data type that holds the information defining
        /// a single segment of a workout. A segment is a period of time with a constant treadmill
        /// speed and incline.
        /// </summary>
        public struct WORKOUT_SEGMENT
        {
            /// <summary>
            /// speed is a single byte containing 10 times the desired miles per hour treadmill speed. 
            /// </summary>
            public byte speed;

            /// <summary>
            /// incline is a single byte containing 10 times the desired percent grade treadmill incline. 
            /// </summary>
            public byte incline;

            /// <summary>
            /// duration is the integer time length in seconds of the segment. 
            /// </summary>
            public int duration;

            /// <summary>
            /// totalduration is the integer time length in seconds of all previous segments plus this segment. 
            /// </summary>
            public int totalduration;

            /// <summary>
            /// The WORKOUT_SEGMENT structure constructor initializes the structure 
            /// data using the information contained in the parameter list. 
            /// </summary>
            /// <param name="initspeed">
            /// initspeed is a single byte containing 10 times the desired miles per hour treadmill speed.
            /// </param>
            /// <param name="initincline">
            /// initincline is a single byte containing 10 times the desired percent grade treadmill incline.
            /// </param>
            /// <param name="initduration">
            /// initduration is the integer time length in seconds of the segment. 
            /// </param>
            /// <param name="inittotalduration">
            /// inittotalduration is the integer time length in seconds of all previous segments plus this segment. 
            /// </param>

            public WORKOUT_SEGMENT(byte initspeed, byte initincline, int initduration, int inittotalduration)
            {
                if (initspeed < MINIMUM_SEGMENT_SPEED)
                {
                    speed = MINIMUM_SEGMENT_SPEED;
                }
                else if (initspeed > MAXIMUM_SEGMENT_SPEED)
                {
                    speed = MAXIMUM_SEGMENT_SPEED;
                }
                else
                {
                    speed = initspeed;
                }

                if (initincline < MINIMUM_SEGMENT_INCLINE)
                {
                    incline = MINIMUM_SEGMENT_INCLINE;
                }
                else if (initspeed > MAXIMUM_SEGMENT_INCLINE)
                {
                    incline = MAXIMUM_SEGMENT_INCLINE;
                }
                else
                {
                    incline = initincline;
                }

                if (initduration < MINIMUM_SEGMENT_DURATION)
                {
                    duration = MINIMUM_SEGMENT_DURATION;
                }
                else if (initduration > MAXIMUM_SEGMENT_DURATION)
                {
                    duration = MAXIMUM_SEGMENT_DURATION;
                }
                else
                {
                    duration = initduration;
                }

                totalduration = inittotalduration;
            }
        }

        //---------------------------------------------------------------------------
        // Fields
        //---------------------------------------------------------------------------

        /// <summary>
        /// workoutSegments is the list of WORKOUT_SEGMENTs containing the speed, incline, and 
        /// duration data defining the treadmill workout.
        /// </summary>
        public List<WORKOUT_SEGMENT> workoutSegments;

        /// <summary>
        /// workoutName is a text label identifying the workout.
        /// </summary>
        public string workoutName;


        //---------------------------------------------------------------------------
        // Properties  
        //---------------------------------------------------------------------------

        //=====================================================================
        // Constructor: public TreadmillWorkout()
        //
        // Description: 
        //  TreadmillWorkout() constructor.
        //
        // History:
        //  08-04-2019, MA: First version completed.
        //  04-16-2020, MA: Updated to implement a 30 s segment duration for the default
        //    "Incline Pyramid" workout.
        //			
        //=====================================================================
        /// <summary>
        /// The TreadmillWorkout class constructor initializes the class data
        /// to default values.  
        /// </summary>
        public TreadmillWorkout()
        {
            int tlength = 8;
            workoutName = "Incline Pyramid";
            workoutSegments = new List<WORKOUT_SEGMENT>();
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)10, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)20, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)30, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)40, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)50, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)60, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)70, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)80, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)70, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)60, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)50, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)40, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)30, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)20, 30, 30));
            }
            for (int i = 0; i < tlength; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT((byte)30, (byte)10, 30, 30));
            }

            //setTotalDurations();
        }


        //=====================================================================
        // Method: void initializeWorkout(int numbersegments, byte initspeed, byte initincline, int initduration, int inittotalduration)
        //
        // Description: 
        //  Initializes the workout to the specified parameters.
        //
        // History:
        //  06-18-2021, MA: First version completed.
        //		
        //=====================================================================
        public void initializeWorkout(int numbersegments, byte newspeed, byte newincline, int newduration, int newtotalduration)
        {
            if (newspeed < MINIMUM_SEGMENT_SPEED)
            {
                newspeed = MINIMUM_SEGMENT_SPEED;
            }
            if (newspeed > MAXIMUM_SEGMENT_SPEED)
            {
                newspeed = MAXIMUM_SEGMENT_SPEED;
            }
            if (newincline < MINIMUM_SEGMENT_INCLINE)
            {
                newincline = MINIMUM_SEGMENT_INCLINE;
            }
            if (newincline > MAXIMUM_SEGMENT_INCLINE)
            {
                newincline = MAXIMUM_SEGMENT_INCLINE;
            }
            if (newduration < MINIMUM_SEGMENT_DURATION)
            {
                newduration = MINIMUM_SEGMENT_DURATION;
            }
            if (newduration > MAXIMUM_SEGMENT_DURATION)
            {
                newduration = MAXIMUM_SEGMENT_DURATION;
            }
            workoutSegments = new List<WORKOUT_SEGMENT>();
            for (int i = 0; i < numbersegments; i++)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT(newspeed, newincline, newduration, newtotalduration));
            }
        }


        //=====================================================================
        // Method: public void insertSegment(int seg, byte newspeed, byte newincline, int newduration, int newtotalduration)
        //
        // Description: 
        //  Adds a new workout segment after the specified segment. The new segment values are
        //  provided as parameters. If the specified segment is negtive, a segment is added at the 
        //  start of the workout. 
        //
        // History:
        //  06-21-2021, MA: First version completed.
        //		
        //=====================================================================
        public void insertSegment(int seg, byte newspeed, byte newincline, int newduration, int newtotalduration)
        {
            // Force segment values to fall within the limits set by this class.

            if (newspeed < MINIMUM_SEGMENT_SPEED)
            {
                newspeed = MINIMUM_SEGMENT_SPEED;
            }
            if (newspeed > MAXIMUM_SEGMENT_SPEED)
            {
                newspeed = MAXIMUM_SEGMENT_SPEED;
            }
            if (newincline < MINIMUM_SEGMENT_INCLINE)
            {
                newincline = MINIMUM_SEGMENT_INCLINE;
            }
            if (newincline > MAXIMUM_SEGMENT_INCLINE)
            {
                newincline = MAXIMUM_SEGMENT_INCLINE;
            }
            if (newduration < MINIMUM_SEGMENT_DURATION)
            {
                newduration = MINIMUM_SEGMENT_DURATION;
            }
            if (newduration > MAXIMUM_SEGMENT_DURATION)
            {
                newduration = MAXIMUM_SEGMENT_DURATION;
            }

            // Add the segment at the start of the workout for negative seg.

            if (seg < 0)
            {
                workoutSegments.Insert(0, new WORKOUT_SEGMENT(newspeed, newincline, newduration, newtotalduration));
            }

            // Add the segment to the end of the workout for seg values larger than the 
            // size of the current workout.

            else if (seg >= workoutSegments.Count)
            {
                workoutSegments.Add(new WORKOUT_SEGMENT(newspeed, newincline, newduration, newtotalduration));
            }

            // Add the segment after seg.
            else
            {
                workoutSegments.Insert(seg + 1, new WORKOUT_SEGMENT(newspeed, newincline, newduration, newtotalduration));
            }
        }


        //=====================================================================
        // Method: public void deleteSegment(int seg)
        //
        // Description: 
        //  Deletes a workout segment. 
        //
        // History:
        //  06-21-2021, MA: First version completed.
        //		
        //=====================================================================
        public void deleteSegment(int seg)
        {
            if (seg < 0)
            {
                return;
            }
            if (seg >= workoutSegments.Count)
            {
                return;
            }
            workoutSegments.RemoveAt(seg);
        }


        //=====================================================================
        // Method: public void replaceSegment(int seg, byte newspeed, byte newincline, int newduration, int newtotalduration)
        //
        // Description: 
        //  Replaces the values associated with the specified workout segment with the
        //  specified values.
        //
        // History:
        //  06-18-2021, MA: First version completed.
        //		
        //=====================================================================
        public void replaceSegment(int seg, byte newspeed, byte newincline, int newduration, int newtotalduration)
        {
            if (seg < 0)
            {
                return;
            }
            if (seg >= workoutSegments.Count)
            {
                return;
            }
            if (newspeed < MINIMUM_SEGMENT_SPEED)
            {
                newspeed = MINIMUM_SEGMENT_SPEED;
            }
            if (newspeed > MAXIMUM_SEGMENT_SPEED)
            {
                newspeed = MAXIMUM_SEGMENT_SPEED;
            }
            if (newincline < MINIMUM_SEGMENT_INCLINE)
            {
                newincline = MINIMUM_SEGMENT_INCLINE;
            }
            if (newincline > MAXIMUM_SEGMENT_INCLINE)
            {
                newincline = MAXIMUM_SEGMENT_INCLINE;
            }
            if (newduration < MINIMUM_SEGMENT_DURATION)
            {
                newduration = MINIMUM_SEGMENT_DURATION;
            }
            if (newduration > MAXIMUM_SEGMENT_DURATION)
            {
                newduration = MAXIMUM_SEGMENT_DURATION;
            }
            workoutSegments[seg] = new WORKOUT_SEGMENT(newspeed, newincline, newduration, newtotalduration);
        }


        //=====================================================================
        // Method: public void setTotalDurations()
        //
        // Description: 
        //  Initializes the total duration values of the workout segment list.
        //
        // History:
        //  04-21-2020, MA: First version completed.
        //		
        //=====================================================================
        /// <summary>
        /// setTotalDurations initializes the total duration values of the workout segment list.
        /// </summary>
        public void setTotalDurations()
        {
            for (int seg = 1; seg < workoutSegments.Count; seg++)
            {
                workoutSegments[seg] = new WORKOUT_SEGMENT(workoutSegments[seg].speed, workoutSegments[seg].incline,
                    workoutSegments[seg].duration, workoutSegments[seg - 1].totalduration + workoutSegments[seg].duration);
            }
        }


        //=====================================================================
        // Methods: public int getSegmentStartTime(int seg)
        //          public int getWorkoutTime()
        //          public double getSegmentStartDistance(int seg)
        //          public double getWorkoutDistance()
        //
        // Description: 
        //  Set of methods for obtaining various statistics on the workout.
        //
        // History:
        //  06-17-2020, MA: First version completed.
        //		
        //=====================================================================

        public int getSegmentStartTime(int seg)
        {
            int starttime = 0;

            if ((seg < 0) || (seg >= workoutSegments.Count))
            {
                return 0;
            }

            for (int iseg = 0; iseg < seg; iseg++)
            {
                starttime = starttime + workoutSegments[iseg].duration;
            }

            return starttime;
        }

        public int getWorkoutTime()
        {
            int workouttime = getSegmentStartTime(workoutSegments.Count - 1)
                + workoutSegments[workoutSegments.Count - 1].duration;

            return workouttime;
        }

        public double getSegmentStartDistance(int seg)
        {
            double startdistance = 0.0;

            if ((seg < 0) || (seg > workoutSegments.Count))
            {
                return 0.0;
            }

            for (int iseg = 0; iseg < seg; iseg++)
            {
                startdistance = startdistance + workoutSegments[iseg].duration * workoutSegments[iseg].speed / 36000.0;
            }

            return startdistance;
        }

        public double getWorkoutDistance()
        {
            double workoutdistance = getSegmentStartDistance(workoutSegments.Count);

            return workoutdistance;
        }


        //=====================================================================
        // Method: public int loadWorkoutDialog(ref string returnedfilespec)
        //         public int loadWorkout(string filespec)
        //
        // Description: 
        //  Loads the workout from the specified text format file.
        //
        // History:
        //  08-05-2019, MA: First version completed.
        //  04-17-2020, MA: Completed loadWorkout().
        //  06-20-2021, MA: Modified to eliminate speed and incline values outside of the 
        //    range specified by the constants MINIMUM_SEGMENT_SPEED, MAXIMUM_SEGMENT_SPEED,
        //    MINIMUM_SEGMENT_INCLINE, and MAXIMUM_SEGMENT_INCLINE.
        //		
        //=====================================================================
        /// <summary>
        /// loadWorkoutDialog loads the treadmill workout data from a file. 
        /// The file is specified by the user through a dialog box.  The file
        /// is assumed to be a plain text file.
        /// </summary>
        /// <param name="returnedfilespec">
        /// returnedfilespec is the returned file specification string identifying the
        /// file chosen by the user.
        /// </param>
        /// <returns>
        /// The method returns zero if it successfully completes the data load.  Otherwise,
        /// one of the following error codes will be returned: (a) 1, the user did not select
        /// a file. (b) 2, the selected file could not be opened. (c) 3, an error occurred
        /// when reading the file.   
        /// </returns>
        public int loadWorkoutDialog(ref string returnedfilespec)
        {
            // Get a filename from the user using a dialog.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Load Treadmill Workout";
            openFileDialog1.DefaultExt = "wrk";
            openFileDialog1.Filter = "WRK files (*.wrk)|*.wrk|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName == "")
            {
                returnedfilespec = "";
                return 1;
            }

            // Attempt to load the date from the specified file.
            // Note that the FileName property actually contains both the file path and the file extension.
            returnedfilespec = openFileDialog1.FileName;
            return loadWorkout(returnedfilespec);
        }


        /// <summary>
        /// loadWorkout loads the treadmill workout data from a file. 
        /// The file is assumed to be a plain text file.
        /// </summary>
        /// <param name="filespec">
        /// filespec is the file specification of the file that will be loaded.
        /// </param>
        /// <returns>
        /// The method returns zero if it successfully completes the data load.  Otherwise,
        /// one of the following error codes will be returned: 
        /// (a) 2, the selected file could not be opened. (b) 3, an error occurred
        /// when reading the file. (c) 4, an error occurred when parsing the file data.
        /// (d) 5, the file did not contain any valid workout segments. 
        /// </returns>
        public int loadWorkout(string filespec)
        {
            string readstring = "";
            string[] lines;
            string[] words;
            string parsedline;
            char[] lineDelimiterChars = { '\r', '\n' };
            char[] wordDelimiterChars = { ' ', ',', '\t' };
            StreamReader instream;
            int testdata;
            int speed, incline;
            bool parsesuccess;

            // Initialize the data fields.
            //FileInfo fileinformation = new FileInfo(filespec);
            //workoutName = fileinformation.Name;
            workoutName = Path.GetFileNameWithoutExtension(filespec);
            workoutSegments = new List<WORKOUT_SEGMENT>();

            // Open the workout file.
            try
            {
                instream = File.OpenText(filespec);
            }
            catch
            {
                return 2;
            }

            // Read the entire file into a string to be later parsed.
            try
            {
                readstring = instream.ReadToEnd();
            }
            catch
            {
                instream.Close();
                return 3;
            }

            // Close the file.
            instream.Close();

            // Parse the file to extract workout segments.

            try
            {
                // Break the read in string into separate lines.
                lines = readstring.Split(lineDelimiterChars);

                // Parse each line.
                foreach (string line in lines)
                {
                    parsedline = line.Trim();
                    if (parsedline == "")
                        continue;

                    // Parse each word in each line until both a speed and incline have been obtained. 
                    // The duration will be set to 30.
                    words = parsedline.Split(wordDelimiterChars);
                    speed = -1;
                    incline = -1;
                    foreach (string testword in words)
                    {
                        try
                        {
                            // Attempt to parse the word into an int.
                            parsesuccess = int.TryParse(testword, out testdata);

                            if (!parsesuccess)
                            {
                                continue;
                            }
                            if (speed == -1)
                            {
                                if ((testdata < MINIMUM_SEGMENT_SPEED) || (testdata > MAXIMUM_SEGMENT_SPEED))
                                {
                                    continue;
                                }
                                else
                                {
                                    speed = (byte)testdata;
                                }
                            }
                            else
                            {
                                if ((testdata < MINIMUM_SEGMENT_INCLINE) || (testdata > MAXIMUM_SEGMENT_INCLINE))
                                {
                                    continue;
                                }
                                else
                                {
                                    incline = (byte)testdata;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    // If the line parsing resulted in a valid speed and incline, add a workout segment.
                    if ((speed != -1) && (incline != -1))
                    {
                        workoutSegments.Add(new WORKOUT_SEGMENT((byte)speed, (byte)incline, 30, 30));
                    }
                }
                setTotalDurations();
            }
            catch
            {
                return 4;
            }

            // Check if valid workout segments were obtained.
            if (workoutSegments.Count < 1)
            {
                return 5;
            }
            return 0;
        }


        //=====================================================================
        // Method: public int saveWorkoutDialog(ref string returnedfilespec)
        //         public int saveWorkout(string filespec)
        //
        // Description: 
        //  Saves the workout to the specified text format file.
        //
        // History:
        //  06-20-2021, MA: First version completed.
        //		
        //=====================================================================
        /// <summary>
        /// saveWorkoutDialog saves the speed and incline workout data to a 
        /// file.  The file is specified by the user through a dialog box.  
        /// </summary>
        /// <param name="titlestring">
        /// titlestring is the text that will be displayed within the title bar of the 
        /// dialog box.
        /// </param>
        /// <param name="returnedfilespec">
        /// returnedfilespec is the returned file specification string identifying the
        /// file chosen by the user.
        /// </param>
        /// <returns>
        /// The method returns zero if it successfully completes the file save.  Otherwise,
        /// one of the following error codes will be returned: (a) 1, the user did not select
        /// a file. (b) 2, the selected file could not be created.
        /// </returns>
        public int saveWorkoutDialog(string titlestring, ref string returnedfilespec)
        {
            // Get a filename from the user using a dialog.

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            if (titlestring == "")
            {
                saveFileDialog1.Title = "Save Workout";
            }
            else
            {
                saveFileDialog1.Title = titlestring;
            }
            saveFileDialog1.DefaultExt = "wrk";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.Filter = "Workout file (*.wrk)|*.wrk";
            saveFileDialog1.FilterIndex = 0;
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName == "")
            {
                returnedfilespec = "";
                return 1;
            }

            // Attempt to open the specified file and save the data.
            returnedfilespec = saveFileDialog1.FileName;
            return saveWorkout(returnedfilespec);
        }


        /// <summary>
        /// saveWorkout saves the speed and incline workout data to a  
        /// file.  
        /// </summary>
        /// <param name="filespec">
        /// filespec is the file specification of the file that will be written.
        /// </param>
        /// <returns>
        /// The method returns zero if it successfully completes the file save.  Otherwise,
        /// 2 is returned to indicate that the specified file could not be created.
        /// </returns>
        public int saveWorkout(string filespec)
        {
            StreamWriter outstream;

            // Create the file.
            try
            {
                outstream = File.CreateText(filespec);
            }
            catch
            {
                return 2;
            }

            // Write the file.
            for (int i = 0; i < workoutSegments.Count; i++)
            {
                outstream.WriteLine(workoutSegments[i].speed.ToString("0") + " " + workoutSegments[i].incline.ToString("0"));
            }

            // Close the output stream.
            outstream.Close();

            return 0;
        }

    }
}
