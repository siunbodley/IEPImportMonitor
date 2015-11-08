using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEPImportMonitor
{
    class Site
    {
        public string CountryCode;
        public string DatabaseName;
        public string ImportRootPath;
        public string ImportProcessedPath;
        public string ImportIncomingPath;
        public int HourToCheck;
        public bool PendingIncoming;
        public List<String> ImportIncomingFiles;
        public bool DBProcessedToday;
        public List<String> DBProcessedTodayFiles;
        public DateTime LastDBLogTimestamp;
        
        public Site()
        {
            CountryCode = null;
            DatabaseName = null;
            ImportRootPath = null;
            ImportProcessedPath = null;
            ImportIncomingPath = null;
            HourToCheck = 0;
            PendingIncoming = false;
            ImportIncomingFiles = null;
            DBProcessedToday = false;
            DBProcessedTodayFiles = null;
            LastDBLogTimestamp = DateTime.Parse("1900-01-01 00:00:00");
        }
    }
}
