using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for Slideshow.xaml
    /// </summary>
    public partial class Slideshow : Window
    {
        Task slideshow;
        Composite composite;
        List<ImageSource> images;
        const int SLIDE_DURATION = 800;

        public Slideshow()
        {
            InitializeComponent();
        }

        void onLoaded(object s, RoutedEventArgs e)
        {
            if (App.eyeTracker.connected)
            {
                (composite = new Composite()).Show();

                // Load images from directory.
                images = new List<ImageSource>();
                foreach (var file in Directory.GetFiles("Input", "*.png"))
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

                Random r = new Random();
                images = images.OrderBy(_ => r.Next()).Take(10).ToList(); //TODO Remove.

                MessageBox.Show("Börja med att manuellt maximera bildspel- och kompositions-fönstrena på sina respektive skärmar.");
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
                // TODO Help user position themselves infront of the camera.

                // Ask user if they want to begin.
                var b = false;
                while (!b)
                {
                    var startDialog = new DialogWindow("STARTA BILDSPEL & AVLÄSNING?", "JA", "NEJ");
                    startDialog.ShowDialog();
                    b = startDialog.DialogResult.Value;
                }

                Slide.Visibility = Visibility.Visible;
            });

            // Display slides and collect reaction data.
            var hits = new Dictionary<ImageSource, int>();
            Random r = new Random();
            var q = new Queue<ImageSource>(images.OrderBy(_ => r.Next()));
            images.ForEach(i => hits.Add(i, 0));
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
                Dispatcher.Invoke(() => { Image1.Source = is1; Image2.Source = is2; });

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

                // Construct a composite image.
                composite.CreateCompositeImage(md);

                // Store composite image to file.
                composite.SaveCompositeImage();

                // Ask user if they want to print the composite image.
                var printDialog = new DialogWindow("AVLÄSNINGEN KLAR.\nVILL DU SKRIVA UT DITT RESULTAT?", "JA", "NEJ");
                printDialog.ShowDialog();
                if (printDialog.DialogResult.Value) composite.PrintCompositeImage();

                // Display statistics.
                var statisticsWindow = new Statistics(md);
                statisticsWindow.Show();

                // Enable new session after some time has passed.
                Task.Delay(15000).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    statisticsWindow.Close();
                    slideshow = Task.Factory.StartNew(runSlideshow);
                }));
            });
        }

        bool uniqueHit(Point point, List<Point> visited)
        {
            const double MINIMUM_DISTANCE = 50;
            return visited.TrueForAll(p => distance(point, p) <= MINIMUM_DISTANCE);
        }

        double distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        void metric(Point p, ref List<Point> points, ref int imageHit, ref int imageNotHit)
        {
            if (imageNotHit == 0 && imageHit == 0) imageHit = 3;  // Large bonus if the image was selected first.
            else if (uniqueHit(p, points)) imageHit += 2;         // Medium bonus if the gaze point is new.
            else ++imageHit;                                      // One point if the user is simply staring at the image.
            points.Add(p);
        }

        void onMaximized(object s, EventArgs e)
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            slideshow = Task.Factory.StartNew(runSlideshow);
        }

        void onNextSlide(object s, DataTransferEventArgs e)
        {

        }
    }
}
