using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SubconsciousDesignGenerator
{
    public class MeasurementData
    {
        public class Measurement
        {
            public string ImagePath { get; set; } //TODO Remove?
            public ImageSource FullSizeImage { get; set; }
            public ImageSource ThumbnailImage { get; set; }
            public int HitCount { get; set; }
            public double HitCountNormalized { get; set; }
        }

        public double AverageHitCount { get; set; }
        public int MedianHitCount { get; set; }
        public double EuclideanNorm { get; set; }
        public List<Measurement> HitCounts { get; set; }

        public MeasurementData(Dictionary<string, int> hitCounts, Dictionary<string, BitmapImage> images, Dictionary<string, BitmapImage> thumbs)
        {
            var kvps = hitCounts.OrderByDescending(kvp => kvp.Value).ToList();
            var max = kvps.Max(kvp => kvp.Value);
            var min = kvps.Min(kvp => kvp.Value);
            var count = kvps.Count;
            var total = (max != 0) ? kvps.Sum(kvp => kvp.Value) : 1;

            AverageHitCount = total / (double)count;
            MedianHitCount = kvps[kvps.Count / 2].Value; // Works because the list is sorted.
            HitCounts = new List<Measurement>();
            kvps.ForEach(kvp => HitCounts.Add(new Measurement
            {
                ImagePath = kvp.Key,
                FullSizeImage = images[kvp.Key],
                ThumbnailImage = thumbs[kvp.Key],
                HitCount = kvp.Value,
                HitCountNormalized = kvp.Value / (double)total
            }));
            EuclideanNorm = Math.Sqrt(HitCounts.Sum(m => Math.Pow(m.HitCount / (double)total, 2)));
#if DEBUG
            Debug.Assert(max >= min);
            Debug.Assert(0 <= EuclideanNorm && EuclideanNorm <= 1);
#endif
        }
    }
}
