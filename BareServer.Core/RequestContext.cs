using System;
using System.Collections.Generic;

namespace BareServer.Core
{
    public class RequestContext
    {
        public Route Route { get; set; }
        public Dictionary<string, object> Get { get; set; }
        public Dictionary<string, object> Post { get; set; }

        public RequestContext()
        {
            Get = new Dictionary<string, object>();
            Post = new Dictionary<string, object>();
        }
    }
}