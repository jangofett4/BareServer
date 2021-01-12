using System;
using System.Collections.Generic;

namespace BareServer.Core
{
    public class AppBackend
    {
        public App App { get; set; }
        public BackendSettings Settings { get; set; }
        
        public AppBackend(App app)
        {
            App = app;
            Settings = new BackendSettings();
        }

        public virtual void Start()
        {
            throw new NotImplementedException();
        }
    }
}