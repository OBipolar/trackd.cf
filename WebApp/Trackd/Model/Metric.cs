using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class Metric
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public MetricType MetricType { get; set; }
    }
}
