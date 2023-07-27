using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Report.DataStructures;

namespace Report.Reports
{
    public class IntensityVsTime :IReport
    {
        private const double fixedTimeDelta = 5; //Интервал времени в секундах, на котором вычисляется интенсивность
        private readonly double evacuationTime;

        private Config config;
        private List<Point> Dataset { get; }

        private IntensityVsTime(SortedDictionary<double, HashSet<Step>> allSteps, double registrator, double f, double w)
        {
            Dataset = new List<Point>();
            config = new Config { F = f, Width = w };

            evacuationTime = Extentions.GetEvacuationTime(ref allSteps, registrator);

            // Вычисляем крайние значения отрезков времени, 
            // на которых будет вычислятся интенсивность
            IEnumerable<double> timeRange = Extentions.Range(0, evacuationTime, fixedTimeDelta)
                .Reverse()
                .Skip(1);

            foreach(double time in timeRange)
            {
                double intensity = CalcIntensityWithFixedTimeDelta(Math.Round(time,2), ref allSteps, registrator, fixedTimeDelta);
                double valueXcoord = time + fixedTimeDelta > evacuationTime ? time + (evacuationTime - time) / 2 : time + fixedTimeDelta / 2;
                Dataset.Add(new Point(valueXcoord, intensity));
            }
        }

        public IntensityVsTime(ref SortedDictionary<double, HashSet<Step>> source, double registrator, Config c) : this(source,
            registrator, c.F, c.Width)
        {
        }

        private double CalcIntensityWithFixedTimeDelta(double time, ref SortedDictionary<double, HashSet<Step>> allSteps, double registratorPosition, double timeDelta)
        {
            // Ищем людей, которые находятся слева от регистратора/двери
            // и считаем их количество
            int stepsAtTimeBeforeRegistratorCount = allSteps[time].Count(step => step.Location.X < registratorPosition);
            // Корректируем значение, если отрезок времени не последний
            if(!(timeDelta + time > evacuationTime))
                stepsAtTimeBeforeRegistratorCount = stepsAtTimeBeforeRegistratorCount - allSteps[Math.Round(time + timeDelta,2)].Count(x => x.Location.X <= registratorPosition);
            return stepsAtTimeBeforeRegistratorCount * config.F * 60 / (config.Width * timeDelta);
        }

        public List<Point> GetDataset()
        {
            return Dataset;
        }
    }

    
}
