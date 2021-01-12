using System;
using System.Collections.Generic;

namespace BareServer.Core
{
    public class Router<T> where T : Route
    {
        public List<T> Routes { get; }
        public App App { get; }

        public Router(App app)
        {
            Routes = new List<T>();
            App = app;
        }
        
        public ResponseContext Run(string[] path)
        {
            foreach (var route in Routes)
            {
                var res = route.Run(App, path);
                if (res.Match)
                    return res;
            }
            return null;
        }
    }
}