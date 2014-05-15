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
        List<ImageSource> imageSources;
#if DEBUG
        const int SLIDE_DURATION = 10;
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
                // Open composite window maximized on second screen if possible, otherwise share half the width of the primary screen with this window.
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
                else
                {
                    MessageBox.Show("Projektorn saknas.");
                    composite.Show();
                    System.Drawing.Rectangle wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    WindowState = composite.WindowState = WindowState.Normal;
                    Top = composite.Top = wa.Top;
                    Left = wa.Left;
                    Width = composite.Width = composite.Left = wa.Width / 2;
                    Height = composite.Height = wa.Height;
                }

                // Load images from directory.
                imageSources = new List<ImageSource>();
                foreach (var file in Directory.GetFiles("Input", "*.png"))
                {
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    BitmapImage bi = new BitmapImage();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.BeginInit();
                    bi.StreamSource = fs;
                    bi.EndInit();
                    imageSources.Add((ImageSource)bi);
                }

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
                (new DialogWindow("STARTA BILDSPEL & AVLÄSNING?", "JA")).ShowDialog();

                Slide.Visibility = Visibility.Visible;
            });

            // Display slides and collect reaction data.
            var hits = new Dictionary<ImageSource, int>();
            Random r = new Random();
            var q = new Queue<ImageSource>(imageSources.OrderBy(_ => r.Next()));
            imageSources.ForEach(i => hits.Add(i, 0));
            int image1HitCount = 0, image2HitCount = 0;
            var points = new List<Point>();
            Image1.MouseMove += (Object s, MouseEventArgs e) => metric(e.GetPosition(this), ref points, ref image1HitCount, ref image2HitCount);
            Image2.MouseMove += (Object s, MouseEventArgs e) => metric(e.GetPosition(this), ref points, ref image2HitCount, ref image1HitCount);

            while (q.Count >= 2)
            {
                // Switch slide.
                points.Clear();
                image1HitCount = 0;
                image2HitCount = 0;
                ImageSource is1 = q.Dequeue();
                ImageSource is2 = q.Dequeue();
                Dispatcher.Invoke(() =>
                {
#if DEBUG
                    Points.Children.Clear();
#endif
                    Image1.SetValue(Image.SourceProperty, is1);
                    Image2.SetValue(Image.SourceProperty, is2);
                });

                // Measure hits for a while.
                slideshow.Wait(SLIDE_DURATION);

                // Update gaze hit data.
                hits[is1] += image1HitCount;
                hits[is2] += image2HitCount;

                // Determine winning image.
                if (image1HitCount > image2HitCount) q.Enqueue(is1);
                else q.Enqueue(is2);
            }

            // Construct result data from the hit counts.
            var md = new MeasurementData(hits);

            Dispatcher.Invoke(() =>
            {
                Slide.Visibility = Visibility.Hidden;

                // Generate image composite while displaying a notice to the user.
                var imageGenerationProgress = new DialogWindow("DIN UNDERMEDVETNA BILDKOMPOSITION SKAPAS.\nVAR GOD VÄNTA.");
                imageGenerationProgress.FadeIn.Completed += ((s, e) =>
                {
                    composite.CreateCompositeImage(md);
                    composite.SaveCompositeImage();
                    imageGenerationProgress.Close();
                });
                imageGenerationProgress.ShowDialog();

                // Ask user if they want to print the composite image.
                var printDialog = new DialogWindow("AVLÄSNINGEN KLAR.\nVILL DU SKRIVA UT DITT RESULTAT?", "JA", "NEJ");
                printDialog.ShowDialog();
                if (printDialog.DialogResult.Value)
                {
                    var printProgress = new DialogWindow("SKRIVER UT DIN UNDERMEDVETNA BILDKOMPOSITION.");
                    printProgress.FadeIn.Completed += ((s, e) =>
                    {
                        composite.PrintCompositeImage();
                        printProgress.Close();
                    });
                    printProgress.ShowDialog();
                }

                // Display statistics.
                var statisticsWindow = new StatisticsWindow(md);
                statisticsWindow.Show();

                // Enable new session after some time has passed.
                Task.Delay(15000).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    statisticsWindow.Close();
                    slideshow = Task.Factory.StartNew(runSlideshow);
                }));
            });
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

        void onNextSlide(object s, DataTransferEventArgs e)
        {
            //TODO Fix.
            var sb = FindResource("BlinkAnimation") as Storyboard;
            Storyboard.SetTarget(sb, this);
            sb.Begin();
        }
    }
}
