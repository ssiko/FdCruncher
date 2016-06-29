using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FdCruncher
{
    public class Player
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public int Salary { get; set; }
        public string Opponent { get; set; }
        public string Venue { get; set; }
        public double Metric { get; set; }
        public string MetricName { get; set; }
        public double SeasonAvgMins { get; set; }
        public string Team { get; set; }
        public double Value { get; set; }
        public double Modifier { get; set; }
        public int PositionRank { get; set; }
        public bool Include { get; set; }
        public bool Exclude { get; set; }
        public double ModifiedMetric
        {
            get
            {
                return Modifier * Metric;
            }
        }
    }
}
