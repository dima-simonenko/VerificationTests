using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace Report.DataStructures
{
    public class AxisLimits
    {
        public double XMin { get; set; }

        public double XMax { get; set; }

        public double YMin { get; set; }

        public double YMax { get; set; }
    }

    public class ChartConfig
    {
        public string XTitle { get; set; }

        public string YTitle { get; set; }

        public string Name { get; set; }

        public SeriesChartType Type { get; set; }

        public Size Size { get; set; }

        public int? PointSize { get; set; }

        public Color? Color { get; set; }
    }
}