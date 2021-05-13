using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using LibraryofAlexandria;
namespace SQLiteToolkit
{
    public class Database
    {
        public SQLiteConnection myConnection;
        public string DatabaseFilePath;
        public static string StaticTableName = "StaticVariables";
        public Database()
        {

        }

        public Database(string dbFilePath)
        {
            this.DatabaseFilePath = dbFilePath;
            if (!LoadDatabase(this.DatabaseFilePath))
            {
                throw new Exception("Failed to initiate database at location: " + dbFilePath);
            }
        }

        public List<QueryJob> QueryJobs = new List<QueryJob>();

        public enum DatabaseState
        {
            Open,
            Closed,
            Connecting
        }

        public DatabaseState State
        {
            get
            {
                if (myConnection == null)
                {
                    return DatabaseState.Closed;
                }

                if (myConnection.State == ConnectionState.Open)
                {
                    return DatabaseState.Open;
                }

                switch (myConnection.State)
                {
                    case ConnectionState.Closed:
                        return DatabaseState.Closed;

                    case ConnectionState.Open:
                        return DatabaseState.Open;

                    case ConnectionState.Connecting:
                        return DatabaseState.Connecting;

                    case ConnectionState.Executing:
                        break;

                    case ConnectionState.Fetching:
                        break;

                    case ConnectionState.Broken:
                        break;

                    default:
                        return DatabaseState.Closed;

                }


                return DatabaseState.Closed;
            }
        }

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

        public Query NewQuery(SQLiteCommand command, bool scalar = false)
        {
            Query query = new Query(command);
            if (scalar)
            {
                query.Type = Query.QueryType.Scalar;
            }
            return query;
        }

