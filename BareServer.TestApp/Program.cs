using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

using BareServer.Core;
using BareServer.Backends;

namespace BareServer.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var testapp = new App("TestApp");

            HttpBackend backend = new HttpBackend(testapp);
            BHtmlProvider bhtml = new BHtmlProvider(testapp, "./www", true);
            HttpFileProvider files = new HttpFileProvider(testapp, "./www", true);

            testapp.Providers.Add(bhtml);
            testapp.Providers.Add(files);

            var th = new Thread(backend.Start);
            th.Start();

            bool exit = false;
            while (!exit)
            {
                Console.Write(">>> ");
                var line = Console.ReadLine();
                line = line.Trim();
                switch (line)
                {
                    case "reload":
                        testapp.Reload();
                        break;
                    case "abort":
                        backend.ShouldStop = true;
                        exit = true;
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    default:
                        Console.WriteLine("Unknown command: {0}", line);
                        break;
                }
            }
        }
    }
}
