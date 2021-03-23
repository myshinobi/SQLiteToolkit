using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using static LibraryofAlexandria.Utilities;
namespace SQLiteToolkit
{
    public static class Utilities
    {
        public static IEnumerable<Column> GetColumnsFromType<T>()
        {
            return GetColumnsFromReflection<T>();
        }

        public static Record ToRecord<T>(this T data)
        {
            return new Record(GetColumnDataFromReflection(data));
        }
        public static Record ToRecord<T>(this DataRow dataRow)
        {
            //var properties = typeof(T).GetProperties();

            List<KeyValuePair<Column, object>> values = new List<KeyValuePair<Column, object>>();

            foreach (DataColumn col in dataRow.Table.Columns)
            {
                var val = dataRow[col];

                values.Add(new KeyValuePair<Column, object>(col.ToColumn<T>(), val));
            }

            return new Record(values);
        }

        public static Table ToTable<T>(this T table)
        {
            var cols = GetColumnsFromType<T>();

            return new Table(GetTableNameFromType(table), cols);
        }

        public static string GetTableNameFromType<T>()
        {
            return GetTableNameFromType(typeof(T));
        }

        public static string GetTableNameFromType<T>(this T obj)
        {
            Type type = obj.GetType();

            return GetTableNameFromType(type);
        }

        public static string GetTableNameFromType(this Type type)
        {
            string name = type.Name;
            return name;

        }

        public static Table ToTable<T>(this DataTable dataTable)
        {
            return Table.Create<T>(dataTable);
        }

        public static void SetValuesFromRecord<T>(this T obj, Record record)
        {
            Type type = typeof(T);
            FieldInfo[] properties = type.GetFields();

            foreach (FieldInfo info in properties)
            {
                info.SetValue(obj, record[info]);
            }
            

        }

        private static ICollection<KeyValuePair<Column, object>> GetColumnDataFromReflection<T>(T data)
        {
            List<KeyValuePair<Column, object>> columnData = new List<KeyValuePair<Column, object>>();
            var fields = GetFieldsToBeColumns<T>();
            foreach (var field in fields)
            {
                if (!columnData.Exists(x => x.Key.Name == field.Name))
                {
                    object fieldData = field.GetValue(data);
                    columnData.Add(new KeyValuePair<Column, object>(field.ToColumn<T>(fieldData), fieldData));
                }
            }

            var properties = GetPropertiesToBeColumns<T>();
            foreach (var property in properties)
            {
                if (!columnData.Exists(x => x.Key.Name == property.Name))
                {
                    object propertyData = property.GetValue(data);
                    columnData.Add(new KeyValuePair<Column, object>(property.ToColumn<T>(propertyData), propertyData));
                }

            }

            return columnData;
        }

        private static IEnumerable<Column> GetColumnsFromReflection<T>()
        {
            T obj = default(T);
            try
            {

                obj = CreateInstance<T>();
            }
            catch (Exception ex)
            {

                throw new Exception("Failed to get columns from reflection for type "+typeof(T).FullName+" with error: "+ex.Message, ex);
            }
            //string indexField = "";
            //if (obj is IIndexableRecordConverter<T>)
            //{
            //    IIndexableRecordConverter<T> indexableRecordConverter;
            //    indexableRecordConverter = (IIndexableRecordConverter<T>)obj;
            //    indexField = indexableRecordConverter.GetIndexColumnName();
            //}

            var a = GetColumnDataFromReflection(obj);
            var b = a.Select(x => x.Key);
            var c = b.OrderByDescending(x => x.IsPrimaryKey);

            return c;

        }

        //private static IEnumerable<MemberInfo> GetMembersToBeColumns<T>()
        //{
        //    var members = typeof(T).GetMembers().Where(x => FieldTypeIsValidForColumn(x.DeclaringType));

