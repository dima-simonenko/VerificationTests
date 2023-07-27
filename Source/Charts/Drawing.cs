using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using Report;
using Report.DataStructures;
using Report.Reports;
using Point = System.Windows.Point;
using Size = System.Drawing.Size;

namespace Charts
{
    public static class Drawing
    {
        public static void RunAndSaveDensityVsLocationReport(ref SortedDictionary<double, HashSet<Step>> allSteps,
            double time,
            string pathtoSave)
        {
            DensityVsLocationChart(allSteps, time, out DensityVsLocation densityVsLocation, out Chart chart);
            SaveChart(chart, Path.Combine(pathtoSave, $"{chart.Name}_{densityVsLocation.Time * 1000}.png"));
        }

        public static List<Point> RunAndSaveIntensityVsTimeReport(ref SortedDictionary<double, HashSet<Step>> allSteps,
            double registrator,
            string pathtoSave, Config config)
        {
            IntensityVsTimeChart(allSteps, config, registrator, out IntensityVsTime intensityVsTime, out Chart chart);
            SaveChart(chart, Path.Combine(pathtoSave, $"{chart.Name}.png"));
            return intensityVsTime.GetDataset();
        }

        public static List<Point>[] RunAndSaveDensityVsTimeReport(ref SortedDictionary<double, HashSet<Step>> allSteps,
            double registrator, string pathtoSave)
        {
            DensityVsTimeChart(allSteps, registrator, out DensityVsTime densityVsTimeForNegativeX,
                out DensityVsTime densityVsTimeForPositiveX,
                out Chart chartForNegativeX, out Chart chartForPositiveX);
            SaveChart(chartForNegativeX, Path.Combine(pathtoSave, $"{chartForNegativeX.Name}.png"));
            SaveChart(chartForPositiveX, Path.Combine(pathtoSave, $"{chartForPositiveX.Name}.png"));
            return new[] {densityVsTimeForNegativeX.GetDataset(), densityVsTimeForPositiveX.GetDataset()};
        }

        private static double CalculateSpeed(double density, double freeMovementDensityLimit, double freeMovementSpeed,
            double a)
        {
            double currentSpeed = freeMovementSpeed * (1 - a * Math.Log(density / freeMovementDensityLimit));
            if (density < freeMovementDensityLimit || a == 0)
                currentSpeed = freeMovementSpeed;
            return currentSpeed;
        }

        private static double ToPerMinuteFormat(double value)
        {
            return value * 60;
        }

        public static void SpeedVsDensityPoints(Report.DataStructures.TestGroup testGroup, double registrator,
            out List<Point> expectedResults, out List<Point> observedResults, out List<Point> evacTimeVsDensity)
        {
            observedResults = new List<Point>();
            expectedResults = new List<Point>();
            evacTimeVsDensity = new List<Point>();
            foreach (TestInfo test in testGroup.Tests)
            {
                SortedDictionary<double, HashSet<Step>> allSteps = Files.Load(test.Path);
                double evacuationTime = Extentions.GetEvacuationTime(ref allSteps, registrator);
                double speed = ToPerMinuteFormat((test.TestConfig.Length + registrator) / evacuationTime);
                if (double.IsInfinity(speed))
                    speed = 0;
                observedResults.Add(new Point(test.TestConfig.Density, speed));
                evacTimeVsDensity.Add(new Point(test.TestConfig.Density, evacuationTime));
                allSteps.Clear();
            }

            SpeedVsDensity(testGroup, out expectedResults);
        }

        private static void SpeedVsDensity(Report.DataStructures.TestGroup testGroup, out List<Point> result)
        {
            result = new List<Point>();
            const double step = 1.0 / 200;
            double freeMovementDensityLimit = testGroup.GroupConfig.D0 * testGroup.GroupConfig.F;
            double funcDomain = Math.Min(1.0, freeMovementDensityLimit * Math.Pow(Math.E, 1 / testGroup.GroupConfig.A));
            if (testGroup.GroupConfig.A == 0) funcDomain = 1; // Для случая с беременными женщинами, когда a = 0.

            for (double density = 0.0; density <= funcDomain; density += step)
            {
                double speedAtdensity = CalculateSpeed(density, freeMovementDensityLimit, testGroup.GroupConfig.V0,
                    testGroup.GroupConfig.A);
                result.Add(new Point(density, speedAtdensity));
            }
        }

