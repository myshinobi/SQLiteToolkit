﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public struct Table
    {
        public string TableName;
        public bool IsStatic;
        private readonly Dictionary<string, Column> _columns;
        public Column this[string colName] { get => _columns[colName]; }
        public Column this[int index] { get => _columns.ElementAt(index).Value; }

        public ICollection<Column> Columns => _columns.Values;

        public Table(string tableName, IEnumerable<Column> columns, bool isStatic)
        {
            this.TableName = tableName;
            _columns = columns.ToDictionary(x => x.Name, x => x);
            this.IsStatic = isStatic;
        }

        //public Table(DataTable dataTable)
        //{
        //    this.TableName = dataTable.TableName;
        //    this._columns = new Dictionary<string, Column>();
        //    foreach (DataColumn item in dataTable.Columns)
        //    {
        //        if (!_columns.ContainsKey(item.ColumnName))
        //        {
        //            _columns.Add(item.ColumnName, item.ToColumn());
        //        }
        //    }
        //}

        public static Table Create<T>(DataTable dataTable, bool isStatic)
        {
            List<Column> columns = new List<Column>();
            foreach (DataColumn item in dataTable.Columns)
            {
                Column col = item.ToColumn<T>(isStatic);
                if (!columns.Contains(col))
                {
                    columns.Add(col);
                }
            }
            Table table = new Table(dataTable.TableName, columns, isStatic);

            return table;
        }

        
    }
}
