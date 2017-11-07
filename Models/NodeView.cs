using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace quantforce
{
    public class NodeView
    {
        public string id;
        public string parentId;
        public int type;
        public string name;
        public long epoch;
        public JToken publicJson;
        public JToken privateJson;
        public JToken inheritedJson;
        public NodeView parent;
    }
}