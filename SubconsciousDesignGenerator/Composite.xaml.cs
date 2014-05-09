using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for Composite.xaml
    /// </summary>
    public partial class Composite : Window
    {
        Random r;
        public Composite()
        {
            InitializeComponent();
            r = new Random();
        }

        public void CreateCompositeImage(MeasurementData md)
        {
            // If the user looked at a few images a lot more than the others, keep only those few images.
            var layers = md.HitCounts.Take((int)Math.Round(md.HitCounts.Count * (1 - md.EuclideanNorm)));

            // Go through all image layers and scale, position and rotate them on the layout canvas.
            var z = 1;
            foreach (var hc in layers)
            {
                // Skip images that got less than the average amount of hits.
                if (hc.HitCount < md.AverageHitCount) continue;

                // Create new image control.
                var i = new Image();
                i.Source = hc.ImageSource;
                Canvas.SetZIndex(i, ++z);

                // Scale image.
                i.Height = hc.HitCountNormalized * Height;
                i.Width = hc.HitCountNormalized * Width;

                // Position image on the canvas.
                int w = (int)(Width - i.Width / 2);
                int h = (int)(Height - i.Height / 2);
                Canvas.SetLeft(i, (-w + r.Next(2 * w + 1)));
                Canvas.SetTop(i, (-h + r.Next(2 * h + 1)));

                // Rotate image randomly.
                i.RenderTransform = new RotateTransform(r.Next(360));

                Layout.Children.Add(i);
            }

            DataContext = md;
        }

        public void SaveCompositeImage()
        {
            if (!Directory.Exists("Output")) Directory.CreateDirectory("Output");
            string f = "Output/" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";

            Size size = new Size(CompositeImage.Width, CompositeImage.Height);
            //CompositeImage.Measure(size);
            //CompositeImage.Arrange(new Rect(size));

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
            PrintQueue printer = LocalPrintServer.GetDefaultPrintQueue();
            var pd = new PrintDialog();
            pd.PrintQueue = printer;
            pd.PrintVisual(Layout, "");
        }

        void onMaximized(object s, EventArgs e)
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
        }
    }
}
