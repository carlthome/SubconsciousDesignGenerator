using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly EyeTrackingEngine eyeTracker = new EyeTrackingEngine();

        void onStartup(object sender, StartupEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.None; // Hide mouse globally in the application.
            new SlideshowWindow().Show();
        }
    }
}
