using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.core;
using QBCH_lib.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XmlService_lib.Services.Interfaces.V3;

namespace XmlService_lib.Services.Implementations.V3;

public class XmlServiceV3(
    IMemoryCache memoryCache,
    IConfiguration config,
    ILogger<XmlService> logger,
    ITicketService ticketService)
    : XmlService(memoryCache, config, logger, ticketService), IXmlServiceV3
{
    private static readonly Type[] V3KnownTypes =
    [
        typeof(ЗапросСведений),
        typeof(ОтветНаЗапросСведений),
        typeof(ПредставлениеСведений),
        typeof(РезультатПредставленияСведений),
        typeof(Результат)
    ];

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
        => ValidateXml(memStream, schemasFullPaths);

    public bool ValidateXmlV3(MemoryStream memStream, string nameOfController, [NotNullWhen(false)] out BaseResult? result)
        => ValidateXml(memStream, nameOfController, "3", out result);

    public Result ValidateXmlV3(MemoryStream memStream, string nameOfController)
        => ValidateXml(memStream, nameOfController, "3");

    private static XmlSerializer CreateSerializerV3<T>() where T : class
        => new(typeof(T), V3KnownTypes);
}
