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

            var responseBodyStream = new MemoryStream();
            var bodyStream = httpContext.Response.Body;

            try
            {
                requestLog = ReadRequestBody(httpContext);
                //await SplunkManager.LogDataToSplunk(new { Request = requestLog });

                

                
                httpContext.Response.Body = responseBodyStream;
                
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
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = new StreamReader(responseBodyStream).ReadToEnd();
                // log here responseBody
                
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(bodyStream);
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
    }
}
