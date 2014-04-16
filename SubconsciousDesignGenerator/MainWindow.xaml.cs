using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tobii.Gaze.Core;

namespace CompositeImageGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Task session;
        IEyeTracker iet;
        Point3D leftEyePosition, rightEyePosition, trackBoxPosition;
        Point2D gaze;
        double gazeMovement;
        bool blinked = false;
        long timeStamp;

        List<Image> images;
        enum Stage { Start, Positioning, Calibration, Slideshow, End };
        Stage stage;
        private double userDistanceFromEyeTracker;
        private bool insideTrackBox;

        public MainWindow()
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
                initializeSession();
            }
            catch (EyeTrackerException e)
            {
                Instructions.Text = "Ögonstyrningsenheten svarar inte.";
            }
            catch (NullReferenceException e)
            {
                Instructions.Text = "Ögonstyrningsenheten hittades ej.";
            }
        }

        void initializeSession()
        {
            stage = Stage.Start;
            Instructions.Text = "Tryck mellanslag för att börja.";
            Slide.Visibility = Visibility.Visible;
            Composite.Children.Clear();
        }

        void runSession()
        {
            var d = Dispatcher.BeginInvoke(new Action(() => { initializeSession(); Instructions.Text = ""; }));
            d.Wait();

            // Help the user find a good position in the track box.
            /*
            while (stage == Stage.Positioning)
            {
                Dispatcher.Invoke(() =>
                {
                    Canvas.SetLeft(LeftEye, gazePosition.X);
                    Canvas.SetLeft(RightEye, eyePosition.X + 20);
                    Canvas.SetTop(LeftEye, eyePosition.Y);
                    Canvas.SetTop(RightEye, eyePosition.Y + 20);
                    LeftEye.Width = LeftEye.Height = eyePosition.Z;
                    RightEye.Width = RightEye.Height = eyePosition.Z;
                });
            }*/

            //TODO Dispatcher.Invoke(() => Instructions.Text = "Blinka för att börja.");
            //while (!blinked) session.Wait(100);

            //Dispatcher.Invoke(() => Instructions.Text = "Titta på bilderna.");
            //session.Wait(100);

            // TODO Calibration.
            /* 
             * while (stage == Stage.Calibration) {
             * 
            iet.StartCalibrationAsync((ErrorCode e) => { });
            iet.AddCalibrationPointAsync(new Point2D(2, 2), (ErrorCode e) => { });
            iet.ComputeAndSetCalibrationAsync((ErrorCode e) => {
                if (e == ErrorCode.FirmwareOperationFailed) throw new Exception();
            });
            iet.StopCalibrationAsync((ErrorCode e) => { });
             * }
            */

            // Display slides and collect reaction data.
            var reactions = new Dictionary<Image, double>();
            iet.StartTracking();
            foreach (var image in images)
            {
                // Switch to the next slide on the GUI thread.
                var switchSlide = Dispatcher.BeginInvoke(new Action(() => Slide.Source = image.Source));
                switchSlide.Wait();

                // Store image reaction by reading gaze data.
                reactions.Add(image, 0);
                var SLIDE_DURATION = 100;
                var SAMPLES = 10;
                for (int s = 0; s < SAMPLES; ++s)
                {
                    reactions[image] += gazeMovement;
                    session.Wait(SLIDE_DURATION / SAMPLES);
                }
            }
            iet.StopTracking();

            // Pick the images that interested the user most.
            var MAX_LAYERS = 5;
            var layers = reactions.OrderBy(x => x.Value).Take(MAX_LAYERS).Reverse();

            // Construct composite from the reaction data.
            Dispatcher.Invoke(() =>
            {
                Slide.Visibility = Visibility.Hidden;
                foreach (var layer in layers) Composite.Children.Add(layer.Key);
                //TODO Render canvas as png.
                //TODO Send png to a wi-fi printer.
            });
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

                // Blink detection
                long MINIMUM_BLINK_DURATION = 3000;
                blinked = (!blinked && d.Timestamp - timeStamp > MINIMUM_BLINK_DURATION) ? true : false;

                // Set current gaze point on screen.
                var gazePointOnScreen = new Action(() => {
                    gaze = new Point2D(Left+Width*(d.Left.GazePointOnDisplayNormalized.X + d.Right.GazePointOnDisplayNormalized.X) / 2, Top+Height*(d.Left.GazePointOnDisplayNormalized.Y + d.Right.GazePointOnDisplayNormalized.Y) / 2);
                });
                Dispatcher.BeginInvoke(gazePointOnScreen).Wait();

                userDistanceFromEyeTracker = (d.Left.EyePositionFromEyeTrackerMM.Z + d.Right.EyePositionFromEyeTrackerMM.Z) / 2;
                var lx = d.Left.EyePositionInTrackBoxNormalized.X;
                var ly = d.Left.EyePositionInTrackBoxNormalized.Y;
                var lz = d.Left.EyePositionInTrackBoxNormalized.Z;
                var rx = d.Right.EyePositionInTrackBoxNormalized.X;
                var ry = d.Right.EyePositionInTrackBoxNormalized.Y;
                var rz = d.Right.EyePositionInTrackBoxNormalized.Z;
                insideTrackBox = ((0 <= lx && lx <= 1 && 0 <= ly && ly <= 1 && 0 <= lz && lz <= 1) || (0 <= rx && rx <= 1 && 0 <= ry && ry <= 1 && 0 <= rz && rz <= 1)) ? true : false;
                trackBoxPosition = d.Left.EyePositionInTrackBoxNormalized;
                timeStamp = d.Timestamp;

                //TODO Replace with XAML data bindings.
                var guiUpdates = new Action(() =>
                {
                    Canvas.SetLeft(Marker, gaze.X);
                    Canvas.SetTop(Marker, gaze.Y);

                    double x = trackBoxPosition.X - 0.5, y = trackBoxPosition.Y - 0.5, z = trackBoxPosition.Z - 0.5;
                    TrackBoxIndicatorBlur.Radius = Marker.Width * Math.Sqrt(x * x + y * y + z * z);
                });
                Dispatcher.Invoke(guiUpdates);
            }
        }

        void onLoaded(object s, RoutedEventArgs e)
        {
            // Load images from directory.
            images = new List<Image>();
            foreach (var file in Directory.GetFiles("Images", "*.png"))
            {
                FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                Image i = new Image();
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = stream;
                bi.EndInit();
                i.Source = bi;
                images.Add(i);
            }
        }

        void onKeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Space && (session == null || session.IsCompleted))
            {
                session = new Task(runSession);
                session.Start();
            }
        }
    }
}
