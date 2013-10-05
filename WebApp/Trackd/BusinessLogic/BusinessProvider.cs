using Data;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class BusinessProvider
    {
        public DataProvider _dataProvider { get; set; }

        public BusinessProvider()
        {
            _dataProvider = new DataProvider();
        }

        public IList<Metric> GetAllMetrics()
        {
            return _dataProvider.GetAllMetrics();
        }

        public void AddUserMetricEntry(UserMetricEntry entry)
        {
            _dataProvider.AddUserMetricEntry(entry);
        }

        public void RemoveUserMetricEntry(UserMetricEntry entry)
        {
            _dataProvider.RemoveUserMetricEntry(entry);
        }

        public IList<UserMetricEntry> GetUserMetricEntries(int userId)
        {
            return _dataProvider.GetUserMetricEntries(userId);
        }
    }
}
