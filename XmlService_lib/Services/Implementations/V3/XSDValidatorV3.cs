using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH_lib.core;
using qbch_lib.domain.errors;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

/// <summary>Проверяет XML по схемам API 3.0, не изменяя состояние обработки запроса.</summary>
public class XSDValidatorV3(IMemoryCache memoryCache, IConfiguration config,
    IXmlServiceV3 xmlService, ILogger<XSDValidatorV3> logger) : IXSDValidatorV3
{
    private readonly IMemoryCache _cache = memoryCache;
    private readonly IConfiguration _config = config;

    private static readonly IReadOnlyDictionary<string, string[]> V3Schemas = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["dlrequest"] = ["qcb_request.xsd", "qcb_common.xsd"],
        ["dlput"] = ["qcb_put.xsd", "qcb_common.xsd"],
        ["qcb_result"] = ["qcb_result.xsd", "qcb_common.xsd"],
        ["qcb_answer"] = ["qcb_answer.xsd", "qcb_common.xsd"],
        ["qcb_putanswer"] = ["qcb_putanswer.xsd", "qcb_common.xsd"]
    };

    /// <inheritdoc />
    public Result<bool> Validate(byte[]? xml, string schemaName)
    {
        if (xml is null || xml.Length == 0)
            return Result<bool>.Failure(AnswerErrorCode.Code2_EmptyRequestBody());

        XmlSchemaSet schemas;
        try
        {
            schemas = GetXmlSchemaSetV3(schemaName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить XSD-схемы {SchemaName}", schemaName);
            return Result<bool>.Failure(AnswerErrorCode.Code99_OtherError("Не удалось загрузить XSD-схемы."));
        }

        try
        {
            using var stream = new MemoryStream(xml);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            var document = XDocument.Load(reader);
            AnswerErrorCode? error = null;
            document.Validate(schemas, (_, e) =>
                error ??= AnswerErrorCode.Code9_InvalidRequestByScheme($"{e.Severity}: {e.Message}"));

            return error is null ? Result<bool>.Success(true) : Result<bool>.Failure(error);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Не удалось проверить XML по схеме {SchemaName}", schemaName);
            return Result<bool>.Failure(AnswerErrorCode.Code9_InvalidRequestByScheme(ex.Message));
        }
    }

    /// <inheritdoc />
    public Result<T> ValidateAndDeserialize<T>(byte[]? xml, string schemaName) where T : class
    {
        var validation = Validate(xml, schemaName);
        return validation.IsSuccess
            ? xmlService.DeserializeV3<T>(xml)
            : Result<T>.Failure(validation.Error!);
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

        schemaSet.Compile();
        _cache.Set(cacheKey, schemaSet, new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
        return schemaSet;
    }

}
