using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public class Query
    {
        private string _queryString;
        public string queryString { 
            get
            {
                if (Type == QueryType.Execute)
                {
                    if (command != null)
                    {
                        return command.CommandText;
                    }
                }

                return _queryString;
            }

            private set
            {
                _queryString = value;
            }
        }
        //private Thread queryThread;
        //public QueryState State = QueryState.Idle;
        public QueryType Type = QueryType.Execute;
        private SQLiteCommand command;
        //private Database database;
        public enum QueryType
        {
            Execute = 0,
            Select = 1,
            Scalar
        }

        //public enum QueryState
        //{
        //    Error = -1,
        //    Idle = 0,
        //    Running = 1,
        //    Finished = 2
        //}

        public SQLiteCommand GetCommand(SQLiteConnection connection = null)
        {
            if (command == null)
            {
                command = new SQLiteCommand(queryString);
            }

            if (connection != null)
            {
                command.Connection = connection;
            }

            return command;
        }

        public QueryType GuessQueryType()
        {
            bool isSelectType = queryString.Substring(0, 6).ToUpper() == "SELECT";


            if (isSelectType)
            {
                return QueryType.Select;
            }




            return QueryType.Execute;
        }

        public Query(SQLiteCommand c)
        {
            this.command = c;
            this.Type = QueryType.Execute;
        }

        public Query(string query)
        {
            this.queryString = query;

            this.Type = GuessQueryType();

        }

        //private void DoQueryJob()
        //{
        //    QueryResult result = new QueryResult();
        //    try
        //    {
        //        State = QueryState.Finished;
        //    }
        //    catch (Exception ex)
        //    {
        //        State = QueryState.Error;
        //        result.exception = ex;
        //    }

        //}

            //public ThreadState GetQueryThreadState()
            //{
            //    return queryThread.ThreadState;
            //}

            //public void RunInBackground(Database database)
            //{
            //    this.database = database;

            //    if (queryThread == null)
            //    {
            //        queryThread = new Thread(DoQueryJob);
            //    }

            //    ThreadState currentThreadState = GetQueryThreadState();
            //    if (currentThreadState == ThreadState.Stopped || currentThreadState == ThreadState.Unstarted)
            //    {
            //        queryThread.IsBackground = true;
            //        queryThread.Name = "Query: " + query;
            //        queryThread.Start();
            //    }
            //    else
            //    {

            //    }
            //}

        public override string ToString()
        {
            return queryString;
        }
    }
}