        public Query NewQuery(string query, bool scalar = false)
        {
            Query q = new Query(query);
            if (scalar)
            {
                q.Type = Query.QueryType.Scalar;
            }
            return q;
        }
        public void RunQuery(string query, Action<QueryJob> onFinished)
        {
            RunQuery(NewQuery(query), onFinished);
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

        public QueryJob RunQuery(string query)
        {
            return RunQuery(NewQuery(query));
        }

        public QueryJob RunQuery(Query query)
        {
            if (!QueryJobs.Exists(x => x.query == query))
            {
                QueryJob queryJob = new QueryJob(this, query);
                QueryJobs.Add(queryJob);
                return queryJob.RunNow();
            }

            return null;
        }

        public SQLiteCommand NewCommand(string query)
        {
            SQLiteCommand command = new SQLiteCommand(query, myConnection);

            return command;
        }

        public bool RecordExists(IIndexableRecordConverter obj)
        {
            if (!obj.IsIndexed)
                return false;

            return RecordExists(obj.TableName, new KeyValuePair<Column, object>[] { new KeyValuePair<Column, object>(obj.PrimaryKeyColumn, obj.GetId())});
        }

        public bool RecordExists(string tablename, params KeyValuePair<Column, object>[] values)
        {
            string sql = "SELECT count(1) FROM "+tablename+ " WHERE "+string.Join(" AND ", values.Select(x => x.Key.GetSQLValue(x.Value)));

            Query query = new Query(sql);
            bool exists = false;
            RunQuery(query, (job) =>
            {
                exists = ((int)job.result.ScalarObject > 0);
            });

            return exists;
        }


        public void TableCreate<T>() where T : ITableConverter
        {
            //create my table
            string tableName = Utilities.GetTableNameFromType<T>();

            IEnumerable<Column> columns = Utilities.GetColumnsFromType<T>();

            var colGroups = columns.GroupBy(x => x.tableType);
            var joinColsGroup = colGroups.Where(x => x.Key == Column.TableType.Join);
            var staticColsGroup = colGroups.Where(x => x.Key == Column.TableType.Static);
            bool hasJoinTable = false;
            bool hasStaticCols = false;
            if (joinColsGroup.Count() > 0)
            {
                hasJoinTable = true;
            }
            if (staticColsGroup.Count() > 0)
            {
                hasStaticCols = true;
            }
            var myCols = colGroups.Where(x => x.Key == Column.TableType.Instance).First().Select(x => x);
            if (!TableExists(tableName))
                TableCreate(tableName, myCols);

            Type tableType = typeof(T);
            //create join tables
            if (hasJoinTable)
            {
                var joinCols = joinColsGroup.First().Select(x => x);

                T testTable = Utilities.CreateInstance<T>();
                if (testTable.IsIndexable())
                {

                    joinCols.ForEach(joinCol =>
                    {

                        IRelationship relationship = testTable.GetValue<IRelationship>(joinCol.Name, tableType);
                        if (relationship == null)
                        {
                            relationship = (IRelationship)Utilities.CreateInstance(joinCol.type);
                        }
                        Type childTableType = relationship.GetChildType();
                        var testChildTable = (ITableConverter)Utilities.CreateInstance(childTableType);
                        if (testChildTable.IsIndexable())
                        {

                            string otherTableName = Utilities.GetTableNameFromType(childTableType);
                            string joinTableName = tableName + "_JOIN_" + joinCol.Name;

                            if (!TableExists(joinTableName))
                            {
                                IIndexableRecordConverter leftRecord = (IIndexableRecordConverter)testTable;
                                IIndexableRecordConverter rightRecord = (IIndexableRecordConverter)testChildTable;
                                IEnumerable<Column> joinTableColumns = new Column[]
                                {
                                    leftRecord.ForeignKeyColumn, rightRecord.ForeignKeyColumn
                                };

                                TableCreate(joinTableName, joinTableColumns);

                            }

                        }
                    });
                }
            }


            //create static tables
            if (hasStaticCols)
            {
                var staticCols = staticColsGroup.First().Select(x => x);

                if (!TableExists(StaticTableName))
                {
                    Column classCol = Column.Create<T>("Class", typeof(string), true);
                    Column variableCol = Column.Create<T>("Variable", typeof(string), true);
                    Column valueCol = Column.Create<T>("Value", typeof(object), true);
                    TableCreate(StaticTableName, new Column[] { classCol, variableCol, valueCol});
                }


                //string className = tableType.Name;

                //staticCols.ForEach(col =>
                //{
                //    string variableName = col.Name;
                //    string valueData = "";


                //});
            }

        }

        public void LoadStaticValues()
        {

        }

        public void SaveStaticValues()
        {

        }

        //public void TableCreate<T>(ITableConverter<T> table)
        //{
        //    TableCreate(table.TableName, table.ToTable().Columns);
        //}

        public void TableCreate(string tableName, IEnumerable<Column> columns)
        {

            string sql = "CREATE TABLE " + tableName + " (";


            string sqlCols = string.Join(",", columns.Select(x => x.GetColumnDefinition()));

            sql = sql + sqlCols + ")";

            Query command = NewQuery(sql);

            RunQuery(command);
        }

        //public void TableCreate(string tablename, params Column[] columns)
        //{
        //    TableCreate(tablename, columns);
        //}

        public bool TableExists(ITableConverter table)
        {
            return TableExists(table.TableName);
        }

        public bool TableExists<T>() where T : ITableConverter
        {
            return TableExists(Utilities.GetTableNameFromType<T>());
        }


        public bool TableExists(string tableName)
        {
            string sql = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='"+tableName+"'";

            Query query = NewQuery(sql, true);
            QueryJob job = RunQuery(query);

            return ((long)job.result.ScalarObject > 0);
        }

        public bool ColumnExists(string tableName, string columnName)
        {
            return false;
        }

        public void SaveRecord(IRecordConverter recordConverter)
        {
            if (recordConverter.IsIndexable())
            {
                IIndexableRecordConverter indexableRecord = recordConverter.GetIndexableRecordConverter();

                if (indexableRecord.IsIndexed)
                {
                    //update
                    Update(recordConverter);
                }
                else
                {
                    //insert
                    Insert(recordConverter);
                }
            }
            else
            {
                //insert
                Insert(recordConverter);
            }
        }

        public void Insert(IRecordConverter recordConverter)
        {
            Insert(recordConverter.ToRecord());
        }

        public void Insert(Record record)
        {
            
        }

        public void Update(IRecordConverter recordConverter)
        {

        }

        public TRecord LoadRecord<TRecord>(TRecord record) where TRecord : IIndexableRecordConverter
        {


            return record;
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
