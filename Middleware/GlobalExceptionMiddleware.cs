using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.DTOs.Common;

namespace DeskGuardBackend.Middleware
{
    /// <summary>
    /// Global exception handler middleware.
    /// Intercepts any unhandled exceptions in the HTTP pipeline, logs details,
    /// and formats a standardized JSON error response matching the frontend contract.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = Guid.NewGuid().ToString();
            var path = context.Request.Path;
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger.LogError(exception, 
                "Unhandled Exception (CorrelationID: {CorrelationId}) | Path: {Path} | Client IP: {Ip}", 
                correlationId, path, ip);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started for {Path}, cannot write error response", path);
                return;
            }

            var code = HttpStatusCode.InternalServerError;
            var message = "An internal server error occurred.";
            object? errors = null;

            if (exception is Exceptions.BaseException baseEx)
            {
                code = (HttpStatusCode)baseEx.StatusCode;
                message = baseEx.Message;
                errors = baseEx.Errors.Count > 0 ? baseEx.Errors : null;
            }

            context.Response.StatusCode = (int)code;
            context.Response.ContentType = "application/json";

            var responsePayload = ApiResponse.Fail(message, errors);
            
            // For developers, we can inject the stack trace or correlation ID into the response message if preferred.
            // Since this is local active development, adding a correlation ID is extremely useful.
            responsePayload.Message += $" (Ref: {correlationId})";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsJsonAsync(responsePayload, options);
        }
    }
}