        //    return members;
        //}
        private static IEnumerable<FieldInfo> GetFieldsToBeColumns(Type type)
        {

            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(x => FieldTypeIsValidForColumn(x.FieldType));


            //var interfaces = type.GetInterfaces();

            //foreach (Type t in interfaces)
            //{
            //    var fields = GetFieldsToBeColumns(t);

            //    foreach (FieldInfo info in fields)
            //    {
            //        fieldInfos.Append(info);
            //    }
            //}

            return fieldInfos;
        }

        private static IEnumerable<FieldInfo> GetFieldsToBeColumns<T>()
        {
            var type = typeof(T);
            return GetFieldsToBeColumns(type);
        }


        private static IEnumerable<PropertyInfo> GetPropertiesToBeColumns(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => FieldTypeIsValidForColumn(x.PropertyType) && x.CanWrite && x.CanRead);


            //var interfaces = type.GetInterfaces();

            //foreach (Type t in interfaces)
            //{
            //    var fields = GetPropertiesToBeColumns(t);

            //    foreach (PropertyInfo info in fields)
            //    {
            //        properties.Append(info);
            //    }
            //}

            return properties;
        }
        private static IEnumerable<PropertyInfo> GetPropertiesToBeColumns<T>()
        {
            Type type = typeof(T);

            return GetPropertiesToBeColumns(type);
        }

        private static bool FieldTypeIsValidForColumn<T>()
        {
            Type t = typeof(T);

            return FieldTypeIsValidForColumn(t);
        }

        private static bool FieldTypeIsValidForColumn(Type t)
        {
            return t == typeof(string) || t == typeof(decimal) || t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(object);// || t.ImplementsType<IRelationship>();
        }

        //private static bool ObjectCanBeConvertedToValidColumnType(Type obj)
        //{
        //    return (obj is int || obj is string || obj is decimal || obj is double || obj is float || obj is bool);
            
        //}

        public static Column ToColumn<T>(this PropertyInfo propertyInfo, object testData)
        {
            Type t = propertyInfo.PropertyType;
            if (t == typeof(object))
            {
                t = testData.GetType();
            }
            Column col = Column.Create<T>(propertyInfo.Name, t);

            return col;
        }

        public static Column ToColumn<T>(this DataColumn dataColumn)
        {
            return Column.Create<T>(dataColumn.ColumnName, dataColumn.DataType);
        }

        public static Column ToColumn<T>(this FieldInfo fieldInfo, object testData)
        {
            Type t = fieldInfo.FieldType;
            if (t == typeof(object))
            {
                t = testData.GetType();
            }
            Column col = Column.Create<T>(fieldInfo.Name, t);


            return col;
        }

        public static bool ImplementsType<T>(this Type type)
        {
            Type IType = typeof(T);
            bool implements = type.IsAssignableFrom(IType);
            Type[] interfaces;
            if (!implements)
            {
                interfaces = type.GetInterfaces();
                implements = interfaces.Contains(IType);

                
                if (!implements)
                {
                    implements = interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == IType);

                }
            }



            return implements;
        }

        //public static Column ToColumn<T>(this MemberInfo memberInfo, object testData)
        //{
        //    Type t = memberInfo.DeclaringType;
        //    if (t == typeof(object))
        //    {
        //        t = testData.GetType();
        //    }

        //    Column col = Column.Create(memberInfo.Name, t);

        //    return col;
        //}

        public static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public static T CreateInstance<T>()
        {
            Type type = typeof(T);
            return (T)CreateInstance(type);
        }

        public static bool TypeHasPropertyThatImplements<T,TInterface>()
        {
            return typeof(T).GetProperties().Any(x => x.PropertyType.ImplementsType<TInterface>()); 
        }

        public static IEnumerable<PropertyInfo> GetPropertiesInTypeThatImplement<T, TInterface>()
        {
            return typeof(T).GetProperties().Where(x => x.PropertyType.ImplementsType<TInterface>());
        }
    }
}
