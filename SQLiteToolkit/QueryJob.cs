using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public class QueryJob
    {
        public Query query;
        public Thread thread;
        public QueryJobState State;
        public QueryResult result;
        private Database database;
        //private int resultId;
        private Action<QueryJob> onFinished;
        public enum QueryJobState
        {
            Error = -1,
            Idle = 0,
            Running = 1,
            Finished = 2
        }

        public QueryJob(Database database, Query query)
        {
            this.database = database;
            this.query = query;
            State = QueryJobState.Idle;
        }

        //public void WaitForResult()
        //{
        //    while (State == QueryJobState.Running)
        //    {
        //        System.Threading.Thread.Sleep(100);
        //    }
        //}

        public void RunThreadInBackground(Action<QueryJob> onFinished)
        {
            lock (this)
            {
                if (State != QueryJobState.Running)
                {
                    this.onFinished = onFinished;
                    //this.resultId = resultId;
                    thread = null;
                    thread = new Thread(DoQueryJob);
                    thread.Name = "Query: " + query;
                    thread.IsBackground = true;
                    State = QueryJobState.Running;
                    thread.Start();
                }
            }
        }

        private DataTable GetDataTable(string query)
        {
            if (database != null)
            {

                SQLiteDataAdapter da = new SQLiteDataAdapter(query, database.myConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "results");
                return ds.Tables[0];
            }

            return null;
        }

        public QueryJob RunNow()
        {
            DoQueryJob();
            return this;
        }

        private void DoQueryJob()
        {
            DateTime startTimeUTC = DateTime.UtcNow;
            result = new QueryResult();
            //result.Id = resultId;
            try
            {

                switch (query.Type)
                {
                    case Query.QueryType.Execute:
                        int affected = query.GetCommand(database.myConnection).ExecuteNonQuery();
                        result.rowsAffected = affected;
                        break;
                    case Query.QueryType.Select:
                        DataTable table = GetDataTable(query.queryString);
                        result.datatable = table;
                        if (table.Rows.Count > 0)
                        {
                            if (table.Columns.Count > 0)
                            {
                                result.ScalarObject = table.Rows[0][0];
                            }
                        }
                        break;
                    case Query.QueryType.Scalar:

                        object obj = query.GetCommand(database.myConnection).ExecuteScalar();
                        result.ScalarObject = obj;
                        break;
                    default:
                        break;
                }

                State = QueryJobState.Finished;
            }
            catch (Exception ex)
            {
                State = QueryJobState.Error;
                result.exception = ex;
            }
            result.timeFinishedUTC = DateTime.UtcNow;

            result.timeSpanElapsed = (result.timeFinishedUTC - startTimeUTC);
            database.DoOnDatabaseMessage(new DatabaseMessageEventArgs(this));
            if (onFinished != null)
            {
                onFinished(this);
            }
        }
    }
}
