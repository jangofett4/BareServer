using System;
using System.Text;
using System.Collections.Generic;

namespace BareServer.Core
{
    public class RouteResponse
    {
        public static RouteResponse Empty => new RouteResponse(new byte[0]);
        
        private Dictionary<string, string> meta { get; set; }

        public byte[] Raw { get; set; }

        public RouteResponse(byte[] data)
        {
            Raw = data;
            meta = new Dictionary<string, string>();
        }

        public static RouteResponse FromString(string str) => new RouteResponse(Encoding.UTF8.GetBytes(str));
        public static RouteResponse FromString(string str, Encoding encoding) => new RouteResponse(encoding.GetBytes(str));

        public static RouteResponse FromString(StringBuilder str) => new RouteResponse(Encoding.UTF8.GetBytes(str.ToString()));
        public static RouteResponse FromString(StringBuilder str, Encoding encoding) => new RouteResponse(encoding.GetBytes(str.ToString()));

        public void AddMetadata(string key, string value) => meta[key] = value;
        public string GetMetadata(string key) => meta.GetValueOrDefault(key);
    }
}