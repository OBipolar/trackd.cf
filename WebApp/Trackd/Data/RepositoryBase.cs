using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace Data
{
    public class RepositoryBase
    {
        protected string _connectionString;
        protected delegate T SqlDel<T>(IDbCommand com);

        public RepositoryBase()
        {
            _connectionString = "";// RoleEnvironment.GetConfigurationSettingValue("SqlConnectionString");
        }

        protected T ExecuteSql<T>(string query, object paramObj, SqlDel<T> work, bool storedProc = false)
        {
            DateTime startTime = DateTime.UtcNow;
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (IDbCommand com = con.CreateCommand())
                    {
                        com.CommandText = query;
                        if (storedProc) com.CommandType = CommandType.StoredProcedure;
                        AddSqlParametersFromObject(com, paramObj);
                        T ret = work(com);
                        return ret;
                    }
                }
            }
            catch (Exception e)
            {
                //LogException("DATAFAIL: " + e.Message, e.StackTrace);
                throw e;
            }
            finally
            {
                //DbLogger.Append(query, DateTime.UtcNow - startTime);
            }
        }

        private string Trim(string str, int maxlength)
        {
            if (str.Length > maxlength) str = str.Substring(0, maxlength);
            return str;
        }

        public void LogException(string message, string stacktrace)
        {
            message = Trim(message + "--" + stacktrace, 500);
            string q = @"INSERT INTO Exception (Message, ts) VALUES (@message, getutcdate())";
            ExecuteNonQuery(q, new { message = message, stacktrace = stacktrace });
        }

        private void LogCall(string queryString, TimeSpan duration)
        {
            if (queryString.Length > 1000) queryString = queryString.Substring(0, 1000);
            double callMS = duration.TotalMilliseconds;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (IDbCommand com = con.CreateCommand())
                {
                    com.CommandText = "insert into dblogger (query, callMS) values (@query, @callMS);";
                    AddSqlParameter("query", queryString, com);
                    AddSqlParameter("callMS", callMS, com);
                    com.ExecuteNonQuery();
                }
            }
        }

        protected void ExecuteNonQuery(string query, object paramObj)
        {
            ExecuteSql<int>(query, paramObj, delegate(IDbCommand com)
            {
                com.ExecuteNonQuery();
                return -1;
            });
        }

        protected void ExecuteStoredProcedure(string procedureName, object paramObj)
        {
            ExecuteSql<int>(procedureName, paramObj, delegate(IDbCommand com)
            {
                com.CommandType = CommandType.StoredProcedure;
                com.ExecuteNonQuery();
                return -1;
            });
        }

        protected int ExecuteScalarInt(string query, object paramObj)
        {
            return ExecuteSql<int>(query, paramObj, delegate(IDbCommand com)
            {
                object o = com.ExecuteScalar();
                if (o != DBNull.Value) return Convert.ToInt32(o);
                else return -1;
            });
        }

        protected long ExecuteScalarLong(string query, object paramObj)
        {
            return ExecuteSql<long>(query, paramObj, delegate(IDbCommand com)
            {
                object o = com.ExecuteScalar();
                if (o != DBNull.Value) return Convert.ToInt64(o);
                else return -1;
            });
        }

        private static IDictionary<string, IDictionary<string, int>> _ordinalCache;
        private static IDictionary<string, IDictionary<string, int>> OrdinalCache
        {
            get
            {
                if (_ordinalCache == null) _ordinalCache = new Dictionary<string, IDictionary<string, int>>();
                return _ordinalCache;
            }
        }

        public static void AddSqlParametersFromList<T>(IDbCommand com, List<T> list)
        {
            int suffix = 0;
            foreach (T lo in list)
                AddSqlParametersFromObject(com, lo, suffix++);
        }

        public static void AddSqlParametersFromObject(IDbCommand com, object o, int suffix = -1)
        {
            if (o == null) return;
            if (o.GetType().Name == "List`1")
            {
                return;
            }
            foreach (PropertyInfo propertyInfo in o.GetType().GetProperties())
            {
                if (propertyInfo.CanRead)
                {
                    //set primitives and basic types
                    if (propertyInfo.PropertyType.IsPrimitive
                        || propertyInfo.PropertyType == typeof(Decimal) || propertyInfo.PropertyType == typeof(String)
                        || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(object))
                    {
                        object value = propertyInfo.GetValue(o, null);
                        if (!(propertyInfo.PropertyType == typeof(DateTime) && (DateTime)value == default(DateTime)))
                        {
                            string paramName = "@" + propertyInfo.Name;
                            if (suffix != -1) paramName += suffix;
                            AddSqlParameter(paramName, value, com);
                        }
                    }
                    else if (propertyInfo.PropertyType.IsEnum)
                    {
                        object enumVal = propertyInfo.GetValue(o, null);
                        int enumIntVal = (int)enumVal;
                        //System.Enum.GetValues(propertyInfo.GetValue(o, null).GetType())
                        string paramName = "@" + propertyInfo.Name;
                        if (suffix != -1) paramName += suffix;
                        AddSqlParameter(paramName, enumIntVal, com);
                    }
                    //set child objects that have an id
                    else
                    {
                        foreach (PropertyInfo childPropertyInfo in propertyInfo.PropertyType.GetProperties())
                        {
                            if (childPropertyInfo.Name == "Id" && childPropertyInfo.PropertyType == typeof(int))
                            {
                                object idProperty = propertyInfo.GetValue(o, null);
                                object paramValue = null;
                                if (idProperty != null)
                                {
                                    paramValue = childPropertyInfo.GetValue(idProperty, null);
                                    if ((int)paramValue == 0) paramValue = null;
                                }
                                string paramName = "@" + propertyInfo.Name + "id";
                                if (suffix != -1) paramName += suffix;
                                AddSqlParameter(paramName, paramValue, com);
                            }
                            else if (childPropertyInfo.Name == "UniqueId" && childPropertyInfo.PropertyType == typeof(long))
                            {
                                object idProperty = propertyInfo.GetValue(o, null);
                                object paramValue = null;
                                if (idProperty != null)
                                {
                                    paramValue = childPropertyInfo.GetValue(idProperty, null);
                                    if ((long)paramValue == 0) paramValue = null;
                                }
                                string paramName = "@" + propertyInfo.Name + "uniqueid";
                                if (suffix != -1) paramName += suffix;
                                AddSqlParameter(paramName, paramValue, com);
                            }
                        }
                    }
                }
            }
        }

        public static void AddSqlParameters(IDbCommand com, string[] paramNames, object[] values)
        {
            if (paramNames.Length != values.Length) throw new Exception("Param count does not match value count");
            for (int i = 0; i < paramNames.Length; i++)
                AddSqlParameter(paramNames[i], values[i], com);
        }

        public static void AddSqlParameter(string paramName, object value, IDbCommand com)
        {
            if (value == null) com.Parameters.Add(new SqlParameter(paramName, DBNull.Value));
            else com.Parameters.Add(new SqlParameter(paramName, value));
        }

        public static T Get<T>(string query, IDataReader r, string column, T nullValue = default(T))
        {
            IDictionary<string, int> thisQueryCache;
            if (!OrdinalCache.TryGetValue(query, out thisQueryCache))
            {
                thisQueryCache = new Dictionary<string, int>();
                OrdinalCache[query] = thisQueryCache;
            }
            int ordinal;
            if (!thisQueryCache.TryGetValue(column, out ordinal))
            {
                ordinal = r.GetOrdinal(column);
                thisQueryCache[column] = ordinal;
            }
            object o = r.GetValue(ordinal);
            if (o != DBNull.Value && o.GetType() == typeof(DateTime))
            {
                o = DateTime.SpecifyKind((DateTime)o, DateTimeKind.Utc);
            }
            return o != DBNull.Value ? (T)o : nullValue;
        }

        public static string CreateParamList(string query, string paramName, int count)
        {
            string list = "";
            for (int i = 0; i < count; i++)
            {
                list += string.Format("@{0}{1}", paramName, i);
                if (i != count - 1) list += ",";
            }
            return query.Replace("@@" + paramName, list);
        }

        public static void FillParamList<T>(IDbCommand com, string paramName, IList<T> ids)
        {
            for (int i = 0; i < ids.Count; i++)
                com.Parameters.Add(new SqlParameter(string.Format("@{0}{1}", paramName, i), ids[i]));
        }

        public string BuildUpdateSQL(object model, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
            {
                if (propertyInfo.CanRead && propertyInfo.Name.ToLower() != "id")
                {
                    string paramName = "";
                    //set primitives and basic types
                    if (propertyInfo.PropertyType.IsPrimitive
                        || propertyInfo.PropertyType == typeof(Decimal) || propertyInfo.PropertyType == typeof(String)
                        || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(object))
                        paramName = propertyInfo.Name;

                    if (paramName != "")
                        sb.Append("[" + paramName + "]" + " = @" + paramName + ",");
                }
            }
            string prams = sb.ToString();
            prams = prams.Substring(0, prams.Length - 1);
            return string.Format("UPDATE {0} SET {1} WHERE id = @id;", tableName, prams);
        }

        public string BuildInsertSQL(object model, string tableName)
        {
            return string.Format("INSERT INTO {0} ({1}) VALUES ({2}); SELECT SCOPE_IDENTITY();", tableName, BuildParamList(model), BuildParamList(model, "@"));
        }

        public string BuildParamList(object model, string prefix = "")
        {
            StringBuilder sb = new StringBuilder();
            foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
            {
                if (propertyInfo.CanRead && propertyInfo.Name.ToLower() != "id")
                {
                    string paramName = "";
                    //set primitives and basic types
                    if (propertyInfo.PropertyType.IsPrimitive
                        || propertyInfo.PropertyType == typeof(Decimal) || propertyInfo.PropertyType == typeof(String)
                        || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(object))
                        paramName = propertyInfo.Name;
                    //set child objects that have an id
                    else if (!propertyInfo.PropertyType.IsEnum)
                    {
                        foreach (PropertyInfo childPropertyInfo in propertyInfo.PropertyType.GetProperties())
                        {
                            if (childPropertyInfo.Name == "Id" && childPropertyInfo.PropertyType == typeof(int))
                            {
                                object idProperty = propertyInfo.GetValue(model, null);
                                object paramValue = null;
                                if (idProperty != null)
                                {
                                    paramValue = childPropertyInfo.GetValue(idProperty, null);
                                    if ((int)paramValue == 0) paramValue = null;
                                }
                                paramName = propertyInfo.Name + "id";
                            }
                        }
                    }
                    if (paramName != "" && prefix == "") paramName = "[" + paramName + "]";
                    if (paramName != "") sb.Append(prefix + paramName + ",");
                }
                
            }
            string prams = sb.ToString();
            prams = prams.Substring(0, prams.Length - 1);
            return prams;
        }

        public T FillModel<T>(string q, object prams)
        {
            return ExecuteSql<T>(q, prams, delegate(IDbCommand com)
            {
                T model = (T)Activator.CreateInstance(typeof(T));
                using (IDataReader r = com.ExecuteReader())
                    if (r.Read())
                    {
                        foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
                        {
                            if (propertyInfo.CanRead)
                            {
                                string paramName = "";
                                //set primitives and basic types
                                if (propertyInfo.PropertyType.IsPrimitive
                                    || propertyInfo.PropertyType == typeof(Decimal) || propertyInfo.PropertyType == typeof(String)
                                    || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(object))
                                {
                                    paramName = propertyInfo.Name;
                                    propertyInfo.SetValue(model, Get<object>(q, r, paramName), null);
                                }
                            }
                        }
                    }
                return model;
            });
        }

        public IList<T> FillModelList<T>(string q, object prams)
        {
            return ExecuteSql<IList<T>>(q, prams, delegate(IDbCommand com)
            {
                IList<T> list = new List<T>();
                using (IDataReader r = com.ExecuteReader())
                    while (r.Read())
                    {
                        T model = (T)Activator.CreateInstance(typeof(T));
                        foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
                        {
                            if (propertyInfo.CanRead)
                            {
                                string paramName = "";
                                //set primitives and basic types
                                if (propertyInfo.PropertyType.IsPrimitive
                                    || propertyInfo.PropertyType == typeof(Decimal) || propertyInfo.PropertyType == typeof(String)
                                    || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(object))
                                {
                                    paramName = propertyInfo.Name;
                                    propertyInfo.SetValue(model, Get<object>(q, r, paramName), null);
                                }
                            }
                        }
                        list.Add(model);
                    }
                return list;
            });
        }

        public int InsertModel<T>(T model)
        {
            if (model == null) return -1;
            string tableName = typeof(T).Name;
            return ExecuteScalarInt(BuildInsertSQL(model, tableName), model);
        }

        public void UpdateModel<T>(T model)
        {
            if (model == null) return;
            string tableName = typeof(T).Name;
            ExecuteNonQuery(BuildUpdateSQL(model, tableName), model);
        }

        protected void DeleteModel<T>(int id)
        {
            string tableName = typeof(T).Name;
            ExecuteNonQuery(string.Format("DELETE {0} WHERE id = @id", tableName), new { id = id });
        }
    }

}
