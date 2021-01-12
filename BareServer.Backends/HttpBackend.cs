using System;
using System.Text;  
using System.Net;
using System.Net.Sockets;
using System.Net.Http;

using BareServer.Core;

namespace BareServer.Backends
{
    public class HttpBackend : AppBackend
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public HttpListener Server { get; }
        public volatile bool ShouldStop = false;

        public HttpBackend(App app) : base(app)
        {
            Server = new HttpListener();
            Server.Prefixes.Add("http://localhost:8080/"); // safe
        }

        public HttpBackend(App app, IPAddress ip, int port = 80) : base(app)
        {
            Server = new HttpListener();
            Server.Prefixes.Add($"http://{ ip.ToString() }:{ port }/");
        }

        public override void Start()
        {
            Server.Start();
            
            while (!ShouldStop)
            {
                var client = Server.GetContext();
                var request = client.Request;
                var response = client.Response;

                Logger.Info("Connection accepted ({})", request.RemoteEndPoint.ToString());
                Logger.Info("Request: {}", request.Url.LocalPath);
                
                var result = App.Run(request.Url.LocalPath);
                if (result == null)
                {
                    Logger.Info("404: Route not found");
                    response.StatusCode = 404;
                    response.Close();
                    continue;
                }

                response.StatusCode = 200;
                response.ContentLength64 = result.Response.Raw.Length;
                response.ContentEncoding = Encoding.UTF8;
                response.AddHeader("Server", "BareServer.HttpBackend");
                response.AddHeader("Content-Type", result.Response.GetMetadata("mime"));
                
                response.OutputStream.Write(result.Response.Raw);

                response.Close();
            }

            Server.Stop();
        }
    }
}
