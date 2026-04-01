using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.core;
using QBCH_lib.qcb_xml.v3_0.Enums;
using QBCH_lib.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using XmlService_lib.Services.Interfaces;

namespace XmlService_lib.Services.Implementations
{
    /// <summary>
    /// Севрис для работф с xml
    /// </summary>
    public class XmlService(IMemoryCache memoryCache, IConfiguration config, ILogger<XmlService> logger, ITicketService ticketService) : IXmlService
    {
        private readonly IMemoryCache _cache = memoryCache;
        private readonly IConfiguration _config = config;
        private readonly ILogger<XmlService> _logger = logger;
        private readonly ITicketService _ticketService = ticketService;

        /// <summary>
        /// Десериализация из xDocument
        /// </summary>
        /// <returns>Десериалиованный объект класса</returns>
        public T? Deserialize<T>(XElement? xml) where T : class
        {
            if (xml is null)
                return null;

            var serializer = new XmlSerializer(typeof(T));
            using var reader = xml.CreateReader();

            try
            {
                return serializer.Deserialize(reader) as T;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Десериализация массива байт в класс
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="bytes">Массив байт</param>
        /// <returns></returns>
        public T? Deserialize<T>(byte[]? bytes) where T : class
        {
            if (bytes is null)
                return null;

            using var ms = new MemoryStream(bytes);
            var serializer = new XmlSerializer(typeof(T));
            return serializer.Deserialize(ms) as T;
        }

        /// <summary>
        /// Десериализация из xDocument
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="stream">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        public T? Deserialize<T>(XDocument? xml) where T : class
        {
            if (xml is null)
                return null;

            var serializer = new XmlSerializer(typeof(T));
            using var reader = xml.CreateReader();
            return serializer.Deserialize(reader) as T;
        }

        /// <summary>
        /// Десериализация из потока
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="stream">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        public T? Deserialize<T>(Stream? stream) where T : class
        {
            if (stream is null)
                return null;

            var serializer = new XmlSerializer(typeof(T));
            using var reader = XmlReader.Create(stream, new() { Async = true });
            return serializer.Deserialize(reader) as T;
        }

        /// <summary>
        /// Десериализация из XmlReader
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="reader">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        public T? Deserialize<T>(XmlReader? reader) where T : class
        {
            if (reader is null)
                return null;

            var serializer = new XmlSerializer(typeof(T));
            return serializer.Deserialize(reader) as T;
        }

        /// <summary>
        /// Сериализация в строку
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>string</returns>
        public async Task<string> SerializeAsync<T>(T item) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces ns = new();
            ns.Add("", "");

            using var ms = new MemoryStream();
            using var tw = XmlWriter.Create(ms);
            serializer.Serialize(tw, item, ns);
            ms.Position = 0;
            using var sr = new StreamReader(ms);
            return await sr.ReadToEndAsync();
        }

        /// <summary>
        /// Сериализация в Stream
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>Stream</returns>
        public Stream SerializeAsStream<T>(T? item) where T : class
        {
            using var ms = new MemoryStream();
            if (item is null)
                return ms;

            var serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces ns = new();
            ns.Add("", "");

            using var tw = XmlWriter.Create(ms);
            serializer.Serialize(tw, item, ns);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Сериалищзация в строку
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public string SerializeAsString<T>(T? item) where T : class
        {
            if (item is null)
                return string.Empty;

            var serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces ns = new();
            using var ms = new MemoryStream();
            ns.Add("", "");

            using StreamWriter sw = new(ms, new UTF8Encoding(false));
            serializer.Serialize(sw, item, ns);
            ms.Position = 0;
            using var sr = new StreamReader(ms);

            return sr.ReadToEnd();
        }

        /// <summary>
        /// Сериализация в Stream
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>Stream</returns>
        public byte[] SerializeAsByte<T>(T? item) where T : class
        {
            if (item is null)
                return [];

            var serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces ns = new();
            ns.Add("", "");

            using var ms = new MemoryStream();
            using StreamWriter sw = new(ms, new UTF8Encoding(false));

            serializer.Serialize(sw, item, ns);
            ms.Position = 0;
            return ms.ToArray();
        }

        /// <summary>
        /// Сериализация в XDocument
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>XDocument</returns>
        public async Task<XDocument> SerializeAsXDocumentAsync<T>(T item) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));

            XmlSerializerNamespaces ns = new();
            ns.Add("", "");

            var ms = new MemoryStream();
            using var tw = XmlWriter.Create(ms);
            serializer.Serialize(tw, item, ns);
            ms.Position = 0;

            return await XDocument.LoadAsync(ms, LoadOptions.None, CancellationToken.None);
        }

