using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for GazeWindow.xaml
    /// </summary>
    public partial class GazeWindow : Window
    {
        public GazeWindow(string instructions)
        {
            InitializeComponent();
            Instructions.Text = instructions;
        }

        void onContentRendered(object s, EventArgs e)
        {
            App.eyeTracker.HeadMovement += onHeadMoved;
        }

        void onClosing(object s, CancelEventArgs e)
        {
            App.eyeTracker.HeadMovement -= onHeadMoved;
        }

        void onHeadMoved(object s, HeadMovementEventArgs e)
        {
            if (e.Distance != -1) Dispatcher.Invoke(() => DialogResult = true);
        }
    }
}
