using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IO;
using SplunkDemoApi.LoggerService;
using SplunkDemoApi.Managers;
using SplunkDemoApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SplunkDemoApi.CustomExceptionMiddleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerManager _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public ExceptionMiddleware(RequestDelegate next, ILoggerManager logger)
        {
            _next = next;
            _logger = logger;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            string requestLog = string.Empty;
            try
            {
                requestLog = ReadRequestBody(httpContext);
                //await SplunkManager.LogDataToSplunk(new { Request = requestLog });

                //await LogResponse2(httpContext);
                await _next(httpContext);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong (from Custom Middleware): {ex}");
                //await SplunkManager.LogDataToSplunk(new { Exception = ex.Message });
                await HandleExceptionAsync(httpContext, ex);
            }
            finally
            {
                //ReadResponse(httpContext);
                //string res = await ReadResponseBody(httpContext.Response);
                
            }
        }

        // working
        private string ReadRequestBody(HttpContext context)
        {
            var injectedRequestStream = new MemoryStream();
            var requestLog = $"REQUEST HttpMethod: {context.Request.Method}, Path: {context.Request.Path}";

            if (context.Request.Method == "POST")
            {
                using (var bodyReader = new StreamReader(context.Request.Body))
                {
                    var bodyAsText = bodyReader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                    {
                        requestLog += $", Body : {bodyAsText}";
                    }

                    var bytesToWrite = Encoding.UTF8.GetBytes(bodyAsText);
                    injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                    injectedRequestStream.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = injectedRequestStream;
                }
            }

            return requestLog;
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(new ErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error from the custom middleware"
            }.ToString());
        }

        private async Task LogResponse2(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var x = $"Http Response Information:{Environment.NewLine}" +
                                   $"Schema:{context.Request.Scheme} " +
                                   $"Host: {context.Request.Host} " +
                                   $"Path: {context.Request.Path} " +
                                   $"QueryString: {context.Request.QueryString} " +
                                   $"Response Body: {text}";

            await responseBody.CopyToAsync(originalBodyStream);
        }

        // not working
        private string ReadResponseBody(HttpContext context)
        {
            var injectedRequestStream = new MemoryStream();
            var requestLog = $"REQUEST HttpMethod: {context.Request.Method}, Path: {context.Request.Path}";

            using (var bodyReader = new StreamReader(context.Request.Body))
            {
                var bodyAsText = bodyReader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                {
                    requestLog += $", Body : {bodyAsText}";
                }

                var bytesToWrite = Encoding.UTF8.GetBytes(bodyAsText);
                injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                injectedRequestStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = injectedRequestStream;
            }

            return requestLog;
        }

        // not working
        private async Task LogResponse(HttpContext context)
        {
            using (Stream originalRequest = context.Response.Body)
            {
                try
                {
                    using (var memStream = new MemoryStream())
                    {
                        context.Response.Body = memStream;
                        // All the Request processing as described above 
                        // happens from here.
                        // Response handling starts from here
                        // set the pointer to the beginning of the 
                        // memory stream to read
                        memStream.Position = 0;
                        // read the memory stream till the end
                        var response = await new StreamReader(memStream)
                                                                .ReadToEndAsync();
                        // write the response to the log object
                        var res = response;
                        var responseCode = context.Response.StatusCode.ToString();
                        var isSuccessStatusCode = (
                              context.Response.StatusCode == 200 ||
                              context.Response.StatusCode == 201);
                        var respondedOn = DateTime.Now;

                        // add the log object to the logger stream 
                        // via the Repo instance injected
                        //repo.AddToLogs(log);

                        // since we have read till the end of the stream, 
                        // reset it onto the first position
                        memStream.Position = 0;

                        // now copy the content of the temporary memory 
                        // stream we have passed to the actual response body 
                        // which will carry the response out.
                        await memStream.CopyToAsync(originalRequest);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    // assign the response body to the actual context
                    context.Response.Body = originalRequest;
                }
            }
        }

        // not using
        private void ReadRequest(HttpContext context)
        {
            if (context.Request.Method == "POST")
            {
                //await ReadBody(context);
                //ReadBody2(context);
                //ReadBody3(context.Request);
                //ReadBody4(context);
                //ReadBody5(context.Request);
                //FormatRequest(context.Request);                
               //await ReadResponseBody(context.Response);
            }
        }

        // not working
        private async Task ReadBody(HttpContext context)
        {
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;

            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            var url = UriHelper.GetDisplayUrl(context.Request);
            var requestBodyText = new StreamReader(requestBodyStream).ReadToEnd();
            string logMessage = $"REQUEST METHOD: {context.Request.Method}, REQUEST BODY: {requestBodyText}, REQUEST URL: {url}";
            _logger.LogInfo(logMessage);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await _next(context);
            context.Request.Body = originalRequestBody;
        }

        // not working
        private async Task ReadBody2(HttpContext context)
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
            string payload = body;
        }

        // not working
        private async Task<string> ReadBody3(HttpRequest request)
        {
            //request.EnableRewind();

            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }

        // not working
        private async Task ReadBody4(HttpContext context)
        {
            using (var reader = new StreamReader(
        context.Request.Body,
        encoding: Encoding.UTF8,
        detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024 * 100 ,
        leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                // Do some processing with body…

                // Reset the request body stream position so the next middleware can read it
                context.Request.Body.Position = 0;
            }
        }

        // not working
        private async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }

        // not working
        private async Task<string> ReadBody5(HttpRequest request)
        {
            var body = request.Body;
            //request.EnableRewind();

            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body = body;

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {bodyAsText}";
        }

        // not working
        private async Task<string> FormatRequest(HttpRequest request)
        {
            var body = request.Body;
            //request.EnableRewind();

            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body = body;

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {bodyAsText}";
        }

        // not working
        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"Response {text}";
        }

        private async Task ReadResponse(HttpContext context)
        {
            var originalBody = context.Response.Body;
            using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            newBody.Seek(0, SeekOrigin.Begin);
            var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            Console.WriteLine($"LoggingMiddleware: {bodyText}");
            newBody.Seek(0, SeekOrigin.Begin);
            await newBody.CopyToAsync(originalBody);
        }
    }
}
