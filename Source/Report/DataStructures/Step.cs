using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Report.DataStructures
{
    public class Step
    {
        public double Density { get; }
        public Point Location { get; }
        public string Name { get; }
        public bool EventOccured { get; }

        public Step(string[] array)
        {
            double[] location = array[1].Split(';').Select(x => double.Parse(x, System.Globalization.NumberStyles.Any)).ToArray();
            Location = new Point(location[0], location[1]);
            Name = array[0];
            Density = double.Parse(array[4], System.Globalization.NumberStyles.Any);
            EventOccured = array[6] != string.Empty;
        }

        public override string ToString()
        {
            return $"{Name}\nLocation: ({Location.X},{Location.Y})\nDensity: {Density}";
        }
    }

    public class PersonComparer : IEqualityComparer<Step>
    {
        public bool Equals(Step first, Step second)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(first?.Name, second?.Name);
        }

        public int GetHashCode(Step item)
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(item.Name);
        }
    }
}