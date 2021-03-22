using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
namespace SQLiteToolkit
{
    //public class IndexableRecord : Record
    //{
    //    string IndexField;

    //    public object Id { get => this[IndexField]; }

    //    public IndexableRecord(string indexField)
    //    {
    //        this.IndexField = indexField;
    //    }
    //}
    public class Record
    {
        private Dictionary<Column, object> data = new Dictionary<Column, object>();
        public object this[Column key] { get => data[key]; }

        public object this[PropertyInfo propertyInfo]
        {
            get
            {
                return this[propertyInfo.Name];
            }
        }
        public object this[FieldInfo fieldInfo]
        {
            get
            {
                return this[fieldInfo.Name];
            }
        }
        public object this[string key] 
        {  
            get
            {

                var result = data.Where(x => x.Key.Name == key);
                if (result.Count() > 0)
                {
                    return result.ElementAt(0);
                }

                throw new Exception("Couldn't find column "+key);

            }
        }

        public Record(IEnumerable<KeyValuePair<Column, object>> data)
        {
            this.data = data.ToDictionary(x=> x.Key, x=>x.Value);
        
        }

        public Record(params KeyValuePair<Column, object>[] data)
        {
            this.data = data.ToDictionary(x => x.Key, x => x.Value);
        }

        public KeyValuePair<Column, object>[] GetKeyValuePairs()
        {
            return data.ToArray();
        }

    }
}
