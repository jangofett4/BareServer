using System;

namespace BareServer.Core
{
    public class RouteProvider
    {
        public App App { get; }

        public RouteProvider(App app) => App = app;

        public virtual ResponseContext Provide(string[] path)
        {
            throw new NotImplementedException();
        }

        public virtual void Reload()
        {
            throw new NotImplementedException();
        }
    }
}