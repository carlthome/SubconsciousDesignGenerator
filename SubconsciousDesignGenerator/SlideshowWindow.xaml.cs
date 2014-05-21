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
        CompositeWindow composite;
        Dictionary<string, BitmapImage> images;
#if DEBUG
        const int SLIDE_DURATION = 100;
#else
        const int SLIDE_DURATION = 800;
#endif
        public SlideshowWindow()
        {
            InitializeComponent();
        }

        void onLoaded(object s, RoutedEventArgs e)
        {
            if (App.eyeTracker.connected)
            {
                // Open composite window maximized on second screen if possible.
                composite = new CompositeWindow();
                if (System.Windows.Forms.SystemInformation.MonitorCount > 1)
                {
                    System.Drawing.Rectangle wa = System.Windows.Forms.Screen.AllScreens[1].WorkingArea;
                    composite.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    composite.Left = wa.Left;
                    composite.Top = wa.Top;
                    composite.Topmost = true;
                    composite.Show();
                }
#if DEBUG
            else
            {
                composite.Show();
                System.Drawing.Rectangle wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                WindowState = composite.WindowState = WindowState.Normal;
                Top = composite.Top = wa.Top;
                Left = wa.Left;
                Width = composite.Width = composite.Left = wa.Width / 2;
                Height = composite.Height = wa.Height;
            }
#endif
                // Load images.
                new DialogWindow(
                    "LADDAR BILDER.\nVAR GOD VÄNTA.",
                    () =>
                    {
                        images = new Dictionary<string, BitmapImage>();
                        foreach (var imagePath in Directory.GetFiles("Input", "*.png").ToList())
                        {
                            FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                            BitmapImage bi = new BitmapImage();
                            bi.BeginInit();
                            bi.DecodePixelHeight = (int)Height;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = fs;
                            bi.EndInit();
                            images.Add(imagePath, bi);
                        }
                    }
                ).ShowDialog();

                // Start slideshow.
                slideshow = Task.Factory.StartNew(runSlideshow);
            }
            else
            {
                Close();
                MessageBox.Show("Kameran fungerar inte.");
            }
        }

        void runSlideshow()
        {
            Dispatcher.Invoke(() =>
            {
                // Help user position themselves infront of the camera.
                (new GazeWindow("SKAPA DIN UNDERMEDVETNA BILDKOMPOSITION HÄR.")).ShowDialog();

                // Ask user if they want to begin.
                (new DialogWindow("STARTA BILDSPEL\n&\nAVLÄSNING?", "JA")).ShowDialog();

                Slide.Visibility = Visibility.Visible;
            });

            // Display slides and collect reaction data.
            var hits = new Dictionary<string, int>();
            Random r = new Random();
            var q = new Queue<string>(images.Keys.OrderBy(_ => r.Next()));
            foreach (var imagePath in images.Keys) hits.Add(imagePath, 0);
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
                Dispatcher.Invoke(() =>
                {
#if DEBUG
                    Points.Children.Clear();
#endif
                    Image1.SetValue(Image.SourceProperty, images[i1]);
                    Image2.SetValue(Image.SourceProperty, images[i2]);
                });

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
                MeasurementData md = null;

                // Generate image composite.
                new DialogWindow(
                    "DIN UNDERMEDVETNA\nBILDKOMPOSITION SKAPAS.\nVAR GOD VÄNTA.",
                    () =>
                    {
                        md = new MeasurementData(hits, images);
                        composite.CreateCompositeImage(md);
                        composite.SaveCompositeImage();
                    }
                ).ShowDialog();

                // TODO Display print preview.

                // Ask user if they want to print the composite image.
                var printDialog = new DialogWindow("AVLÄSNINGEN KLAR.\nVILL DU SKRIVA UT DITT RESULTAT?", "JA", "NEJ");
                printDialog.ShowDialog();
                if (printDialog.DialogResult.Value)
                {
                    new DialogWindow(
                        "SKRIVER UT DIN UNDERMEDVETNA BILDKOMPOSITION.",
                        () => composite.PrintCompositeImage()
                    ).ShowDialog();
                }

                // Display statistics for a while.
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
