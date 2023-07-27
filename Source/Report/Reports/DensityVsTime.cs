using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Report.DataStructures;

namespace Report.Reports
{
    public class DensityVsTime : IReport
    {
        private ConcurrentBag<Point> Dataset { get; }

        public DensityVsTime(ref SortedDictionary<double, HashSet<Step>> allSteps, Func<Step, int, bool> filter)
        {
            Dataset = new ConcurrentBag<Point>();
            Parallel.ForEach(allSteps, step => {
                double pointsValueMean = Mean(step.Value.Where(filter));
                Dataset.Add(new Point(step.Key, pointsValueMean));
            });
        }

        private static double Mean(IEnumerable<Step> steps)
        {
            try
            {
                return steps.Average(x => x.Density);
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        public List<Point> GetDataset()
        {
            return Dataset.ToList();
        }
    }
}