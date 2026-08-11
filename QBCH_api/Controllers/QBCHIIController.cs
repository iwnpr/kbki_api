using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QBCH_lib.CommonTypes.Api;

namespace QBCH_api.Controllers;

/// <summary>
/// 
/// </summary>
/// <param name="logger"></param>
[ApiVersion("2.0")]
[Route("v{version:apiVersion}")]
[ApiController]
public class QBCHIIController(ILogger<QBCHIIController> logger) : ControllerBase
{
    private readonly ILogger<QBCHIIController> _logger = logger;

    /// <summary>
    /// Момент, начиная с которого методы версии API v2 недоступны (01.08.2026 00:00 по московскому времени, UTC+3).
    /// </summary>
    private static readonly DateTimeOffset V2UnavailableFrom = new(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(3));

    /// <summary>
    /// Запрос сведений о среднемесячных платежах Субъекта.
    /// </summary>
    /// <remarks>
    /// 
    /// Для снижения нагрузки в квитанцию, содержащую идентификатор ответа, сервер может поместить атрибут «ВремяГотовности», указав в качестве значения время(в миллисекундах), требующееся серверу на подготовку ответа.
    /// При наличии атрибута «ВремяГотовности» клиент должен обращаться за получением сведений о среднемесячных платежах Субъекта не ранее, чем по истечении времени, указанного в атрибуте.
    ///
    /// </remarks>
    /// <response code="200">Результат запроса содержит сведения о среднемесячных платежах Субъекта.</response>
    /// <response code="202">Результат запроса содержит квитанцию с идентификатором ответа.</response>
    /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке.</response>
    /// <response code="404">Метод недоступен с 01.08.2026. Необходимо использовать v3/dlrequest.</response>
    [HttpPost("dlrequest")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> DlRequest_v_2(ApiVersion apiVersion)
    {
        var v3Endpoint = "dlrequest";

        return BuildV2UnavailableResult(v3Endpoint);

    }

    /// <summary>
    /// получение сведений о среднемесячных платежах Субъекта по идентификатору ответа
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <remarks>
    /// В случае получения ошибки «Ответ не готов» клиент должен повторить запрос не ранее, чем через 1 секунду
    /// </remarks>
    /// <response code="200">Результат запроса содержит сведения о среднемесячных платежах Субъекта.</response>
    /// <response code="202">результат запроса содержит квитанцию с информацией об ошибке «Ответ не готов».</response>
    /// <response code="400">результат запроса содержит квитанцию с информацией об ошибке, кроме ошибки «Ответ не готов»</response>
    /// <response code="404">Метод недоступен с 01.08.2026. Необходимо использовать v3/dlanswer.</response>
    [HttpGet("dlanswer")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> DlAnswer_v_2(string? id = null)
    {
        var v3Endpoint = "dlanswer";

        return BuildV2UnavailableResult(v3Endpoint);
    }

    /// <summary>
    /// Передача от БКИ данных, необходимых для формирования и предоставления пользователям кредитных историй сведений о среднемесячных платежах Субъекта.
    /// </summary>
    /// <returns>Результат запроса – информация о результатах загрузки данных в базу данных КБКИ</returns>
    /// <response code="200">Результат запроса содержит информацию о результатах загрузки данных в базу данных КБКИ</response>
    /// <response code="202">Результат запроса содержит квитанцию с идентификатором ответа</response>
    /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке</response>
    /// <response code="404">Метод недоступен с 01.08.2026. Необходимо использовать v3/dlput.</response>
    [MapToApiVersion("2.0")]
    [HttpPost("dlput")]
    public async Task<IActionResult> DlPut_v_2(ApiVersion apiVersion)
    {
        var v3Endpoint = "dlput";

        return BuildV2UnavailableResult(v3Endpoint);
    }

    /// <summary>
    /// Получение информации о результатах загрузки данных
    /// </summary>
    /// <param name="version"></param>
    /// <param name="id">Значение идентификатора ответа, содержащегося в квитанции, полученной при передаче данных о среднемесячных платежах Субъекта</param>
    /// <returns>Информация о результатах загрузки данных в базу данных КБКИ</returns>
    /// <response code="200">Результат запроса содержит информацию о результатах загрузки данных в базу данных КБКИ</response>
    /// <response code="202">Результат запроса содержит квитанцию с информацией об ошибке «Ответ не готов»</response>
    /// <response code="400">Результат запроса содержит квитанцию с информацией об ошибке, кроме ошибки «Ответ не готов»</response>
    /// <response code="404">Метод недоступен с 01.08.2026. Необходимо использовать v3/dlputanswer.</response>
    /// <remarks>
    /// В случае получения ошибки «Ответ не готов» абонент должен повторить запрос не ранее, чем через 1 секунду.
    /// </remarks>
    [HttpGet("dlputanswer")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> DlPutAnswer(ApiVersion version, string? id = null)
    {
        var v3Endpoint = "dlputanswer";

        return BuildV2UnavailableResult(v3Endpoint);
    }

    /// <summary>
    /// Добавление нового сертификата абонента
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Квитанция содержит информацию об успешной обработке запроса</response>
    /// <response code="400">Квитанция содержит информацию об ошибке</response>
    /// <response code="404">Метод недоступен с 01.08.2026. Необходимо использовать v3/certadd.</response>
    [HttpPost("certadd")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> CertAdd([FromForm] CertForm form)
    {
        var v3Endpoint = "certadd";

        return BuildV2UnavailableResult(v3Endpoint);
    }

    /// <summary>
    /// Отзыв сертификата абонента
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <response code="200">Квитанция содержит информацию об успешной обработке запроса</response>
    /// <response code="400">Квитанция содержит информацию об ошибке</response>
    /// <response code="404">Метод недоступен с 01.08.2026. Необходимо использовать v3/certrevoke.</response>
    [HttpPost("certrevoke")]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> CertRevoke([FromForm] CertForm form)
    {
        var v3Endpoint = "certrevoke";

        return BuildV2UnavailableResult(v3Endpoint);
    }

    /// <summary>
    /// Возвращает ответ 404 «Недоступно с 01.08.2026», если версия API v2 уже недоступна,
    /// либо <c>null</c>, если обращение ещё допустимо (текущий момент раньше порога).
    /// </summary>
    /// <param name="v3Endpoint">Имя эквивалентного метода версии v3, который следует использовать (например, «dlrequest»).</param>
    private IActionResult? BuildV2UnavailableResult(string v3Endpoint)
    {
        if (DateTimeOffset.UtcNow < V2UnavailableFrom)
            return null;

        _logger.LogWarning("Обращение к недоступному методу v2/{Endpoint} после 01.08.2026", v3Endpoint);

        return new ContentResult
        {
            StatusCode = StatusCodes.Status404NotFound,
            Content = $"Недоступно с 01.08.2026. Используйте v3/{v3Endpoint}",
            ContentType = "text/plain; charset=utf-8"
        };
    }
}
