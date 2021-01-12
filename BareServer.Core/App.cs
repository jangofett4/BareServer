using System;
using System.Collections.Generic;

namespace BareServer.Core
{
    public class App
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string Name { get; }
        public Router<Route> Router { get; }
        public List<RouteProvider> Providers { get; set; }

        public App(string name)
        {
            Name = name;
            Router = new Router<Route>(this);
            Providers = new List<RouteProvider>();
        }

        public ResponseContext Run(string path)
        {
            var split = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var result = Router.Run(split);
            if (result != null)
                return result;
            foreach (var provider in Providers)
            {
                result = provider.Provide(split);
                if (result != null)
                    return result;
            }
            
            return null;
        }

        public void Reload()
        {
            int start = DateTime.Now.Millisecond;
            {
                Logger.Info("Reloading providers...");
                foreach (var p in Providers)
                    p.Reload();
            }
            int end = DateTime.Now.Millisecond;
            Logger.Info("Reload complete (took {}ms)", end - start);
        }

        public void AddRoute(Route route) => Router.Routes.Add(route);
        public void AddRoute(params Route[] routes) => Router.Routes.AddRange(routes);
    }
}
