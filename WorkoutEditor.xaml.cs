//*****************************************************************************
//*****************************************************************************
// File: WorkoutEditor.xaml.cs
// Hacker: M. Amman
// Description:
//   This file contains the WorkoutEditor class that enables the graphical
//   creation, editing, and saving of treadmill workouts.
//
// Required classes:
//
// History:
//   See the notes for the individual methods below.
//   06-21-2021, MA: First fully functional version completed. 
//   06-23-2021, MA: Added mousewheel adjustment of speed/incline.
//
//*****************************************************************************
//*****************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace NotSoFitTreadmill
{
    /// <summary>
    /// Interaction logic for WorkoutEditor.xaml
    /// </summary>
    public partial class WorkoutEditor : Window
    {
        // Variable types definitions.
        //---------------------------------------------------------------------

        enum WindowScale { P100, P150, P200 }

        //---------------------------------------------------------------------
        // Constants and public static fields.
        //---------------------------------------------------------------------

        // Reference to the WindowEditor window instance.
        public static WorkoutEditor workoutEditorInstance = null;

        // Window width and height at 100 % scale.
        const int WINDOW_WIDTH = 400;
        const int WINDOW_HEIGHT = 515;

        //---------------------------------------------------------------------
        // Fields.
        //---------------------------------------------------------------------

        // Default directory containing workout files.
        private string workoutDefaultDirectory;

        // Workout being edited.
        private TreadmillWorkout editorTreadmillWorkout;

        // Workout display.
        private WorkoutDisplay editorWorkoutDisplay;

        // Group number of workout segments currently being edited (0 based).
        private int currentGroup;

        // Number of segments in each group.
        private int segmentsPerGroup;

        // Flag indicating whether the incline (true) or speed (false) are being 
        // edited.
        private bool editIncline;

        // Scale for the window and its contents.
        private WindowScale windowScale;


        //=====================================================================
        //=====================================================================
        // Initialization and shutdown methods.
        //=====================================================================
        //=====================================================================

        //=====================================================================
        // Constructor: public WorkoutEditor(string defaultdirectory)
        //
        // Description:  WorkoutEditor class constructor.
        //
        // History:
        //  06-13-2021, MA: First version completed.
        //
        //=====================================================================
        public WorkoutEditor(string defaultdirectory)
        {
            InitializeComponent();

            // Set the reference to this window.
            workoutEditorInstance = this;

            // Set the default directory location for the workouts.
            workoutDefaultDirectory = defaultdirectory;

            // Initialize the scale for the window and its contents.
            windowScale = WindowScale.P100;

            // Initialize the default treadmill workout to be edited.
            editorTreadmillWorkout = new TreadmillWorkout();
            editorTreadmillWorkout.workoutName = "New Workout";
            currentGroup = 0;
            segmentsPerGroup = 8;
            comboBoxGroupSize.SelectedIndex = 7;
            integerUpDownNumberGroups.Value = 15;
            editIncline = true;

            // Initialize the workout display.
            editorWorkoutDisplay = new WorkoutDisplay(editorWorkoutDisplayCanvas);
            writeStatusText("Dummy workout loaded.");
            setWindowScale();
            updateEditorDisplay();

            // Set the control focus.
            defaultFocus();
        }


        //=====================================================================
        // Method:	private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //
        // Description:
        //  Event handler to catch the close window request and perform a 
        //  proper shutdown of WorkoutEditor before closing the window.
        //
        // History:
        //  Prehistory, MA: First version completed.
        // 
        //=====================================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Null the WorkoutEditorInstance thereby indicating that a WorkoutEditor window 
            // is not displayed.
            if (workoutEditorInstance != null)
            {
                workoutEditorInstance = null;
            }
        }


        //=====================================================================
        //=====================================================================
        // Menu click event handlers.
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private void editorCommandsMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Event handler for a commands menu item mouse click on the workout editor window.
        //
        // History:
        //  06-14-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorCommandsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            defaultFocus();
        }

        //=====================================================================
        // Method:	editorOpenMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Event handler for an open workout menu item mouse click.
        //
        // History:
        //  06-20-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int filereaderror;
            string returnedfilespec = "";
            string outtext;
            TreadmillWorkout tempworkout = new TreadmillWorkout();

            // Read the workout into tempworkout.
            filereaderror = tempworkout.loadWorkoutDialog(ref returnedfilespec);

            // Error check and add workout to workout list.
            if ((filereaderror == 0) && (tempworkout.workoutSegments.Count > 0))
            {
                editorTreadmillWorkout = tempworkout;
                currentGroup = 0;
                segmentsPerGroup = 1;
                comboBoxGroupSize.SelectedIndex = 0;
                integerUpDownNumberGroups.Value = tempworkout.workoutSegments.Count;
                outtext = "Workout loaded from " + returnedfilespec + ".";
                writeStatusText(outtext);
                updateEditorDisplay();
            }
            else
            {
                writeStatusText("No workout loaded.");
            }
        defaultFocus();
        }


        //=====================================================================
        // Method:	editorSaveMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Event handler for a save workout menu item mouse click.
        //
        // History:
        //  06-20-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorSaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int saveerror;
            string savedfilespec = "";
            string title;

            title = "Save Workout";
            saveerror = editorTreadmillWorkout.saveWorkoutDialog(title, ref savedfilespec);

            switch (saveerror)
            {
                case (0):
                    writeStatusText("Workout saved to " + savedfilespec + ".");
                    editorTreadmillWorkout.workoutName = System.IO.Path.GetFileNameWithoutExtension(savedfilespec);
                    updateEditorDisplay();
                    break;
                case (1):
                    writeStatusText("No file saved.");
                    break;
                case (2):
                    writeStatusText("Error saving file " + savedfilespec + ".");
                    break;
                default:
                    writeStatusText("No file saved.");
                    break;
            }
            defaultFocus();
        }

        //=====================================================================
        // Method:	private void scaleMenuItem_Click(object sender, RoutedEventArgs e)
        //          private void setWindowScale()
        //
        // Description:
        //  Event handler for a Window scale menu item mouse click.
        //
        // History:
        //  06-19-2021, MA: First version completed.
        // 
        //=====================================================================
        private void scaleMenuItem_Click(object sender, RoutedEventArgs e)
        {

            string menuitemtext;

            menuitemtext = ((MenuItem)sender).Header.ToString();

            if (menuitemtext == "150 %")
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
                    windowWorkoutEditor.Width = WINDOW_WIDTH;
                    windowWorkoutEditor.Height = WINDOW_HEIGHT;
                    gridEditorWindow.LayoutTransform = new ScaleTransform(1.0, 1.0);
                    break;
                case WindowScale.P150:
                    menuItemScale100.IsChecked = false;
                    menuItemScale150.IsChecked = true;
                    menuItemScale200.IsChecked = false;
                    windowWorkoutEditor.Width = 1.5 * WINDOW_WIDTH;
                    windowWorkoutEditor.Height = 1.5 * WINDOW_HEIGHT - 18;
                    gridEditorWindow.LayoutTransform = new ScaleTransform(1.5, 1.5);
                    break;
                case WindowScale.P200:
                    menuItemScale100.IsChecked = false;
                    menuItemScale150.IsChecked = false;
                    menuItemScale200.IsChecked = true;
                    windowWorkoutEditor.Width = 2 * WINDOW_WIDTH;
                    windowWorkoutEditor.Height = 2 * WINDOW_HEIGHT - 36;
                    gridEditorWindow.LayoutTransform = new ScaleTransform(2.0, 2.0);
                    break;
                default:
                    menuItemScale100.IsChecked = true;
                    menuItemScale150.IsChecked = false;
                    menuItemScale200.IsChecked = false;
                    windowWorkoutEditor.Width = WINDOW_WIDTH;
                    windowWorkoutEditor.Height = WINDOW_HEIGHT;
                    gridEditorWindow.LayoutTransform = new ScaleTransform(1.0, 1.0);
                    break;
            }
        }


        //=====================================================================
        // Method:	private void editorExitMenuItem_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Event handler for exiting the workout editor.
        //
        // History:
        //  06-16-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        //=====================================================================
        // Method:	private void comboBoxGroupSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //
        // Description:
        //  Event handler that changes the group size.
        //
        // History:
        //  06-18-2021, MA: First version completed.
        //  06-22-2021, MA: Added resetting of currentGroup.
        // 
        //=====================================================================
        private void comboBoxGroupSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            segmentsPerGroup = comboBoxGroupSize.SelectedIndex + 1;
            currentGroup = 0;
            updateEditorDisplay();
            defaultFocus();
        }

        private void comboBoxGroupSize_DropDownClosed(object sender, EventArgs e)
        {
            defaultFocus();
        }


        private void IntegerUpDownNumberGroups_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            defaultFocus();
        }


        //=====================================================================
        // Method:	private void comboBoxGroupSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //
        // Description:
        //  Event handler for the Initialize button. This method reinitializes the 
        //  workout based on default values and the settings of the group size and 
        //  number of groups controls.
        //
        // History:
        //  06-18-2021, MA: First version completed.
        // 
        //=====================================================================
        private void buttonInitialize_Click(object sender, RoutedEventArgs e)
        {
            int numbergroups = (int)integerUpDownNumberGroups.Value;
            segmentsPerGroup = comboBoxGroupSize.SelectedIndex + 1;
            int numbersegments = numbergroups * segmentsPerGroup;
            editorTreadmillWorkout.initializeWorkout(numbersegments, (byte)30, (byte)40, 30, 0);
            currentGroup = 0;
            updateEditorDisplay();
            defaultFocus();
        }


        //=====================================================================
        //=====================================================================
        // Keyboard and mouse event handlers.
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private void editorWorkoutDisplayCanvas_KeyDown(object sender, KeyEventArgs e)
        //
        // Description:
        //  Event handler for processing key presses when the workout display has the focus.
        //  The key presses are used to edit/modify the workout.
        //
        // History:
        //  06-16-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorWorkoutDisplayCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            int segment;
            byte incline;
            byte speed;
            int maxgroup;

            // labelWorkoutEditorStatus.Content = "Key pressed in canvas.";
            e.Handled = true;
            switch (e.Key)
            {
                // Move to the next workout group.
                case (Key.Right):
                    maxgroup = editorTreadmillWorkout.workoutSegments.Count / segmentsPerGroup -1;
                    //labelWorkoutEditorStatus.Content = "Max, current: " + maxgroup.ToString() + ", " + currentGroup.ToString();
                    if (currentGroup < maxgroup)
                    {
                        currentGroup = currentGroup + 1;
                        updateEditorDisplay();
                    }
                    break;

                // Move to the previous workout group.
                case (Key.Left):
                    if(currentGroup > 0)
                    {
                        currentGroup = currentGroup - 1;
                        updateEditorDisplay();
                    }
                    break;

                // Toggle between editing speed and incline.
                case (Key.Space):
                    if (editIncline)
                    {
                        editIncline = false;
                    }
                    else
                    {
                        editIncline = true;
                    }
                    updateEditorDisplay();
                    break;

                // Increment speed/incline.
                case (Key.Up):
                    incrementDecrementSpeedIncline(true, editIncline);
                    break;

                // Decrement speed/incline.
                case (Key.Down):
                    incrementDecrementSpeedIncline(false, editIncline);
                    break;

                // Add a new workout group prior to the current one.
                case (Key.Insert):
                case (Key.I):
                    segment = currentGroup * segmentsPerGroup;
                    incline = editorTreadmillWorkout.workoutSegments[segment].incline;
                    speed = editorTreadmillWorkout.workoutSegments[segment].speed;
                    for (int i = 0; i < segmentsPerGroup; i++)
                    {
                        // insertSegment() inserts the new segment after the specified one.
                        editorTreadmillWorkout.insertSegment(segment - 1, speed, incline, 30, 0);
                    }
                    currentGroup = currentGroup + 1;
                    writeStatusText("New group inserted.");
                    updateEditorDisplay();
                    break;

                // Add a new workout group after the current one.
                case (Key.A):
                    segment = currentGroup * segmentsPerGroup;
                    incline = editorTreadmillWorkout.workoutSegments[segment].incline;
                    speed = editorTreadmillWorkout.workoutSegments[segment].speed;
                    segment = (currentGroup + 1) * segmentsPerGroup;
                    for (int i = 0; i < segmentsPerGroup; i++)
                    {
                        // insertSegment() inserts the new segment after the specified one.
                        editorTreadmillWorkout.insertSegment(segment - 1, speed, incline, 30, 0);
                    }
                    writeStatusText("New group appended.");
                    updateEditorDisplay();
                    break;

                // Delete the current workout group.
                case (Key.Delete):
                    maxgroup = editorTreadmillWorkout.workoutSegments.Count / segmentsPerGroup - 1;
                    if (maxgroup < 1)
                    {
                        writeStatusText("Cannot delete last group.");
                        break;
                    }
                    segment = currentGroup * segmentsPerGroup;
                    for(int i = 0; i < segmentsPerGroup; i++)
                    {
                        editorTreadmillWorkout.deleteSegment(segment);
                    }
                    maxgroup = editorTreadmillWorkout.workoutSegments.Count / segmentsPerGroup - 1;
                    if (currentGroup > maxgroup)
                    {
                        currentGroup = maxgroup;
                    }
                    writeStatusText("Group deleted.");
                    updateEditorDisplay();
                    break;

                default:
                    break;

            }      
        }


        //=====================================================================
        // Methods:	private void editorWorkoutDisplayCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        //          private void incrementDecrementSpeedIncline(bool incrementflag, bool editinclineflag)
        //
        // Description:
        //  Event handler for processing mousewheel movement when the workout display has the focus.
        //  The movement is used to increment/decrement the speed/incline group values.
        //  The method incrementDecrementSpeedIncline() takes care of the value changes.
        //
        // History:
        //  06-23-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorWorkoutDisplayCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
             if (e.Delta > 0)
            {
                incrementDecrementSpeedIncline(true, editIncline);
            }
            else if(e.Delta < 0)
            {
                incrementDecrementSpeedIncline(false, editIncline);
            }
        }


        private void incrementDecrementSpeedIncline(bool incrementflag, bool editinclineflag)
        {
            int segment;
            byte incline;
            byte speed;

            segment = currentGroup * segmentsPerGroup;
            incline = editorTreadmillWorkout.workoutSegments[segment].incline;
            speed = editorTreadmillWorkout.workoutSegments[segment].speed;

            if(incrementflag)
            {
                if (segment >= editorTreadmillWorkout.workoutSegments.Count)
                {
                    return;
                }
                if (editinclineflag)
                {
                    incline = (byte)(incline + TreadmillWorkout.INCLINE_INCREMENT);
                    if (incline <= TreadmillWorkout.MAXIMUM_SEGMENT_INCLINE)
                    {
                        updateWorkoutGroup(currentGroup, incline, speed);
                    }
                }
                else
                {
                    speed = (byte)(speed + TreadmillWorkout.SPEED_INCREMENT);
                    if (speed <= TreadmillWorkout.MAXIMUM_SEGMENT_SPEED)
                    {
                        updateWorkoutGroup(currentGroup, incline, speed);
                    }
                }
            }
            else
            {
                if (segment >= editorTreadmillWorkout.workoutSegments.Count)
                {
                    return;
                }
                if (editinclineflag)
                {
                    if (incline <= TreadmillWorkout.MINIMUM_SEGMENT_INCLINE)
                    {
                        return;
                    }
                    incline = (byte)(incline - TreadmillWorkout.INCLINE_INCREMENT);
                    if (incline >= TreadmillWorkout.MINIMUM_SEGMENT_INCLINE)
                    {
                        updateWorkoutGroup(currentGroup, incline, speed);
                    }
                }
                else
                {
                    if (speed <= TreadmillWorkout.MINIMUM_SEGMENT_SPEED)
                    {
                        return;
                    }
                    speed = (byte)(speed - TreadmillWorkout.SPEED_INCREMENT);
                    if (speed >= TreadmillWorkout.MINIMUM_SEGMENT_SPEED)
                    {
                        updateWorkoutGroup(currentGroup, incline, speed);
                    }
                }
            }
            updateEditorDisplay();
        }


        //=====================================================================
        // Methods:	private void editorWorkoutDisplayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //          
        //
        // Description:
        //  Event handler for processing left mouse button downs when the workout display has the focus.
        //  This is currently not properly implemented. 
        //
        // History:
        //  06-24-2021, MA: First version completed.
        // 
        //=====================================================================
        private void editorWorkoutDisplayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double multiplier = 1.0;
            Point position;
            int xcanvas;
            int segment;
            int startsegment = 0;

            // Determine the mouse pointer x location within the display canvas.
            // Account for the effect of windowScale.
            switch (windowScale)
            {
                case WindowScale.P100:
                    multiplier = 1.0;
                    break;
                case WindowScale.P150:
                    multiplier = 1.5;
                    break;
                case WindowScale.P200:
                    multiplier = 2.0;
                    break;
                default:
                    break;
            }
            position = e.GetPosition(this);
            xcanvas = (int)(position.X / multiplier - (multiplier - 1.0) * 2.0 - editorWorkoutDisplay.displayCanvas.Margin.Left);

            // Convert the x location to the segment number in editorTreadmillWorkout.
            segment = currentGroup * segmentsPerGroup;
            if (segment > WorkoutDisplay.WORKOUT_SEGMENT_SHIFT)
            {
                startsegment = segment - WorkoutDisplay.WORKOUT_SEGMENT_SHIFT;
            }
            else
            {
                startsegment = segment;
            }
            segment = editorWorkoutDisplay.segmentFromXLocation(editorTreadmillWorkout, startsegment, xcanvas);
            writeStatusText("xcanvas, clicksegment = " + xcanvas.ToString() + ", " + segment.ToString());
        }


        private void windowWorkoutEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(null);
            int xwindow = (int)position.X;
            writeStatusText("xwindow=" + xwindow.ToString());

            defaultFocus();
        }


        //=====================================================================
        //=====================================================================
        // General methods.
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private bool updateWorkoutDisplay()
        //
        // Description:
        //  This method updates the treadmill workout editor window to reflect
        //  the current state of all variables.
        //
        // History:
        //  06-16-2021, MA: First version completed.
        // 
        //=====================================================================
        private bool updateEditorDisplay()
        {
            string outtext = "";
            TimeSpan workouttime;
            double distance;
            int segment = currentGroup * segmentsPerGroup;

            if (editorTreadmillWorkout == null)
            {
                return false;
            }
            if (editorWorkoutDisplay == null)
            {
                return false;
            }
            if (segment >= editorTreadmillWorkout.workoutSegments.Count)
            {
                return false;
            }

            outtext = String.Format("{0:F2}", editorTreadmillWorkout.getWorkoutDistance());
            labelDistanceValue.Content = outtext;

            workouttime = TimeSpan.FromSeconds(editorTreadmillWorkout.getWorkoutTime());
            outtext = string.Format("{0:D2}:{1:D2}:{2:D2}",
                workouttime.Hours, workouttime.Minutes, workouttime.Seconds);
            labelTimeValue.Content = outtext;

            if(editIncline)
            {
                labelInclineValue.FontWeight = FontWeights.Bold;
                labelSpeedValue.FontWeight = FontWeights.Normal;
            }
            else
            {
                labelInclineValue.FontWeight = FontWeights.Normal;
                labelSpeedValue.FontWeight = FontWeights.Bold;
            }

            int speed = editorTreadmillWorkout.workoutSegments[segment].speed;
            outtext = String.Format("{0:F1}", speed / 10.0);
            labelSpeedValue.Content = outtext;

            int incline = editorTreadmillWorkout.workoutSegments[segment].incline;
            outtext = String.Format("{0:F1}", incline / 10.0);
            labelInclineValue.Content = outtext;

            outtext = String.Format("{0:D0}", currentGroup);
            labelGroupNumberValue.Content = outtext;

            workouttime = TimeSpan.FromSeconds(editorTreadmillWorkout.getSegmentStartTime(segment));
            outtext = string.Format("{0:D2}:{1:D2}:{2:D2}",
                workouttime.Hours, workouttime.Minutes, workouttime.Seconds);
            labelStartTimeValue.Content = outtext;

            workouttime = TimeSpan.FromSeconds(editorTreadmillWorkout.workoutSegments[segment].duration*segmentsPerGroup);
            outtext = string.Format("{0:D2}:{1:D2}:{2:D2}",
                workouttime.Hours, workouttime.Minutes, workouttime.Seconds);
            labelAddedTimeValue.Content = outtext;

            distance = editorTreadmillWorkout.getSegmentStartDistance(segment);
            outtext = String.Format("{0:F2}", distance);
            labelStartDistanceValue.Content = outtext;

            distance = editorTreadmillWorkout.getSegmentStartDistance(segment + segmentsPerGroup) - distance;
            outtext = String.Format("{0:F2}", distance);
            labelAddedDistanceValue.Content = outtext;

            if (segment > WorkoutDisplay.WORKOUT_SEGMENT_SHIFT)
            {
                editorWorkoutDisplay.plotWorkout(editorTreadmillWorkout, segment - WorkoutDisplay.WORKOUT_SEGMENT_SHIFT,
                    editorTreadmillWorkout.workoutSegments.Count, segment, segment + segmentsPerGroup - 1);
            }
            else
            {
                editorWorkoutDisplay.plotWorkout(editorTreadmillWorkout, 0, editorTreadmillWorkout.workoutSegments.Count, segment, segment + segmentsPerGroup - 1);
            }

            return true;

        }


        //=====================================================================
        // Method:	public void updateWorkoutGroup(int group, byte incline, byte speed)
        //
        // Description:
        //  This method replaces the incline and speed values in the workout 
        //  editorTreadmillWorkout with set incline and setspeed over the workout
        //  segments contained in the group setgroup.
        //
        // History:
        //  06-18-2021, MA: First version completed.
        // 
        //=====================================================================
        public void updateWorkoutGroup(int setgroup, byte setincline, byte setspeed)
        {
            if (setgroup < 0)
            {
                return;
            }
            int maxgroup = editorTreadmillWorkout.workoutSegments.Count / segmentsPerGroup;
            if(setgroup>maxgroup)
            {
                return;
            }
            for (int seg = currentGroup * segmentsPerGroup; seg < (currentGroup + 1) * segmentsPerGroup; seg++)
            {
                editorTreadmillWorkout.replaceSegment(seg, setspeed, setincline, 30, 0);
            }
        }


        //=====================================================================
        // Method:	void writeStatusText(string text, long duration)
        //
        // Description:
        //  Writes text to the status bar at the bottom of the screen.
        //
        // History:
        //  06-20-2021, MA: First version completed.
        // 
        //=====================================================================
        void writeStatusText(string text)
        {
            labelWorkoutEditorStatus.Content = text;
        }


        //=====================================================================
        // Method:	private void defaultFocus()
        //
        // Description:
        //  Sets the focus of this object to a specific control.
        //
        // History:
        //  06-16-2021, MA: First version completed.
        // 
        //=====================================================================
        private void defaultFocus()
        {
            if (editorWorkoutDisplayCanvas != null)
            {
                editorWorkoutDisplayCanvas.Focus();
            }
        }

    }
}
