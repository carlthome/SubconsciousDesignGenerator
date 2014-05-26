using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for SlideshowWindow.xaml
    /// </summary>
    public partial class SlideshowWindow : Window
    {
        Task slideshow;
        CompositeWindow compositeWindow;
        Dictionary<string, BitmapImage> images, slides, thumbs;
        const int SLIDE_DURATION = 800;

        public SlideshowWindow()
        {
            InitializeComponent();
        }

        void onLoaded(object s, RoutedEventArgs e)
        {
            if (!App.eyeTracker.connected) MessageBox.Show("Kameran fungerar inte. Använder datormusen istället.");

            compositeWindow = new CompositeWindow();
            if (System.Windows.Forms.SystemInformation.MonitorCount > 1)
            {
                System.Drawing.Rectangle wa = System.Windows.Forms.Screen.AllScreens[1].WorkingArea;
                compositeWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                compositeWindow.Left = wa.Left;
                compositeWindow.Top = wa.Top;
                compositeWindow.Topmost = true;
                compositeWindow.Show();
            }
            else
            {
                compositeWindow.Show();
                System.Drawing.Rectangle wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                WindowState = compositeWindow.WindowState = WindowState.Normal;
                Top = compositeWindow.Top = wa.Top;
                Left = wa.Left;
                Width = compositeWindow.Width = compositeWindow.Left = wa.Width / 2;
                Height = compositeWindow.Height = wa.Height;
            }
        }

        void onContentRendered(object s, EventArgs e)
        {
            // Load images.
            new GazeWindow(
                "SKALAR BILDER.\nVAR GOD VÄNTA.",
                () =>
                {
                    images = new Dictionary<string, BitmapImage>();
                    slides = new Dictionary<string, BitmapImage>();
                    thumbs = new Dictionary<string, BitmapImage>();
                    string[] searchPatterns = { "*.png", "*.jpg" };
                    foreach (var imagePath in searchPatterns.SelectMany(filter => Directory.GetFiles("Input", filter)).ToArray())
                    {
                        using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            BitmapImage slide = new BitmapImage();
                            slide.BeginInit();
                            slide.DecodePixelWidth = (int)Width / 4;
                            slide.CacheOption = BitmapCacheOption.OnLoad;
                            slide.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                            slide.StreamSource = fs;
                            slide.EndInit();
                            slide.Freeze();
                            slides.Add(imagePath, slide);
                        }

                        using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                        {
                            BitmapImage thumb = new BitmapImage();
                            thumb.BeginInit();
                            thumb.DecodePixelHeight = 48;
                            thumb.CacheOption = BitmapCacheOption.OnLoad;
                            thumb.StreamSource = fs;
                            thumb.EndInit();
                            thumb.Freeze();
                            thumbs.Add(imagePath, thumb);
                        }

                        FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.None;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        images.Add(imagePath, image);
                    }
                }
            ).ShowDialog();

            slideshow = Task.Factory.StartNew(runSlideshow);
        }

        void runSlideshow()
        {
            Dispatcher.Invoke(() =>
            {
                if (!(new GazeWindow("STARTA BILDSPEL\n& AVLÄSNING?")).ShowDialog().Value)
                    while (!(new GazeWindow("GRATTIS PÅ MORS DAG!\nSTARTA?")).ShowDialog().Value) ;

                Slide.Visibility = Visibility.Visible;
            });

            // Display slides and collect reaction data.
            var hits = new Dictionary<string, int>();
            Random r = new Random();
            var q = new Queue<string>(slides.Keys.OrderBy(_ => r.Next()));
            foreach (var imagePath in slides.Keys) hits.Add(imagePath, 0);
            int hc1 = 0, hc2 = 0;
            var points = new List<Point>();
            Image1.MouseMove += (Object s, MouseEventArgs e) => metric(e.GetPosition(this), ref points, ref hc1, ref hc2);
            Image2.MouseMove += (Object s, MouseEventArgs e) => metric(e.GetPosition(this), ref points, ref hc2, ref hc1);

            while (q.Count > 2)
            {
                // Switch slide.
                points.Clear();
                hc1 = 0;
                hc2 = 0;
                string i1 = q.Dequeue();
                string i2 = q.Dequeue();
                Dispatcher.BeginInvoke(new Action(() =>
                {
#if DEBUG
                    Points.Children.Clear();
#endif
                    Image1.SetValue(Image.SourceProperty, slides[i1]);
                    Image2.SetValue(Image.SourceProperty, slides[i2]);
                })).Wait();

                // Measure hits for a while.
                slideshow.Wait(SLIDE_DURATION);

                // Update gaze hit data.
                hits[i1] += hc1;
                hits[i2] += hc2;

                // Determine winning image.
                if (hc1 > hc2) q.Enqueue(i1);
                else q.Enqueue(i2);
            }

            Dispatcher.Invoke(() =>
            {
                Slide.Visibility = Visibility.Hidden;
                MeasurementData md = new MeasurementData(hits, images, thumbs);

                // Generate image composite.
                new GazeWindow(
                    "DIN UNDERMEDVETNA\nBILDKOMPOSITION SKAPAS.\nVAR GOD VÄNTA.",
                    () => { compositeWindow.CreateCompositeImage(md); compositeWindow.SaveCompositeImage(); }
                ).ShowDialog();

                // Ask user if they want to print the composite image.
                if (new GazeWindow("VILL DU SKRIVA UT DITT RESULTAT?").ShowDialog().Value)
                {
                    new GazeWindow(
                        "SKRIVER UT DIN UNDERMEDVETNA BILDKOMPOSITION.",
                        () => compositeWindow.PrintCompositeImage()
                    ).ShowDialog();
                }

                // Display statistics until user stops looking.
                new StatisticsWindow(md).ShowDialog();
            });

            // Reset session.
            slideshow = Task.Factory.StartNew(runSlideshow);
        }

        bool unique(Point point, List<Point> visited)
        {
            const double MINIMUM_DISTANCE = 50;
            return visited.TrueForAll(p => distance(point, p) > MINIMUM_DISTANCE);
        }

        double distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        void metric(Point p, ref List<Point> points, ref int imageHit, ref int imageNotHit)
        {
            if (imageNotHit == 0 && imageHit == 0)  // Large bonus if the image was selected first.
            {
                imageHit = 3;

#if DEBUG
                var e = new Ellipse();
                e.Width = e.Height = 75;
                e.Fill = Brushes.Red;
                Points.Children.Add(e);
                Canvas.SetLeft(e, p.X);
                Canvas.SetTop(e, p.Y);
#endif
            }
            else if (unique(p, points)) // Medium bonus if the gaze point is new.
            {
                imageHit += 2;
                points.Add(p);

#if DEBUG
                var e = new Ellipse();
                e.Width = e.Height = 50;
                e.Fill = Brushes.Orange;
                Points.Children.Add(e);
                Canvas.SetLeft(e, p.X);
                Canvas.SetTop(e, p.Y);
#endif
            }
            else // One point if the user is simply staring at the image.
            {
                ++imageHit;

#if DEBUG
                var e = new Ellipse();
                e.Width = e.Height = 25;
                e.Fill = Brushes.Yellow;
                Points.Children.Add(e);
                Canvas.SetLeft(e, p.X);
                Canvas.SetTop(e, p.Y);
#endif
            }
        }
    }
}
