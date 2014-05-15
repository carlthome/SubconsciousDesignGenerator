﻿using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;
using System.Windows.Media.Animation;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for CompositeWindow.xaml
    /// </summary>
    public partial class CompositeWindow : Window
    {
        Random r;
        public CompositeWindow()
        {
            InitializeComponent();
            r = new Random();
        }

        public void CreateCompositeImage(MeasurementData md)
        {
            DataContext = md;

            // If the user looked at a few images a lot more than the others, keep only those few images. Also, skip images that got less than the average amount of hits.
            var layers = md.HitCounts
                .Take((int)Math.Round(md.HitCounts.Count * (1 - md.EuclideanNorm)))
                .Where(l => l.HitCount >= md.AverageHitCount)
                .ToList();

            // Go through all image layers and scale, position and rotate them on the layout canvas.
            var z = 1;
            foreach (var hc in layers)
            {
                // Create new image control.
                var i = new Image();
                RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.Fant);
                i.Source = hc.ImageSource;
                Canvas.SetZIndex(i, ++z);

                // Scale image.
                i.Height = md.EuclideanNorm * layers.Count * CompositeImage.Height / z;
                i.Width = md.EuclideanNorm * layers.Count * CompositeImage.Width / z;

                // Position image on the canvas.
                Func<double> center = () => (CompositeImage.Width - i.Width) / 2; // Image center position on canvas.
                Func<double, double> random = (double x) => r.NextDouble() * 2 * x - x; // Random number between -x and x.
                Canvas.SetLeft(i, center() + random((1 - md.EuclideanNorm) * CompositeImage.Width));
                Canvas.SetTop(i, center() + random((1 - md.EuclideanNorm) * CompositeImage.Height));

                // Rotate image randomly.
                i.RenderTransform = new RotateTransform(r.Next(360));

                Layout.Children.Add(i);
            }
        }

        public void SaveCompositeImage()
        {
            if (!Directory.Exists("Output")) Directory.CreateDirectory("Output");
            string f = "Output/" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";

            Size size = new Size(CompositeImage.Width, CompositeImage.Height);
            CompositeImage.UpdateLayout();
            CompositeImage.Arrange(new Rect(size));

            RenderTargetBitmap r = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            r.Render(CompositeImage);

            using (FileStream fs = File.Create(f))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(r));
                encoder.Save(fs);
            }
        }

        public void PrintCompositeImage()
        {
            var pd = new PrintDialog();
            pd.PrintQueue = LocalPrintServer.GetDefaultPrintQueue();
            pd.PrintVisual(CompositeImage, "");
        }

        void onLoaded(object s, EventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        void onDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
        {
            Layout.Children.Clear();
            //TODO Fix.
            var sb = FindResource("BlinkAnimation") as Storyboard;
            Storyboard.SetTarget(sb, this);
            sb.Begin();
        }
    }
}
