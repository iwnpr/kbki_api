using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.core;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

public class XmlServiceV3(
    IMemoryCache memoryCache,
    IConfiguration config,
    ILogger<XmlServiceV3> logger)
    : IXmlServiceV3
{
    private const int SchemaValidationErrorCode = 9;
    private readonly IMemoryCache _cache = memoryCache;
    private readonly IConfiguration _config = config;
    private readonly ILogger<XmlServiceV3> _logger = logger;

    private static readonly Type[] V3KnownTypes =
    [
        typeof(ЗапросСведений),
        typeof(ОтветНаЗапросСведений),
        typeof(ПредставлениеСведений),
        typeof(РезультатПредставленияСведений),
        typeof(Результат)
    ];

    private static readonly IReadOnlyDictionary<string, string[]> V3Schemas = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["dlrequest"] = ["qcb_request.xsd", "qcb_common.xsd"],
        ["dlput"] = ["qcb_put.xsd", "qcb_common.xsd"],
        ["qcb_result"] = ["qcb_result.xsd", "qcb_common.xsd"],
        ["qcb_answer"] = ["qcb_answer.xsd", "qcb_common.xsd"],
        ["qcb_putanswer"] = ["qcb_putanswer.xsd", "qcb_common.xsd"]
    };

    public T? DeserializeV3<T>(XDocument? xml) where T : class
    {
        if (xml is null)
            return null;

        var serializer = CreateSerializerV3<T>();
        using var reader = xml.CreateReader();
        return serializer.Deserialize(reader) as T;
    }

    public T? DeserializeV3<T>(byte[]? bytes) where T : class
    {
        if (bytes is null)
            return null;

        using var ms = new MemoryStream(bytes);
        var serializer = CreateSerializerV3<T>();
        return serializer.Deserialize(ms) as T;
    }

    public byte[] SerializeAsByteV3<T>(T? item) where T : class
    {
        if (item is null)
            return [];

        var serializer = CreateSerializerV3<T>();
        XmlSerializerNamespaces ns = new();
        ns.Add("", "");

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false));
        serializer.Serialize(sw, item, ns);

        return ms.ToArray();
    }

    public string SerializeAsStringV3<T>(T? item) where T : class
    {
        if (item is null)
            return string.Empty;

        var serializer = CreateSerializerV3<T>();
        XmlSerializerNamespaces ns = new();
        ns.Add("", "");

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false));
        serializer.Serialize(sw, item, ns);
        ms.Position = 0;

        using var sr = new StreamReader(ms);
        return sr.ReadToEnd();
    }

    public XDocument? SerializeAsXDocumentV3<T>(T? item) where T : class
    {
        if (item is null)
            return null;

        var xml = SerializeAsByteV3(item);
        using var ms = new MemoryStream(xml);
        return XDocument.Load(ms);
    }

    public BaseResult? ValidateXmlV3(MemoryStream memStream, string[] schemasFullPaths)
    {
        var schemaSet = new XmlSchemaSet
        {
            XmlResolver = new XmlUrlResolver()
        };

        foreach (var schemaPath in schemasFullPaths)
            schemaSet.Add(null, schemaPath);

        return ValidateAgainstSchemaSet(memStream, schemaSet);
    }

    public bool ValidateXmlV3(MemoryStream memStream, string nameOfController, [NotNullWhen(false)] out BaseResult? result)
    {
        var schemaSet = GetXmlSchemaSetV3(nameOfController);
        result = ValidateAgainstSchemaSet(memStream, schemaSet);
        return result is null;
    }

    public Result ValidateXmlV3(MemoryStream memStream, string nameOfController)
    {
        var schemaSet = GetXmlSchemaSetV3(nameOfController);
        var validationError = ValidateAgainstSchemaSet(memStream, schemaSet);
        return validationError is null
            ? Result.Success()
            : Result.Failure(new QBCH_lib.core.Error(SchemaValidationErrorCode, $"Запрос не соответствует схеме: {validationError.Error}"));
    }

    private XmlSchemaSet GetXmlSchemaSetV3(string nameOfController)
    {
        if (!V3Schemas.TryGetValue(nameOfController, out var schemaFiles))
            throw new InvalidOperationException($"Не найден набор схем для '{nameOfController}' в API 3.0.");

        var cacheKey = $"3.0:{nameOfController}";
        if (_cache.TryGetValue(cacheKey, out XmlSchemaSet? schemaSet) && schemaSet is not null)
            return schemaSet;

        var xsdFolder = _config.GetValue<string>("Paths:Xsd") ?? "xsd";
        var schemaSearchRoots = new[]
        {
            Path.Combine(AppContext.BaseDirectory, xsdFolder, "3.0"),
            Path.Combine(AppContext.BaseDirectory, xsdFolder, "3"),
            Path.Combine(Directory.GetCurrentDirectory(), "qbch_lib", "qcb_xml", "v3_0")
        };

        schemaSet = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        foreach (var schemaFile in schemaFiles)
        {
            var resolvedPath = schemaSearchRoots
                .Select(root => Path.Combine(root, schemaFile))
                .FirstOrDefault(File.Exists);

            if (resolvedPath is null)
                throw new FileNotFoundException($"Не найдена XSD-схема API 3.0: {schemaFile}");

            schemaSet.Add(null, resolvedPath);
        }

        _cache.Set(cacheKey, schemaSet, new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
        return schemaSet;
    }

    private BaseResult? ValidateAgainstSchemaSet(MemoryStream memStream, XmlSchemaSet schemaSet)
    {
        try
        {
            memStream.Position = 0;
            var xDoc = XDocument.Load(memStream);
            BaseResult? xsdError = null;

            xDoc.Validate(schemaSet, (_, e) =>
            {
                var reason = string.Concat(e.Severity, ": ", e.Message);
                _logger.LogError("Запрос не соответствует схеме:\r\n{error}", reason);
                xsdError = CreateSchemaError(reason);
            });

            return xsdError;
        }
        catch (Exception ex)
        {
            return CreateSchemaError(ex.Message);
        }
    }

    private BaseResult CreateSchemaError(string reason) =>
        new()
        {
            ErrorCode = SchemaValidationErrorCode,
            Error = reason,
            ErrorMessage = reason
        };

    private static XmlSerializer CreateSerializerV3<T>() where T : class
        => new(typeof(T), V3KnownTypes);
}
