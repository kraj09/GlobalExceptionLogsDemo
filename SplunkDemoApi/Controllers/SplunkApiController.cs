using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Splunk.BLL;
using Splunk.Logging;
using SplunkDemoApi.LoggerService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SplunkDemoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SplunkApiController : ControllerBase
    {
        private ILoggerManager _logger;
        public SplunkApiController(ILoggerManager logger)
        {
            _logger = logger;
        }

        #region Testing
        [HttpGet]
        [Route("getsample")]
        public string GetSample()
        {
            _logger.LogInfo("Getting sample from api");
            return "Hello!";
        }

        [HttpGet]
        [Route("wishperson")]
        public string WishPerson(string name)
        {
            _logger.LogInfo($"wishing {name}");
            return $"Hello, {name}!";
        }

        [HttpGet]
        [Route("getraiseerror")]
        public string GetRaiseError(string name)
        {
            throw new Exception("Raising an error ing GET method");
            return $"Hello, {name}!";
        }

        [HttpPost]
        [Route("postraiseerror")]
        public async Task<IActionResult> PostRaiseError([FromBody]Person person)
        {
            BusinessLogic bll = new BusinessLogic();
            bll.RaiseException();
            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] Person person)
        {
            try
            {
                throw new Exception("Demo Exception");
                var injectedRequestStream = new MemoryStream();
                var requestLog = $"REQUEST HttpMethod: {HttpContext.Request.Method}, Path: {HttpContext.Request.Path}";

                if (HttpContext.Request.Method == "POST")
                {
                    using (var bodyReader = new StreamReader(HttpContext.Request.Body))
                    {
                        var bodyAsText = bodyReader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                        {
                            requestLog += $", Body : {bodyAsText}";
                        }

                        var bytesToWrite = Encoding.UTF8.GetBytes(bodyAsText);
                        injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                        injectedRequestStream.Seek(0, SeekOrigin.Begin);
                        HttpContext.Request.Body = injectedRequestStream;
                    }
                }

                using (var Reader = new StreamReader(Request.Body, Encoding.UTF8))
                {

                    Request.EnableBuffering();
                    var body = await Reader.ReadToEndAsync();

                    //
                    // Allows using several time the stream in ASP.Net Core
                    var buffer = new byte[Convert.ToInt32(Request.ContentLength)];
                    await Request.Body.ReadAsync(buffer, 0, buffer.Length);
                    var requestContent = Encoding.UTF8.GetString(buffer);
                    //


                    var sb = new StringBuilder();

                    sb.AppendFormat("ContentType: {0}\n", Request.ContentType);
                    sb.AppendFormat("Request: {0}\n", Request.ToString());
                    sb.AppendFormat("ContentLength: {0}\n", Request.ContentLength.ToString());
                    if (Request.IsHttps)
                        sb.AppendFormat("{0}\n", "HTTPS!");

                    var headers = String.Empty;
                    foreach (var key in Request.Headers)
                        headers += key.Key + "=" + key.Value + Environment.NewLine;
                    sb.AppendFormat("Headers: \n{0}\n", headers);

                    sb.AppendFormat("QueryString: {0}\n", Request.QueryString);

                    var text = await Reader.ReadToEndAsync();
                    sb.AppendFormat("Body: {0}\n", text);
                    return Ok(sb.ToString());
                }
                return Ok("OK");
            }
            catch (System.Exception ex)
            {
                return Unauthorized($"{ex.Message}: {ex.StackTrace}");
            }
        }
        #endregion


        [HttpPost]
        [Route("sendlogstosplunk")]
        public async Task<IActionResult> SaveLogsToSplunk()
        {
            //var result = M2();
            var result = await M3();
            return Ok(result);
        }

        // Send Json objects directly to HTTP Event Collector
        private async Task<string> M3()
        {
            try
            {
                var middleware = new HttpEventCollectorResendMiddleware(100);
                var esSender = new HttpEventCollectorSender(new Uri("http://localhost:8088"), "73f4c508-35da-4d14-9d4b-12a8d820935f",
                    null, HttpEventCollectorSender.SendMode.Sequential, 0, 0, 0, middleware.Plugin);
                esSender.OnError += EsSender_OnError;
                esSender.Send(Guid.NewGuid().ToString(), "INFO", "My Message", new Person() { FirstName = "Raj 1", LastName = "Konduri" });

                var flushTask = esSender.FlushAsync();
                flushTask.Start();
                await flushTask;
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void EsSender_OnError(HttpEventCollectorException obj)
        {
            Console.WriteLine(obj.Message);
        }

        // Add logging using a .Net trace listener
        private string M2()
        {
            try
            {
                Person person = new Person() { FirstName = "Raj", LastName = "Konduri" };
                var traceSource = new System.Diagnostics.TraceSource("MyLogger");
                traceSource.Switch.Level = System.Diagnostics.SourceLevels.All;
                traceSource.Listeners.Clear();
                traceSource.Listeners.Add(new HttpEventCollectorTraceListener(new Uri("http://localhost:8088"), "73f4c508-35da-4d14-9d4b-12a8d820935f"));
                traceSource.TraceEvent(System.Diagnostics.TraceEventType.Information, 2, JsonConvert.SerializeObject(person));
                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }

    public class Person
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
