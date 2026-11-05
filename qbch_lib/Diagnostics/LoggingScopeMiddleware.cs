using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Serilog.Context;

namespace qbch_lib.Diagnostics;

/// <summary>
/// Подключает свойства контекста логирования на время обработки HTTP-запроса.
/// </summary>
/// <param name="next">Следующий компонент конвейера обработки запроса.</param>
/// <param name="initializeContext">Необязательная функция заполнения свойств контекста.</param>
public sealed class LoggingScopeMiddleware(RequestDelegate next, Func<HttpContext, LoggingContext, Task>? initializeContext = null)
{
    /// <summary>
    /// Выполняет обработку запроса внутри контекста логирования.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    /// <param name="logContext">Общие для запроса свойства логирования.</param>
    /// <returns>Задача, представляющая обработку запроса.</returns>
    public async Task InvokeAsync(HttpContext httpContext, LoggingContext logContext)
    {
        using (LogContext.Push(logContext))
        {
            if (initializeContext is not null)
                await initializeContext(httpContext, logContext);

            await next(httpContext);
        }
    }
}