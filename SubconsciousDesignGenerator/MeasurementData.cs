using System.Collections.Generic;
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
        public double AverageHitCountNormalized { get; set; }
        public List<Measurement> HitCounts { get; set; }

        public MeasurementData(Dictionary<ImageSource, int> hitCounts)
        {
            var arr = hitCounts.OrderByDescending(x => x.Value).ToArray();
            var max = arr[0].Value;
            var min = arr[arr.Length - 1].Value;
            var range = max - min;

            foreach (var layer in arr) AverageHitCount += layer.Value / arr.Length;
            AverageHitCountNormalized = (range != 0) ? (AverageHitCount - min) / (double)range : 0;

            HitCounts = new List<Measurement>();
            foreach (var m in hitCounts.OrderByDescending(x => x.Value))
            {
                HitCounts.Add(new Measurement { 
                    ImageSource = m.Key,
                    HitCount = m.Value, 
                    HitCountNormalized = (range != 0) ? (m.Value - min) / (double)range : 0
                });
            }
        }
    }
}
