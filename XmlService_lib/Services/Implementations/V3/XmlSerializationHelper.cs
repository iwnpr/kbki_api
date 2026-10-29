using QBCH_lib.core;
using qbch_lib.domain.errors;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

/// <summary>Единая точка сериализации и десериализации XML API 3.0.</summary>
public class XmlSerializationHelper : IXmlServiceV3
{
    public Result<T> DeserializeV3<T>(XElement? xml) where T : class
    {
        if (xml is null)
            return Result<T>.Failure(AnswerErrorCode.Code2_EmptyRequestBody());

        try
        {
            using var reader = xml.CreateReader();
            return Deserialize<T>(reader);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(AnswerErrorCode.Code9_InvalidRequestByScheme(ex.Message));
        }
    }

    public Result<T> DeserializeV3<T>(byte[]? bytes) where T : class
    {
        if (bytes is null || bytes.Length == 0)
            return Result<T>.Failure(AnswerErrorCode.Code2_EmptyRequestBody());

        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            return Deserialize<T>(reader);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(AnswerErrorCode.Code9_InvalidRequestByScheme(ex.Message));
        }
    }

    public Result<byte[]> SerializeAsByteV3<T>(T? item) where T : class
    {
        if (item is null)
            return Result<byte[]>.Failure(AnswerErrorCode.Code99_OtherError("Отсутствует объект для сериализации."));

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            serializer.Serialize(writer, item, namespaces);
            writer.Flush();
            return Result<byte[]>.Success(stream.ToArray());
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure(AnswerErrorCode.Code99_OtherError($"Ошибка сериализации XML: {ex.Message}"));
        }
    }

    public Result<string> SerializeAsStringV3<T>(T? item) where T : class
    {
        var result = SerializeAsByteV3(item);
        return result.IsSuccess
            ? Result<string>.Success(Encoding.UTF8.GetString(result.Value))
            : Result<string>.Failure(result.Error!);
    }

    public Result<XDocument> SerializeAsXDocumentV3<T>(T? item) where T : class
    {
        var result = SerializeAsByteV3(item);
        if (!result.IsSuccess)
            return Result<XDocument>.Failure(result.Error!);

        try
        {
            using var stream = new MemoryStream(result.Value);
            return Result<XDocument>.Success(XDocument.Load(stream));
        }
        catch (Exception ex)
        {
            return Result<XDocument>.Failure(AnswerErrorCode.Code99_OtherError($"Ошибка чтения XML: {ex.Message}"));
        }
    }

    private static Result<T> Deserialize<T>(XmlReader reader) where T : class
    {
        var value = new XmlSerializer(typeof(T)).Deserialize(reader) as T;
        return value is not null
            ? Result<T>.Success(value)
            : Result<T>.Failure(AnswerErrorCode.Code9_InvalidRequestByScheme($"Не удалось прочитать {typeof(T).Name}."));
    }
}
