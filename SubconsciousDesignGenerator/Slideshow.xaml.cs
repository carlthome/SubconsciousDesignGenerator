using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Tobii.Gaze.Core;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for Slideshow.xaml
    /// </summary>
    public partial class Slideshow : Window
    {
        Task session;
        List<ImageSource> images;
        int SLIDE_DURATION = 200;
        IEyeTracker iet;
        Point2D gaze;
        double userDistanceFromEyeTracker = -1;
        double gazeMovement = 0;
        bool blinked = false;
        long timeStamp;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public Slideshow()
        {
            InitializeComponent();

            try
            {
                // Connect to eye tracker.
                Uri url = new EyeTrackerCoreLibrary().GetConnectedEyeTracker();
                iet = new EyeTracker(url);
                iet.EyeTrackerError += onEyeTrackerError;
                iet.GazeData += onGazeData;
                Task t = new Task(() => { iet.RunEventLoop(); });
                t.Start();
                iet.Connect();
                iet.StartTracking();
                Instructions.Text = "Tryck mellanslag för att börja.";
            }
            catch (EyeTrackerException e)
            {
                Instructions.Text = "Ögonstyrningsenheten svarar inte.";
                this.IsEnabled = false;
            }
            catch (NullReferenceException e)
            {
                Instructions.Text = "Ögonstyrningsenheten hittades ej.";
                this.IsEnabled = false;
            }
        }
        void newSession()
        {
            // Display slides and collect reaction data.
            var wins = new Dictionary<ImageSource, int>();
            var q = new Queue<ImageSource>(images);
            images.ForEach(i => wins.Add(i, 0));
            long hits1 = 0, hits2 = 0;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Instructions.Visibility = Visibility.Hidden;
                Slide.Visibility = Visibility.Visible;
                Image1.MouseEnter += (Object s, MouseEventArgs e) => { hits1 = (hits1 == 0) ? 100 : hits1++; };
                Image2.MouseEnter += (Object s, MouseEventArgs e) => { hits1 = (hits2 == 0) ? 100 : hits2++; };
            })).Wait();

            while (q.Count >= 2)
            {
                // Switch slide.
                ImageSource is1 = q.Dequeue();
                ImageSource is2 = q.Dequeue();
                hits1 = 0;
                hits2 = 0;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Image1.Source = is1;
                    Image2.Source = is2;
                })).Wait();

                // Measure hits for a while.
                session.Wait(SLIDE_DURATION);

                // Determine winning image.
                if (hits1 > hits2)
                {
                    ++wins[is1];
                    q.Enqueue(is1);
                }
                else
                {
                    ++wins[is2];
                    q.Enqueue(is2);
                }
            }

            // Pick the images that interested the user most.
            var MAX_LAYERS = 10;
            List<KeyValuePair<ImageSource, int>> layers = wins.OrderByDescending(x => x.Value).Take(MAX_LAYERS).ToList();

            // Construct composite from the reaction data.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Instructions.Text = "Bilden skrivs ut.";
                Instructions.Visibility = Visibility.Visible;
                Slide.Visibility = Visibility.Hidden;

                var w = new Composite(layers);
                w.Show();
            })).Wait();

            session.Wait(3000);

            Dispatcher.BeginInvoke(new Action(() => Instructions.Text = "Tryck mellanslag för att börja.")).Wait();
        }

        void onEyeTrackerError(object s, EyeTrackerErrorEventArgs e)
        {
            //TODO Handle connection errors.
            Debug.WriteLine(e.Message);
        }

        void onGazeData(object s, GazeDataEventArgs e)
        {
            var d = e.GazeData;

            if (d.TrackingStatus == TrackingStatus.NoEyesTracked)
            {
                if (!blinked) timeStamp = d.Timestamp;
            }
            else
            {
                // Calculate gaze movement
                if (gaze != null) gazeMovement = Math.Sqrt(Math.Pow(d.Left.GazePointOnDisplayNormalized.X - gaze.X, 2) + Math.Pow(d.Left.GazePointOnDisplayNormalized.Y - gaze.Y, 2));

                // TODO Blink detection
                long MINIMUM_BLINK_DURATION = 3000;
                blinked = (!blinked && d.Timestamp - timeStamp > MINIMUM_BLINK_DURATION) ? true : false;
                timeStamp = d.Timestamp;

                // Set mouse cursor at the gaze point on the screen, so WPF mouse events are triggered by the gaze.
                Dispatcher.BeginInvoke(new Action(() =>
                    SetCursorPos(
                        (int)(Left + Width * (d.Left.GazePointOnDisplayNormalized.X + d.Right.GazePointOnDisplayNormalized.X) / 2),
                        (int)(Top + Height * (d.Left.GazePointOnDisplayNormalized.Y + d.Right.GazePointOnDisplayNormalized.Y) / 2)
                    ))).Wait();

                // Calculate the user's head distance from the eye tracker camera.
                userDistanceFromEyeTracker = (d.Left.EyePositionFromEyeTrackerMM.Z + d.Right.EyePositionFromEyeTrackerMM.Z) / 2;
            }
        }

        void onLoaded(object s, RoutedEventArgs e)
        {
            // Load images from directory.
            images = new List<ImageSource>();
            foreach (var file in Directory.GetFiles("Images", "*.png"))
            {
                FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                Image i = new Image();
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = fs;
                bi.EndInit();
                i.Source = bi;
                images.Add((ImageSource)bi);
            }
        }

        void onKeyDown(object s, KeyEventArgs e)
        {
            if (session != null && !session.IsCompleted) return;
            switch (e.Key)
            {
                case Key.Space:
                    (session = new Task(newSession)).Start();
                    break;
            }
        }
    }
}
