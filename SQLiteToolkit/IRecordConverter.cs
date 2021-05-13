using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
namespace SQLiteToolkit
{
    public interface IIndexableRecordConverter : IRecordConverter
    {
        Column PrimaryKeyColumn { get; }

        Column ForeignKeyColumn { get; }
        //object Id { get; set; }
        object GetId();

        //bool UseAutoIncrementId { get; }
        bool IsIndexed { get; }

        Type GetPrimaryKeyType();

        string GetPrimaryKey();
    }
    public interface IRecordConverter<T> : IRecordConverter, ITableConverter<T>
    {

        T FromDataRow(DataRow row);
        T ToObject<T>();
    }

    public interface IRecordConverter : ITableConverter
    {
        Record ToRecord();

        Type GetRecordType();
        void Save(Database database);

    }

    public class AutoIndexableRecordConverter<T> : IndexableRecordConverter<T>
    {
        //public override bool UseAutoIncrementId => true;
        private int _rowid = -1;

        public override object GetId()
        {
            return _rowid;
        }
        public int ROWID
        {
            get
            {
                return _rowid;
            }

            set
            {
                _rowid = value;
            }
        }

        //public override object Id { get => base.Id; set => base.Id = value; }
        public override bool IsIndexed
        {
            get
            {
                return ROWID > 0;
            }
        }

        public override string GetPrimaryKey()
        {
            return nameof(ROWID);
        }

        public override Column PrimaryKeyColumn => Column.Create(GetPrimaryKey(), typeof(int), true, false, true, false);
    }
    public class IndexableRecordConverter<T> : RecordConverter<T>, IIndexableRecordConverter
    {
        public virtual Column PrimaryKeyColumn => Column.Create(GetPrimaryKey(), GetPrimaryKeyType(), true, false, false, false);

        public virtual bool IsIndexed => GetId() != null;

        public Column ForeignKeyColumn
        {
            get
            {
                Column pk = PrimaryKeyColumn;

                pk.IsForeignKey = true;
                pk.IsPrimaryKey = false;
                pk.IsAutoIncrement = false;
                pk.NotNull = true;

                return pk;
            }
        }

        public virtual string GetPrimaryKey()
        {
            throw new Exception(typeof(T).Name + " must override GetPrimaryKey()");
        }
        public Type GetPrimaryKeyType()
        {
            return this.GetRecordType().GetVariableType(GetPrimaryKey());
        }

        public virtual object GetId()
        {
            return this.GetValue(GetPrimaryKey());
        }

    }

    public class RecordConverter<T> : TableConverter<T>, IRecordConverter<T>
    {
        public DataRow dataRow;
        //public T Data;
        public virtual T FromDataRow(DataRow row)
        {
            this.dataRow = row;

            var record = Utilities.ToRecord(row);

            T t = default(T);


            t.SetValuesFromRecord(record);


            return t;
        }

        private Type _recordType = null;
        public Type GetRecordType()
        {
            if (_recordType == null)
                _recordType = typeof(T);

            return _recordType;
        }

        public virtual Record ToRecord()
        {

            return Utilities.ToRecord<T>(ToObject<T>());

        }
        public void Save(Database database)
        {
            database.SaveRecord(this);
        }

        public T ToObject<T>()
        {
            return (T)((object)this);
        }
    }

}
