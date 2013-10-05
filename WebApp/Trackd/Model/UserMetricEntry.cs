using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class UserMetricEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Metric Metric { get; set; }
        public DateTime EntryTimestamp { get; set; }
        public double NumericalValue { get; set; }
        public string TextValue { get; set; }
    }
}
