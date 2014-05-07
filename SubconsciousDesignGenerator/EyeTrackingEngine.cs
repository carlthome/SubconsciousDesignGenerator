using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Tobii.Gaze.Core;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Uses the Tobii Gaze SDK to establish a connection to an eye tracker unit and places the mouse pointer at the primary screen gaze point so that all WPF mouse events are gaze enabled.
    /// </summary>
    public class EyeTrackingEngine
    {
        public readonly IEyeTracker iet; //TODO Make public methods instead of keeping the field public.
        public bool connected; //TODO Exchange for enum and event. Shouldn't be public.
        public event EventHandler<GazePointEventArgs> GazePoint;
        public event EventHandler<HeadMovementEventArgs> HeadMovement;

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public EyeTrackingEngine()
        {
            try
            {
                Uri url = new EyeTrackerCoreLibrary().GetConnectedEyeTracker();
                iet = new EyeTracker(url);
                iet.EyeTrackerError += onEyeTrackerError;
                iet.GazeData += onGazeData;
                Task.Factory.StartNew(() => iet.RunEventLoop());
                iet.Connect();
                iet.StartTracking();
                connected = true;
            }
            catch (EyeTrackerException) { connected = false; }
            catch (NullReferenceException) { connected = false; }
        }

        void onEyeTrackerError(object s, EyeTrackerErrorEventArgs e)
        {
            connected = false;
        }

        void onGazeData(object s, GazeDataEventArgs e)
        {
            //TODO Add support for one-eyed users (pirate-mode, yeaarrrgh!).
            if (e.GazeData.TrackingStatus == TrackingStatus.NoEyesTracked) return;
            else if (e.GazeData.TrackingStatus == TrackingStatus.BothEyesTracked)
            {
                // Calculate the user's head's distance from the eye tracker camera.
                //TODO Include the eye tracker angle in the mount when calculating the distance to the user's eyes.
                raiseHeadMoved((e.GazeData.Left.EyePositionFromEyeTrackerMM.Z + e.GazeData.Right.EyePositionFromEyeTrackerMM.Z) / 2);

                // Set gaze point on the screen.
                var x = (int)(System.Windows.SystemParameters.WorkArea.Width * (e.GazeData.Left.GazePointOnDisplayNormalized.X + e.GazeData.Right.GazePointOnDisplayNormalized.X) / 2);
                var y = (int)(System.Windows.SystemParameters.WorkArea.Height * (e.GazeData.Left.GazePointOnDisplayNormalized.Y + e.GazeData.Right.GazePointOnDisplayNormalized.Y) / 2);
                raiseGazePoint(x, y);

                // Set mouse cursor at the gaze point on the screen, so WPF mouse events are triggered by the gaze.
                SetCursorPos(x, y);
            }
        }

        void raiseHeadMoved(double d)
        {
            var handler = HeadMovement;
            if (handler != null)
            {
                handler(this, new HeadMovementEventArgs(d));
            }
        }

        void raiseGazePoint(int x, int y)
        {
            var handler = GazePoint;
            if (handler != null)
            {
                handler(this, new GazePointEventArgs(x, y));
            }
        }
    }

    /// <summary>
    /// Class representing the user's gaze point on the screen.
    /// </summary>
    public class GazePointEventArgs : EventArgs
    {
        public GazePointEventArgs(int x, int y) { GazePoint = new Point(x, y); }
        public Point GazePoint { get; private set; }
    }

    /// <summary>
    /// Class representing the user's head distance from the eye tracker camera.
    /// </summary>
    public class HeadMovementEventArgs : EventArgs
    {
        public HeadMovementEventArgs(double d) { Distance = d; }
        public double Distance { get; private set; }
    }
}
