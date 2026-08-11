using QBCH_lib.core;

namespace qbch_lib.domain.errors;

//NOTE: Переименовал во избежание конфликтов с классами Error из других сборок
/// <summary>
/// Класс ошибки
/// </summary>
public class AnswerErrorCode : BaseError
{
    public AnswerErrorCode(int code, string msq) : base(code, msq) { }

    /// <summary>
    /// Метод передачи запроса не соответствует ожидаемому
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code1_WrongRequestMethod() => new(1, "Метод передачи запроса не соответствует ожидаемому");

    /// <summary>
    /// Запрос не содержит данных
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code2_EmptyRequestBody() => new(2, "Запрос не содержит данных");

    /// <summary>
    /// Запрос не содержит обязательных параметров
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code3_EmptyRequiredParameters(string parameterName) => new(3, $"Запрос не содержит обязательных параметров: {parameterName}");

    /// <summary>
    /// УЭП некорректна
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code4_SignatureIsNotCorrect() => new(4, "УЭП некорректна");

    /// <summary>
    /// Истек срок сертификата УЭП
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code5_TheCertificateIsExpired() => new(5, "Истек срок сертификата УЭП");

    /// <summary>
    /// Реквизиты абонента не совпадают
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code6_DetailsDoNotMatch(string description) => new(6, $"Реквизиты абонента не совпадают: {description}");

    /// <summary>
    /// Некорректный формат запроса
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code7_IncorrectRequestFormat() => new(7, "Некорректный формат запроса");

    //TODO: В описании ошибки должно быть продублировано название неподдерживаемой кодировки
    /// <summary>
    /// Неподдерживаемая кодировка
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code8_UnsupportedEncoding() => new(8, "Неподдерживаемая кодировка");

    /// <summary>
    /// Запрос не соответствует схеме
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code9_InvalidRequestByScheme() => new(9, "Запрос не соответствует схеме");

    /// <summary>
    /// Запрос не соответствует схеме с указанием причины несоответствия
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code9_InvalidRequestByScheme(string reason) => new(9, $"Запрос не соответствует схеме:\r\n{reason}");

    /// <summary>
    /// Реквизиты запроса не соответствуют абоненту
    /// </summary>
    /// <param name="reqInn"></param>
    /// <param name="storedInn"></param>
    /// <param name="reqOgrn"></param>
    /// <param name="storeOgrn"></param>
    /// <returns></returns>
    public static AnswerErrorCode Code10_RequestAndAbonentDataNotMach(string? reqInn, string? storedInn, string? reqOgrn, string? storeOgrn) => new(10, $"Реквизиты запроса не соответствуют реквизитам сертификата: Абонент ИНН:{reqInn}, ИНН в сертификате:{storedInn ?? "{}"}. Абонент ОГРН:{reqOgrn}, ОГРН в сертификате:{storeOgrn ?? "{}"}");

    /// <summary>
    /// Идентификатор запроса не уникален
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code11_RequestIdIsNotUnique() => new(11, "Идентификатор запроса не уникален");

    /// <summary>
    /// Ответ не готов
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code12_ResponseIsIncomplete() => new(12, "Ответ не готов");

    /// <summary>
    /// Отсутствует действующее согласие субъекта
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static AnswerErrorCode Code13_СonsentDenied(string message) => new(13, $"Отсутствует действующее согласие Субъекта: {message}");

    /// <summary>
    /// Запрос не доступен для абонента
    /// </summary>
    public static AnswerErrorCode Code14_SingleWindowDenied() => new(14, "Взаимодействие с абонентом в режиме «одно окно» не предусмотрено договором");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public static AnswerErrorCode Code15_InvalidRequestData(string message) => new(15, $"Запрос содержит некорректные данные: {message}");

    /// <summary>
    /// Указан некорректный идентификатор ответа
    /// </summary>
    public static AnswerErrorCode Code16_InvalidRequestId() => new(16, "Указан некорректный идентификатор ответа");

