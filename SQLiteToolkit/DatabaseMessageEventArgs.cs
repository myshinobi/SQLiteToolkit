using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteToolkit
{
    public class DatabaseMessageEventArgs : EventArgs
    {
        public DatabaseMessage databaseMessage;

        public DatabaseMessageEventArgs(Exception ex)
        {
            databaseMessage = new DatabaseMessage();
            databaseMessage.error = true;
            databaseMessage.message = ex.Message;
        }
        public DatabaseMessageEventArgs(string message)
        {
            databaseMessage = new DatabaseMessage();
            databaseMessage.error = false;
            databaseMessage.message = message;
        }

        public DatabaseMessageEventArgs(QueryJob queryJob)
        {
            databaseMessage = new DatabaseMessage();
            databaseMessage.queryJob = queryJob;
        }
    }
}
