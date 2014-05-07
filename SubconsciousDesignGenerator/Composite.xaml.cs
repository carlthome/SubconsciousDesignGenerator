using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            var z = 1;
            foreach (var hc in md.HitCounts)
            {
                // Only keep images in the composition with a normalized hit count larger or equal to the average.
                if (hc.HitCountNormalized < md.AverageHitCountNormalized) continue;

                // Create new image control.
                var i = new Image();
                i.Source = hc.ImageSource;
                Canvas.SetZIndex(i, ++z);

                // Scale image.
                i.Height = (md.AverageHitCountNormalized != 0) ? hc.HitCountNormalized * Height / md.AverageHitCountNormalized : Height/md.HitCounts.Count;
                i.Width = (md.AverageHitCountNormalized != 0) ? hc.HitCountNormalized * Width / md.AverageHitCountNormalized : Width/md.HitCounts.Count;

                // Position image on the canvas.
                int w = (int)(Width + i.Width / 2);
                int h = (int)(Height + i.Height / 2);
                Canvas.SetLeft(i, (-w + r.Next(2 * w + 1)));
                Canvas.SetTop(i, (-h + r.Next(2 * h + 1)));

                // Rotate image randomly.
                i.RenderTransform = new RotateTransform(r.Next(360));

                Layout.Children.Add(i);
            }
        }

        public void SaveCompositeImage()
        {
            if (!Directory.Exists("Output")) Directory.CreateDirectory("Output");
            string filename = "Output/" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)Layout.Width, (int)Layout.Height, 96d, 96d, PixelFormats.Pbgra32);
            Layout.Measure(new Size((int)Layout.Width, (int)Layout.Height));
            Layout.Arrange(new Rect(new Size((int)Layout.Width, (int)Layout.Height)));
            renderBitmap.Render(Layout);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (FileStream fs = File.Create(filename)) encoder.Save(fs);
        }

        public void PrintCompositeImage()
        {
            MessageBox.Show("Utskriftsfunktionen saknas.");
        }
    }
}
