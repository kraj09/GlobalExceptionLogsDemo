using Splunk.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SplunkDemoFW.Controllers
{
    [RoutePrefix("splunk")]
    public class SplunkApiController : ApiController
    {
        #region Default
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
        #endregion

        [HttpGet]
        [Route("sampleget")]
        public string SampleGet()
        {
            return "Hello, I'm a string";
        }

        [HttpPost]
        [Route("sendtosplunk")]
        public async Task<IHttpActionResult> SendDataToSplunk()
        {
            string message = "some message";
            var middleware = new HttpEventCollectorResendMiddleware(100);

            // preparing parameters
            Uri uri = new Uri("http://localhost:8088");
            string token = "73f4c508-35da-4d14-9d4b-12a8d820935f";

            try
            {
                var ecSender = new HttpEventCollectorSender(uri, token, null, HttpEventCollectorSender.SendMode.Sequential, 0, 0, 0, middleware.Plugin);

                ecSender.OnError += EcSender_OnError;

                ecSender.Send(Guid.NewGuid().ToString(), "INFO", null, new { Foo = message });
                var flushTask = ecSender.FlushAsync();
                flushTask.Start();
                await flushTask;

                return Ok("data saved to splunk");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        private void EcSender_OnError(HttpEventCollectorException obj)
        {
            Console.WriteLine(obj.Message);
        }
    }
}