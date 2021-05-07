using Splunk.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplunkDemoApi.Managers
{
    public class SplunkManager
    {
        public static async Task LogDataToSplunk(object data)
        {
            try
            {
                var middleware = new HttpEventCollectorResendMiddleware(100);
                var esSender = new HttpEventCollectorSender(new Uri("http://localhost:8088"), "73f4c508-35da-4d14-9d4b-12a8d820935f",
                    null, HttpEventCollectorSender.SendMode.Sequential, 0, 0, 0, middleware.Plugin);
                esSender.OnError += EsSender_OnError;
                esSender.Send(Guid.NewGuid().ToString(), "INFO", "My Message", data);

                var flushTask = esSender.FlushAsync();
                flushTask.Start();
                await flushTask;
            }
            catch (Exception ex)
            {
                
            }
        }

        private static void EsSender_OnError(HttpEventCollectorException obj)
        {
            Console.WriteLine(obj.Message);
        }
    }
}
