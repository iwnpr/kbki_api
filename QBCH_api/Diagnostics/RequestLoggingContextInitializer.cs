using qbch_lib.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace QBCH_api.Diagnostics;

/// <summary>
/// Заполняет свойства логирования, определяемые из данных входящего запроса API.
/// </summary>
public sealed class RequestLoggingContextInitializer : ILoggingContextInitializer
{
    /// <summary>
    /// Определяет имя контрагента из клиентского сертификата один раз перед обработкой запроса.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    /// <param name="logContext">Общие для запроса свойства логирования.</param>
    /// <returns>Задача инициализации контекста.</returns>
    public async Task InitializeAsync(HttpContext httpContext, LoggingContext logContext)
    {
        var certificate = await httpContext.Connection.GetClientCertificateAsync(httpContext.RequestAborted);
        logContext.Counterparty = certificate?.GetNameInfo(X509NameType.SimpleName, false);
    }
}
