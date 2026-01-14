using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Application_lib;
using CryptoPro.Security.Cryptography.Pkcs;
using CryptoPro.Security.Cryptography.X509Certificates;
using Domain;
using Domain.QBCHModels.CommonTypes;
using Domain.QBCHModels.CryptoModels;
using Domain.QBCHModels.qcb_xml.v2_0.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.X509;

namespace Adapters_lib.Crypto
{
    /// <summary>
    /// Сервис криптографии
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    /// <param name="config">Конфигурация</param>
    public class CryptoAdapter(
        IConfiguration config,
        ILogger<CryptoAdapter> logger,
        ITicketService ticketService) : ICryptoAdapter
    {
        private readonly string? _storeLocation = config.GetValue<string>("Signer:StoreLocation");
        private readonly string? _storeName = config.GetValue<string>("Signer:StoreName");
        private readonly string? _findType = config.GetValue<string>("Signer:FindType");
        private readonly string? _searchValue = config.GetValue<string>("Signer:SearchValue");
        private readonly ILogger<CryptoAdapter> _logger = logger;
        private readonly ITicketService _ticketService = ticketService;

        /// <summary>
        /// Проверка подписи файла (перезрузка)
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        public Result<CryptoServiceResult> ValidateMsg(byte[] msg, X509Certificate2? requestCert, byte[]? encodedSignature = null)
        {
            var result = new CryptoServiceResult();
            if (requestCert is null)
            {
                _logger.LogError("Сертификат запроса не найден");
                var processingError = Error.Code99_CertificateIsNotFound();
                result.Error = processingError.Message;
                result.ErrorCode = processingError.Code;
                result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "99", "Сертификат не найден");
                return Result<CryptoServiceResult>.Failure(processingError);
            }

            // Субъект из сертификата в запросе
            var requestSubject = new ParsedSubject();
            MapSubject(requestCert.RawData, requestSubject);

            // Добавляем сведения о сертификате запроса в результат
            result.RequestINN = requestSubject.InnLE ?? requestSubject.Inn;
            result.RequestOGRN = requestSubject.InnLE is not null ? requestSubject.Ogrn : requestSubject.OgrnIP ?? requestSubject.Ogrn;
            result.RequestThumbprint = requestCert.Thumbprint;

            /* 5. Истек срок действия сертификата УЭП.
             * Дату сертификата необходимо проверять заранее, 
             * Т.к. метод validate у серфиса криптографии
             * Возвращает любые ошибки в виде exception
             * Это влечет за собой невозможность определния
             * Типа ошибки.
             */

            if (requestCert?.NotAfter != null)
            {
                if (requestCert.NotAfter <= DateTime.Now)
                {

                    var processingError = Error.Code5_TheCertificateIsExpired();
                    _logger.LogError(processingError.Message);
                    result.ErrorCode = processingError.Code;
                    result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "5", "Истек срок сертификата УЭП");
                    return Result<CryptoServiceResult>.Failure(processingError);
                }
            }
            else
            {
                _logger.LogWarning("Не удалось проверить срок действия сертификата в запросе.");
            }

            // Создаем SignedCms для декодирования и проверки.
            CpSignedCms signedCms;

