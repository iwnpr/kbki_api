using QBCH_lib.CommonTypes.Api;
using QBCH_lib.domain.aggregate;
using QBCHService_lib.Models;

namespace QBCHService_lib.Services.Interfaces.V3;

/// <summary>
/// Операции обработки запросов КБКИ по API версии 3.
/// </summary>
public interface IQBCHServiceV3
{
    /// <summary>
    /// Формирует ответ на запрос сведений, используя данные из локальной базы.
    /// </summary>
    /// <param name="processing">Транзакция обработки с исходным запросом и состоянием валидации.</param>
    /// <returns>Результат обработки с заполненным ответом КБКИ или признаком ошибки.</returns>
    Task<QBCHTaskResult> RequestFromDB(QBCHProcessingTransaction processing);

    /// <summary>
    /// Отправляет запрос во внешнее бюро и возвращает итоговый ответ API v3.
    /// </summary>
    /// <param name="processing">Транзакция обработки с данными запроса.</param>
    /// <param name="client">HTTP-клиент для взаимодействия с внешним сервисом бюро.</param>
    /// <param name="bureau">Реквизиты целевого бюро кредитных историй.</param>
    /// <returns>Результат обработки, содержащий ответ бюро или информацию об ошибке.</returns>
    Task<QBCHTaskResult> RequestFromExternalBureau(QBCHProcessingTransaction processing, HttpClient client, QBCHRequisite bureau);
}