        /// <summary>
        /// Валидация xml
        /// </summary>
        /// <param name="memoryStream">xml</param>
        /// <param name="xsdFullPaths">Массив путей к xsd</param>
        /// <returns>Результат проверки</returns>
        public BaseResult? ValidateXml(MemoryStream memoryStream, string[] xsdFullPaths)
        {
            XmlSchemaSet schemaSet = new()
            {
                XmlResolver = new XmlUrlResolver()
            };

            foreach (var xsdFullPath in xsdFullPaths)
            {
                schemaSet.Add(null, xsdFullPath);
            }

            BaseResult? xsdError = null;

            try
            {
                var xDoc = XDocument.Load(memoryStream);
                xDoc.Validate(schemaSet, (sender, e) =>
                {
                    var error = string.Concat(e.Severity, ": ", e.Message);
                    _logger.LogError("Запрос не соответствует схеме:\r\n{error}", error);
                    xsdError = new()
                    {
                        Error = error,
                        ErrorCode = 9,
                        Ticket = _ticketService.CreateResult(ResponseType.Error, "9", $"Запрос не соответствует схеме:\r\n{error}")
                    };
                });
                return xsdError;
            }
            catch (Exception ex)
            {
                return new()
                {
                    Error = $"Запрос не соответствует схеме:\r\n{ex.Message}",
                    ErrorMessage = ex.Message,
                    Ticket = _ticketService.CreateResult(ResponseType.Error, "9", $"Запрос не соответствует схеме:\r\n{ex.Message}")
                };
            }
        }

        /// <summary>
        /// Получить схема сет из кеша
        /// </summary>
        /// <param name="nameOfController"></param>
        /// <returns></returns>
        private XmlSchemaSet GetXmlSchemaSet(string nameOfController, string version)
        {
            // Папка с xsd
            var xsdFolder = _config.GetValue<string>("Paths:Xsd");

            // Xsd согласно запросу
            var requiredxsd = _config.GetSection($"XSD:{version}:{nameOfController}").Get<string[]>();

            // Пытаемся достать схемасэт из кэша
            if (!_cache.TryGetValue(version + nameOfController, out XmlSchemaSet? schemaSet) || schemaSet is null)
            {
                schemaSet = new()
                {
                    XmlResolver = new XmlUrlResolver()
                };

                // Забираем все xsd которые нам нужны
                foreach (var xsdFile in requiredxsd)
                {
                    string dlRequestXsdFullPath = Path.Combine(AppContext.BaseDirectory, xsdFolder, version, xsdFile);
                    schemaSet.Add(null, dlRequestXsdFullPath);
                }

                // Добавление сэта схем в memorycache
                try
                {
                    _cache.Set(version + nameOfController, schemaSet, new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка добавления набора схем xsd в memoiry cache");
                }
            }

            return schemaSet;
        }

        /// <summary>
        /// Проверка что xml не валидна 
        /// </summary>
        /// <param name="memStream"></param>
        /// <param name="nameOfController"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool ValidateXml(MemoryStream memStream, string nameOfController, string apiversion, [NotNullWhen(false)] out BaseResult? result)
        {
            // Достаем схемасет из кэша, если его там нет или он null создаем сет из
            var schemaSet = GetXmlSchemaSet(nameOfController, apiversion);
            BaseResult? xsdError = null;

            try
            {
                var xDoc = XDocument.Load(memStream);
                xDoc.Validate(schemaSet, (sender, e) =>
                {
                    var error = string.Concat(e.Severity, ": ", e.Message);
                    _logger.LogError("Запрос не соответствует схеме:\r\n{error}", error);
                    xsdError = new()
                    {
                        Error = error,
                        ErrorCode = 9,
                        Ticket = _ticketService.CreateResult(ResponseType.Error, "9", $"Запрос не соответствует схеме:\r\n{error}")
                    };
                });
                result = xsdError;
            }
            catch (Exception ex)
            {
                result = new()
                {
                    Error = $"Запрос не соответствует схеме:\r\n{ex.Message}",
                    ErrorMessage = ex.Message,
                    Ticket = _ticketService.CreateResult(ResponseType.Error, "9", $"Запрос не соответствует схеме:\r\n{ex.Message}")
                };
            }

            return result is null;
        }

        public Result ValidateXml(MemoryStream memStream, string nameOfController, string apiversion)
        {
            // Достаем схемасет из кэша, если его там нет или он null создаем сет из
            var schemaSet = GetXmlSchemaSet(nameOfController, apiversion);
            BaseResult? xsdError = null;

            try
            {
                var xDoc = XDocument.Load(memStream);
                xDoc.Validate(schemaSet, (sender, e) =>
                {
                    var error = string.Concat(e.Severity, ": ", e.Message);
                    _logger.LogError("Запрос не соответствует схеме: {error}", error);
                    xsdError = new()
                    {
                        Error = error,
                        ErrorCode = 9,
                        Ticket = _ticketService.CreateResult(ResponseType.Error, "9", $"Запрос не соответствует схеме: {error}")
                    };
                });
                return xsdError is not null ? Result.Failure(new QBCH_lib.core.Error(9, $"Запрос не соответствует схеме: {xsdError.Error}")) : Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new QBCH_lib.core.Error(9, $"Запрос не соответствует схеме: {ex.Message}"));
            }
        }
    }
}
