using QBCH_lib.core;
using qbch_lib.domain.errors;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

/// <summary>Единая точка сериализации и десериализации XML API 3.0.</summary>
public class XmlSerializerV3 : IXmlSerializerV3
{
    /// <inheritdoc />
    public Result<T?> DeserializeV3<T>(XElement? xml) where T : class
    {
        if (xml is null)
            return Result<T?>.Success(null);

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = xml.CreateReader();
            try
            {
                return Result<T?>.Success(serializer.Deserialize(reader) as T);
            }
            catch
            {
                // XML-фрагменты из хранилища и раньше возвращали null при ошибке чтения.
                return Result<T?>.Success(null);
            }
        }
        catch (Exception ex)
        {
            return Result<T?>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<T?> DeserializeV3<T>(byte[]? bytes) where T : class
    {
        if (bytes is null)
            return Result<T?>.Success(null);

        try
        {
            using var stream = new MemoryStream(bytes);
            return Result<T?>.Success(new XmlSerializer(typeof(T)).Deserialize(stream) as T);
        }
        catch (Exception ex)
        {
            return Result<T?>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<byte[]> SerializeAsByteV3<T>(T? item) where T : class
    {
        if (item is null)
            return Result<byte[]>.Success([]);

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            serializer.Serialize(writer, item, namespaces);
            return Result<byte[]>.Success(stream.ToArray());
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<string> SerializeAsStringV3<T>(T? item) where T : class
    {
        var result = SerializeAsByteV3(item);
        if (!result.IsSuccess)
            return Result<string>.Failure(result.Error!);

        try
        {
            using var stream = new MemoryStream(result.Value);
            using var reader = new StreamReader(stream);
            return Result<string>.Success(reader.ReadToEnd());
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(new OperationError(ex));
        }
    }

    /// <inheritdoc />
    public Result<XDocument?> SerializeAsXDocumentV3<T>(T? item) where T : class
    {
        if (item is null)
            return Result<XDocument?>.Success(null);

        var result = SerializeAsByteV3(item);
        if (!result.IsSuccess)
            return Result<XDocument?>.Failure(result.Error!);

        try
        {
            using var stream = new MemoryStream(result.Value);
            return Result<XDocument?>.Success(XDocument.Load(stream));
        }
        catch (Exception ex)
        {
            return Result<XDocument?>.Failure(new OperationError(ex));
        }
    }
}
