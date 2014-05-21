using System;
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
        string filePath;

        public CompositeWindow()
        {
            InitializeComponent();
            r = new Random();
        }

        public void CreateCompositeImage(MeasurementData md)
        {
            DataContext = md;

            // If the user looked at a few images a lot more than the others, keep only those few images. Also, skip images that got less than the median amount of hits.
            var layers = md.HitCounts
                .Take((int)Math.Round(md.HitCounts.Count * (1 - md.EuclideanNorm)))
                .Where(l => l.HitCount > md.MedianHitCount)
                .ToList();

            // Go through all image layers and scale, position and rotate them on the layout canvas.
            var z = 1;
            foreach (var hc in layers)
            {
                // Create new image control.
                var i = new Image();
                RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.Fant);

                // Scale image.
                i.Height = i.Width = md.EuclideanNorm * layers.Count * CompositeImage.Height / z;

                // Load image file.
                using (FileStream fs = new FileStream(hc.ImagePath, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.None;
                    //bi.DecodePixelHeight = ((int)i.Height < bi.PixelHeight) ? (int)i.Height : bi.PixelHeight;
                    bi.StreamSource = fs;
                    bi.EndInit();
                    i.Source = bi;
                };

                // Set depth ordering.
                Canvas.SetZIndex(i, ++z);

                // Position image on the canvas.
                Func<double, double> random = (double x) => r.NextDouble() * 2 * x - x; // Random number between -x and x.
                Canvas.SetLeft(i, (CompositeImage.Width - i.Width) / 2 + random((1 - md.EuclideanNorm) * CompositeImage.Width));
                Canvas.SetTop(i, (CompositeImage.Height - i.Height) / 2 + random((1 - md.EuclideanNorm) * CompositeImage.Height));

                // Rotate image randomly.
                i.RenderTransform = new RotateTransform(r.Next(360));

                Layout.Children.Add(i);
            }
        }

        public void SaveCompositeImage()
        {
            if (!Directory.Exists("Output")) Directory.CreateDirectory("Output");
            filePath = "Output/" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".tiff";

            Size size = new Size(CompositeImage.Width, CompositeImage.Height);
            CompositeImage.UpdateLayout();
            CompositeImage.Arrange(new Rect(size));

            RenderTargetBitmap r = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            r.Render(CompositeImage);

            using (FileStream fs = File.Create(filePath))
            {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Compression = TiffCompressOption.Lzw;
                encoder.Frames.Add(BitmapFrame.Create(r));
                encoder.Save(fs);
            }
        }

        public void PrintCompositeImage()
        {
            if (!File.Exists(filePath)) SaveCompositeImage();

            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(filePath);
            bi.EndInit();

            var dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            dc.DrawImage(bi, new Rect { Width = bi.Width, Height = bi.Height });
            dc.Close();

            var pd = new PrintDialog();
            pd.PrintQueue = LocalPrintServer.GetDefaultPrintQueue();
            pd.PrintVisual(dv, "");
            //pd.PrintVisual(CompositeImage, ""); TODO Too slow?
        }

        void onLoaded(object s, EventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        void onDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
        {
            Layout.Children.Clear();
        }
    }
}