        public static void EvacTimeVsDensity(Report.DataStructures.TestGroup testGroup, double registrator, out List<Point> observedResults)
        {
            observedResults = new List<Point>();
            foreach (TestInfo test in testGroup.Tests)
            {
                SortedDictionary<double, HashSet<Step>> allSteps = Files.Load(test.Path);
                double evacuationTime = Extentions.GetEvacuationTime(ref allSteps, registrator);
                observedResults.Add(new Point(test.TestConfig.Density, evacuationTime));
                allSteps.Clear();
            }
        }

        public static void IntensityVsWidthChartPoints(Report.DataStructures.TestGroup testGroup,
            out List<Point> observedResults)
        {
            observedResults = new List<Point>();
            foreach (TestInfo test in testGroup.Tests)
            {
                SortedDictionary<double, HashSet<Step>> allSteps = Files.Load(test.Path);
                SortedDictionary<double, double> amountOfEvacuatedPeopleOnTime =
                    NVsTimeChartPoints(ref allSteps);
                Point endPointOfLinearFunctionSegment =
                    CalculateEndPointOfLinearFunctionSegment(amountOfEvacuatedPeopleOnTime);
                double intensity = endPointOfLinearFunctionSegment.Y * test.TestConfig.F * 60 /
                                   (endPointOfLinearFunctionSegment.X * test.TestConfig.Width);
                observedResults.Add(new Point(test.TestConfig.Width, intensity));
                allSteps.Clear();
                GC.Collect();
            }
        }

        public static void IntensityVsDensityChartPoints(Report.DataStructures.TestGroup testGroup, out List<Point> observedResults, out List<Point> expectedResults, out List<Point> expectedResultsPart2)
        {
            observedResults = new List<Point>();
            expectedResults = new List<Point>();
            expectedResultsPart2 = new List<Point>();
            foreach (TestInfo test in testGroup.Tests)
            {
                SortedDictionary<double, HashSet<Step>> allSteps = Files.Load(test.Path);
                double timeFirst = allSteps.FirstOrDefault(x => x.Value.Any(v => v.EventOccured)).Key;
                double timeLast = allSteps.LastOrDefault(x => x.Value.Any(v => v.EventOccured)).Key;
                double N = allSteps.SelectMany(x => x.Value.Where(v => v.EventOccured)).Count();
                double intensity = N * test.TestConfig.F * 60 / ((timeLast - timeFirst) * test.TestConfig.Width);
                observedResults.Add(new Point(test.TestConfig.Density, intensity));
                allSteps.Clear();
            }

            SpeedVsDensity(testGroup, out List<Point> expectedSpeed);

            foreach(Point point in expectedSpeed)
            {
                if(point.X <= 0.9)
                {
                    expectedResults.Add(new Point(point.X, point.X * point.Y));
                }
                else
                {
                    expectedResultsPart2.Add(new Point(point.X, 8.5 ));
                }
            }
        }

        public static void SetupIntensityVsWidthChart(out Chart targetChart, IEnumerable<Point> dataSource,
            IEnumerable<Point> dataSource2)
        {
            targetChart = NewChart(dataSource, new AxisLimits
                {
                    XMin = 0,
                    XMax = double.NegativeInfinity,
                    YMin = 0,
                    YMax = double.NegativeInfinity
                },
                new ChartConfig
                {
                    XTitle = "W, м",
                    YTitle = "q, м / мин",
                    Name = "IntensityVsWidth",
                    Type = SeriesChartType.Point,
                    Size = new Size { Height = 420, Width = 630 }
                });

            Series additionalSeries = new Series();
            targetChart.Series.Add(additionalSeries);

            targetChart.Series.Last().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.Last().XValueMember = "X";
            targetChart.Series.Last().YValueMembers = "Y";
            targetChart.Series.Last().ChartType = SeriesChartType.Point;
            targetChart.Series.Last().MarkerSize = 5;
            targetChart.Series.Last().Points.DataBind(dataSource, "X", "Y", "");
            targetChart.Series.Last().Color = Color.Blue;

            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Line;
            targetChart.Series.First().BorderWidth = 2;
            targetChart.Series.First().MarkerSize = 2;
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().Points.DataBind(dataSource2, "X", "Y", "");
            targetChart.Series.First().Color = Color.Green;
        }

