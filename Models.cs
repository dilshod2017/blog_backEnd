using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace blog
{
    public partial class ReturnObj
    {
        public dynamic data { get; set; }
    }
    public class Action
    {
        public string url { get; set; }
        public string data { get; set; }
        public string to { get; set; }
        public string method { get; set; }
        public string do_action { get; set; }
    }
    public partial class PortMapConfiguration
    {
        public string post { get; set; }
        public string Like { get; set; }
        public string comment { get; set; }
        public string map { get; set; }
        public string router { get; set; }
    }
}
