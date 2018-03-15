using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace quantforce
{
    public class Common_In
    {
        public string id;
        public string token;
        public string callback;
        public string nodeUri;
    }

    public class Common_Out
    {
        public int error; // 0 = no error
        public string message;
    }

    public class ActionView
    {
        public string name;
        public string verb;
        public JObject parameters;
    }

    public class Process_In : Common_In
    {
        public ActionView action;
    }

    public class Process_Out : Common_Out
    {
        public JObject result;
        public string excelId;
        public string jsonId;
    }

    public class DataOut
    {
        public string chunkId;
        public string MD5;
    }

    public class DataCheckout_Out : Common_Out
    {
        public string MD5_Final;
        public List<DataOut> dataOut = new List<DataOut>();
    }

}
