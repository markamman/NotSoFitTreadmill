using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NotSoFitTreadmill
{
    //=====================================================================
    //=====================================================================
    // Class: DebugWindow
    // Description:
    //  This is the class that generates the GUI form for debug testing.
    //
    //=====================================================================
    //=====================================================================
    /// <summary>
    /// DebugWindow is the class that generates the GUI form for debug testing.
    /// </summary>
    public partial class DebugWindow : Window
    {
        //---------------------------------------------------------------------
        // Public static fields.
        //---------------------------------------------------------------------

        // Reference to the DebugWindow instance.
        public static DebugWindow DebugWindowInstance = null;


        //=====================================================================
        // Constructor: public DebugWindow()
        //
        // Description: 
        //  DebugWindow constructor.
        //
        // History:
        //  08-06-2019, MA: First version completed.
        //			
        //=====================================================================
        public DebugWindow()
        {
            InitializeComponent();

            // Set the reference to this form.
            DebugWindowInstance = this;

        }


        //=====================================================================
        //=====================================================================
        // Event handlers
        //=====================================================================
        //=====================================================================


        //=====================================================================
        // Method:	private void ButtonDebugSend_Click(object sender, RoutedEventArgs e)
        //  private void ButtonDebugLoop_Click(object sender, RoutedEventArgs e)
        //
        // Description:
        //  Button click event handlers. These methods read the GUI debug window data
        //  and respond accordingly.
        //
        // History:
        //  08-07-2019, MA: First version completed.
        // 
        //=====================================================================
        private void ButtonDebugSend_Click(object sender, RoutedEventArgs e)
        {
            byte sendspeed;
            byte sendincline;

            textBoxStatus.Text = "Ok";

            // Read the textbox data.
            try
            {
                sendspeed = (byte)Int32.Parse(textBoxSpeed.Text);
            }
            catch
            {
                textBoxStatus.Text = "Invalid Speed";
                return;
            }
            try
            {
                sendincline = (byte)Int32.Parse(textBoxIncline.Text);
            }
            catch
            {
                textBoxStatus.Text = "Invalid Incline";
                return;
            }

            // Send the command to the treadmill.
            IFitCommunication.setSpeedIncline(sendspeed, sendincline);
            textBoxStatus.Text = "Command sent";
        }

        private void ButtonDebugLoop_Click(object sender, RoutedEventArgs e)
        {
            byte sendspeed;
            byte i;

            textBoxStatus.Text = "Ok";

            // Read the textbox data.
            try
            {
                sendspeed = (byte)Int32.Parse(textBoxSpeed.Text);
            }
            catch
            {
                textBoxStatus.Text = "Invalid Speed";
                return;
            }

            // Send the commands to the treadmill.
            for (i = sendspeed; i < 255; i++)
            {
                if (i != 252)
                {
                    IFitCommunication.setSpeedIncline(i, i);
                    textBoxSpeed.Text = i.ToString();
                    textBoxIncline.Text = i.ToString();
                    textBoxStatus.Text = "Command " + i.ToString() + " sent";
                    textBoxSpeed.Refresh();
                    textBoxIncline.Refresh();
                    textBoxStatus.Refresh();
                    Thread.Sleep(1000);
                }
            }
        }


        //=====================================================================
        // Method:	private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //
        // Description:
        //  This nulls the DebugWindowInstance object thereby indicating that a 
        //  DebugWindow is not displayed.
        //
        // History:
        //  08-06-2019, MA: First version completed.
        // 
        //=====================================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DebugWindowInstance = null;
        }

    }


    //=====================================================================
    //=====================================================================
    // Class: ExtensionMethods
    // Description:
    //  From http://geekswithblogs.net/NewThingsILearned/archive/2008/08/25/refresh--update-wpf-controls.aspx.
    //  Adds a helper method to refresh a WPF control.
    //
    //=====================================================================
    //=====================================================================
    /// <summary>
    /// ExtensionMethods adds a helper method to refresh a WPF control.
    /// </summary>
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };


        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            uiElement.Dispatcher.Invoke(DispatcherPriority.ContextIdle, EmptyDelegate); // For Windows 7.
        }
    }
}
