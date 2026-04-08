namespace QBCH_api.Configuration;

/// <summary>
/// Настройки контрактных ограничений API 3.0.
/// Позволяют переопределить значения по умолчанию из конфигурации приложения.
/// </summary>
public class ApiV3ContractOptions
{
    /// <summary>
    /// Имя секции конфигурации, из которой считываются настройки контрактных ограничений API 3.0.
    /// </summary>
    public const string SectionName = "ApiV3Contract";

    /// <summary>
    /// Максимальное количество блоков <c>Запрос</c>, допустимое в одном пакетном запросе <c>/dlrequest</c>.
    /// По умолчанию соответствует контрактному лимиту API 3.0.
    /// </summary>
    public int MaxDlRequestBatchSize { get; init; } = ApiV3ContractDefaults.MaxDlRequestBatchSize;

    /// <summary>
    /// Максимальное количество сущностей, допустимое в одном запросе <c>/dlput</c>.
    /// Лимит применяется к суммарному числу договоров и обращений/обязательств.
    /// </summary>
    public int MaxDlPutEntities { get; init; } = ApiV3ContractDefaults.MaxDlPutEntities;

    /// <summary>
    /// Минимальный интервал между повторными запросами <c>/dlanswer</c> или <c>/dlputanswer</c>
    /// после получения квитанции с ошибкой «Ответ не готов».
    /// Значение задаётся в секундах.
    /// </summary>
    public int MinAnswerPollingIntervalSeconds { get; init; } = ApiV3ContractDefaults.MinAnswerPollingIntervalSeconds;

    /// <summary>
    /// Время, в течение которого готовый ответ должен оставаться доступным для получения
    /// по идентификатору ответа.
    /// Значение задаётся в часах.
    /// </summary>
    public int ResponseRetentionHours { get; init; } = ApiV3ContractDefaults.ResponseRetentionHours;

    /// <summary>
    /// Предельное время, в течение которого сервис пытается отдать готовый результат без квитанции.
    /// После превышения этого времени допустим переход на асинхронную модель с ИдентификаторомОтвета.
    /// Значение задаётся в секундах.
    /// </summary>
    public int ImmediateResponseDeadlineSeconds { get; init; } = ApiV3ContractDefaults.ImmediateResponseDeadlineSeconds;

    /// <summary>
    /// Таймаут исходящих HTTP-вызовов к другим КБКИ.
    /// Значение задаётся в секундах.
    /// </summary>
    public int HttpClientTimeoutSeconds { get; init; } = ApiV3ContractDefaults.HttpClientTimeoutSeconds;
}
