using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Charts;
using Report;
using Report.DataStructures;
using Report.Reports;

namespace FenixReporter
{
    public partial class FenixReporterForm : Form
    {
        public FenixReporterForm()
        {
            InitializeComponent();
        }

        private SortedDictionary<double, HashSet<Step>> _allSteps = new SortedDictionary<double, HashSet<Step>>();
        private readonly Dictionary<int, List<Point>> _intensityVsTimeData = new Dictionary<int, List<Point>>();
        private readonly Dictionary<int, List<Point>> _densityVsTimeForPositiveXData = new Dictionary<int, List<Point>>();
        private readonly Dictionary<int, List<Point>> _densityVsTimeForNegativeXData = new Dictionary<int, List<Point>>();
        private List<Point> evacTimeVsDensity = new List<Point>();

        private string _testResultsFolder;
        private Report.DataStructures.TestGroup _testGroup;

        private TestGroup _testGroupFromXML;
        private const int TestRoomLength = 25;


        private void ResetValues()
        {
            _allSteps.Clear();
            TimeReportsTabProgressBar.Value = 0;
            _intensityVsTimeData.Clear();
            TimeReportsTabTextBox.Text = TimeReportsTabProgressBar.Value.ToString();
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            ResetValues();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = @"txt files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                _testGroup = Files.LoadTestInfoList(openFileDialog.FileName, out _testGroupFromXML);
                TestResultsTabComboBox.DataSource = _testGroup.Tests.Select(x => x.Name.Trim()).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"Error: {ex.Message}");
            }
            _testResultsFolder = Path.GetDirectoryName(openFileDialog.FileName);
            LabelForTestGroup.Text = _testGroup.Description;
            TestResultsTabRunButton.Visible = true;
            TestResultsTabSaveButton.Visible = true;
            TestResultsTabComboBox.Visible = true;

            SpeedVsIntensityTabRunButton.Visible = true;
            IntensityVsWidthTabRunButton.Visible = true;
            IntensityVsDensityRunButton.Visible = true;
            IntensityVsWidthTabSaveButton.Visible = true;

