using Asp.Versioning;
using Cache_lib.Interfaces;
using Crypto_lib.Model;
using Crypto_lib.Service;
using QBCH_api.Services.Interfaces;
using Qbch_db_lib.Services.Interfaces;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v1_3.Enums;
using QBCH_lib.qcb_xml.v1_3.qcb_request;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.qcb_xml.v3_0.qcb_request;
using QBCH_lib.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using XmlService_lib.Services.Interfaces;

namespace QBCH_api.Services.Implementations
{

    /// <summary>
    /// Сервис валидации
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    public class ValidationService(
        IXmlService xmlService,
        ICryptoService cryptoService,
        IRepository qbch_db,
        ILogger<ValidationService> logger,
        ICacheService cache,
        ITicketService ticketService,
        IRepository repository) : IValidationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="nameOfController"></param>
        /// <param name="apiVersion"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ValidateXml(MemoryStream memoryStream, string nameOfController, ApiVersion apiVersion, [NotNullWhen(false)] out BaseResult? result)
        {
            return xmlService.ValidateXml(memoryStream, nameOfController, apiVersion.ToString(), out result);
        }

        /// <summary>
        /// Валидация кодировки
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ValidateEncoding(byte[] message, [NotNullWhen(false)] out BaseResult? result)
        {
            try
            {
                var encoding = new UTF8Encoding(false, true);
                encoding.GetCharCount(message);
            }
            catch (DecoderFallbackException ex)
            {
                logger.LogError(ex, "Не в UTF-8");

                result = new()
                {
                    Error = "Неподдерживаемая кодировка, файл не в кодировке Utf-8",
                    ErrorCode = 8,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "8", "Неподдерживаемая кодировка, файл не в кодировке Utf-8")
                };
                return false;
            }

            result = null;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestDate"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ValidateRequestDate(DateTime? requestDate, [NotNullWhen(false)] out BaseResult? result)
        {
            if (requestDate != DateTime.Today)
            {
                logger.LogError("Дата запроса указана некорректно");
                result = new()
                {
                    Error = "Дата запроса указана некорректно",
                    ErrorCode = 23,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "23", "Дата запроса указана некорректно")
                };
                return false;
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Проверка подписи файла
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="requestCertificate">Сертификат из запроса</param>
        /// <param name="result"></param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        public bool ValidateMsg(byte[] msg, X509Certificate2? requestCertificate, out CryptoServiceResult result, byte[]? encodedSignature = null)
        {
            return cryptoService.ValidateMsg(msg, requestCertificate, out result, encodedSignature);
        }

        /// <summary>
        /// Валидация сертификата
        /// </summary>
        /// <param name="requestCert">Сертификат из запроса</param>
        /// <param name="result">Результат</param>
        /// <returns></returns>
        public bool ValidateCertificate(X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult? result)
        {
            return cryptoService.ValidateCertificate(requestCert, out result);
        }

        /// <summary>
        /// Валидация запроса
        /// </summary>
        /// <param name="thumbprint">Отпечаток сертификата</param>
        /// <param name="inn">ИНН</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="result"></param>
        /// <returns>Результат проверки</returns>
        public bool AbonentValidation(string? thumbprint, string? inn, string? ogrn, out AbonentValidatationResult result)
        {
            XElement? xInnOgrnDb = qbch_db.GetInnOgrnByThumbprint_old(thumbprint?.ToUpper()).Result;
            var dbinn = xInnOgrnDb?.Element("inn")?.Value;
            var dbogrn = xInnOgrnDb?.Element("ogrn")?.Value;

            if (xInnOgrnDb is null || dbinn != inn || dbogrn != ogrn)
            {
                logger.LogError("Сертификат в запросе не соответствует реквизитам в файле.");
                result = new(xInnOgrnDb)
                {
                    Error = "Реквизиты запроса не соответствуют абоненту",
                    ErrorCode = 10,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "10", $"Реквизиты запроса не соответствуют абоненту: Абонент ИНН:{inn}, ожидаемый ИНН:{dbinn}. Абонент ОГРН:{ogrn}, ожидаемый ОГРН:{dbogrn}")
                };
                return false;
            }

            result = new(xInnOgrnDb);
            return true;
        }