        public static void SetupSpeedVsDensityChart(out Chart targetChart, IEnumerable<Point> dataSource,
            IEnumerable<Point> dataSource2)
        {
            targetChart = NewChart(dataSource.ToList(),
                new AxisLimits
                {
                    XMin = 0,
                    XMax = 1,
                    YMin = 0,
                    YMax = double.NegativeInfinity
                },
                new ChartConfig
                {
                    XTitle = "D, м ² /м ²",
                    YTitle = "V, м /мин",
                    Name = "SpeedVsDensity",
                    Type = SeriesChartType.Point,
                    Size = new Size { Height = 420, Width = 630 }
                });

            Series additionalSeries = new Series();
            targetChart.Series.Add(additionalSeries);

            targetChart.Series.Last().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.Last().XValueMember = "X";
            targetChart.Series.Last().YValueMembers = "Y";
            targetChart.Series.Last().ChartType = SeriesChartType.Point;
            targetChart.Series.Last().MarkerSize = 5;
            targetChart.Series.Last().Points.DataBind(dataSource, "X", "Y", "");
            targetChart.Series.Last().Color = Color.Blue;

            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Line;
            targetChart.Series.First().BorderWidth = 2;
            targetChart.Series.First().MarkerSize = 2;
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().Points.DataBind(dataSource2, "X", "Y", "");
            targetChart.Series.First().Color = Color.Green;
        }

        public static SortedDictionary<double, double> NVsTimeChartPoints(
            ref SortedDictionary<double, HashSet<Step>> allSteps)
        {
            SortedDictionary<double, double> amountOfEvacuatedPeopleOnTime = new SortedDictionary<double, double>();
            HashSet<string> evacuatedPeopleNames = new HashSet<string>();
            foreach (KeyValuePair<double, HashSet<Step>> stepAtTime in allSteps)
            {
                foreach (string stepEventOccuredName in stepAtTime.Value.Where(x => x.EventOccured).Select(x => x.Name))
                {
                    evacuatedPeopleNames.Add(stepEventOccuredName);
                }

                amountOfEvacuatedPeopleOnTime.Add(stepAtTime.Key, evacuatedPeopleNames.Count);
            }

            return amountOfEvacuatedPeopleOnTime;
        }

        public static Point CalculateEndPointOfLinearFunctionSegment(
            SortedDictionary<double, double> amountOfEvacuatedPeopleOnTime)
        {
            SortedDictionary<double, double> amountOfEvacuatedPeopleOnTimeDerivative2 =
                new SortedDictionary<double, double>();
            const double step = 0.2;
            object aLock = new object();

            double maxValue = amountOfEvacuatedPeopleOnTime.Values.Max();

            double Func(double x)
            {
                double key = Math.Round(x, 1);
                if (amountOfEvacuatedPeopleOnTime.TryGetValue(key, out double value))
                    return value;
                if (key > maxValue)
                    return maxValue;
                return 0;
            }

            double Derivative(double x) => (Func(x + step) - Func(x - step)) / (2 * step);
            double Derivative2(double x) => (Derivative(x + step) - Derivative(x - step)) / (2 * step);

            IEnumerable<double> timePoints =
                amountOfEvacuatedPeopleOnTime.Keys.Skip(amountOfEvacuatedPeopleOnTime.Count / 4);
            timePoints = timePoints.Reverse();

            Parallel.ForEach(timePoints.Skip(1), timePoint =>
            {
                lock (aLock)
                {
                    amountOfEvacuatedPeopleOnTimeDerivative2.Add(timePoint, Derivative2(timePoint));
                }
            });

            //ищем максимум второй производной
            double maxDerivativative2Value = amountOfEvacuatedPeopleOnTimeDerivative2.Select(d => d.Value).Max();
            KeyValuePair<double, double> maxDerivative2Point =
                amountOfEvacuatedPeopleOnTimeDerivative2.First(d => d.Value == maxDerivativative2Value);
            return new Point(maxDerivative2Point.Key,
                amountOfEvacuatedPeopleOnTime.First(x => x.Key == maxDerivative2Point.Key).Value);
        }

