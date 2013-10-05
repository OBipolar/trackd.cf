using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class DataProvider : RepositoryBase
    {
        public void AddUserMetricEntry(UserMetricEntry entry)
        {
            string q = "insert into usermetricentry (userid, metricid, entrytimestamp, numericalvalue, textvalue) values (@userid, @metricid, @entrytimestamp, @numericalvalue, @textvalue)";
            var p = new {
                        userid = entry.UserId,
                        metricid = entry.Metric.Id,
                        entrytimestamp = entry.EntryTimestamp,
                        numericalvalue = entry.NumericalValue,
                        textValue = entry.TextValue
                    };
            ExecuteNonQuery(q, p);
        }

        public IList<UserMetricEntry> GetUserMetricEntries(int userId)
        {
            string q = "select ume.*, m.name as metricname from usermetricentry ume inner join metric m on ume.metricid = m.id where userid = @userId";
            return ExecuteSql<IList<UserMetricEntry>>(q, new { userId = userId }, delegate(IDbCommand com)
            {
                List<UserMetricEntry> list = new List<UserMetricEntry>();
                using (IDataReader r = com.ExecuteReader())
                    while (r.Read())
                        list.Add(new UserMetricEntry
                        {
                            Id = Get<int>(q, r, "id"),
                            UserId = Get<int>(q, r, "userid"),
                            Metric = new Metric
                            {
                                Id = Get<int>(q, r, "metricid"),
                                Name = Get<string>(q, r, "metricname")
                            },
                            EntryTimestamp = Get<DateTime>(q,r,"entrytimestamp"),
                            NumericalValue = Get<double>(q, r, "numericalvalue"),
                            TextValue = Get<string>(q, r, "textvalue")
                        });
                return list;
            });
        }

        public void RemoveUserMetricEntry(UserMetricEntry entry)
        {
            string q = "delete usermetricentry where id = @id";
            var p = new
            {
                id = entry.Id
            };
            ExecuteNonQuery(q, p);
        }

        public IList<Metric> GetAllMetrics()
        {
            string q = "select ume.*, m.name as metricname from usermetricentry ume inner join metric m on ume.metricid = m.id where userid = @userId";
            return ExecuteSql<IList<Metric>>(q, null, delegate(IDbCommand com)
            {
                List<Metric> list = new List<Metric>();
                using (IDataReader r = com.ExecuteReader())
                    while (r.Read())
                        list.Add(new Metric
                        {
                            Id = Get<int>(q, r, "id"),
                            Name = Get<string>(q, r, "name")
                        });
                return list;
            });
        }
    }
}
