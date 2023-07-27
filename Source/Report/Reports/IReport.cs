using System.Collections.Generic;
using System.Windows;

namespace Report.Reports
{
    public interface IReport
    {
        List<Point> GetDataset();
    }
}