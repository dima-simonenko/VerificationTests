using Report.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Report
{
    public static class Extentions
    {
        private const double Tolerance = 0.000000001;
        public static IEnumerable<double> Range(double min, double max, double step)
        {
            double i;
            for (i = min; i <= max; i += step)
                yield return Math.Round(i,2);

            if (Math.Abs(i - (max + step)) > Tolerance) // Возвращаем, включая последнее значение.
                yield return Math.Round(max, 1);
        }

        public static double GetEvacuationTime(ref SortedDictionary<double, HashSet<Step>> allSteps, double registrator)
        {
            double evacuationTime = 0.0;
            const double timeStep = 0.2;
            double sourceLastKey = allSteps.Last().Key;
            try
            {
                while (evacuationTime < sourceLastKey && allSteps[evacuationTime].Any(x => x.Location.X <= registrator))
                {
                    evacuationTime = Math.Round(evacuationTime + timeStep, 1);
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return evacuationTime;
        }
    }
}