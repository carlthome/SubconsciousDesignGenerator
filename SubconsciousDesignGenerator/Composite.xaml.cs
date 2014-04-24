using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SubconsciousDesignGenerator
{
    /// <summary>
    /// Interaction logic for Composite.xaml
    /// </summary>
    public partial class Composite : Window
    {
        List<KeyValuePair<ImageSource, int>> layers;
        public Composite(List<KeyValuePair<ImageSource, int>> layers)
        {
            InitializeComponent();
            this.layers = layers;
        }

        void onLoaded(object s, RoutedEventArgs e)
        {
            Random rng = new Random();
            var r = Height / 6;
            var c = Width / 6;

            // Create an image layout.
            int l = layers.Count;
            int z = 0;
            foreach (var layer in layers)
            {
                var i = new Image();
                i.Source = layer.Key;
                i.Height = l * r;
                i.Width = l * c;
                Canvas.SetZIndex(i, z++);
                Canvas.SetLeft(i, (rng.Next(12) - 6) * r);
                Canvas.SetTop(i, (rng.Next(12) - 6) * c);
                Layout.Children.Add(i);
                --l;
            }

            //TODO Render window as png.
            //TODO Send png to a wi-fi printer.
        }


    }
}
