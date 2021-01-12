using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BareServer.Core
{
    public enum RouteRegex
    {
        Numeric,
        Boolean,
        Symbol,
        Custom
    }
    
    public delegate RouteResponse ResponseDelegate(App app, RequestContext ctx);

    public class Route
    {
        public struct RoutePart
        {
            public string String { get; }
            public bool IsGet { get; }
            public RouteRegex Regex { get; }

            public RoutePart(string str)
            {
                String = str;
                IsGet = false;
                Regex = RouteRegex.Custom;
            }

            public RoutePart(string str, RouteRegex regex)
            {
                String = str;
                IsGet = true;
                Regex = regex;
            }
        }

        public ResponseDelegate Response { get; set; } = (app, ctx) => RouteResponse.Empty;

        public string FullPath { get; private set; }
        public string Folder { get; private set; }

        public List<RoutePart> Parts { get; private set; }

        // Route("/")
        // Route("/user/:id/", new[] {  })
        public Route(string path)
        {
            FullPath = path;
            Folder = path.Substring(0, path.LastIndexOf('/')).TrimStart('.');

            path = SanitizePath(path);
            Parts = new List<RoutePart>();
            var split = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in split)
            {
                // variable
                if (part.StartsWith(':'))
                {
                    Parts.Add(new RoutePart(part.Substring(1), RouteRegex.Numeric));
                }
                // exact match
                else
                {
                    Parts.Add(new RoutePart(part));
                }
            }
        }

        public Route(string path, params RouteRegex[] regexes)
        {
            FullPath = path;
            path = SanitizePath(path);
            Parts = new List<RoutePart>();
            var split = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var vars = 0;
            for (int i = 0; i < split.Length; i++)
            {
                var part = split[i];
                // variable
                if (part.StartsWith(':'))
                {
                    if (vars >= regexes.Length)
                        Parts.Add(new RoutePart(part.Substring(1), RouteRegex.Numeric));
                    else
                        Parts.Add(new RoutePart(part.Substring(1), regexes[vars]));
                    vars++;
                }
                // exact match
                else
                {
                    Parts.Add(new RoutePart(part));
                }
            }
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

        public ResponseContext Run(App app, string[] request)
        {
            // root (/), no get no post, just send app
            if (request.Length == 0 && Parts.Count == 0)
                return new ResponseContext(this, true, Response(app, new RequestContext()));
            
            if (request.Length != Parts.Count)
                return new ResponseContext(null, false, RouteResponse.Empty);

            var ctx = new RequestContext();
            for (int i = 0; i < request.Length; i++)
            {
                var part = Parts[i];
                var req = request[i];

                if (!part.IsGet)
                {
                    if (part.String != req)
                        return new ResponseContext(null, false, RouteResponse.Empty);
                }
                else
                {
                    switch (part.Regex)
                    {
                        case RouteRegex.Numeric:
                            if (!(new Regex("^[0-9]+$").IsMatch(req)))
                                return new ResponseContext(null, false, RouteResponse.Empty);
                            ctx.Get.Add(part.String, int.Parse(req));
                            break;
                        case RouteRegex.Boolean:
                            switch (req)
                            {
                                case "true":
                                    ctx.Get.Add(part.String, true);
                                    break;
                                case "false":
                                    ctx.Get.Add(part.String, false);
                                    break;
                                default:
                                    return new ResponseContext(null, false, RouteResponse.Empty);
                            }
                            break;
                        case RouteRegex.Symbol:
                        case RouteRegex.Custom:
                            return new ResponseContext(null, false, RouteResponse.Empty);
                    }
                }
            }
            ctx.Route = this;

            return new ResponseContext(this, true, Response(app, ctx));
        }
    }
}