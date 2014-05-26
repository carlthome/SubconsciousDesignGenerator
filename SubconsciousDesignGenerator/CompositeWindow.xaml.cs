using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for CompositeWindow.xaml
    /// </summary>
    public partial class CompositeWindow : Window
    {
        RenderTargetBitmap r; // TODO Remember the law of Demeter.
        public CompositeWindow()
        {
            InitializeComponent();
        }

        public void CreateCompositeImage(MeasurementData md)
        {
            DataContext = md;
            Layout.Children.Clear();
            Random rng = new Random();

            // Remove the images the user wasn't interested in.
            var layers = md.HitCounts
                .Take((int)Math.Round(md.HitCounts.Count * (1 - md.EuclideanNorm)))
                .Where(l => l.HitCount > md.MedianHitCount)
                .Where(l => l.HitCount > md.AverageHitCount)
                .ToList();

            // Make sure most popular images are on top.
            layers.Reverse();

            // Go through all images and scale, position and rotate them on a canvas.
            var z = 1;
            foreach (var hc in layers)
            {
                // Create new image control.
                var i = new Image();
                RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.Fant);

                // Scale image.
                i.Height = md.EuclideanNorm * layers.Count * Layout.Height / z;
                i.Width = i.Height * (hc.FullSizeImage.PixelHeight / hc.FullSizeImage.PixelWidth);

                // Get image source.
                i.Source = hc.FullSizeImage;

                // Soften enlarged images.
                if (i.Height > hc.FullSizeImage.Height)
                {
                    var be = new BlurEffect();
                    be.RenderingBias = RenderingBias.Quality;
                    be.KernelType = KernelType.Box;
                    be.Radius = 1 + (int)i.Height / hc.FullSizeImage.Height;
                    i.Effect = be;
                }

                // Set depth ordering.
                Canvas.SetZIndex(i, ++z);

                // Position image on the canvas.
                Func<double, double> random = (double x) => rng.NextDouble() * 2 * x - x; // Random number between -x and x.
                var compactness = 0.9;
                Canvas.SetLeft(i, (Layout.Width - i.Width) / 2 + random(compactness * (1 - md.EuclideanNorm) * Layout.Width));
                Canvas.SetTop(i, (Layout.Height - i.Height) / 2 + random(compactness * (1 - md.EuclideanNorm) * Layout.Height));

                // Rotate image randomly.
                i.RenderTransform = new RotateTransform(rng.Next(360), i.Width / 2, i.Height / 2);

                // Add image to canvas.
                Layout.Children.Add(i);
            }

            // Rasterize.
            Size size = new Size(CompositeImage.Width, CompositeImage.Height);
            CompositeImage.UpdateLayout();
            CompositeImage.Arrange(new Rect(size));
            r = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            r.Render(CompositeImage);
        }

        public void SaveCompositeImage()
        {
            if (!Directory.Exists("Output")) Directory.CreateDirectory("Output");
            string filePath = "Output/" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".tiff";
            using (FileStream fs = File.Create(filePath))
            {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(r));
                encoder.Save(fs);
            }
        }

        public void PrintCompositeImage()
        {
            var pd = new PrintDialog();
            pd.PrintQueue = LocalPrintServer.GetDefaultPrintQueue();
            var pc = pd.PrintQueue.GetPrintCapabilities(pd.PrintTicket);
            double scale = Math.Min(pc.PageImageableArea.ExtentWidth / CompositeImage.Width, pc.PageImageableArea.ExtentHeight / CompositeImage.Height);
            var s = new Size(pc.PageImageableArea.ExtentWidth, pc.PageImageableArea.ExtentHeight);
            var o = new Point(pc.PageImageableArea.OriginWidth, pc.PageImageableArea.OriginHeight);

            var c = new Canvas();
            var i = new Image();
            i.Source = r;
            c.Children.Add(i);
            c.LayoutTransform = new ScaleTransform(scale, scale);
            c.Measure(s);
            c.Arrange(new Rect(o, s));
            pd.PrintVisual(c, "");
        }

        void onLoaded(object s, EventArgs e)
        {
            WindowState = WindowState.Maximized;
        }
    }
}
