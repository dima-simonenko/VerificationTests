using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Report.DataStructures;

namespace Report.Reports
{
    public class ProbabilityVsDensity : IReport
    {
        private const double MinRangeValue = 0;
        private const double MaxRangeValue = 1;
        private const double IntervalStep = 0.05;
        private List<Point> Dataset { get; }
        public double Time { get; }
        public double Mean { get; }
        public double EV { get; private set; }
        public double SD { get; private set; }

        public ProbabilityVsDensity(ref HashSet<Step> steps, double time, Func<Step, int, bool> filter)
        {
            Time = time;
            const double offset = IntervalStep / 2;
            Dataset = new List<Point>();
            double[] intervals = Extentions.Range(MinRangeValue, MaxRangeValue, IntervalStep).ToArray();
            EV = 0;
            int stepsCount = steps.Where(filter).Count();
            try
            {
                Mean = steps.Where(filter).Average(x => x.Density);
            }
            catch (InvalidOperationException)
            {
                Mean = 0;
            }

            EV = CalculateEV(steps, filter, offset, intervals, stepsCount, out int filteredStepsCount);

            SD = CalculateSD(steps, filter, offset, intervals, filteredStepsCount);

        }

        private double CalculateEV(HashSet<Step> steps, Func<Step, int, bool> filter, double offset, double[] intervals, int stepsCount, out int filteredStepsCount)
        {
            double ev = 0;
            filteredStepsCount = 0;
            for (int i = 0; i < intervals.Length - 1; ++i)
            {
                var stepsOnInterval = steps.Where(step => filter(step, 0) && step.Density > intervals[i] && step.Density <= intervals[i + 1]).ToList();
                double prob = (double)stepsOnInterval.Count / stepsCount;
                Dataset.Add(new Point(intervals[i] + offset, prob));

                ev += (intervals[i] + offset) * stepsOnInterval.Count;
                filteredStepsCount += stepsOnInterval.Count;
            }

            if (filteredStepsCount != 0)
            {
                ev /= filteredStepsCount;
            }
            else
            {
                ev = 0;
            }

            return ev;
        }

        private double CalculateSD(HashSet<Step> steps, Func<Step, int, bool> filter, double offset, double[] intervals, int filteredStepsCount)
        {
            double sd = 0;
            for (int i = 0; i < intervals.Length - 1; ++i)
            {
                List<Step> stepsOnInterval = steps.Where(step => filter(step, 0) && step.Density >= intervals[i] && step.Density <= intervals[i + 1]).ToList();
                sd += Math.Pow((intervals[i] + offset) - EV, 2.0) * stepsOnInterval.Count;
            }

            if (filteredStepsCount != 0)
            {
                sd /= filteredStepsCount;
            }
            else
            {
                sd = 0;
            }
            return sd;
        }


        public List<Point> GetDataset()
        {
            return Dataset;
        }
    }
}