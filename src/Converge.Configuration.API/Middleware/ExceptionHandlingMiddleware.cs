using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Converge.Configuration.Application.Exceptions;

namespace Converge.Configuration.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ConfigurationAlreadyExistsException ex)
            {
                await WriteError(context, StatusCodes.Status409Conflict, "ConfigurationAlreadyExists", ex.Message);
            }
            catch (VersionConflictException ex)
            {
                await WriteError(context, StatusCodes.Status409Conflict, "VersionConflict", ex.Message);
            }
            catch (ArgumentException ex)
            {
                await WriteError(context, StatusCodes.Status400BadRequest, "InvalidRequest", ex.Message);
            }
            catch (Exception)
            {
                await WriteError(context, StatusCodes.Status500InternalServerError, "InternalServerError", "An unexpected error occurred.");
            }
        }

        private static async Task WriteError(HttpContext context, int statusCode, string errorCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();

            var response = new
            {
                error = errorCode,
                message,
                correlationId
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