            try
            {
                // Для открепленной подписи требуется вызывать метод иначе
                if (encodedSignature != null)
                {
                    var content = new ContentInfo(msg);
                    signedCms = new(content, true);
                    signedCms.Decode(encodedSignature);
                }
                else
                {
                    signedCms = new();
                    signedCms.Decode(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Некорректный формат запроса:  Полученный в запросе файл не идентифицируется как криптографическое сообщение в формате PKCS#7, содержащее запрос и УЭП");
                var processingMessage = new Error(7, ex.Message);
                result.Error = processingMessage.Message;
                result.ErrorCode = processingMessage.Code;
                result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "7", "Некорректный формат запроса:  Полученный в запросе файл не идентифицируется как криптографическое сообщение в формате PKCS#7, содержащее запрос и УЭП");
                return Result<CryptoServiceResult>.Failure(processingMessage);
            }

            var enumerator = signedCms.SignerInfos.GetEnumerator();
            bool IsValidCert = false;


            List<Error> errors = [];
            while (enumerator.MoveNext()) //TODO нужно понять что тут происходит
            {
                var current = enumerator.Current;

                if (!IsValidCert)
                {
                    // Сверка реквизитов сертифкатов запроса и подписи.
                    if (IsCertComapreFailed(requestSubject, current.Certificate, result))
                    {
                        _logger.LogError("УЭП не соответствует абоненту, request_inn:{request_inn}, sign_inn:{sign_inn}. request_psrn:{request_inn}, sign_psrn:{sign_inn}", result.RequestINN, result.SignINN, result.RequestOGRN, result.SignOGRN);
                        var processingError = Error.Code6_DetailsDoNotMatch();

                        result = new CryptoServiceResult()
                        {
                            ErrorCode = processingError.Code,
                            Error = processingError.Message,
                            Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "6", $"УЭП не соответствует абоненту:  {result.CertCompareResult}")
                        };
                        errors.Add(processingError);
                        continue;
                    }

                    try
                    {
                        current.CheckSignature(true);
                        IsValidCert = true;
                    }
                    catch (Exception ex)
                    {
                        var processingError = new Error(4, ex.Message);
                        _logger.LogError("УЭП некорректна {Error}", ex.Message); // TODO странно 
                        result.Error = processingError.Message;
                        result.ErrorCode = processingError.Code;
                        result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "4", $"УЭП некорректна: {ex.Message}");
                        continue;
                    }
                }
            }

            if (!IsValidCert)
                return Result<CryptoServiceResult>.Failure(errors.First());

            result.Body = signedCms.ContentInfo.Content;
            result.SignedBody = msg;

            return Result<CryptoServiceResult>.Success(result);
        }
        /// <summary>
        /// Проверка подписи файла
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        public bool ValidateMsg(byte[] msg, X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult result, byte[]? encodedSignature = null)
        {
            result = new CryptoServiceResult();

            if (requestCert is null)
            {
                _logger.LogError("Сертификат запроса не найден");
                result.Error = "УЭП некорректна";
                result.ErrorCode = 5;
                result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "4", "УЭП некорректна");
                return false;
            }

            // Субъект из сертификата в запросе
            var requestSubject = new ParsedSubject();
            MapSubject(requestCert.RawData, requestSubject);

            // Добавляем сведения о сертификате запроса в результат
            result.RequestINN = requestSubject.InnLE ?? requestSubject.Inn;
            result.RequestOGRN = requestSubject.InnLE is not null ? requestSubject.Ogrn : requestSubject.OgrnIP ?? requestSubject.Ogrn;
            result.RequestThumbprint = requestCert.Thumbprint;

            /* 5. Истек срок действия сертификата УЭП.
             * Дату сертификата необходимо проверять заранее, 
             * Т.к. метод validate у серфиса криптографии
             * Возвращает любые ошибки в виде exception
             * Это влечет за собой невозможность определния
             * Типа ошибки.
             */
            if (requestCert?.NotAfter != null)
            {
                if (requestCert.NotAfter <= DateTime.Now)
                {
                    _logger.LogError("Истек срок сертификата УЭП");
                    result.Error = "Истек срок сертификата УЭП";
                    result.ErrorCode = 5;
                    result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "5", "Истек срок сертификата УЭП");
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("Не удалось проверить срок действия сертификата в запросе.");
            }

            // Создаем SignedCms для декодирования и проверки.
            CpSignedCms signedCms;

            try
            {
                // Для открепленной подписи требуется вызывать метод иначе
                if (encodedSignature != null)
                {
                    var content = new ContentInfo(msg);
                    signedCms = new(content, true);
                    signedCms.Decode(encodedSignature);
                }
                else
                {
                    signedCms = new();
                    signedCms.Decode(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Некорректный формат запроса:  Полученный в запросе файл не идентифицируется как криптографическое сообщение в формате PKCS#7, содержащее запрос и УЭП");
                result.Error = ex.Message;
                result.ErrorCode = 7;
                result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "7", "Некорректный формат запроса:  Полученный в запросе файл не идентифицируется как криптографическое сообщение в формате PKCS#7, содержащее запрос и УЭП");
                return false;
            }

            var enumerator = signedCms.SignerInfos.GetEnumerator();
            bool IsValidCert = false;

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (!IsValidCert)
                {
                    // Сверка реквизитов сертифкатов запроса и подписи.
                    if (IsCertComapreFailed(requestSubject, current.Certificate, result))
                    {
                        _logger.LogError("УЭП не соответствует абоненту, request_inn:{request_inn}, sign_inn:{sign_inn}. request_psrn:{request_inn}, sign_psrn:{sign_inn}", result.RequestINN, result.SignINN, result.RequestOGRN, result.SignOGRN);
                        result = new CryptoServiceResult()
                        {
                            ErrorCode = 6,
                            Error = "Реквизиты абонента не совпадают",
                            Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "6", $"УЭП не соответствует абоненту:  {result.CertCompareResult}")
                        };
                        continue;
                    }

                    try
                    {
                        current.CheckSignature(true);
                        IsValidCert = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("УЭП некорректна {Error}", ex.Message);
                        result.Error = ex.Message;
                        result.ErrorCode = 4;
                        result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "4", $"УЭП некорректна: {ex.Message}");
                        continue;
                    }
                }
            }

            if (!IsValidCert)
                return false;

            result.Body = signedCms.ContentInfo.Content;
            result.SignedBody = msg;

            return true;
        }


        /// <summary>
        /// Проверка подписи файла
        /// </summary>
        /// <param name="msg">Подписанный файл сообщения</param>
        /// <param name="encodedSignature">Отсоединенная подпись default(null)</param>
        /// <returns>Результат проверки подписи</returns>
        public bool ValidateMsg(byte[] msg, [NotNullWhen(false)] out CryptoServiceResult result, byte[]? encodedSignature = null, CancellationToken? ct = null)
        {
            result = new CryptoServiceResult();

            // Создаем SignedCms для декодирования и проверки.
            CpSignedCms signedCms = new();
            try
            {
                signedCms.Decode(msg);
            }
            catch (Exception ex)
            {
                result = new CryptoServiceResult
                {
                    Error = ex.Message,
                    ErrorCode = 7
                };
                return false;
            }

            var enumerator = signedCms.SignerInfos.GetEnumerator();
            bool IsValidCert = false;

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                if (!IsValidCert)
                {
                    /* 5. Истек срок действия сертификата УЭП.
                        * Дату сертификата необходимо проверять заранее, 
                        * Т.к. метод validate у серфиса криптографии
                        * Возвращает любые ошибки в виде exception
                        * Это влечет за собой невозможность определния
                        * Типа ошибки.
                        */
                    if (current.Certificate?.NotAfter != null)
                    {
                        if (current.Certificate.NotAfter <= DateTime.Now)
                        {
                            _logger.LogError("Истек срок сертификата УЭП");
                            result = new CryptoServiceResult
                            {
                                Error = "Истек срок сертификата УЭП",
                                ErrorCode = 5,
                                Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "5", "Истек срок сертификата УЭП")
                            };
                            return false;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Не удалось проверить срок действия сертификата в запросе.");
                    }

                    try
                    {
                        current.CheckSignature(true);
                        IsValidCert = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("УЭП некорректна {Error}", ex.Message);
                        result.Error = ex.Message;
                        result.ErrorCode = 4;
                        result.Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "4", $"УЭП некорректна: {ex.Message}");
                        continue;
                    }
                }
            }

            if (!IsValidCert)
                return false;

            result.Body = signedCms.ContentInfo.Content;
            result.SignedBody = msg;

            return true;
        }

        /// <summary>
        /// Валидация сертификата
        /// </summary>
        /// <param name="requestCert">Сертификат</param>
        /// <param name="result">Результат проверки</param>
        /// <returns></returns>
        public bool ValidateCertificate(X509Certificate2? requestCert, [NotNullWhen(false)] out CryptoServiceResult? result)
        {
            /* 5. Истек срок действия сертификата УЭП.
             * Дату сертификата необходимо проверять заранее, 
             * Т.к. метод validate у серфиса криптографии
             * Возвращает любые ошибки в виде exception
             * Это влечет за собой невозможность определния
             * Типа ошибки.
             */
            if (requestCert?.NotAfter != null)
            {
                if (requestCert.NotAfter <= DateTime.Today)
                {
                    _logger.LogError("Истек срок сертификата УЭП");
                    result = new CryptoServiceResult()
                    {
                        ErrorCode = 5,
                        Error = "Истек срок сертификата УЭП",
                        Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "5", "Истек срок сертификата УЭП")
                    };
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("Не удалось проверить срок действия сертификата в запросе.");
            }

            // Если сертифкат не найден
            if (requestCert is null)
            {
                _logger.LogError("УЭП отсутствует.");
                result = new CryptoServiceResult
                {
                    ErrorCode = 4,
                    Error = "УЭП некорректна: УЭП отсутствует.",
                    Ticket_v2 = _ticketService.CreateResultv2(ResponseType.Error, "4", "УЭП некорректна: УЭП отсутствует.")
                };
                return false;
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Подписываем сообщение секретным ключем.
        /// </summary>
        /// <param name="msg">Сообщение в формет byte</param>
        /// <param name="signerCert">Сертифкат подписанта</param>
        /// <returns>Подписанный файл</returns>
        public byte[] SignMsg(byte[] msg)
        {
            // Создаем объект ContentInfo по сообщению.
            // Это необходимо для создания объекта SignedCms.
            ContentInfo contentInfo = new(msg);


            // Создаем объект SignedCms по только что созданному
            // объекту ContentInfo.
            // SubjectIdentifierType установлен по умолчанию в 
            // IssuerAndSerialNumber.
            // Свойство Detached устанавливаем явно в true, таким 
            // образом сообщение будет отделено от подписи.
            CpSignedCms signedCms = new(contentInfo, false);

            // Определяем подписывающего, объектом CmsSigner.
            CpCmsSigner cmsSigner = new(GetCertificateFromStore());

            // Подписываем CMS/PKCS #7 сообщение.
            try
            {
                signedCms.ComputeSignature(cmsSigner);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // Кодируем CMS/PKCS #7 подпись сообщения.
            return signedCms.Encode();
        }

        /// <summary>
        /// Сравнение сертификатов в запросе и подписанном файле
        /// </summary>
        /// <param name="result"></param>
        /// <param name="requestCert">Сертификат запроса</param>
        /// <param name="fileSignCert">Сертифкат подписи</param>
        /// <returns>Коллекция запись в первом сертификате - запись во втором сертификате.</returns>
        private static bool IsCertComapreFailed(ParsedSubject? requestSubject, CpX509Certificate2? fileSignCert, CryptoServiceResult result)
        {
            if (fileSignCert is null)
                throw new Exception("Отсутствует сертификат подписи");

            // Субъект из сертифката в подписи
            var signerSubject = new ParsedSubject();
            MapSubject(fileSignCert.RawData, signerSubject);

            // Добавляем сведения о сертификате запроса в результат
            result.SignINN = signerSubject.InnLE ?? signerSubject.Inn;
            result.SignOGRN = signerSubject.InnLE is not null ? signerSubject.Ogrn : signerSubject.OgrnIP ?? signerSubject.Ogrn;
            result.SignThumbprint = fileSignCert.Thumbprint;

            var sb = new StringBuilder();

            // Сверка ИНН
            if (result.RequestINN != result.SignINN)
                sb.AppendLine($"ИНН в запросе:{result.RequestINN}, в подписи: {result.SignINN}.");

            // Сверка ОГРН
            if (result.RequestOGRN != result.SignOGRN)
                sb.AppendLine($"ОГРН в запросе:{result.RequestOGRN}, в подписи: {result.SignOGRN}.");

            result.CertCompareResult = sb;
            return sb.Length != 0;
        }

        /// <summary>
        /// Получение сертифката из хранилища по реквизитам
        /// </summary>
        /// <param name="thumbprint">Отпечаток</param>
        /// <param name="storeLocation">Расположение хранилища сертифкатов</param>
        /// <param name="storeName">Директория в хранилище</param>
        /// <returns>Сертификат</returns>
        private CpX509Certificate2? GetCertificateFromStore()
        {
            // Расположение хранилища сертифкатов
            if (!Enum.TryParse<StoreLocation>(_storeLocation, true, out var storeLocation))
                storeLocation = StoreLocation.LocalMachine;

            // Директория в хранилище
            if (!Enum.TryParse<StoreName>(_storeName, true, out var storeName))
                storeName = StoreName.My;

            // Определения параметра поиска
            if (!Enum.TryParse<X509FindType>(_findType, true, out var findType))
                findType = X509FindType.FindByThumbprint;

            CpX509Store store = new(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            if (string.IsNullOrWhiteSpace(_searchValue))
            {
                throw new Exception("Сертификат не найден");
            }

            // Находим сертификаты с нужным значением и возвращаем.
            return store.Certificates.Find(findType, _searchValue, true).FirstOrDefault();
        }

        /// <summary>
        /// Маппинг свойтсв
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="subject"></param>
        private static void MapSubject(byte[] bytes, ParsedSubject subject)
        {
            X509CertificateParser parser = new();
            var cert = parser.ReadCertificate(bytes);

            // Разбор поля subject = SEQ of SET of SEQ of {OID/value}
            DerSequence? certSubject = cert.SubjectDN.ToAsn1Object() as DerSequence;

            foreach (var setItem in certSubject ?? new())
            {
                if (setItem is not DerSet subSet)
                    continue;

                // Первый элемент множества SET - искомая последовательность SEQ of {OID/value}
                DerSequence? subSeq = subSet[0] as DerSequence;

                foreach (Asn1Encodable subSeqItem in subSeq ?? new())
                {
                    if (subSeqItem is not DerObjectIdentifier oid)
                        continue;

                    var value = subSeq?[1].ToString();

                    switch (oid.Id)
                    {
                        case "2.5.4.3":
                            subject.CommonName = value;
                            break;
                        case "2.5.4.8":
                            subject.CountryName = value;
                            break;
                        case "1.2.643.100.4":
                            subject.InnLE = value;
                            break;
                        case "1.2.643.3.131.1.1":
                            subject.Inn = value;
                            break;
                        case "1.2.643.100.1":
                            subject.Ogrn = value;
                            break;
                        case "1.2.643.100.5":
                            subject.OgrnIP = value;
                            break;
                        case "1.2.643.100.3":
                            subject.Snils = value;
                            break;
                        case "1.2.840.113549.1.9.1":
                            subject.Email = value;
                            break;
                        default:
                            continue;
                    }
                }
            }
        }
    }
}
