//*****************************************************************************
//*****************************************************************************
// File: WorkoutDisplay.cs
// Hacker: M. Amman
// Description:
//   This file contains the WorkoutDisplay class.
//
// Required classes:
//
// History:
//   See the notes for the individual methods below.
//   04-22-2020, MA: First fully functional version completed.
//   06-17-2021, MA: Updated as indicated in the comments below.
//   06-23-2021, MA: Added segmentFromXLocation().
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
using System.Windows.Media;
using System.Windows.Shapes;

namespace NotSoFitTreadmill
{
    //=====================================================================
    // Class: WorkoutDisplay
    // Description:
    //  See below.
    //
    //=====================================================================
    //=====================================================================
    /// <summary>
    /// The WorkoutDisplay class contains the methods for displaying the treadmill speed and 
    /// incline workout data.
    /// </summary>
    class WorkoutDisplay
    {
        //---------------------------------------------------------------------------
        // Constants
        //---------------------------------------------------------------------------

        /// <summary>
        /// CANVAS_HEIGHT is the height of the canvas used to display the workout.
        /// </summary>
        public const int CANVAS_HEIGHT = 125;

        /// <summary>
        /// CANVAS_WIDTH is the width of the canvas used to display the workout.
        /// </summary>
        public const int CANVAS_WIDTH = 364;

        /// <summary>
        /// PLOT_XORIGIN is the x location in pixels for the start of the speed and incline plots.
        /// </summary>
        public const int PLOT_XORIGIN = 2;

        /// <summary>
        /// PLOT_YORIGIN is the y location in pixels for the start of the speed and incline plots.
        /// </summary>
        public const int PLOT_YORIGIN = 2;

        /// <summary>
        /// PLOT_XSCALE is the number of x pixels in each segment of the speed and incline plots.
        /// </summary>
        public const int PLOT_XSCALE = 3;

        /// <summary>
        /// PLOT_YSCALE is the number of y pixels for each speed or incline increment in the speed and incline plots.
        /// </summary>
        public const int PLOT_YSCALE = 1;

        /// <summary>
        /// Current display segement number at which the display should start shifting.
        /// <summary>
        public const int WORKOUT_SEGMENT_SHIFT = 100;

        /// <summary>
        /// SPEED_COLOR is the color of the speed plot.
        /// </summary>
        public static SolidColorBrush SPEED_COLOR = Brushes.LightSkyBlue;

        /// <summary>
        /// INCLINE_COLOR is the color of the incline plot.
        /// </summary>
        public static SolidColorBrush INCLINE_COLOR = Brushes.LimeGreen;

        /// <summary>
        /// SPEEDINCLINE_STROKE_THICKNESS is the color of the incline plot.
        /// </summary>
        public static int SPEEDINCLINE_STROKE_THICKNESS = 2;

        /// <summary>
        /// HIGHLIGHT_COLOR is the color of highlighted areas of the plot.
        /// </summary>
        public static SolidColorBrush HIGHLIGHT_COLOR = new SolidColorBrush(Color.FromArgb(255, 200, 100, 100));

        /// <summary>
        /// FRAME_COLOR is the color of the plot frame.
        /// </summary>
        public static SolidColorBrush FRAME_COLOR = Brushes.DarkGray;
        /// <summary>

        /// GRID_COLOR is the color of the plot grid.
        /// </summary>
        public static SolidColorBrush GRID_COLOR = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

