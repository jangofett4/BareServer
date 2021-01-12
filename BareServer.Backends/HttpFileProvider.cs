using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using BareServer.Core;

using NLog;

namespace BareServer.Backends
{
    public class HttpFileProvider : RouteProvider
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static Dictionary<string, string> Mimes = new Dictionary<string, string>();

        private Router<Route> Router { get; }

        private string Basedir { get; }
        private bool Recurse { get; }

        public HttpFileProvider(App app, string dir, bool recurse = false) : base(app)
        {
            Router = new Router<Route>(app);
            Basedir = dir;
            Recurse = recurse;
            Reload();
        }

        public override void Reload()
        {
            // Clear old routes (if they exist at all)
            Router.Routes.Clear();

            // Ignore list
            var ignoreList = ParseIgnoreList(Basedir);
            Logger.Info("Compiled {} ignore rule(s).", ignoreList.Count);

            // MIMEs
            var mimes = ParseMimeList(Basedir);
            Logger.Info("Compiled {} MIME(s).", mimes.Count);
            Mimes = mimes;

            // Enumerate
            DoFolder(ignoreList, Basedir, Recurse);
            Logger.Info("Set-up {} file routes.", Router.Routes.Count);
        }

        private void DoFile(string file)
        {
            Router.Routes.Add(new Route(file) { Response = (app, ctx) => {
                var path = ctx.Route.FullPath;
                var data = File.ReadAllBytes(path);
                var resp = new RouteResponse(data);
                resp.AddMetadata("mime", Mimes.GetValueOrDefault(Path.GetExtension(path)));
                return resp;
            }});
        }

        private void DoFolder(List<Regex> ignore, string folder, bool recurse)
        {
            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                var sanitized = file;
                var shouldIgnore = false;
                foreach (var r in ignore)
                    if (r.IsMatch(sanitized))
                    {
                        shouldIgnore = true;
                        break;
                    }
                if (shouldIgnore)
                    continue;
                DoFile(sanitized);
            }
            if (recurse)
            {
                var dirs = Directory.GetDirectories(folder);
                foreach (var dir in dirs)
                    DoFolder(ignore, dir, true);
            }
        }

        public List<Regex> ParseIgnoreList(string dir)
        {
            var res = new List<Regex>();
            var file = Path.Combine(dir, ".htignore");
            if (!File.Exists(file))
                return res;
            var content = File.ReadAllLines(file);
            foreach (var line in content)
            {
                var lineTrim = line.Trim();
                if (lineTrim.StartsWith('#'))
                    continue;
                res.Add(new Regex(lineTrim, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
            return res;
        }

        public Dictionary<string, string> ParseMimeList(string dir)
        {
            var res = new Dictionary<string, string>();
            var file = Path.Combine(dir, ".htmime");
            if (!File.Exists(file))
                return res;
            var content = File.ReadAllLines(file);
            for (int i = 0; i < content.Length; i++)
            {
                var line = content[i];
                var lineTrim = line.Trim();
                if (lineTrim.StartsWith('#'))
                    continue;
                var split = lineTrim.Split(new []{ ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 2)
                {
                    Logger.Warn("File: [.htmime] Line {}: Unrecognized format", i);
                    continue;
                }
                res[split[0]] = split[1];
            }
            return res;
        }

        private string SanitizePath(string path)
        {
            var split = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var result = "";
            foreach (var s in split)
                if (s == ".")
                    continue;
                else
                    result += s + "/";
            return result;
        }

        public override ResponseContext Provide(string[] path)
        {
            var result = Router.Run(path);
            if (result != null)
                Logger.Info("Provided route for file: {}", path[path.Length - 1]);
            return result;
        }
    }
}