        public static void RunAndSaveProbabilityVsDensityReport(ref SortedDictionary<double, HashSet<Step>> steps,
            double time,
            double registrator, string pathtoSave)
        {
            ProbabilityVsDensityChart(steps, time, registrator, out ProbabilityVsDensity dxp, out Chart chart1,
                out Chart chart2);
            SaveChart(chart1, Path.Combine(pathtoSave, $"{chart1.Name}_{dxp.Time * 1000}.png"));
            SaveChart(chart2, Path.Combine(pathtoSave, $"{chart2.Name}_{dxp.Time * 1000}.png"));
        }

        private static void DensityVsLocationChart(SortedDictionary<double, HashSet<Step>> steps, double time,
            out DensityVsLocation dvl, out Chart chart)
        {
            HashSet<Step> stepsAtTime = steps[time];
            dvl = new DensityVsLocation(ref stepsAtTime, time);
            chart = NewChart(
                dvl.GetDataset(),
                new AxisLimits
                {
                    XMin = -25,
                    XMax = 25,
                    YMin = 0,
                    YMax = 1
                },
                new ChartConfig
                {
                    XTitle = "x, м",
                    YTitle = "D, м ²/м ²",
                    Name = "DensityVsLocation",
                    Type = SeriesChartType.Point,
                    Size = new Size { Height = 420, Width = 630 }
                });
        }

        private static void IntensityVsTimeChart(SortedDictionary<double, HashSet<Step>> steps, Config config,
            double registrator,
            out IntensityVsTime intensityVsTime, out Chart chart)
        {
            intensityVsTime = new IntensityVsTime(ref steps, registrator, config);
            chart = NewChart(intensityVsTime.GetDataset(),
                new AxisLimits
                {
                    XMin = 0,
                    XMax = double.NegativeInfinity,
                    YMin = 0,
                    YMax = double.NegativeInfinity
                },
                new ChartConfig
                {
                    XTitle = "t, сек",
                    YTitle = "q, м ²/м ²",
                    Name = "IntensityVsTime",
                    Type = SeriesChartType.Point,
                    Size = new Size { Height = 420, Width = 630 }
                });
        }

        private static void DensityVsTimeChart(SortedDictionary<double, HashSet<Step>> steps, double registrator,
            out DensityVsTime densityVsTimeForNegativeX, out DensityVsTime densityVsTimeForPositiveX,
            out Chart chartForNegativeX,
            out Chart chartForPositiveX)
        {
            densityVsTimeForNegativeX = new DensityVsTime(ref steps, (x, i) => x.Location.X < registrator);
            chartForNegativeX = NewChart(densityVsTimeForNegativeX.GetDataset(),
                new AxisLimits
                {
                    XMin = 0,
                    XMax = double.NegativeInfinity,
                    YMin = 0,
                    YMax = 1
                },
                new ChartConfig
                {
                    XTitle = "t, сек",
                    YTitle = "D, м ²/м ²",
                    Name = "DensityVsTimeNegX",
                    Type = SeriesChartType.Point,
                    Size = new Size {Width = 512, Height = 300}
                });
            densityVsTimeForPositiveX = new DensityVsTime(ref steps, (x, i) => x.Location.X > registrator);
            chartForPositiveX = NewChart(densityVsTimeForPositiveX.GetDataset(),
                new AxisLimits
                {
                    XMin = 0,
                    XMax = double.NegativeInfinity,
                    YMin = 0,
                    YMax = 1
                },
                new ChartConfig
                {
                    XTitle = "t, сек",
                    YTitle = "D, м ²/м ²",
                    Name = "DensityVsTimePosX",
                    Type = SeriesChartType.Point,
                    Size = new Size { Height = 420, Width = 630 }
                });
        }

