namespace QBCH_lib.core;

/// <summary>
/// Класс ошибки
/// </summary>
public class Error : BaseError
{
    /// <summary>
    /// Метод передачи запроса не соответствует ожидаемому
    /// </summary>
    /// <returns></returns>
    public static Error Code1_WrongRequestMethod() => new(1, "Метод передачи запроса не соответствует ожидаемому");
    /// <summary>
    /// Запрос не содержит данных
    /// </summary>
    /// <returns></returns>
    public static Error Code2_EmptyRequestBody() => new(2, "Запрос не содержит данных");
    /// <summary>
    /// УЭП некорректна
    /// </summary>
    /// <returns></returns>
    public static Error Code4_SignatureIsNotCorrect() => new(4, "УЭП некорректна");
    /// <summary>
    /// Сертификат не найден
    /// </summary>
    /// <returns></returns>
    public static Error Code99_CertificateIsNotFound() => new(99, "Сертификат не найден");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code5_TheCertificateIsExpired() => new(5, "Истек срок сертификата УЭП");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code6_DetailsDoNotMatch() => new(6, "Реквизиты абонента не совпадают");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code9_InvalidRequestByScheme() => new(9, "Запрос не соответствует схеме");
    /// <summary>
    /// 
    /// </summary>
    /// <param name="reqInn"></param>
    /// <param name="storedInn"></param>
    /// <param name="reqOgrn"></param>
    /// <param name="storeOgrn"></param>
    /// <returns></returns>
    public static Error Code10_RequestAndAbonentDataNotMach(string? reqInn, string? storedInn, string? reqOgrn, string? storeOgrn) => new(10, $"Реквизиты запроса не соответствуют реквизитам сертификата: Абонент ИНН:{reqInn}, ИНН в сертификате:{storedInn ?? "{}"}. Абонент ОГРН:{reqOgrn}, ОГРН в сертификате:{storeOgrn ?? "{}"}");
    /// <summary>
    /// 
    /// </summary>
    /// <param name="reqOgrn"></param>
    /// <param name="storeOgrn"></param>
    /// <returns></returns>
    public static Error Code10_CertificateAndBKIOGRNNotMach(string? reqOgrn, string? storeOgrn) => new(10, $"Реквизиты запроса не соответствуют реквизитам сертификата: БКИ ОГРН:{reqOgrn}, ОГРН в сертификате:{storeOgrn ?? "{}"}");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code11_RequestIdIsNotUnique() => new(11, "Идентификатор запроса не уникален");
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static Error Code13_СonsentDenied(string message) => new(13, $"Отсутствует действующее согласие Субъекта: {message}");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code14_SingleWindowDenied() => new(14, "Запрос не доступен для абонента");
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static Error Code15_InvalidRequestData(string message) => new(15, $"Запрос содержит некорректные данные: {message}");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code22_AccessDenied() => new(22, "Запрос не доступен для абонента");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code23_InvalidRequestDate() => new(23, "Дата запроса указана некорректно");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code25_SelfLockedUpError() => new(25, "Сведения о запрете (снятии запрета) не могут быть предоставлены в связи с отсутствием информации об ИНН и (или) результатах проверки ИНН");
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static Error Code26_WrongBlockCount(string message) => new(26, message);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code27_СonsentIsNull() => new(27, "Отсутствует согласие субъекта");
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Error Code28_RequestDataNotFound() => new(28, "В ответе КБКИ отсутствуют запрошенные сведения");
    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <param name="msq"></param>
    public Error(int code, string msq) : base(code, msq) { }
}
