using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public class Relationship<ChildType> : Dictionary<object, ChildType>, IRelationship<ChildType> where ChildType : IIndexableRecordConverter
    {
        //private Dictionary<object, IIndexableRecordConverter> data = new Dictionary<object, IIndexableRecordConverter>();
        //public virtual IIndexableRecordConverter this[object key] 
        //{
        //    get 
        //    {
        //        return data[key]; 
        //    }

        //    set
        //    {
        //        if (!data.ContainsKey(key))
        //        {
        //            data.Add(key, value);
        //        }
        //        else
        //        {
        //            data[key] = value;
        //        }
        //    }
        //}
        public object[] GetForeignKeys()
        {
            return Keys.ToArray();
        }

        public KeyType[] GetForeignKeys<KeyType>()
        {
            return Keys.Select(x => (KeyType)x).ToArray();
        }

        public ChildType GetChild(object key)
        {
            return this[key];
        }
    }

    public class OneToManyRelationship<ChildType> : OneToManyRelationship<object, ChildType> where ChildType : IIndexableRecordConverter
    {

    }
    public class OneToManyRelationship<FKType,ChildType> : Relationship<ChildType> where ChildType :IIndexableRecordConverter
    {
        public ChildType this[FKType a]
        {
            get
            {
                return (ChildType)base[a];
            }

            set
            {
                base[a] = value;
            }
        }
    }

    public interface IRelationship<ChildType> : IDictionary<object, ChildType>, IRelationship where ChildType : IIndexableRecordConverter
    {
        ChildType GetChild(object key);
    }
    public interface IRelationship
    {
        object[] GetForeignKeys();
        KeyType[] GetForeignKeys<KeyType>();

    }
}
