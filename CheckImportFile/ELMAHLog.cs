using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEPImportMonitor
{
    class ELMAHLog
    {
        public Guid ErrorId;
        public string Application;
        public string Host;
        public string Type;
        public string Source;
        public string Message;
        public string User;
        public int StatusCode;
        public DateTime TimeUtc;
        public int Sequence;
        public string AllXml;

        public ELMAHLog()
        {
            ErrorId = new Guid();
            Application = null;
            Host = null;
            Type = null;
            Source = null;
            Message = null;
            User = null;
            StatusCode = 0;
            TimeUtc = DateTime.Parse("1900-01-01 00:00:00");
            Sequence = 0;
            AllXml = null;
        }
    }
}
