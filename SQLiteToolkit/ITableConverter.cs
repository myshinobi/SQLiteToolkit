using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
//using System.Reflection;

namespace SQLiteToolkit
{
    //public interface IIndexableTableConverter<T> : ITableConverter<T>
    //{ 
    
    //}


    public interface ITableConverter<T> : ITableConverter
    {
        //T FromDataTable(DataTable dataTable);
    }


    public interface ITableConverter
    {
        string TableName { get; }
        Table ToTable();

        bool HasChildTables();
    }

    //public class IndexableTableConverter<T> : TableConverter<T>, IIndexableTableConverter<T>
    //{

    //}

    public class TableConverter<T> : ITableConverter<T>
    {
        public DataTable dataTable;

        private string _tableName = "";
        public virtual string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_tableName))
                {
                    var me = this.GetType().Name;
                    _tableName = me;
                }
                return _tableName;
            }
        }

        public bool HasChildTables()
        {
            return Utilities.TypeHasPropertyThatImplements<T, IRelationship>();
        }

        //public virtual T FromDataTable(DataTable dataTable)
        //{
        //    this.dataTable = dataTable;


        //    var table = dataTable.ToTable();
        //}

        public virtual Table ToTable()
        {
            return Utilities.ToTable(this);
        }
    }
}
