using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace qbch_lib.Diagnostics;

/// <summary>
/// Заполняет свойства контекста логирования перед обработкой HTTP-запроса.
/// </summary>
public interface ILoggingContextInitializer
{
    /// <summary>
    /// Инициализирует контекст один раз в начале запроса.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    /// <param name="logContext">Заполняемые свойства логирования.</param>
    /// <returns>Задача инициализации контекста.</returns>
    Task InitializeAsync(HttpContext httpContext, LoggingContext logContext);
}