            OpenTestGroupFolderButton.Visible = true;
        }

        private void ProgressBar1_Scroll(object sender, EventArgs e)
        {
            TimeReportsTabTextBox.Text = (TimeReportsTabProgressBar.Value / 5.0).ToString(CultureInfo.CurrentCulture);
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TimeReportsTabProgressBar.Value = Convert.ToInt32(TimeReportsTabTextBox.Text) * 5;
            }
            catch
            {
                // ignored
            }
        }

        private void ProgressBar1_ValueChanged(object sender, EventArgs e)
        {
            UpdateCharts();
        }

        private void UpdateCharts()
        {
            try
            {
                double time = TimeReportsTabProgressBar.Value / 5.0;
                HashSet<Step> stepAtTime = _allSteps[time];
                DensityVsLocation densityVsLocation = new DensityVsLocation(ref stepAtTime, time);

                SetupCharts(ref densityVsLocationChart, SeriesChartType.Point, densityVsLocation.GetToolTipDataset(), "X", "Y",
                    new AxisLimits { XMin = -TestRoomLength, XMax = TestRoomLength, YMin = 0, YMax = 1 }, "{0:0}");

                ProbabilityVsDensity probabilityVsDensityForPositiveX = new ProbabilityVsDensity(ref stepAtTime, time,
                    (x, i) => x.Location.X > RegistratorXBar.Value);
                ProbabilityVsDensity probabilityVsDensityForNegativeX =
                    new ProbabilityVsDensity(ref stepAtTime, time, (x, i) => x.Location.X <= RegistratorXBar.Value);

                SetupProbCharts(probabilityVsDensityForNegativeXChart, probabilityVsDensityForNegativeX, new AxisLimits { XMin = 0, XMax = 1, YMin = 0, YMax = 1 }, "{0:0.00}");
                SetupProbCharts(probabilityVsDensityForPositiveXChart, probabilityVsDensityForPositiveX, new AxisLimits { XMin = 0, XMax = 1, YMin = 0, YMax = 1 }, "{0:0.00}");
            }
            catch (Exception)
            {
                // ignored
            }
        }


        private void UpdateReportData()
        {
            _intensityVsTimeData[RegistratorXBar.Value] = new List<Point>();
            IntensityVsTime intensityVsTimeResult = new IntensityVsTime(ref _allSteps, RegistratorXBar.Value, _testGroup.Tests.First(x => x.Name.Trim() == TestResultsTabComboBox.SelectedValue.ToString()).TestConfig);
            _intensityVsTimeData[RegistratorXBar.Value] = intensityVsTimeResult.GetDataset();
            int rValue = RegistratorXBar.Value;
            DensityVsTime densityVsTimeForPositiveXResult =
                new DensityVsTime(ref _allSteps, (x, i) => x.Location.X > rValue);
            DensityVsTime densityVsTimeForNegativeXResult =
                new DensityVsTime(ref _allSteps, (x, i) => x.Location.X < rValue);

            _densityVsTimeForPositiveXData[RegistratorXBar.Value] = densityVsTimeForPositiveXResult.GetDataset();
            _densityVsTimeForNegativeXData[RegistratorXBar.Value] = densityVsTimeForNegativeXResult.GetDataset();
        }

        private void UpdateReportChart()
        {
            if (!_intensityVsTimeData.TryGetValue(RegistratorXBar.Value, out List<Point> _))
            {
                UpdateReportData();
            }

            try
            {
                SetupReportCharts(ref densityVsTimeForNegativeXChart, SeriesChartType.Point,
                    _densityVsTimeForNegativeXData[RegistratorXBar.Value],
                    new AxisLimits { XMin = 0, XMax = TimeReportsTabProgressBar.Maximum, YMin = 0, YMax = 1 });
                SetupReportCharts(ref densityVsTimeForPositiveXChart, SeriesChartType.Point,
                    _densityVsTimeForPositiveXData[RegistratorXBar.Value],
                    new AxisLimits { XMin = 0, XMax = TimeReportsTabProgressBar.Maximum, YMin = 0, YMax = 1 });
                double evacuationTime = Extentions.GetEvacuationTime(ref _allSteps, RegistratorXBar.Value);
                SetupIntensityCharts(
                    ref intensityVsTimeChart, 
                    SeriesChartType.Point,
                    _intensityVsTimeData[RegistratorXBar.Value], 
                    new AxisLimits { XMin = 0, XMax = evacuationTime, YMin = 0, YMax = _intensityVsTimeData[RegistratorXBar.Value].Select(x =>x.Y).Max() +2 });
            }
            catch
            {
                // ignored
            }
        }

        private static void SetupCharts<T>(ref Chart targetChart, SeriesChartType chartType, IEnumerable<T> dataSource,
            string xValueField, string yValueField, AxisLimits axisLimit, string labelFormat)
        {
            targetChart.ChartAreas.First().AxisX.Minimum = axisLimit.XMin;
            targetChart.ChartAreas.First().AxisX.Maximum = axisLimit.XMax;
            targetChart.ChartAreas.First().AxisY.Minimum = axisLimit.YMin;
            targetChart.ChartAreas.First().AxisY.Maximum = axisLimit.YMax;
            targetChart.ChartAreas.First().AxisX.LabelStyle.Format = labelFormat;
            targetChart.ChartAreas.First().AxisY.LabelStyle.Format = "{0:0.00}";
            targetChart.Series.First().XValueMember = xValueField;
            targetChart.Series.First().YValueMembers = yValueField;
            targetChart.Series.First().Points.DataBind(dataSource, xValueField, yValueField, "ToolTip=ToolTip");
            targetChart.Series.First().IsValueShownAsLabel = false;
            targetChart.Series.First().ChartType = chartType;
        }

        private static void SetupProbCharts(Chart targetChart, ProbabilityVsDensity dataSource, AxisLimits axisLimit, string labelFormat)
        {
            targetChart.DataSource = dataSource.GetDataset();
            targetChart.ChartAreas.First().AxisX.Minimum = axisLimit.XMin;
            targetChart.ChartAreas.First().AxisX.Maximum = axisLimit.XMax;
            targetChart.ChartAreas.First().AxisY.Minimum = axisLimit.YMin;
            targetChart.ChartAreas.First().AxisY.Maximum = axisLimit.YMax;
            targetChart.ChartAreas.First().AxisX.LabelStyle.Format = labelFormat;
            targetChart.ChartAreas.First().AxisY.LabelStyle.Format = "{0:0.0}";
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Column;

            targetChart.Series.ElementAt(1).ChartType = SeriesChartType.Line;
            targetChart.Series.ElementAt(1).Points
                .DataBind(
                    new List<dynamic>
                    {
                        new {x = dataSource.Mean, y = 0, label = $"Mean: {dataSource.Mean:0.###}"},
                        new {x = dataSource.Mean, y = 1, label = ""}
                    }, "x", "y", "Label=label");
            targetChart.Series.ElementAt(1).Points.First().IsValueShownAsLabel = true;

            targetChart.Series.ElementAt(2).ChartType = SeriesChartType.Line;
            targetChart.Series.ElementAt(2).Points
                .DataBind(
                    new List<dynamic>
                    {
                        new {x = dataSource.EV, y = 0, label = $"EV: {dataSource.EV:0.###} (SD = {dataSource.SD:0.###})"},
                        new {x = dataSource.EV, y = 1, label = ""}
                    }, "x", "y", "Label=label");
            targetChart.Series.ElementAt(2).Points.First().IsValueShownAsLabel = true;

            targetChart.Visible = true;
        }

        private static void SetupReportCharts(ref Chart targetChart, SeriesChartType type, IEnumerable<Point> dataSource,
            AxisLimits axisLimit)
        {
            targetChart.ChartAreas.First().AxisX.Interval = CalculateChartInterval(dataSource.Select(x => x.X).Max());
            targetChart.DataSource = dataSource.Skip(1);
            targetChart.ChartAreas.First().AxisX.Minimum = axisLimit.XMin;
            targetChart.ChartAreas.First().AxisY.Minimum = axisLimit.YMin;
            targetChart.ChartAreas.First().AxisY.Maximum = axisLimit.YMax;
            targetChart.ChartAreas.First().AxisY.LabelStyle.Format = "{0:0.00}";
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = type;
            targetChart.Series.First().MarkerSize = 5;
            targetChart.Series.First().YValuesPerPoint = 5;
            targetChart.Visible = true;
        }
        private static void SetupIntensityCharts(ref Chart targetChart, SeriesChartType type, IEnumerable<Point> dataSource, AxisLimits axisLimit)
        {
            targetChart.ChartAreas.First().AxisX.Minimum = axisLimit.XMin;
            targetChart.ChartAreas.First().AxisX.Maximum = axisLimit.XMax;
            targetChart.ChartAreas.First().AxisY.Minimum = axisLimit.YMin;
            targetChart.ChartAreas.First().AxisY.Maximum = axisLimit.YMax;
            targetChart.ChartAreas.First().AxisY.LabelStyle.Format = "{0:0.00}";
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = type;
            targetChart.Series.First().Points.DataBind(dataSource, "X", "Y", "");
            targetChart.Series.First().MarkerSize = 5;
            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
        }

        private void RegistratorXBar_Scroll(object sender, EventArgs e)
        {
            RegistratorBox.Text = RegistratorXBar.Value.ToString();
        }

        private void RegistratorBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TimeReportsTabProgressBar.Value = Convert.ToInt32(RegistratorBox.Text);
            }
            catch
            {
                // ignored
            }
        }

        private void RegistratorXBar_ValueChanged(object sender, EventArgs e)
        {
            if (ReportsTab.SelectedIndex == 0)
                UpdateCharts();
            else
                UpdateReportChart();
        }

        private static void SetupSpeedVsDensityChart<T>(ref Chart targetChart, IEnumerable<T> dataSource, IEnumerable<T> dataSource2)
        {
            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.First().MarkerSize = 2;
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Line;
            targetChart.Series.First().BorderWidth = 2;
            targetChart.Series.First().Points.DataBind(dataSource, "X", "Y", "");
            targetChart.Series.First().Color = System.Drawing.Color.Green;

            targetChart.Series.Last().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.Last().MarkerSize = 6;
            targetChart.Series.Last().XValueMember = "X";
            targetChart.Series.Last().YValueMembers = "Y";
            targetChart.Series.Last().ChartType = SeriesChartType.Point;
            targetChart.Series.Last().Points.DataBind(dataSource2, "X", "Y", "");

            targetChart.Series.Last().Color = System.Drawing.Color.Blue;
        }
        private static double CalculateChartInterval(double time)
        {
            if (time > 500)
                return 100;
            if (time > 250)
                return 50;
            if (time > 150)
                return 20;
            if (time > 50)
                return 10;
            if (time > 30)
                return 5;
            return time > 15 ? 2 : 1;
        }
        private static void SetupNVsTimeChart<T>(ref Chart targetChart, IEnumerable<Point> dataSource2, IEnumerable<T> dataSource3)
        {
            targetChart.Series.Last().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.Last().MarkerSize = 5;
            targetChart.Series.Last().XValueMember = "X";
            targetChart.Series.Last().YValueMembers = "Y";
            targetChart.Series.Last().ChartType = SeriesChartType.Point;
            targetChart.Series.Last().Points.DataBind(dataSource2, "X", "Y", "");

            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.First().MarkerSize = 5;
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Line;
            targetChart.Series.First().BorderWidth = 3;
            targetChart.Series.First().Points.DataBind(dataSource3, "X", "Y", "");
            targetChart.Series.First().Color = System.Drawing.Color.OrangeRed;
        }

        private static void SetupIntensityVsWidthChart<T>(ref Chart targetChart, IEnumerable<T> dataSource, IEnumerable<T> dataSource3)
        {
            targetChart.Series.Last().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.Last().XValueMember = "X";
            targetChart.Series.Last().YValueMembers = "Y";
            targetChart.Series.Last().ChartType = SeriesChartType.Point;
            targetChart.Series.Last().MarkerSize = 5;
            targetChart.Series.Last().Points.DataBind(dataSource, "X", "Y", "");
            targetChart.Series.Last().Color = System.Drawing.Color.Blue;

            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Line;
            targetChart.Series.First().BorderWidth = 2;
            targetChart.Series.First().MarkerSize = 2;
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().Points.DataBind(dataSource3, "X", "Y", "");
            targetChart.Series.First().Color = System.Drawing.Color.Green;
        }

        private static void SetupIntensityVsDensityChart<T>(ref Chart targetChart, IEnumerable<T> dataSource, IEnumerable<T> dataSource2, IEnumerable<T> dataSource3)
        {
            targetChart.Series.Last().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.Last().XValueMember = "X";
            targetChart.Series.Last().YValueMembers = "Y";
            targetChart.Series.Last().ChartType = SeriesChartType.Point;
            targetChart.Series.Last().MarkerSize = 5;
            targetChart.Series.Last().Points.DataBind(dataSource, "X", "Y", "");
            targetChart.Series.Last().Color = System.Drawing.Color.Blue;

            targetChart.Series.First().MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.First().YValueMembers = "Y";
            targetChart.Series.First().ChartType = SeriesChartType.Line;
            targetChart.Series.First().BorderWidth = 2;
            targetChart.Series.First().MarkerSize = 2;
            targetChart.Series.First().XValueMember = "X";
            targetChart.Series.First().Points.DataBind(dataSource2, "X", "Y", "");
            targetChart.Series.First().Color = System.Drawing.Color.Green;


            targetChart.Series.ElementAt(1).MarkerStyle = MarkerStyle.Circle;
            targetChart.Series.ElementAt(1).YValueMembers = "Y";
            targetChart.Series.ElementAt(1).ChartType = SeriesChartType.Line;
            targetChart.Series.ElementAt(1).BorderWidth = 2;
            targetChart.Series.ElementAt(1).MarkerSize = 2;
            targetChart.Series.ElementAt(1).XValueMember = "X";
            targetChart.Series.ElementAt(1).Points.DataBind(dataSource3, "X", "Y", "");
            targetChart.Series.ElementAt(1).Color = System.Drawing.Color.Green;
        }


        private void NTimeRunButton_Click(object sender, EventArgs e)
        {
            _allSteps.Clear();
            GC.Collect();
            string testPath = _testGroup.Tests.Where(x => x.Name.Trim().Equals(TestResultsTabComboBox.SelectedValue)).Select(x => x.Path.Trim()).FirstOrDefault();
            if (testPath is null) return;
            {
                _allSteps = Files.Load(testPath);
                TimeReportsTabProgressBar.Maximum = _allSteps.Count;
                if (_allSteps.Count == 0)
                    throw new Exception($"No data found in {testPath} folder!");

                SortedDictionary<double,double> amountOfEvacuatedPeopleOnTime = Drawing.NVsTimeChartPoints(ref _allSteps);
                SetupNVsTimeChart(ref NVsTimeChart, amountOfEvacuatedPeopleOnTime.Select(x => new Point(x.Key, x.Value)), new List<Point>());

                TimeReportsTabProgressBar.Maximum = _allSteps.Count;
                UpdateReportChart();

                UpdateCharts();

                TimeReportsTabProgressBar.Visible = true;
                TimeReportsTabTextBox.Visible = true;
                tabControl2.SelectTab(1);
            }
        }

        private void IntensityVsWidthTabRunButton_Click(object sender, EventArgs e)
        {
            _allSteps.Clear();
            Stopwatch stopwatch = Stopwatch.StartNew();
            Drawing.IntensityVsWidthChartPoints(_testGroup, out List<Point> realResults);
            stopwatch.Stop();

            List<Point> dataBefore16 = realResults.Where(x => x.X <= 1.6).ToList();
            List<Point> dataAfter16 = realResults.Where(x => x.X >= 1.6).ToList();

            const double expectedA = 3.75;
            const double expectedB = 2.5;
            const double expectedB2 = 8.5;
            List<Point> expectedResults = new List<Point> {
                new Point(dataBefore16.LastOrDefault().X, expectedA * dataBefore16.LastOrDefault().X + expectedB),
                new Point(dataBefore16.FirstOrDefault().X, expectedA * dataBefore16.FirstOrDefault().X + expectedB ),
                new Point(dataAfter16.FirstOrDefault().X, expectedB2),
                new Point(dataAfter16.LastOrDefault().X, expectedB2)
            };

            SetupIntensityVsWidthChart(ref IntensityVsWidthChart, realResults, expectedResults);

            double elapsedminutes = stopwatch.ElapsedMilliseconds / (double)(1000 * 60);
            string elapsedText = elapsedminutes < 1 ? $"{Math.Round(elapsedminutes * 60, 3)} seconds" : $"{Math.Round(elapsedminutes, 3)} minutes";
            MessageBox.Show($@"Completed on {elapsedText}");
        }

        private void SpeedVsIntensityTabRunButton_Click(object sender, EventArgs e)
        {
            _allSteps.Clear();
            Drawing.SpeedVsDensityPoints(_testGroup, RegistratorXBar.Value, out List<Point> expectedFuncData, out List<Point> realResults, out evacTimeVsDensity);
            SetupSpeedVsDensityChart(ref SpeedVsDensityChart, expectedFuncData, realResults);
            
            SpeedVsDensitySaveButton.Visible = true;
            SpeedVsDensitySaveReportButton.Visible = true;
        }

        private void SpeedVsIntensityTabSaveButton_Click(object sender, EventArgs e)
        {
            SaveChart(SpeedVsDensityChart, Path.Combine(_testResultsFolder, $"{SpeedVsDensityChart.Text}.png"));
            Files.SaveSpeedVsDensityReportToDocX(Path.Combine(_testResultsFolder, $"{SpeedVsDensityChart.Text}.png"), _testGroup, _testGroupFromXML, evacTimeVsDensity);
        }

        private void IntensityVsWidthTabSaveButton_Click(object sender, EventArgs e)
        {
            string directoryName = _testResultsFolder;
            SaveChart(IntensityVsWidthChart, Path.Combine(directoryName, $"{IntensityVsWidthChart.Text}.png"));
        }

        private void NTimeSaveButton_Click(object sender, EventArgs e)
        {
            string directoryName = _testResultsFolder;

            SaveChart(NVsTimeChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{NVsTimeChart.Text}.png"));

            SaveChart(intensityVsTimeChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{intensityVsTimeChart.Text}.png"));
            SaveChart(densityVsTimeForNegativeXChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{densityVsTimeForNegativeXChart.Text}.png"));
            SaveChart(densityVsTimeForPositiveXChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{densityVsTimeForPositiveXChart.Text}.png"));

            SaveChart(densityVsLocationChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{densityVsLocationChart.Text}_{Convert.ToDouble(TimeReportsTabTextBox.Text) * 1000}.png"));
            SaveChart(probabilityVsDensityForNegativeXChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{probabilityVsDensityForNegativeXChart.Text}_{Convert.ToDouble(TimeReportsTabTextBox.Text) * 1000}.png"));
            SaveChart(probabilityVsDensityForPositiveXChart, Path.Combine(directoryName, $"{TestResultsTabComboBox.SelectedValue}_{probabilityVsDensityForPositiveXChart.Text}_{Convert.ToDouble(TimeReportsTabTextBox.Text) * 1000}.png"));
        }

        private static void SaveChart(Chart chart, string path)
        {
                Chart chartCopy = new Chart();
                MemoryStream myStream = new MemoryStream();
                chart.Serializer.Save(myStream);
                chartCopy.Serializer.Load(myStream);
                myStream.Close();
                chartCopy.Size = new System.Drawing.Size { Height = 420, Width = 630 };
                chartCopy.SaveImage(path, ChartImageFormat.Png);
        }

        private void OpenTestGroupFolderButton_Click(object sender, EventArgs e)
        {
            Process.Start(_testResultsFolder);
        }

        private void TestResultsTabComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _intensityVsTimeData.Clear();
            string testDescription = _testGroup.Tests.Single(x => x.Name.Trim() == TestResultsTabComboBox.SelectedValue.ToString()).TestConfig.Description;
            LabelForTest.Text = testDescription;
        }

        private void SpeedVsDensitySaveButton_Click(object sender, EventArgs e)
        {
            SaveChart(SpeedVsDensityChart, Path.Combine(_testResultsFolder, $"{SpeedVsDensityChart.Text}.png"));
        }

        private void SaveDocXTableButton_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                var path = folderBrowserDialog.SelectedPath;
                Files.ValidationTableToDocX(path);
                MessageBox.Show("Done");
            }

        }

        private void IntensityVsDensityRunButton_Click(object sender, EventArgs e)
        {
            _allSteps.Clear();
            Stopwatch stopwatch = Stopwatch.StartNew();
            Drawing.IntensityVsDensityChartPoints(_testGroup, out List<Point> realResults, out List<Point> expectedResults, out List<Point> expectedResultsPart2);
            stopwatch.Stop();

            SetupIntensityVsDensityChart(ref intensityVsDensityChart, realResults, expectedResults, expectedResultsPart2);

            double elapsedminutes = stopwatch.ElapsedMilliseconds / (double)(1000 * 60);
            string elapsedText = elapsedminutes < 1 ? $"{Math.Round(elapsedminutes * 60, 3)} seconds" : $"{Math.Round(elapsedminutes, 3)} minutes";
            MessageBox.Show($@"Completed on {elapsedText}");
            IntensityVsDensitySaveButton.Visible = true;
        }

        private void IntensityVsDensitySaveButton_Click(object sender, EventArgs e)
        {
            SaveChart(intensityVsDensityChart, Path.Combine(_testResultsFolder, $"{intensityVsDensityChart.Text}.png"));
        }
    }
}