using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MovChat.Core.Logger;
using MovChat.Data.Repositories;
using System;
using System.Threading.Tasks;

namespace MoveleirosChatServer.Utils
{
    public sealed class CustomExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public CustomExceptionHandlerMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<CustomExceptionHandlerMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                try
                {
                    var uow = (UOW)context.RequestServices.GetService(typeof(UOW));
                    var logRep = uow.GetRepository<LogRepository>();

                    await logRep.RecLog(
                        NopLogLevel.Error, 
                        ex.Message,
                        ex.InnerException?.Message ?? "");
                }
                catch (Exception ex2)
                {
                    _logger.LogError(
                        0, ex2,
                        "An exception was thrown attempting " +
                        "to execute the error handler.");
                }

                throw;
            }
        }
    }
}
