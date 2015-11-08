using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckImportFile
{
    public class MandrillAPI
    {

        string apiKey;
        bool useSsl;
        int timeout;
        const string BASE_SECURE_URL = "https://mandrillapp.com/api/1.0/";
        const string BASE_URL = "https://mandrillapp.com/api/1.0/";
       
        public MandrillAPI()
        {
            apiKey = null;
            useSsl = true;
            timeout = 0;
        }

        


    }
}
