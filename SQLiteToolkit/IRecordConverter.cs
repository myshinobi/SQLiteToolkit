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
        Column IndexColumn { get; }
        object Id { get; set; }

        bool UseAutoIncrementId { get; }
        bool IsIndexed { get; }

        Type GetIndexColumnType();

        string GetIndexColumnName();
    }
    public interface IRecordConverter<T> : IRecordConverter, ITableConverter<T>
    {

        T FromDataRow(DataRow row);
    }

    public interface IRecordConverter : ITableConverter
    {
        Record ToRecord();
    }

    public class AutoIndexableRecordConverter<T> : IndexableRecordConverter<T>
    {
        public override bool UseAutoIncrementId => true;
    }
    public class IndexableRecordConverter<T> : RecordConverter<T>, IIndexableRecordConverter
    {
        public virtual Column IndexColumn
        {
            get
            {
                if (!UseAutoIncrementId)
                    throw new Exception(typeof(T).Name +" must override IndexColumn because UseAutoIncrementId is true.");

                return Column.Create(GetIndexColumnName(), typeof(int), true);
            }
        }

        //public object Id = -1;

        public virtual bool IsIndexed
        {
            get
            {

                //return Id != null;

                //if (!UseAutoIncrementId)
                //    throw new Exception(typeof(T).Name + " must override IsIndexed because UseAutoIncrementId is true.");

                //return (int)Id > -1;

                if (Id == null)
                {
                    return false;
                }
                else
                {
                    if (UseAutoIncrementId)
                    {
                        return (int)Id > -1;
                    }
                    else
                    {
                        throw new Exception(typeof(T).Name + " must override IsIndexed because UseAutoIncrementId is true.");
                    }
                }
            }
        }


        //public virtual object Id { get; set; }

        public virtual bool UseAutoIncrementId
        {
            get
            {
                return false;
            }
        }

        public virtual string GetIndexColumnName()
        {
            return nameof(Id);
        }

        public object Id { get; set; } = -1;

        public Type GetIndexColumnType()
        {
            return IndexColumn.type;
        }

        //public object GetId()
        //{
        //    return Id;
        //}
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

        public virtual Record ToRecord()
        {

            return Utilities.ToRecord(this);

        }
    }

}
