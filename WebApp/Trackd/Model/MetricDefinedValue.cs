using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class MetricDefinedValue
    {
        public int Id { get; set; }
        public Metric Metric { get; set; }
        public double NumericalId { get; set; }
        public string TextValue { get; set; }
    }
}
