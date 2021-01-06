using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public class DatabaseMessage
    {
        public bool error = false;
        public string message;
        public DateTime dateTime = DateTime.Now;
        public QueryJob queryJob;

        public override string ToString()
        {
            string date = dateTime.ToString("MM/dd/yy H:mm:ss zzz");
            string text = message;
            if (string.IsNullOrEmpty(message) && queryJob.result.exception != null)
            {
                text = queryJob.result.exception.Message;
            }
            string msg = date + "\t - \t" + text;



            return msg;
        }
    }
}
