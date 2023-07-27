using CommandLine;
using System;
using System.Collections.Generic;
using Report;
using Charts;
using Report.DataStructures;
using System.Linq;
using System.Windows;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;

namespace ReportCLI
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptionsAndReturnExitCode)
                .WithNotParsed(HandleParseError);
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
            SortedDictionary<double, HashSet<Step>> allSteps = new SortedDictionary<double, HashSet<Step>>();
            Report.DataStructures.TestGroup testGroup = Files.LoadTestInfoList(options.Path, out TestGroup testGroupFromXML);

            if (options.Reports < 8)
                allSteps = Files.Load(testGroup.Tests[0].Path);
            switch (options.Reports)
            {
                case 1:
                    {
                        Drawing.RunAndSaveDensityVsLocationReport(ref allSteps, options.Time ?? 0F, options.PathToSave);
                        break;
                    }
                case 2:
                    {
                        Drawing.RunAndSaveIntensityVsTimeReport(ref allSteps, options.RegistratorPosition ?? 0F, options.PathToSave,
                            testGroup.Tests[0].TestConfig);
                        break;
                    }
                case 3:
                    {
                        Drawing.RunAndSaveDensityVsTimeReport(ref allSteps, options.RegistratorPosition ?? 0F, options.PathToSave);
                        break;
                    }
                case 4:
                    {
                        Drawing.RunAndSaveProbabilityVsDensityReport(ref allSteps, options.Time ?? 0F,
                            options.RegistratorPosition ?? 0F,
                            options.PathToSave);
                        break;
                    }
                case 5:
                    {
                        Drawing.RunAndSaveDensityVsLocationReport(ref allSteps, options.Time ?? 0F, options.PathToSave);
                        Drawing.RunAndSaveProbabilityVsDensityReport(ref allSteps, options.Time ?? 0F, options.RegistratorPosition ?? 0F, options.Path);
                        break;
                    }
                case 6:
                    {
                        List<Point>[] densityVsTimeReportResults = Drawing.RunAndSaveDensityVsTimeReport(ref allSteps, options.RegistratorPosition ?? 0F, options.PathToSave);
                        List<Point> intensityVsTimeReportResults = Drawing.RunAndSaveIntensityVsTimeReport(ref allSteps,
                            options.RegistratorPosition ?? 0F, options.PathToSave, testGroup.Tests[0].TestConfig);
                        Files.DumpReport(densityVsTimeReportResults[1], densityVsTimeReportResults[0],
                            intensityVsTimeReportResults, options.RegistratorPosition ?? 0F, options.PathToSave);
                        break;
                    }
                case 7:
                    {
                        Drawing.RunAndSaveDensityVsLocationReport(ref allSteps, options.Time ?? 0F, options.PathToSave);
                        List<Point> intensityVsTimeReportResults = Drawing.RunAndSaveIntensityVsTimeReport(ref allSteps,
                            options.RegistratorPosition ?? 0F, options.PathToSave,
                            testGroup.Tests[0].TestConfig);
                        List<Point>[] densityVsTimeReportResults = Drawing.RunAndSaveDensityVsTimeReport(ref allSteps, options.RegistratorPosition ?? 0F, options.PathToSave);
                        Drawing.RunAndSaveProbabilityVsDensityReport(ref allSteps, options.Time ?? 0F,
                            options.RegistratorPosition ?? 0F,
                            options.PathToSave);
                        Files.DumpReport(densityVsTimeReportResults[1], densityVsTimeReportResults[0],
                            intensityVsTimeReportResults, options.RegistratorPosition ?? 0F, options.PathToSave);
                        break;
                    }
                case 8:
                    {
                        Drawing.SpeedVsDensityPoints(testGroup, 0, out List<Point> expectedFuncPoints, out List<Point> realResults, out List<Point> evacTimeVsDensity);
                        Drawing.SetupSpeedVsDensityChart(out Chart speedVsDensityChart, realResults, expectedFuncPoints);
                        Drawing.SaveChart(speedVsDensityChart, options.PathToSave);
                        string pathToImage = Path.Combine(options.PathToSave, $"{speedVsDensityChart.Name}.png");
                        Files.SaveSpeedVsDensityReportToDocX(options.PathToSave, testGroup, testGroupFromXML, evacTimeVsDensity);
                        break;
                    }
                case 9:
                    {
                        Drawing.IntensityVsWidthChartPoints(testGroup, out List<Point> results);
                        List<Point> pointsBefore16 = results.Where(x => x.X <= 1.6).ToList();
                        List<Point> pointsAfter16 = results.Where(x => x.X >= 1.6).ToList();

                        const double expectedA = 3.75;
                        const double expectedB = 2.5;
                        const double expectedB2 = 8.5;

                        List<Point> expected = new List<Point> {
                            new Point(pointsBefore16.Last().X, expectedA *pointsBefore16.Last().X + expectedB),
                            new Point(pointsBefore16.First().X, expectedA *pointsBefore16.First().X + expectedB ),
                            new Point(pointsAfter16.Last().X, expectedB2),
                            new Point(pointsAfter16.First().X, expectedB2)
                        };
                        Drawing.SetupIntensityVsWidthChart(out Chart intensityVsWidthChart, results, expected);
                        Drawing.SaveChart(intensityVsWidthChart, options.PathToSave);
                        break;
                    }
            }
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine(
                @"  Available options to run reports:
    1 - DensityVsLocation
    2 - IntensityVsTime
    3 - DensityVsTime
    4 - ProbabilityVsDensity
    5 - DensityVsTime + IntensityVsTime
    6 - DensityVsLocation + ProbabilityVsDensity
    7 - All
    8 - SpeedVsDensity
    9 - IntensityVsWidth");
            Console.ReadKey();
        }
    }
}