using System;
using System.Collections.Generic;

namespace BareServer.Core
{
    public class ResponseContext
    {
        public Route Route { get; }
        public Dictionary<string, string> Meta { get; set; }

        public bool Match { get; }
        public RouteResponse Response { get; }

        public ResponseContext(Route route, bool match, RouteResponse response)
        {
            Route = route;
            Match = match;
            Response = response;
            Meta = new Dictionary<string, string>();
        }
    }
}