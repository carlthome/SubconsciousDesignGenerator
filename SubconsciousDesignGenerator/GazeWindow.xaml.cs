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
        public GazeWindow(string instructions, Action a)
        {
            InitializeComponent();
            Instructions.Text = instructions;
            FadeIn.Completed += (s, e) => { a(); Close(); };
        }

        public GazeWindow(string instructions, bool confirm, bool cancel)
        {
            InitializeComponent();
            Instructions.Text = instructions;
            if (confirm) Confirm.Visibility = Visibility.Visible;
            if (cancel) Cancel.Visibility = Visibility.Visible;
        }

        void onConfirm(object s, EventArgs e)
        {
            DialogResult = true;
        }

        void onCancel(object s, EventArgs e)
        {
            DialogResult = false;
        }

        void onClose(object s, EventArgs e)
        {
            Close();
        }
    }
}
