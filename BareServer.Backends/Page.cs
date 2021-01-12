using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.Encodings;

using BareServer.Core;

namespace BareServer.Backends
{
    // Current way of re-using same page object is bad, this would make async a hell to work with
    // Make it so constructor is compiled into expression and later called by provider
    // TODO:
    public class Page : Route
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public App App { get; }
        public RequestContext Request { get; set; }

        private Dictionary<string, string> metadata { get; set; }
        private StringBuilder Content { get; set; }

        public Page(App app, string path) : base(path)
        {   
            App = app;
            Content = new StringBuilder();
        }

        public void Write(string str) => Content.Append(str);
        public void Write(string str, params object[] format) => Content.Append(string.Format(str, format));

        public string ToJson<T>(T obj) => System.Text.Json.JsonSerializer.Serialize<T>(obj);
        public T FromJson<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json);

        public void Include(string file)
        {
            var res = App.Run(file);
            if (res == null)
            {
                // relative test
                file = Path.Combine(Request.Route.Folder, file);
                res = App.Run(file);
                if (res == null)
                {
                    Logger.Error("Included file not found: {}", file);
                    return;
                }

                Write(Encoding.UTF8.GetString(res.Response.Raw));
                return;
            }
            Write(Encoding.UTF8.GetString(res.Response.Raw));
        }

        public void Metadata(string key, string value) => metadata[key] = value;
        public string Metadata(string key) => metadata.GetValueOrDefault(key);
        public void Mime(string mime) => Metadata("mime", mime);
        
        public Dictionary<string, string> GetMetadata => metadata;

        public virtual string Run()
        {
            Content = new StringBuilder();
            metadata = new Dictionary<string, string>();
            return "";
        }

        public override string ToString()
        {
            return Content.ToString();
        }
    }
}