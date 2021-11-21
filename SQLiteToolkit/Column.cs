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

        public static Column Create(System.Reflection.FieldInfo fieldInfo, Type tCol, Type tTable)
        {
            return Create(fieldInfo.Name, tCol, fieldInfo.IsStatic, tTable);
        }
        public static Column Create(System.Reflection.PropertyInfo propertyInfo, Type tCol, Type tTable)
        {
            return Create(propertyInfo.Name, tCol, propertyInfo.GetMethod.IsStatic, tTable);
        }

        public static Column Create(string name, Type tCol, bool isStatic, Type tTable)
        {
            string pkName = "";
            bool isAutoInc = false;
            if (tTable != null)
            {

                if (tTable.ImplementsType<IIndexableRecordConverter>())
                {
                    var t = (IIndexableRecordConverter)Utilities.CreateInstance(tTable);
                    pkName = t.GetPrimaryKey();

                    if (t is IAutoIndexableRecordConverter && pkName == name)
                    {

                        isAutoInc = true;
                    }

                }
            }
            bool isPK = (name == pkName);
            bool isFK = tCol.ImplementsType<IRelationship>();
            return Create(name, tCol, isPK, isFK, isAutoInc, isStatic);
        }

        public static Column Create<TTable>(string name, Type tCol, bool isStatic)
        {
            string pkName = "";
            bool isAutoInc = false;
            if (typeof(TTable).ImplementsType<IIndexableRecordConverter>())
            {
                var t = (IIndexableRecordConverter)Utilities.CreateInstance<TTable>();
                pkName = t.GetPrimaryKey();

                if (t is AutoIndexableRecordConverter<TTable> && pkName == name)
                {
                   
                    isAutoInc = true;
                }
                
            }
            bool isPK = (name == pkName);
            bool isFK = tCol.ImplementsType<IRelationship>();
            return Create(name, tCol, isPK, isFK, isAutoInc, isStatic);
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




            return col;
        }

        public string GetColumnDefinition()
        {
            // + (IsPrimaryKey ? " PRIMARY KEY" : "")
            return Name +" "+ GetSQLDataType() + (NotNull ? " NOT NULL" : "");
        }
        
        public string GetSQLValue(object value, bool onlyValue = false)
        {
            string prefix = Name;

            if (value == null)
            {

                if (onlyValue)
                {
                    return "NULL";
                }
                else
                {
                    return prefix + " IS NULL"; ;

                }

            }
            else
            {
                prefix += " = ";
                string val = "";
                //switch (GetSQLDataType())
                //{
                    

                //    case SQLDataTypes.TEXT:
                //        return prefix + "'" + value.ToString() + "'";

                //    default:
                //        return prefix + value.ToString();
                //}

                SQLDataTypes dataType = GetSQLDataType();

                if (dataType == SQLDataTypes.BLOB || dataType == SQLDataTypes.TEXT)
                {
                    //bool isJSON = false;
                    //Type valType = value.GetType();
                    if (value is string)
                    {
                         val = "'" + value.ToString() + "'";
                    }
                    else
                    {
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);

                        val = "'" + json + "'";
                    }
                }
                else
                {
                    val = value.ToString();
                }

                if (onlyValue)
                {
                    return val;
                }
                else 
                {
                    return prefix + val;
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
