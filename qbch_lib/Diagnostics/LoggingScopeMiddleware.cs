using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Serilog.Context;
using System.Security.Cryptography.X509Certificates;

namespace qbch_lib.Diagnostics;

/// <summary>
/// Подключает свойства контекста логирования на время обработки HTTP-запроса.
/// </summary>
/// <param name="next">Следующий компонент конвейера обработки запроса.</param>
public sealed class LoggingScopeMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Выполняет обработку запроса внутри контекста логирования.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    /// <param name="logContext">Общие для запроса свойства логирования.</param>
    /// <returns>Задача, представляющая обработку запроса.</returns>
    public async Task InvokeAsync(HttpContext httpContext, LoggingContext logContext)
    {
        var certificate = await httpContext.Connection.GetClientCertificateAsync(httpContext.RequestAborted);
        logContext.Counterparty = certificate?.GetNameInfo(X509NameType.SimpleName, false);

        using (LogContext.Push(logContext))
        {
            await next(httpContext);
        }
    }
}