        private static void ProbabilityVsDensityChart(SortedDictionary<double, HashSet<Step>> steps, double time,
            double registrator,
            out ProbabilityVsDensity pvd, out Chart chartForPositiveX, out Chart chartForNegativeX)
        {
            HashSet<Step> stapsAtTime = steps[time];
            pvd = new ProbabilityVsDensity(ref stapsAtTime, time, (x, i) => x.Location.X >= registrator);
            chartForPositiveX = NewChart(pvd.GetDataset(),
                new AxisLimits
                {
                    XMin = 0,
                    XMax = 1,
                    YMin = 0,
                    YMax = 1
                },
                new ChartConfig
                {
                    XTitle = "D, м ²/м ²",
                    YTitle = "Вероятность",
                    Name = "ProbabilityVsDensityPosX",
                    Type = SeriesChartType.Column,
                    Size = new Size { Height = 420, Width = 630 },
                    PointSize = 0
                });

            pvd = new ProbabilityVsDensity(ref stapsAtTime, time, (x, i) => x.Location.X < registrator);
            chartForNegativeX = NewChart(pvd.GetDataset(),
                new AxisLimits
                {
                    XMin = 0,
                    XMax = 1,
                    YMin = 0,
                    YMax = 1
                },
                new ChartConfig
                {
                    XTitle = "D, м ²/м ²",
                    YTitle = "Вероятность",
                    Name = "ProbabilityVsDensityNegX",
                    Type = SeriesChartType.Column,
                    Size = new Size { Height = 420, Width = 630 },
                    PointSize = 0
                });
        }

        private static Chart NewChart(IEnumerable<Point> points, AxisLimits chartMinMax, ChartConfig chartData)
        {
            ChartArea chartArea = new ChartArea
            {
                AxisX =
                {
                    IntervalAutoMode = IntervalAutoMode.VariableCount,
                    TitleAlignment = StringAlignment.Far
                },
                AxisY =
                {
                    IntervalAutoMode = IntervalAutoMode.VariableCount,
                    TitleAlignment = StringAlignment.Far
                },
                CursorX =
                {
                    AutoScroll = false,
                    LineColor = Color.Black
                },
                CursorY = {LineColor = Color.Black}
            };
            chartArea.AxisX.IsStartedFromZero = false;

            chartArea.Name = "ChartArea";
            chartArea.AxisX.Title = chartData.XTitle;
            chartArea.AxisY.Title = chartData.YTitle;

            Chart resultChart = new Chart
            {
                Size = chartData.Size
            };

            resultChart.ChartAreas.Add(chartArea);
            resultChart.ChartAreas[0].AxisX.Minimum = chartMinMax.XMin;
            if (!double.IsNegativeInfinity(chartMinMax.XMax))
            {
                resultChart.ChartAreas[0].AxisX.Maximum = chartMinMax.XMax;
            }

            resultChart.ChartAreas[0].AxisY.Minimum = chartMinMax.YMin;
            if (!double.IsNegativeInfinity(chartMinMax.YMax))
            {
                resultChart.ChartAreas[0].AxisY.Maximum = chartMinMax.YMax;
            }

            resultChart.ChartAreas[0].AxisY.LabelStyle.Format = "{0:0.00}";
            resultChart.Name = chartData.Name;
            Series series = new Series();

            resultChart.Series.Add(series);
            resultChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            resultChart.Series.First().MarkerSize = chartData.PointSize ?? 4;
            resultChart.Series.First().XValueMember = "X";
            resultChart.Series.First().YValueMembers = "Y";
            resultChart.Series.First().ChartType = chartData.Type;
            resultChart.Series.First().BorderWidth = chartData.PointSize ?? 2;
            resultChart.Series.First().Points.DataBind(points, "X", "Y", "");
            resultChart.Series.First().Color = chartData.Color ?? Color.Blue;

            return resultChart;
        }

        public static void SaveChart(Chart chart, string path)
        {
            if (!Directory.Exists(Path.GetPathRoot(path)))
            {
                Directory.CreateDirectory(Path.GetPathRoot(path));
            }

            chart.SaveImage(path, ChartImageFormat.Png);
        }
    }
}