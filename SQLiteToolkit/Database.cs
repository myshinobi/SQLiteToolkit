using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;

namespace SQLiteToolkit
{
    public class Database
    {
        public SQLiteConnection myConnection;
        public string DatabaseFilePath;
        public Database()
        {

        }

        public List<QueryJob> QueryJobs = new List<QueryJob>();

        public void OpenConnection()
        {
            if (myConnection.State != System.Data.ConnectionState.Open)
            {
                myConnection.Open();
            }
        }

        public void CloseConnection()
        {
            if (myConnection.State != System.Data.ConnectionState.Closed)
            {
                myConnection.Close();
            }
        }

        public Query NewQuery(SQLiteCommand command)
        {
            return new Query(command);
        }

        public Query NewQuery(string query)
        {
            return new Query(query);
        }

        public void RunQuery(Query query, Action<QueryJob> onFinished)
        {
            if (!QueryJobs.Exists(x => x.query == query))
            {
                QueryJob queryJob = new QueryJob(this, query);
                QueryJobs.Add(queryJob);
                queryJob.RunThreadInBackground(onFinished);
            }
        }

        public SQLiteCommand NewCommand(string query)
        {
            SQLiteCommand command = new SQLiteCommand(query, myConnection);

            return command;
        }

        

        public bool LoadDatabase(string databaseFile)
        {
            try
            {
                DatabaseFilePath = databaseFile;


                if (!File.Exists(DatabaseFilePath))
                {
                    SQLiteConnection.CreateFile(DatabaseFilePath);

                }
                if (File.Exists(DatabaseFilePath))
                {
                    myConnection = new SQLiteConnection("Data Source=" + DatabaseFilePath);
                    OpenConnection();
                    Properties.Settings.Default.LastDatabaseFilePath = databaseFile;
                    Properties.Settings.Default.Save();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                DoOnDatabaseMessage(new DatabaseMessageEventArgs(ex));
                return false;
            }
        }



        public delegate void DatabaseMessageHandler(DatabaseMessageEventArgs e);
        public event DatabaseMessageHandler OnDatabaseMessage;



        public void DoOnDatabaseMessage(DatabaseMessageEventArgs e)
        {
            if (e.databaseMessage.queryJob != null)
            {
                QueryJobs.Remove(e.databaseMessage.queryJob);
            }
            OnDatabaseMessage?.Invoke(e);
        }

    }
}
