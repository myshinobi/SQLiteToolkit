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

        public static Database DefaultDatabase = null;
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
        public QueryJob RunQuery(string query, Action<QueryJob> onFinished)
        {
            return RunQuery(NewQuery(query), onFinished);
        }
        public QueryJob RunQuery(Query query, Action<QueryJob> onFinished)
        {
            if (!QueryJobs.Exists(x => x.query == query))
            {
                QueryJob queryJob = new QueryJob(this, query);
                QueryJobs.Add(queryJob);
                queryJob.RunThreadInBackground(onFinished);

                return queryJob;
            }
            else
            {
                return QueryJobs.Find(x => x.query == query);
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

        public bool RecordExists(string tablename, params KeyValuePair<Column, object>[] keys)
        {
            string sql = "SELECT count(1) FROM "+tablename+ " WHERE "+string.Join(" AND ", keys.Select(x => x.Key.GetSQLValue(x.Value)));

            Query query = new Query(sql);
            //bool exists = false;
            QueryJob queryJob = RunQuery(query);/*, (job) =>
            {
                exists =  ((long)job.result.ScalarObject) > 0d;
            });

            queryJob.WaitForResult();*/

            return ((long)queryJob.result.ScalarObject) > 0d;
        }

        public DataTable GetDataTable(string tablename, params KeyValuePair<Column, object>[] keys)
        {

            string sql = "SELECT * FROM " + tablename + " WHERE " + string.Join(" AND ", keys.Select(x => x.Key.GetSQLValue(x.Value)));


            Query query = new Query(sql);
            QueryJob queryJob = RunQuery(query);
            return queryJob.result.datatable;
        }

        public Column[] GetStaticTableColumns()
        {

            Column classCol = Column.Create("Class", typeof(string), true, null);
            classCol.IsPrimaryKey = true;

            Column variableCol = Column.Create("Variable", typeof(string), true, null);
            variableCol.IsPrimaryKey = true;

            Column valueCol = Column.Create("Value", typeof(object), true, null);

            return new Column[] {classCol, variableCol, valueCol };
        }


        public void TableCreate<TTable>() where TTable : ITableConverter
        {
            //create my table
            string tableName = Utilities.GetTableNameFromType<TTable>();

            IEnumerable<Column> columns = Utilities.GetColumnsFromType<TTable>();

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

            Type tableType = typeof(TTable);
            //create join tables
            if (hasJoinTable)
            {
                var joinCols = joinColsGroup.First().Select(x => x);

                TTable testTable = Utilities.CreateInstance<TTable>();
                if (testTable.IsIndexable())
                {

                    joinCols.ForEach(joinCol =>
                    {

                        IRelationship relationship = testTable.GetValue(joinCol.Name) as IRelationship;
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
                Column[] cols = GetStaticTableColumns();
                Column classCol = cols[0];
                Column variableCol = cols[1];
                Column valueCol = cols[2];

                if (!TableExists(StaticTableName))
                {
                    TableCreate(StaticTableName, cols);
                }


                string className = tableType.Name;

                //staticCols.ForEach(col =>
                //{
                //    string variableName = col.Name;
                //    //string valueData = "";
                //});

                KeyValuePair<Column, object> classKey = new KeyValuePair<Column, object>(classCol, className);

                foreach (var col in staticCols)
                {
                    string variableName = col.Name;

                    object value = tableType.GetValue(variableName);

                    KeyValuePair<Column, object>[] keys = new KeyValuePair<Column, object>[]{
                    classKey,
                    new KeyValuePair<Column, object>(variableCol, variableName)
                    };

                    KeyValuePair<Column, object>[] data = new KeyValuePair<Column, object>[]
                    {
                        keys.ElementAt(0),
                        keys.ElementAt(1),
                        new KeyValuePair<Column, object>(valueCol, value)
                    };



                    if (!RecordExists(StaticTableName, keys))
                    {
                        //insert record
                        Record rec = new Record(StaticTableName, data);
                        Insert(rec);
                    }
                    //else
                    //{

                    //    //update record
                    //    //Record rec = new Record(StaticTableName, data);
                    //    //Update(rec);
                    //}

                }

                //load initial data 
                DataTable dt = GetDataTable(StaticTableName, classKey);
                //var type = typeof(T);
                foreach (Column col in staticCols)
                {
                    string variableName = col.Name;

                    KeyValuePair<Column, object>[] keys = new KeyValuePair<Column, object>[]{
                    classKey,
                    new KeyValuePair<Column, object>(variableCol, variableName)
                    };

                    var field = tableType.GetField(variableName,System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                    if (field != null)
                    {

                        var row = dt.Rows.Retrieve<DataRow>(x =>
                        {
                            return ((string)x[variableCol.Name] == variableName);
                        });

                        var dbValue = row[valueCol.Name];
                        var objValue = dbValue;
                        if (dbValue.GetType() == typeof(byte[]))
                        {
                            var json = System.Text.Encoding.Default.GetString((byte[])dbValue);
                            
                            objValue = Newtonsoft.Json.JsonConvert.DeserializeObject(json, col.type);
                        }
                        if (dbValue.GetType() == typeof(DBNull))
                        {
                            objValue = null;
                        }

                        field.SetValue(null, objValue);
                    }
                    else
                    {

                    }
                }
            }

        }

        //public void TableCreate<T>(ITableConverter<T> table)
        //{
        //    TableCreate(table.TableName, table.ToTable().Columns);
        //}

        public void TableCreate(string tableName, IEnumerable<Column> columns)
        {
            IEnumerable<Column> pks = columns.Where(x => x.IsPrimaryKey);
            string pkDef = ", PRIMARY KEY ("+ string.Join(",", pks.Select(x => x.Name))+")";
            string sql = "CREATE TABLE " + tableName + " (";


            string sqlCols = string.Join(",", columns.Select(x => x.GetColumnDefinition()));

            sql = sql + sqlCols +(pks.Count() > 0? pkDef:"")+ ")";

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

        //public bool ColumnExists(string tableName, string columnName)
        //{
        //    return false;
        //}

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
            string cols = "";
            string vals = "";
            bool first = true;
            record.ForEach(cell => 
            { 

                cols += (first?"":", ")+"'"+cell.Key.Name+"'";
                vals += (first ? "" : ", ") + cell.Key.GetSQLValue(cell.Value, true);
                first = false;
            });

            string sql = "INSERT INTO " + record.TableName + " (" + cols + ") VALUES (" + vals + ")";

            Query query = new Query(sql);
            RunQuery(query);

        }

        public void Update(IRecordConverter recordConverter)
        {
            Update(recordConverter.ToRecord());
        }

        public void Update(Record record)
        {

            string cols = "";
            string vals = "";
            bool first = true;
            record.ForEach(cell =>
            {

                cols += (first ? "" : ", ") + "'" + cell.Key.Name + "'";
                vals += (first ? "" : ", ") + cell.Key.GetSQLValue(cell.Value, true);
                first = false;
            });

            var fields = record.Where(x => x.Key.IsPrimaryKey == false);
            var keys = record.Where(x => x.Key.IsPrimaryKey);
            
            string sql = "UPDATE TABLE " + record.TableName + " SET "+ string.Join(", ",fields.Select(x => x.Key.GetSQLValue(x.Value)))+" WHERE "+ string.Join(" AND ",keys.Select(x => x.Key.GetSQLValue(x.Value)));

            Query query = new Query(sql);
            RunQuery(query);
        }

        public void MassUpdateStaticVariables(Type tTable, ICollection<KeyValuePair<Column, object>> data)
        {
            Column[] staticTableColumns = GetStaticTableColumns();

            string className = tTable.Name;
            Column classCol = staticTableColumns[0];
            Column variablesCol = staticTableColumns[1];
            Column valueCol = staticTableColumns[2];

            string sql = "UPDATE StaticVariables SET "+ valueCol.Name+" = CASE "+data.Select(x => "WHEN "+ classCol.Name+" = "+classCol.GetSQLValue(className));
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

                    if (DefaultDatabase == null)
                    DefaultDatabase = this;

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
