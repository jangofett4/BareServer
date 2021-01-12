using System;
using System.IO;
using System.Collections.Generic;

using BareServer.Core;

using NLog;

namespace BareServer.Backends
{
    public class BHtmlProvider : RouteProvider
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private Router<Page> Router { get; }
        
        private string Basedir { get; }
        private bool Recurse { get; }

        public BHtmlProvider(App app, string dir, bool recurse = false) : base(app)
        {
            Router = new Router<Page>(app);
            Basedir = dir;
            Recurse = recurse;
            Reload();
        }

        // TODO: Fix recursion
        public override void Reload()
        {
            Router.Routes.Clear();
            DoFolder(Basedir, Recurse);
            Logger.Info("Compiled & Added {} BHtml routes.", Router.Routes.Count);
        }

        private void DoFile(string file)
        {
            Router.Routes.Add(BHtmlCompiler.Compile(App, File.ReadAllText(file), file));
        }

        private void DoFolder(string folder, bool recurse)
        {
            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                if (!file.EndsWith(".bhtml"))
                    continue;
                var sanitized = file;
                DoFile(sanitized);
            }
            if (Recurse)
            {
                var dirs = Directory.GetDirectories(folder);
                foreach (var dir in dirs)
                    DoFolder(dir, true);
            }
        }

        public override ResponseContext Provide(string[] path)
        {
            var result = Router.Run(path);
            if (result != null)
            {
                Logger.Info("Provided page route for {}", path[path.Length - 1]);
                var page = (Page)result.Route;
                result.Meta = new Dictionary<string, string>(page.GetMetadata);
            }
            
            return result;
        }
    }
}