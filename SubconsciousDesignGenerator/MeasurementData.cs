using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace SubconsciousDesignGenerator
{
    public class MeasurementData
    {
        public class Measurement
        {
            public ImageSource ImageSource { get; set; }
            public int HitCount { get; set; }
            public double HitCountNormalized { get; set; }
        }

        public int AverageHitCount { get; set; }
        public double EuclideanNorm { get; set; }
        public List<Measurement> HitCounts { get; set; }

        public MeasurementData(Dictionary<ImageSource, int> hitCounts)
        {
            var kvps = hitCounts.OrderByDescending(kvp => kvp.Value).ToList();
            var max = kvps.Max(kvp => kvp.Value);
            var min = kvps.Min(kvp => kvp.Value);
            var count = kvps.Count;
            var total = (max != 0) ? kvps.Sum(kvp => kvp.Value) : 1;

            AverageHitCount = total / count;
            HitCounts = new List<Measurement>();
            kvps.ForEach(kvp => HitCounts.Add(new Measurement { 
                ImageSource = kvp.Key, 
                HitCount = kvp.Value,  
                HitCountNormalized = kvp.Value / (double) total
            }));

            EuclideanNorm = Math.Sqrt(HitCounts.Sum(m => Math.Pow(m.HitCount / total, 2)));

            Debug.Assert(max >= min);
            Debug.Assert(0 <= EuclideanNorm && EuclideanNorm <= 1);
        }
    }
}
