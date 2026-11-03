using QBCH_lib.core;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

/// <summary>Сериализация и десериализация XML API 3.0.</summary>
public class XmlSerializerV3 : IXmlSerializerV3
{
    /// <inheritdoc />
    public Result<T?> DeserializeV3<T>(XElement? xml) where T : class
    {
        if (xml is null)
            return Result<T?>.Success(null);

        var serializer = CreateSerializerV3<T>();
        using var reader = xml.CreateReader();

        try
        {
            return Result<T?>.Success(serializer.Deserialize(reader) as T);
        }
        catch
        {
            return Result<T?>.Success(null);
        }
    }

    /// <inheritdoc />
    public Result<T?> DeserializeV3<T>(byte[]? bytes) where T : class
    {
        if (bytes is null)
            return Result<T?>.Success(null);

        using var ms = new MemoryStream(bytes);
        var serializer = CreateSerializerV3<T>();
        return Result<T?>.Success(serializer.Deserialize(ms) as T);
    }

    /// <inheritdoc />
    public Result<byte[]> SerializeAsByteV3<T>(T? item) where T : class
    {
        if (item is null)
            return Result<byte[]>.Success([]);

        var serializer = CreateSerializerV3<T>();
        XmlSerializerNamespaces ns = new();
        ns.Add("", "");

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false));
        serializer.Serialize(sw, item, ns);

        return Result<byte[]>.Success(ms.ToArray());
    }

    /// <inheritdoc />
    public Result<string> SerializeAsStringV3<T>(T? item) where T : class
    {
        if (item is null)
            return Result<string>.Success(string.Empty);

        var serializer = CreateSerializerV3<T>();
        XmlSerializerNamespaces ns = new();
        ns.Add("", "");

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false));
        serializer.Serialize(sw, item, ns);
        ms.Position = 0;

        using var sr = new StreamReader(ms);
        return Result<string>.Success(sr.ReadToEnd());
    }

    /// <inheritdoc />
    public Result<XDocument?> SerializeAsXDocumentV3<T>(T? item) where T : class
    {
        if (item is null)
            return Result<XDocument?>.Success(null);

        var xml = SerializeAsByteV3(item).Value!;
        using var ms = new MemoryStream(xml);
        return Result<XDocument?>.Success(XDocument.Load(ms));
    }

    private static XmlSerializer CreateSerializerV3<T>() where T : class
        => new(typeof(T));
}
