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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window
    {
        bool requireConfirm;

        public DialogWindow(string instructions, string confirm, string cancel)
        {
            InitializeComponent();
            Instructions.Text = instructions;
            ConfirmText.Text = confirm;
            CancelText.Text = cancel;
            requireConfirm = false;
        }

        public DialogWindow(string instructions, string confirm)
        {
            InitializeComponent();
            Instructions.Text = instructions;
            ConfirmText.Text = confirm;
            CancelInstructions.Opacity = 0.1;
            requireConfirm = true;
        }

        public DialogWindow(string instructions)
        {
            InitializeComponent();
            Instructions.Text = instructions;
            ButtonInstructions.Visibility = Visibility.Hidden;
            IsEnabled = false;
        }

        void onKeyDown(object s, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter: DialogResult = true; break;
                case Key.Decimal: if (!requireConfirm) DialogResult = false; break;
                case Key.Escape: Application.Current.Shutdown(); break;
            }
        }

        void onFadedIn(object s, EventArgs e)
        {
            IsEnabled = true;
        }
    }
}