        /// TEXT_COLOR is the color of all text that appears in the display.
        /// </summary>
        public static SolidColorBrush TEXT_COLOR = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));

        //---------------------------------------------------------------------------
        // Fields
        //---------------------------------------------------------------------------

        public Canvas displayCanvas;


        //=====================================================================
        // Constructor: public WorkoutDisplay()
        //
        // Description: 
        //  WorkoutDisplay() constructor.
        //
        // History:
        //  04-16-2020, MA: First version completed.
        //			
        //=====================================================================
        /// <summary>
        /// The WorkoutDisplay class constructor initializes the class data
        /// to default values.  
        /// </summary>
        public WorkoutDisplay(Canvas dcanvas)
        {
            displayCanvas = dcanvas;

            displayCanvas.MinHeight = CANVAS_HEIGHT;
            displayCanvas.MaxHeight = CANVAS_HEIGHT;
            displayCanvas.Height = CANVAS_HEIGHT;
            displayCanvas.MinWidth = CANVAS_WIDTH;
            displayCanvas.MaxWidth = CANVAS_WIDTH;
            displayCanvas.Width = CANVAS_WIDTH;

            plotWorkout(new TreadmillWorkout(), 0, 120, 16, 16);
        }


        //=====================================================================
        // Method: public int plotWorkout(TreadmillWorkout workout, int startsegment, int numbersegments, int highlightsegstart, int highlightsegstop)
        //  private void addPlotDecorations1()
        //  private void addPlotDecorations2(string title)
        //  private void addText(string text, Point location, double angle, double fsize, HorizontalAlignment halign, VerticalAlignment valign)
        //
        // Description: 
        //  Displays the specified TreadmillWorkout workout on the WorkoutDisplay canvas.
        //
        // History:
        //  04-17-2020, MA: First version completed.
        //  04-21-2020, MA: Add additional plot decorations.
        //  06-17-2021, MA: Changed the hightlight from a single segment to a range of segments.
        //		
        //=====================================================================
        /// <summary>
        /// plotWorkout displays the specified TreadmillWorkout workout on the WorkoutDisplay canvas. 
        /// </summary>
        /// <param name="workout">
        /// workout is the TreadmillWorkout to be displayed on the WorkoutDisplay canvas.
        /// </param>
        /// <param name="startsegment">
        /// startsegment is the first workout segment that should be plotted on the WorkoutDisplay canvas.
        /// </param>
        /// <param name="numbersegments">
        /// numbersegments is the number of workout segments that should be plotted on the WorkoutDisplay canvas.
        /// </param>
        /// <param name="highlightsegstart">
        /// highlightsegstart is the first in the range of workout segments that should be highlighted with a background rectangle.
        /// </param>
        /// <param name="highlightsegstop">
        /// highlightsegstop is the last in the range of workout segments that should be highlighted with a background rectangle.
        /// </param>
        /// <returns>
        /// The method returns zero if it successfully updates the WorkoutDisplay canvas.   
        /// </returns>
        public int plotWorkout(TreadmillWorkout workout, int startsegment, int numbersegments, int highlightsegstart, int highlightsegstop)
        {
            GeometryGroup speedgeom = new GeometryGroup();
            GeometryGroup inclinegeom = new GeometryGroup();
            GeometryGroup highgeom = new GeometryGroup();
            Path speedgeompath = new Path();
            Path inclinegeompath = new Path();
            Path highgeompath = new Path();
            Rectangle highrect = new Rectangle();
            int i, istop, xloc1, xloc2, speedyloc1, speedyloc2, inclineyloc1, inclineyloc2;

            // Error check the parameters.
            if (startsegment < 0)
                return 1;
            if (startsegment > workout.workoutSegments.Count - 1)
                return 1;
            istop = startsegment + numbersegments - 1;
            if (istop > workout.workoutSegments.Count - 1)
                istop = workout.workoutSegments.Count - 1;

            // Loop through and create the line geometries from the data.

            speedyloc1 = 0;
            inclineyloc1 = 0;
            for(i = startsegment; i <= istop; i++)
            {
                xloc1 = PLOT_XORIGIN + (i - startsegment) * PLOT_XSCALE;
                xloc2 = xloc1 + PLOT_XSCALE;
                speedyloc2 = CANVAS_HEIGHT - PLOT_YORIGIN - workout.workoutSegments[i].speed * PLOT_YSCALE;
                inclineyloc2 = CANVAS_HEIGHT - PLOT_YORIGIN - workout.workoutSegments[i].incline * PLOT_YSCALE;
                speedgeom.Children.Add(new LineGeometry(new Point(xloc1, speedyloc2), new Point(xloc2, speedyloc2)));
                inclinegeom.Children.Add(new LineGeometry(new Point(xloc1, inclineyloc2), new Point(xloc2, inclineyloc2)));
                if (i > startsegment)
                {
                    speedgeom.Children.Add(new LineGeometry(new Point(xloc1, speedyloc1), new Point(xloc1, speedyloc2)));
                    inclinegeom.Children.Add(new LineGeometry(new Point(xloc1, inclineyloc1), new Point(xloc1, inclineyloc2)));
                }
                speedyloc1 = speedyloc2;
                inclineyloc1 = inclineyloc2;
            }

            // Clear the canvas before adding the updated graphics.
            displayCanvas.Children.Clear();

            // Highlight the active segment area if it is valid.
            if(highlightsegstop < highlightsegstart)
            {
                int temp = highlightsegstop;
                highlightsegstop = highlightsegstart;
                highlightsegstart = temp;
            }
            if((highlightsegstop >= startsegment) && (highlightsegstart <= istop))
            {
                if(highlightsegstart < startsegment)
                {
                    highlightsegstart = startsegment;
                }
                if(highlightsegstop > istop)
                {
                    highlightsegstop = istop;
                }
                highgeompath.StrokeThickness = 0;
                highgeompath.Stroke = HIGHLIGHT_COLOR;
                xloc1 = PLOT_XORIGIN + (highlightsegstart - startsegment) * PLOT_XSCALE;
                highrect.Width = 3 * (highlightsegstop - highlightsegstart + 1);
                highrect.Height = CANVAS_HEIGHT - 2;
                highrect.Fill = HIGHLIGHT_COLOR;
                Canvas.SetLeft(highrect, xloc1);
                Canvas.SetTop(highrect, 1);
                displayCanvas.Children.Add(highrect);
            }

            // Add the background plot decorations.
            addPlotDecorations2(workout.workoutName);

            // Add the data plots.
            speedgeompath.StrokeThickness = SPEEDINCLINE_STROKE_THICKNESS;
            speedgeompath.Stroke = SPEED_COLOR;
            speedgeompath.Data = speedgeom;
            inclinegeompath.StrokeThickness = SPEEDINCLINE_STROKE_THICKNESS;
            inclinegeompath.Stroke = INCLINE_COLOR;
            inclinegeompath.Data = inclinegeom;
            displayCanvas.Children.Add(speedgeompath);
            displayCanvas.Children.Add(inclinegeompath);

            // Add the foreground plot decorations.
            addPlotDecorations1();

            displayCanvas.UpdateLayout();

            return 0;
        }


        private void addPlotDecorations1()
        {
            GeometryGroup bordergeom = new GeometryGroup();
            Path bordergeompath = new Path();

            bordergeompath.StrokeThickness = 2;
            bordergeompath.Stroke = FRAME_COLOR;
            bordergeom.Children.Add(new RectangleGeometry(new Rect(new Point(1, 1), new Point(CANVAS_WIDTH-1, CANVAS_HEIGHT-1))));
            bordergeompath.Data = bordergeom;

            displayCanvas.Children.Add(bordergeompath);
        }


        private void addPlotDecorations2(string title)
        {
            int yvalue;
            GeometryGroup gridgeom = new GeometryGroup();
            Path gridgeompath = new Path();

            gridgeompath.StrokeThickness = 1;
            gridgeompath.StrokeDashArray = new DoubleCollection() { 1, 3 };
            gridgeompath.Stroke = GRID_COLOR;
            for (int i = 0; i < 13; i++)
            {
                yvalue = CANVAS_HEIGHT - PLOT_YORIGIN - i * 10 * PLOT_YSCALE;
                gridgeom.Children.Add(new LineGeometry(new Point(PLOT_XORIGIN, yvalue), new Point(PLOT_XORIGIN + 120 * PLOT_XSCALE, yvalue)));
                if ((i > 0) && (i < 12))
                {
                    addText(i.ToString(), new Point(PLOT_XORIGIN + 2, yvalue), 0, 8, HorizontalAlignment.Left, VerticalAlignment.Center);
                    addText(i.ToString(), new Point(PLOT_XORIGIN + 120 * PLOT_XSCALE - 2, yvalue), 0, 8, HorizontalAlignment.Right, VerticalAlignment.Center);
                }
            }
            addText(title, new Point(CANVAS_WIDTH/2, 12), 0, 10, HorizontalAlignment.Center, VerticalAlignment.Center);
            gridgeompath.Data = gridgeom;
            displayCanvas.Children.Add(gridgeompath);
        }


        private void addText(string text, Point location, double angle, double fsize, HorizontalAlignment halign, VerticalAlignment valign)
        {
            Label textlabel = new Label();
            textlabel.Content = text;
            textlabel.FontSize = fsize;
            textlabel.Foreground = TEXT_COLOR;

            // Rotate the text.
            if (angle != 0)
                textlabel.LayoutTransform = new RotateTransform(angle);

            // Position the text.
            textlabel.Measure(new Size(double.MaxValue, double.MaxValue));

            double x = location.X;
            if (halign == HorizontalAlignment.Center)
                x = x - textlabel.DesiredSize.Width / 2;
            else if (halign == HorizontalAlignment.Right)
                x = x - textlabel.DesiredSize.Width;
            Canvas.SetLeft(textlabel, x);

            double y = location.Y;
            if (valign == VerticalAlignment.Center)
                y = y - textlabel.DesiredSize.Height / 2;
            else if (valign == VerticalAlignment.Bottom)
                y = y - textlabel.DesiredSize.Height;
            Canvas.SetTop(textlabel, y);

            // Display the text.
            displayCanvas.Children.Add(textlabel);
        }


        //=====================================================================
        // Method: public int segmentFromXLocation(TreadmillWorkout workout, int startsegment, int xlocation)
        //
        // Description: 
        //  Returns the workout segment number associated with the canvas location xlocation.
        //
        // History:
        //  06-23-2021, MA: First version completed.
        //		
        //=====================================================================
        public int segmentFromXLocation(TreadmillWorkout workout, int startsegment, int xlocation)
        {
            int foundsegment = -1;
            int xloc1 = 0;
            int i, xloc2;

            // Error check the parameters.
            if ((startsegment < 0) || (startsegment > workout.workoutSegments.Count - 1))
            {
                return -1;
            }
            if ((xlocation < 0) || (xlocation > CANVAS_WIDTH))
                {
                return -1;
            }

            // Identify the segment.
            for (i = startsegment; i <= workout.workoutSegments.Count - 1; i++)
            {
                xloc1 = PLOT_XORIGIN + (i - startsegment) * PLOT_XSCALE;
                xloc2 = xloc1 + PLOT_XSCALE;
                if((xlocation >= xloc1) && (xlocation <= xloc2))
                {
                    foundsegment = i;
                    break;
                }
            }
            if(xloc1 > CANVAS_WIDTH)
            {
                foundsegment = -1;
            }

            return foundsegment;
        }
    }
}