        /// <summary>
        /// Валидация запроса на добавление данных
        /// </summary>
        /// <param name="thumbprint">Отпечаток</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool AbonentValidation(string? thumbprint, string? ogrn, out AbonentValidatationResult result)
        {
            var xInnOgrnDb = qbch_db.GetInnOgrnByThumbprint(thumbprint).Result;
            var dbogrn = xInnOgrnDb?.Element("ogrn")?.Value;

            if (string.IsNullOrWhiteSpace(ogrn) || dbogrn != ogrn)
            {
                logger.LogError("Сертификат в запросе не соответствует реквизитам в файле.");
                result = new(xInnOgrnDb)
                {
                    Error = "Реквизиты запроса не соответствуют абоненту",
                    ErrorCode = 10,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "10", $"Реквизиты запроса не соответствуют абоненту: Абонент ОГРН:{ogrn}, ожидаемый ОГРН:{dbogrn}")
                };
                return false;
            }

            result = new(xInnOgrnDb);
            return true;
        }

        /// <summary>
        /// Проверка согласия
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result"></param>
        /// <returns>Результат проверки</returns>
        public bool ValidateAgreement(ЗапросСведенийОПлатежах? request, [NotNullWhen(false)] out BaseResult? result)
        {
            string? error = null;

            /* Проверка даты окончания действия согласия
                Указанная дата выдачи согласия плюс срок
                действия согласия более ранняя, чем текущая
                дата; реквизиты лица, указанные в блоке
                «Выдано», не соответствуют реквизитам лица,
                указанным в блоке «Источник»; одна или
                несколько целей, указанных в блоке «Запрос»
                отсутствует, в блоке «Согласие»
             */
            var AgreementDate = request?.Запрос?.Согласие?.ДатаВыдачи;

            // Дата выдачи согласия не заполнена
            if (!AgreementDate.HasValue)
            {
                logger.LogError("Отсутствует дата согласия.");
                result = new()
                {
                    ErrorCode = 13,
                    Error = "Отсутствует дата согласия.",
                    Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Отсутствует дата согласия.")
                };

                return false;
            }

            // Дата выдачи согласия больше текущей
            if (AgreementDate > DateTime.Today)
            {
                error = $"Дата выдачи согласия {AgreementDate:dd.MM.yyyy} больше текущей.";
                logger.LogError("Дата выдачи согласия {AgreementDate} больше текущей.", AgreementDate?.ToString("dd.MM.yyyy"));
                result = new()
                {
                    Error = error,
                    ErrorCode = 15,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                };
                return false;
            }

            switch (request?.Запрос?.Согласие?.СрокДействия)
            {
                // 6 Месяцев со дня оформления
                case КодыСрокаСогласия.I1:
                    if (DateTime.Today >= AgreementDate.Value.AddMonths(6).AddDays(1))
                    {
                        result = new()
                        {
                            ErrorCode = 13,
                            Error = "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты",
                            Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты")
                        };

                        return false;
                    }
                    break;
                // 12 месяцев со дня офрмления
                case КодыСрокаСогласия.I2:
                    if (DateTime.Today >= AgreementDate.Value.AddMonths(12).AddDays(1))
                    {
                        result = new()
                        {
                            ErrorCode = 13,
                            Error = "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты",
                            Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты")
                        };

                        return false;
                    }
                    break;
                /* В течение срока действия согласия с субъектом кредитной истории были
                    * заключены договор займа(кредита), договор лизинга, договор залога, договор
                    * поручительства, выдана независимая гарантия
                */
                case КодыСрокаСогласия.I3:
                    // Если код 3 то договор обязателен
                    if (request?.Запрос?.Согласие?.Договор is null)
                    {
                        result = new()
                        {
                            ErrorCode = 15,
                            Error = "Элемент \"Договор\" обязателен т.к. значение атрибута \"СрокДействия\"=\"3\"",
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", "Элемент \"Договор\" обязателен т.к. значение атрибута \"СрокДействия\"=\"3\"")
                        };
                        return false;
                    }

                    var contractDate = request?.Запрос?.Согласие?.Договор.Дата;

                    // Дата выдачи согласия больше даты договора
                    if (AgreementDate > contractDate)
                    {
                        error = $"Дата выдачи согласия {AgreementDate:dd.MM.yyyy} больше даты договора {contractDate:dd.MM.yyyy}";
                        logger.LogError("Дата выдачи согласия {AgreementDate} больше даты договора {contractDate}", AgreementDate?.ToString("dd.MM.yyyy"), contractDate?.ToString("dd.MM.yyyy"));
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }

                    if (contractDate > DateTime.Today)
                    {
                        error = $"Дата договора {contractDate:dd.MM.yyyy} больше текущей";
                        logger.LogError("Дата договора {AgreementDate} больше текущей", contractDate?.ToString("dd.MM.yyyy"));
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }
                    break;
                // Код не найден
                default:
                    result = new()
                    {
                        ErrorCode = 13,
                        Error = $"Значение \"Срок действия\" блока \"Согласие\" {request?.Запрос?.Согласие?.СрокДействия}, не найдено в справочнике.",
                        Ticket = ticketService.CreateResult(ResponseType.Error, "13", $"Значение \"Срок действия\" блока \"Согласие\" {request?.Запрос?.Согласие?.СрокДействия}, не найдено в справочнике.")
                    };

                    return false;
            }

            // Наличие оснвоания для передачи согласия другому лицу
            bool HasBasement = request?.Запрос?.Согласие?.TransferBasementSpecified ?? false;

            // ИНН из согласия
            string? innAgreement =
                request?.Запрос?.Согласие?.Выдано?.ЮридическоеЛицо?.ИНН ??
                request?.Запрос?.Согласие?.Выдано?.ИндивидуальныйПредприниматель?.ИНН ??
                request?.Запрос?.Согласие?.Выдано?.ИностранноеЮЛ?.ИНН ??
                request?.Запрос?.Согласие?.Выдано?.ИностранныйПредприниматель?.ИНН;

            // ОГРН из согласия
            string? ogrnAgreement =
                request?.Запрос?.Согласие?.Выдано?.ЮридическоеЛицо?.ОГРН ??
                request?.Запрос?.Согласие?.Выдано?.ИндивидуальныйПредприниматель?.ОГРН ??
                request?.Запрос?.Согласие?.Выдано?.ИностранноеЮЛ?.ОГРН ??
                request?.Запрос?.Согласие?.Выдано?.ИностранныйПредприниматель?.ОГРН;

            if (string.IsNullOrWhiteSpace(innAgreement))
            {
                result = new()
                {
                    ErrorCode = 13,
                    Error = "В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие.",
                    Ticket = ticketService.CreateResult(ResponseType.Error, "13", "В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие.")
                };
                return false;
            }
            if (string.IsNullOrWhiteSpace(ogrnAgreement))
            {
                result = new()
                {
                    ErrorCode = 13,
                    Error = "Отсутствуют реквизиты лица, которому было выдано согласие.",
                    Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Отсутствуют реквизиты лица, которому было выдано согласие.")
                };
                return false;
            }

            // ИНН источника
            string? innSource =
               request?.Запрос?.Источник?.ЮридическоеЛицо?.ИНН ??
               request?.Запрос?.Источник?.ИндивидуальныйПредприниматель?.ИНН ??
               request?.Запрос?.Источник?.ИностранныйПредприниматель?.ИНН ??
               request?.Запрос?.Источник?.ИностранноеЮЛ?.ИНН;

            // ОГРН Источника
            string? ogrnSource =
                request?.Запрос?.Источник?.ЮридическоеЛицо?.ОГРН ??
                request?.Запрос?.Источник?.ИндивидуальныйПредприниматель?.ОГРН ??
                request?.Запрос?.Источник?.ИностранныйПредприниматель?.ОГРН ??
                request?.Запрос?.Источник?.ИностранноеЮЛ?.ОГРН;

            bool ComapreINN = innAgreement == innSource;
            bool CompareOGRN = ogrnAgreement == ogrnSource;

            // Есть основание
            if (HasBasement)
            {
                // ИНН совпадает - ошибка
                if (ComapreINN)
                {
                    error = $"Запрос содержит некорректные данные: При наличии в согласии атрибута \"ОснованиеПередачи\" ИНН ({innAgreement}) лица, которому было выдано согласие, не должен совпадать с ИНН ({innSource}) источника.";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
                // ОГРН совпадает - ошибка
                else if (CompareOGRN)
                {
                    error = $"Запрос содержит некорректные данные: При наличии в согласии атрибута \"ОснованиеПередачи\" ОГРН ({ogrnAgreement}) лица, которому было выдано согласие, не должен совпадать с ОГРН ({ogrnSource}) источника.";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
            }
            else
            {
                // ИНН не совпадает - ошибка
                if (!ComapreINN)
                {
                    error = $"Запрос содержит некорректные данные: ИНН ({innAgreement}) лица, которому было выдано согласие, должен совпадать с ИНН источника ({innSource}).";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
                // ОГРН не совпадает - ошибка
                else if (!CompareOGRN)
                {
                    error = $"Запрос содержит некорректные данные: ОГРН ({ogrnAgreement}) лица, которому было выдано согласие, должен совпадать с ОГРН ({ogrnSource}) источника.";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
            }

            // Если у цели 99 нет описания
            if (request?.Запрос?.Цель?.Any(x => x.КодЦели == QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
            {
                error = $"Запрос содержит некорректные данные: Код цели запроса со значением \"99\" не содержит описания.";
                result = new()
                {
                    ErrorCode = 15,
                    Error = error,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                };
                return false;
            }

            // Если в согласии у цели 99 нет описания
            if (request?.Запрос?.Согласие?.Цель?.Any(x => x.КодЦели == QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
            {
                error = $"Запрос содержит некорректные данные: Код цели согласия со значением \"99\" не содержит описания.";
                result = new()
                {
                    ErrorCode = 15,
                    Error = error,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                };
                return false;
            }


            //  Проверка кодов цели запроса Одна или несколько целей запроса отсутствует в согласии
            for (int i = 0; i < request?.Запрос?.Цель?.Count; i++)
            {
                if (!request?.Запрос.Согласие?.Цель?.Any(x => x.КодЦели == request.Запрос.Цель[i].КодЦели) ?? false)
                {
                    result = new()
                    {
                        ErrorCode = 13,
                        Error = "Одна или несколько целей, указанных в блоке «Запрос» отсутствует.",
                        Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Одна или несколько целей, указанных в блоке «Запрос» отсутствует.")
                    };

                    return false;
                }
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Проверка согласия
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result"></param>
        /// <returns>Результат проверки</returns>
        public bool ValidateAgreement(ЗапросСведенийЗапрос request, [NotNullWhen(false)] out BaseResult? result) //3.0
        {
            string? error = null;

            /* Проверка даты окончания действия согласия
                Указанная дата выдачи согласия плюс срок
                действия согласия более ранняя, чем текущая
                дата; реквизиты лица, указанные в блоке
                «Выдано», не соответствуют реквизитам лица,
                указанным в блоке «Источник»; одна или
                несколько целей, указанных в блоке «Запрос»
                отсутствует, в блоке «Согласие»
             */
            var AgreementDate = request?.Согласие?.ДатаВыдачи;

            // Дата выдачи согласия не заполнена
            if (!AgreementDate.HasValue)
            {
                logger.LogError("Отсутствует дата согласия.");
                result = new()
                {
                    ErrorCode = 13,
                    Error = "Отсутствует дата согласия.",
                    Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Отсутствует дата согласия.")
                };

                return false;
            }

            // Дата выдачи согласия больше текущей
            if (AgreementDate > DateTime.Today)
            {
                error = $"Дата выдачи согласия {AgreementDate:dd.MM.yyyy} больше текущей.";
                logger.LogError("Дата выдачи согласия {AgreementDate} больше текущей.", AgreementDate?.ToString("dd.MM.yyyy"));
                result = new()
                {
                    Error = error,
                    ErrorCode = 15,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                };
                return false;
            }

            switch (request?.Согласие?.СрокДействия)
            {
                // 6 Месяцев со дня оформления
                case СправочникСрокиСогласия.I1:
                    if (DateTime.Today >= AgreementDate.Value.AddMonths(6).AddDays(1))
                    {
                        result = new()
                        {
                            ErrorCode = 13,
                            Error = "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты",
                            Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты")
                        };

                        return false;
                    }
                    break;
                // 12 месяцев со дня офрмления
                case СправочникСрокиСогласия.I2:
                    if (DateTime.Today >= AgreementDate.Value.AddMonths(12).AddDays(1))
                    {
                        result = new()
                        {
                            ErrorCode = 13,
                            Error = "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты",
                            Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Дата окончания действия согласия (дата выдачи + срок действия) меньше текущей даты")
                        };

                        return false;
                    }
                    break;
                /* В течение срока действия согласия с субъектом кредитной истории были
                    * заключены договор займа(кредита), договор лизинга, договор залога, договор
                    * поручительства, выдана независимая гарантия
                */
                case СправочникСрокиСогласия.I3:
                    // Если код 3 то договор обязателен
                    if (request?.Согласие?.Договор is null)
                    {
                        result = new()
                        {
                            ErrorCode = 15,
                            Error = "Элемент \"Договор\" обязателен т.к. значение атрибута \"СрокДействия\"=\"3\"",
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", "Элемент \"Договор\" обязателен т.к. значение атрибута \"СрокДействия\"=\"3\"")
                        };
                        return false;
                    }

                    var contractDate = request?.Согласие?.Договор.Дата;

                    // Дата выдачи согласия больше даты договора
                    if (AgreementDate > contractDate)
                    {
                        error = $"Дата выдачи согласия {AgreementDate:dd.MM.yyyy} больше даты договора {contractDate:dd.MM.yyyy}";
                        logger.LogError("Дата выдачи согласия {AgreementDate} больше даты договора {contractDate}", AgreementDate?.ToString("dd.MM.yyyy"), contractDate?.ToString("dd.MM.yyyy"));
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }

                    if (contractDate > DateTime.Today)
                    {
                        error = $"Дата договора {contractDate:dd.MM.yyyy} больше текущей";
                        logger.LogError("Дата договора {AgreementDate} больше текущей", contractDate?.ToString("dd.MM.yyyy"));
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }
                    break;
                // Код не найден
                default:
                    result = new()
                    {
                        ErrorCode = 13,
                        Error = $"Значение \"Срок действия\" блока \"Согласие\" {request?.Согласие?.СрокДействия}, не найдено в справочнике.",
                        Ticket = ticketService.CreateResult(ResponseType.Error, "13", $"Значение \"Срок действия\" блока \"Согласие\" {request?.Согласие?.СрокДействия}, не найдено в справочнике.")
                    };

                    return false;
            }

            // Наличие оснвоания для передачи согласия другому лицу
            bool HasBasement = request?.Согласие?.ОснованиеПередачиSpecified ?? false;

            // ИНН из согласия
            string? innAgreement =
                request?.Согласие?.Выдано?.ЮридическоеЛицо?.ИНН ??
                request?.Согласие?.Выдано?.ИндивидуальныйПредприниматель?.ИНН ??
                request?.Согласие?.Выдано?.ИностранноеЮЛ?.ИНН ??
                request?.Согласие?.Выдано?.ИностранныйПредприниматель?.ИНН;

            // ОГРН из согласия
            string? ogrnAgreement =
                request?.Согласие?.Выдано?.ЮридическоеЛицо?.ОГРН ??
                request?.Согласие?.Выдано?.ИндивидуальныйПредприниматель?.ОГРН ??
                request?.Согласие?.Выдано?.ИностранноеЮЛ?.ОГРН ??
                request?.Согласие?.Выдано?.ИностранныйПредприниматель?.ОГРН;

            if (string.IsNullOrWhiteSpace(innAgreement))
            {
                result = new()
                {
                    ErrorCode = 13,
                    Error = "В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие.",
                    Ticket = ticketService.CreateResult(ResponseType.Error, "13", "В блоке \"Выдано\" отсутствуют реквизиты лица, которому было выдано согласие.")
                };
                return false;
            }
            if (string.IsNullOrWhiteSpace(ogrnAgreement))
            {
                result = new()
                {
                    ErrorCode = 13,
                    Error = "Отсутствуют реквизиты лица, которому было выдано согласие.",
                    Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Отсутствуют реквизиты лица, которому было выдано согласие.")
                };
                return false;
            }

            // ИНН источника
            string? innSource =
               request?.Источник?.ЮридическоеЛицо?.ИНН ??
               request?.Источник?.ИндивидуальныйПредприниматель?.ИНН ??
               request?.Источник?.ИностранныйПредприниматель?.ИНН ??
               request?.Источник?.ИностранноеЮЛ?.ИНН;

            // ОГРН Источника
            string? ogrnSource =
                request?.Источник?.ЮридическоеЛицо?.ОГРН ??
                request?.Источник?.ИндивидуальныйПредприниматель?.ОГРН ??
                request?.Источник?.ИностранныйПредприниматель?.ОГРН ??
                request?.Источник?.ИностранноеЮЛ?.ОГРН;

            bool ComapreINN = innAgreement == innSource;
            bool CompareOGRN = ogrnAgreement == ogrnSource;

            // Есть основание
            if (HasBasement)
            {
                // ИНН совпадает - ошибка
                if (ComapreINN)
                {
                    error = $"Запрос содержит некорректные данные: При наличии в согласии атрибута \"ОснованиеПередачи\" ИНН ({innAgreement}) лица, которому было выдано согласие, не должен совпадать с ИНН ({innSource}) источника.";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
                // ОГРН совпадает - ошибка
                else if (CompareOGRN)
                {
                    error = $"Запрос содержит некорректные данные: При наличии в согласии атрибута \"ОснованиеПередачи\" ОГРН ({ogrnAgreement}) лица, которому было выдано согласие, не должен совпадать с ОГРН ({ogrnSource}) источника.";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
            }
            else
            {
                // ИНН не совпадает - ошибка
                if (!ComapreINN)
                {
                    error = $"Запрос содержит некорректные данные: ИНН ({innAgreement}) лица, которому было выдано согласие, должен совпадать с ИНН ({innSource})({innSource}) источника.";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
                // ОГРН не совпадает - ошибка
                else if (!CompareOGRN)
                {
                    error = $"Запрос содержит некорректные данные: ОГРН лица ({ogrnAgreement}), которому было выдано согласие, должен совпадать с ОГРН источника ({ogrnSource}).";
                    result = new()
                    {
                        ErrorCode = 15,
                        Error = error,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                    };
                    return false;
                }
            }

            // Если у цели 99 нет описания
            if (request?.Цель?.Any(x => x.КодЦели == QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
            {
                error = $"Запрос содержит некорректные данные: Код цели запроса со значением \"99\" не содержит описания.";
                result = new()
                {
                    ErrorCode = 15,
                    Error = error,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                };
                return false;
            }

            // Если в согласии у цели 99 нет описания
            if (request?.Согласие?.Цель?.Any(x => x.КодЦели == QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item99 && string.IsNullOrWhiteSpace(x.Описание)) ?? false)
            {
                error = $"Запрос содержит некорректные данные: Код цели согласия со значением \"99\" не содержит описания.";
                result = new()
                {
                    ErrorCode = 15,
                    Error = error,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", error)
                };
                return false;
            }


            //  Проверка кодов цели запроса Одна или несколько целей запроса отсутствует в согласии
            for (int i = 0; i < request?.Цель?.Count; i++)
            {
                if (!request.Согласие?.Цель?.Any(x => x.КодЦели == request.Цель[i].КодЦели) ?? false)
                {
                    result = new()
                    {
                        ErrorCode = 13,
                        Error = "Одна или несколько целей, указанных в блоке «Запрос» отсутствует.",
                        Ticket = ticketService.CreateResult(ResponseType.Error, "13", "Одна или несколько целей, указанных в блоке «Запрос» отсутствует.")
                    };

                    return false;
                }
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Проверка запроса на содержание ошибок не выявляющихся xsd
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result">Ответ</param>
        /// <returns>Результат проверки</returns>
        public bool AdditionalValidation(ЗапросСведенийОПлатежах request, [NotNullWhen(false)] out BaseResult? result)
        {
            string? error = null;

            // Наименование ДУЛ при коде 999 для ИП
            if (request?.Запрос?.Источник?.ИндивидуальныйПредприниматель?.ДокументЛичности != null &&
                request.Запрос.Источник.ИндивидуальныйПредприниматель.ДокументЛичности.КодДУЛ == QBCH_lib.qcb_xml.v1_3.Enums.СправочникДУЛ.Item999 &&
               string.IsNullOrWhiteSpace(request.Запрос.Источник.ИндивидуальныйПредприниматель.ДокументЛичности.НаименованиеДУЛ))
            {
                error = $"При значении \"КодДУЛ\" - 999 у источника, \"НаименованиеДУЛ\" обязательно к заполнению";
                logger.LogError("Запрос содержит некорректные данные: {error}", error);
                result = new()
                {
                    Error = error,
                    ErrorCode = 15,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                };
                return false;
            }

            // Наименование ДУЛ при коде 999 для иностранного ИП
            if (request?.Запрос?.Источник?.ИностранныйПредприниматель?.ДокументЛичности != null &&
                request.Запрос.Источник.ИностранныйПредприниматель.ДокументЛичности.КодДУЛ == QBCH_lib.qcb_xml.v1_3.Enums.СправочникДУЛ.Item999 &&
               string.IsNullOrWhiteSpace(request.Запрос.Источник.ИностранныйПредприниматель.ДокументЛичности.НаименованиеДУЛ))
            {
                error = $"При значении \"КодДУЛ\" - 999 у источника, \"НаименованиеДУЛ\" обязательно к заполнению";
                logger.LogError("Запрос содержит некорректные данные: {error}", error);
                result = new()
                {
                    Error = error,
                    ErrorCode = 15,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                };
                return false;
            }

            // Проверка субъекта
            if (request?.Запрос?.Субъект?.ДокументЛичности is not null)
            {
                var dateOfBirth = request.Запрос.Субъект.ДатаРождения;

                // Дата рождения больше или равна текущей дате
                if (dateOfBirth >= DateTime.Today)
                {
                    error = $"Дата рождения {dateOfBirth:dd.MM.yyyy} больше или равна текущей дате";
                    logger.LogError("Запрос содержит некорректные данные: {error}", error);
                    result = new()
                    {
                        Error = error,
                        ErrorCode = 15,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                    };
                    return false;
                }

                // Проверка документов
                foreach (var item in request.Запрос.Субъект.ДокументЛичности)
                {
                    // Дата выдачи документа больше или равна дате рождения
                    if (dateOfBirth >= item.ДатаВыдачи)
                    {
                        error = $"Дата выдачи ДУЛ {item.ДатаВыдачи:dd.MM.yyyy} более ранняя или равна дате рождения {dateOfBirth:dd.MM.yyyy}";
                        logger.LogError("Запрос содержит некорректные данные: {error}", error);
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }
                }
            }

            // Проверка наличия суммы обязательства для цеей кедитования
            if (request?.Запрос?.Цель != null)
            {
                var creditTargets = new[]
                {
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item1,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item2,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item3,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item4,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item5,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item10,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item11,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item12,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item13,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item14,
                    QBCH_lib.qcb_xml.v1_3.Enums.ТипЦельКодЦели.Item15
                };
                bool HasCreditTarget = false;

                foreach (var item in request.Запрос.Цель.Select(x => x.КодЦели))
                {
                    if (creditTargets.Any(x => x == item))
                    {
                        HasCreditTarget = true;
                        break;
                    }
                }

                if (HasCreditTarget)
                {
                    if (request.Запрос.СуммаОбязательства is null)
                    {
                        error = $"Для целей кредитования \"СуммаОбязательства\" обязательна к заполнению";
                        logger.LogError("Запрос содержит некорректные данные: {error}", error);
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }
                }
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Проверка запроса на содержание ошибок не выявляющихся xsd
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="result">Ответ</param>
        /// <returns>Результат проверки</returns>
        public bool AdditionalValidation(ЗапросСведенийЗапрос request, [NotNullWhen(false)] out BaseResult? result) //3.0
        {
            string? error = null;

            // Наименование ДУЛ при коде 999 для ИП
            if (request.Источник?.ИндивидуальныйПредприниматель?.ДокументЛичности != null &&
                request.Источник.ИндивидуальныйПредприниматель.ДокументЛичности.КодДУЛ == QBCH_lib.qcb_xml.v3_0.Enums.СправочникДУЛ.Item999 &&
               string.IsNullOrWhiteSpace(request.Источник.ИндивидуальныйПредприниматель.ДокументЛичности.НаименованиеДУЛ))
            {
                error = $"При значении \"КодДУЛ\" - 999 у источника, \"НаименованиеДУЛ\" обязательно к заполнению";
                logger.LogError("Запрос содержит некорректные данные: {error}", error);
                result = new()
                {
                    Error = error,
                    ErrorCode = 15,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                };
                return false;
            }

            // Наименование ДУЛ при коде 999 для иностранного ИП
            if (request.Источник?.ИностранныйПредприниматель?.ДокументЛичности != null &&
                request.Источник.ИностранныйПредприниматель.ДокументЛичности.КодДУЛ == QBCH_lib.qcb_xml.v3_0.Enums.СправочникДУЛ.Item999 &&
               string.IsNullOrWhiteSpace(request.Источник.ИностранныйПредприниматель.ДокументЛичности.НаименованиеДУЛ))
            {
                error = $"При значении \"КодДУЛ\" - 999 у источника, \"НаименованиеДУЛ\" обязательно к заполнению";
                logger.LogError("Запрос содержит некорректные данные: {error}", error);
                result = new()
                {
                    Error = error,
                    ErrorCode = 15,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                };
                return false;
            }

            // Проверка субъекта
            if (request.Субъект?.ДокументЛичности is not null)
            {
                var dateOfBirth = request.Субъект.ДатаРождения;

                // Дата рождения больше или равна текущей дате
                if (dateOfBirth >= DateTime.Today)
                {
                    error = $"Дата рождения {dateOfBirth:dd.MM.yyyy} больше или равна текущей дате";
                    logger.LogError("Запрос содержит некорректные данные: {error}", error);
                    result = new()
                    {
                        Error = error,
                        ErrorCode = 15,
                        Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                    };
                    return false;
                }

                // Проверка документов
                foreach (var item in request.Субъект.ДокументЛичности ?? [])
                {
                    // Дата выдачи документа больше или равна дате рождения
                    if (dateOfBirth >= item.ДатаВыдачи)
                    {
                        error = $"Дата выдачи ДУЛ {item.ДатаВыдачи:dd.MM.yyyy} более ранняя или равна дате рождения {dateOfBirth:dd.MM.yyyy}";
                        logger.LogError("Запрос содержит некорректные данные: {error}", error);
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }
                }
            }

            // Проверка наличия суммы обязательства для цеей кедитования
            if (request.Цель != null)
            {
                var creditTargets = new[]
                {
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item1,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item2,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item3,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item4,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item5,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item10,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item11,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item12,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item13,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item14,
                    QBCH_lib.qcb_xml.v3_0.Enums.ТипЦельКодЦели.Item15
                };
                bool HasCreditTarget = false;

                foreach (var item in request.Цель.Select(x => x.КодЦели) ?? [])
                {
                    if (creditTargets.Any(x => x == item))
                    {
                        HasCreditTarget = true;
                        break;
                    }
                }

                if (HasCreditTarget)
                {
                    if (request.СуммаОбязательства is null)
                    {
                        error = $"Для целей кредитования \"СуммаОбязательства\" обязательна к заполнению";
                        logger.LogError("Запрос содержит некорректные данные: {error}", error);
                        result = new()
                        {
                            Error = error,
                            ErrorCode = 15,
                            Ticket = ticketService.CreateResult(ResponseType.Error, "15", $"Запрос содержит некорректные данные: {error}")
                        };
                        return false;
                    }
                }
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Являестся ли запрос от организации уникальным в течение дня
        /// </summary>
        /// <param name="requestId">Id запроса</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="methodName">Наименование метода из которого прилетел запрос</param>
        /// <param name="validationResult">Результат</param>
        /// <returns></returns>
        public bool IsUniqueRequestId(string requestId, string methodName, string ogrn, [NotNullWhen(false)] out BaseResult? validationResult)
        {
            bool isUnuqie = cache.IsUniqueRequestId(requestId, ogrn, methodName).Result;
            if (!isUnuqie)
            {
                validationResult = new()
                {
                    Error = "Идентификатор запроса не уникален",
                    ErrorCode = 11,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "11", $"Идентификатор запроса не уникален")
                };
                return false;
            }

            validationResult = null;
            return isUnuqie;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public async Task<bool> IsCertExists(byte[] cert) => await repository.IsCertExist(cert);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="ct"></param>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        public async Task<bool> ValidateRules(string? thumbprint, string? serviceName, CancellationToken? ct = null) => await repository.IsPermissionGranted(thumbprint, serviceName, ct);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="psrn"></param>
        /// <param name="QBCHList"></param>
        /// <param name="validationResult"></param>
        /// <returns></returns>
        public bool ValidateQBCH(string? psrn, List<QBCHRequisite> QBCHList, [NotNullWhen(false)] out BaseResult? validationResult)
        {
            bool IsValid = QBCHList.All(x => x.ogrn != psrn);

            if (!IsValid)
            {
                validationResult = new()
                {
                    Error = "Взаимодействие с абонентом в режиме «одно окно» не предусмотрено договором",
                    ErrorCode = 11,
                    Ticket = ticketService.CreateResult(ResponseType.Error, "14", $"Взаимодействие с абонентом в режиме «одно окно» не предусмотрено договором")
                };
                return IsValid;
            }

            validationResult = null;
            return IsValid;
        }
    }
}
