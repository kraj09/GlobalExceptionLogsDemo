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
        private LogMetadata log;

        public ExceptionMiddleware(RequestDelegate next, ILoggerManager logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            string severity = "INFO";
            string message = "My Demo Message";
            log = new LogMetadata();
            var responseBodyStream = new MemoryStream();
            var bodyStream = httpContext.Response.Body;

            try
            {
                ReadRequestBody(httpContext);
                
                httpContext.Response.Body = responseBodyStream;
                
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                severity = "Exception";
                _logger.LogError($"Something went wrong (from Custom Middleware): {ex}");                
                await HandleExceptionAsync(httpContext, ex);
            }
            finally
            {   
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                log.ResponseBody = new StreamReader(responseBodyStream).ReadToEnd();
                log.ResponseTimestamp = DateTime.Now;
                log.ResponseContentType = httpContext.Response.ContentType;
                log.ResponseStatusCode = (HttpStatusCode)httpContext.Response.StatusCode;
                
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(bodyStream);

                await SplunkManager.LogDataToSplunk(severity, message, log);
            }
        }
        
        private void ReadRequestBody(HttpContext context)
        {
            var injectedRequestStream = new MemoryStream();
            log.Scheme = context.Request.Scheme;
            log.Host = context.Request.Host.ToString();
            log.QueryString = context.Request.QueryString.ToString();
            log.RequestContentType = context.Request.ContentType;
            log.RequestUri = context.Request.GetDisplayUrl();
            log.RequestPath = context.Request.Path;
            log.RequestMethod = context.Request.Method;
            log.RequestTimestamp = DateTime.Now;

            if (context.Request.Method == "POST")
            {
                using (var bodyReader = new StreamReader(context.Request.Body))
                {
                    var bodyAsText = bodyReader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                    {
                        log.RequestBody = bodyAsText;
                    }

                    var bytesToWrite = Encoding.UTF8.GetBytes(bodyAsText);
                    injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                    injectedRequestStream.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = injectedRequestStream;
                }
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            log.ExceptionMessage = ex.Message;
            log.ExceptionStackTrace = ex.StackTrace;

            return context.Response.WriteAsync(new ErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error from the custom middleware"
            }.ToString());
        }
    }
}
