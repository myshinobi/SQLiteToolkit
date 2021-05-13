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
        public bool IsForeignKey;
        public bool IsAutoIncrement;
        public bool IsStatic;
        public TableType tableType;
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

        public override string ToString()
        {
            return Name;
        }
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


        public static Column Create<T>(string name, Type type, bool isStatic)
        {
            string pkName = "";
            bool isAutoInc = false;
            if (typeof(T).ImplementsType<IIndexableRecordConverter>())
            {
                var t = (IIndexableRecordConverter)Utilities.CreateInstance<T>();
                pkName = t.GetPrimaryKey();

                if (t is AutoIndexableRecordConverter<T> && pkName == name)
                {
                   
                    isAutoInc = true;
                }
                
            }
            bool isPK = (name == pkName);
            bool isFK = type.ImplementsType<IRelationship>();
            return Create(name, type, isPK, isFK, isAutoInc, isStatic);
        }

        //public static Column Create<T>(string name, T defaultValue, bool isStatic)
        //{

        //}

        //public static Column Create(string name, object defaultValue, bool isStatic)
        //{
        //    return Create<object>(name, defaultValue, isStatic);
        //}

        public static Column Create(string name, Type type, bool IsPrimaryKey, bool isForeignKey, bool isAutoIncrement, bool isStatic)
        {
            Column col = new Column();
            col.type = type;
            col.Name = name;
            col.IsPrimaryKey = IsPrimaryKey;
            col.IsForeignKey = isForeignKey;
            col.IsAutoIncrement = isAutoIncrement;
            col.IsStatic = isStatic;

            col.tableType = (isStatic ? TableType.Static : (isForeignKey ? TableType.Join: TableType.Instance));



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

        public enum TableType
        {
            Instance = 0,
            Join = 1,
            Static = 2
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
