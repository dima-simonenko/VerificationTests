using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Report.DataStructures;

namespace Report.Reports
{
    public class DensityVsLocation : IReport
    {
        private List<Point> Dataset { get; }
        private List<dynamic> ToolTip { get; }

        public double Time { get; }

        public DensityVsLocation(ref HashSet<Step> steps, double time)
        {
            Time = time;
            Dataset = new List<Point> { new Point { X = 26, Y = 0} };
            ToolTip = new List<dynamic> { new { X = 26, Y = 0, ToolTip = "" } };

            foreach(var step in steps)
            {
                Dataset.Add(new Point(step.Location.X, step.Density));
                ToolTip.Add(new
                {
                    X = step.Location.X,
                    Y = step.Density,
                    ToolTip = step.ToString()
                });

            }
        }

        public List<Point> GetDataset()
        {
            return Dataset;
        }

        public dynamic GetToolTipDataset()
        {
            return ToolTip;
        }
    }
}