using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
namespace SQLiteToolkit
{
    public class QueryResult
    {
        //public int Id;
        public int rowsAffected = 0;
        public DataTable datatable;
        public TimeSpan timeSpanElapsed;
        public DateTime timeFinishedUTC;
        public Exception exception;
        public object ScalarObject;
        public bool Failed 
        { 
            get
            {
                return exception != null;
            } 
        }
    }
}
