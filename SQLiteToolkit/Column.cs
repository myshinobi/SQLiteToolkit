using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public struct Column
    {
        public string Name;
        public bool NotNull;
        public bool IsPrimaryKey;
        //public object DefaultValue;
        //public object type;
        public Type type;
        //public static Column Create<T>(string name, T defaultValue = default(T), bool notNull = true, bool isPrimaryKey = false)
        //{
        //    Column col = new Column();
        //    //col.type = default(T);
        //    col.type = typeof(T);
        //    col.Name = name;
        //    col.NotNull = notNull;
        //    col.IsPrimaryKey = isPrimaryKey;
        //    col.DefaultValue = defaultValue;

        //    return col;
        //}

        public override bool Equals(object obj)
        {
            if (typeof(Column) == obj.GetType())
            {
                Column otherCol = (Column)obj;

                return this.Name == otherCol.Name;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        public static Column Create<T>(string name, Type type)
        {
            string pkName = "";
            if (typeof(T).ImplementsType<IIndexableRecordConverter>())
            {
                var t = (IIndexableRecordConverter)Utilities.CreateInstance<T>();
                pkName = t.GetIndexColumnName();
            }
            bool isPK = (name == pkName);
            return Create(name, type, isPK);
        }

        public static Column Create(string name, Type type, bool IsPrimaryKey)
        {
            Column col = new Column();
            col.type = type;
            col.Name = name;
            col.IsPrimaryKey = IsPrimaryKey;



            //var sqlType = col.GetSQLDataType();

            //switch (sqlType)
            //{
            //    case SQLDataTypes.TEXT:
            //        col.DefaultValue = "";
            //        break;
            //    case SQLDataTypes.INTEGER:
            //        col.DefaultValue = 0;
            //        break;
            //    case SQLDataTypes.REAL:
            //        col.DefaultValue = 0d;
            //        break;
            //    case SQLDataTypes.NUMERIC:
            //        col.DefaultValue = 0m;
            //        break;

            //    default:
            //        col.DefaultValue = null;
            //        break;
            //}


            return col;
        }

        public string GetColumnDefinition()
        {
            return Name +" "+ GetSQLDataType() + (IsPrimaryKey ? " PRIMARY KEY" : "") + (NotNull ? " NOT NULL" : "");
        }
        
        public string GetSQLValue(object value)
        {
            string prefix = Name;

            if (value == null)
            {
                prefix += " IS NULL";

                return prefix;
            }
            else
            {
                prefix += " = ";
                switch (GetSQLDataType())
                {
                    case SQLDataTypes.TEXT:
                        return prefix + "'" + value.ToString() + "'";

                    default:
                        return prefix + value.ToString();
                }
            }



        }

        public enum SQLDataTypes
        {
            TEXT,
            INTEGER,
            REAL,
            NUMERIC,
            BLOB
        }

        public SQLDataTypes GetSQLDataType()
        {
            //Type t = type.GetType();
            if (type == typeof(decimal))
            {
                return SQLDataTypes.NUMERIC;
            }

            if (type == typeof(string))
            {
                return SQLDataTypes.TEXT;
            }

            if (type == typeof(float) || type == typeof(double))
            {
                return SQLDataTypes.REAL;
            }

            if (type == typeof(int) || type == typeof(bool))
            {
                return SQLDataTypes.INTEGER;
            }

            return SQLDataTypes.BLOB;
        }
    }
}