    /// <summary>
    /// Не удалось установить соединение
    /// </summary>
    public static AnswerErrorCode Code17_NoConnection() => new(17, "Не удалось установить соединение");

    /// <summary>
    /// Время ожидания ответа истекло
    /// </summary>
    public static AnswerErrorCode Code18_WaitForResponseExpired() => new(18, "Время ожидания ответа истекло");

    /// <summary>
    /// Ответ не соответствует схеме
    /// </summary>
    public static AnswerErrorCode Code19_NotMatchTheScheme() => new(19, "Ответ не соответствует схеме");

    /// <summary>
    /// Договор с указанным УИД не найден
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code20_ContractNotFound_V2() => new(20, "Договор с указанным УИД не найден");

    /// <summary>
    /// Договор субъекта с указанным УИД не найден
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code20_ContractNotFound_V3() => new(20, "Договор субъекта с указанным УИД не найден");

    /// <summary>
    /// Сведения о величине среднемесячного платежа по договору и дате его расчета не найдены
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code21_CalculationDateNotFound() => new(21, "Сведения о величине среднемесячного платежа по договору и дате его расчета не найдены");

    /// <summary>
    /// Запрос не доступен для абонента
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code22_AccessDenied() => new(22, "Запрос не доступен для абонента");

    /// <summary>
    /// Дата запроса указана некорректно
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code23_InvalidRerquestDate() => new(23, "Дата запроса указана некорректно");

    /// <summary>
    /// Ошибка при проверке УКЭП
    /// </summary>
    public static AnswerErrorCode Code24_UKEPVerificationError() => new(24, "Ошибка при проверке УКЭП");

    /// <summary>
    /// Сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code25_SelfLockedUpError_V2() => new(25, "Сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН");

    /// <summary>
    /// Сведения для предупреждения мошенничества, сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code25_SelfLockedUpError_V3() => new(25, "Сведения для предупреждения мошенничества, сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН");

    /// <summary>
    /// Количество блоков «Запрос» не соответствует режиму запроса
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static AnswerErrorCode Code26_WrongBlockCount() => new(26, "Количество блоков «Запрос» не соответствует режиму запроса");

    /// <summary>
    /// Отсутствует согласие субъекта
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code27_СonsentIsNull() => new(27, "Отсутствует согласие субъекта");

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code28_RequestDataNotFound() => new(28, "В ответе КБКИ отсутствуют запрошенные сведения");

    /// <summary>
    /// В запросе /dlput в блоке «Договор» или «ОбращениеОбязательство» с вложенным блоком «Удалить»
    /// указаны сведения о Субъекте, информация о котором ранее не передавалась абонентом.
    /// </summary>
    public static AnswerErrorCode Code29_SubjectNotFound() => new(29, "Субъект не найден");

    /// <summary>
    /// В запросе /dlput в атрибуте «УИД» для обращения (обязательства) с операцией «Удалить»
    /// указан УИД обращения (договора (сделки)), информация о котором ранее не передавалась
    /// абонентом для указанного Субъекта.
    /// </summary>
    public static AnswerErrorCode Code30_AppealObligationNotFound() => new(30, "Обращение (обязательство) с указанным УИД не найдено");

    /// <summary>
    /// В запросе /dlput в атрибуте «СтадияРассмотрения» для обращения (обязательства)
    /// с операцией «Удалить» указана стадия, информация о которой ранее не передавалась
    /// абонентом для указанного обращения (обязательства).
    /// </summary>
    public static AnswerErrorCode Code31_AntiFraudDataNotFound() => new(31, "Сведения для предупреждения мошенничества не найдены");

    /// <summary>
    /// Другая ошибка
    /// </summary>
    /// <returns></returns>
    public static AnswerErrorCode Code99_OtherError(string errorMessage) => new(99, errorMessage);
}
