using qbch_lib.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Middleware;

/// <summary>
/// Заполняет контрагента в контексте логирования входящего запроса.
/// </summary>
/// <param name="next">Следующий компонент конвейера обработки запроса.</param>
public sealed class CounterpartyLoggingContextMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Определяет имя контрагента из клиентского сертификата один раз перед обработкой запроса.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    /// <param name="logContext">Общие для запроса свойства логирования.</param>
    /// <returns>Задача обработки запроса.</returns>
    public async Task InvokeAsync(HttpContext httpContext, LoggingContext logContext)
    {
        var certificate = await httpContext.Connection.GetClientCertificateAsync(httpContext.RequestAborted);
        logContext.Counterparty = certificate?.GetNameInfo(X509NameType.SimpleName, false);

        await next(httpContext);
    }
}
