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

        public Dictionary<object, ChildType> ToDictionary()
        {
            return (Dictionary<object, ChildType>)this;
        }
        public virtual RelationshipType RelationshipType => throw new NotImplementedException();

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

        private Type _childType = null;
        public Type GetChildType()
        {
            if (_childType == null)
            {
                _childType = typeof(ChildType);
            }

            return _childType;
        }
    }

    public class OneToOneRelationship<FKType, ChildType> : Relationship<ChildType> where ChildType : IIndexableRecordConverter
    {
        public override RelationshipType RelationshipType => RelationshipType.OneToOne;
        public ChildType GetChild()
        {
            return (ChildType)this.First().Value;
        }

        public FKType GetForeignKey()
        {
            return (FKType)this.First().Key;
        }
    }


    public class OneToOneRelationship<ChildType> : OneToOneRelationship<object, ChildType> where ChildType : IIndexableRecordConverter
    {

    }

    public class OneToManyRelationship<ChildType> : OneToManyRelationship<object, ChildType> where ChildType : IIndexableRecordConverter
    {

    }
    public class OneToManyRelationship<FKType,ChildType> : Relationship<ChildType> where ChildType :IIndexableRecordConverter
    {
        public override RelationshipType RelationshipType => RelationshipType.OneToMany;
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

        
        RelationshipType RelationshipType { get; }

        Type GetChildType();
    }

    public enum RelationshipType
    {
        OneToOne = 0,
        OneToMany = 1,
        ManyToMany = 2,
        ManyToOne = 3
    }
}
