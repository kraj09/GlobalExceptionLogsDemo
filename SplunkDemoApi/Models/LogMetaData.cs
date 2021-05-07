using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SplunkDemoApi.Models
{
    public class LogMetadata
    {
        public string Scheme { get; set; }
        public string Host { get; set; }
        public string RequestContentType { get; set; }
        public string QueryString { get; set; }
        public string RequestUri { get; set; }
        public string RequestPath { get; set; }
        public string RequestMethod { get; set; }
        public string RequestBody { get; set; }
        public DateTime? RequestTimestamp { get; set; }
        public string ResponseContentType { get; set; }
        public string ResponseBody { get; set; }
        public HttpStatusCode ResponseStatusCode { get; set; }
        public DateTime? ResponseTimestamp { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionStackTrace { get; set; }
    }
}
