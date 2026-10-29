using QBCH_lib.core;
using System.Xml.Linq;

namespace XmlService_lib.Services.Interfaces.V3;

/// <summary>Преобразование объектов в XML и обратно, без XSD и бизнес-проверок.</summary>
public interface IXmlServiceV3
{
    Result<T> DeserializeV3<T>(XElement? xml) where T : class;
    Result<T> DeserializeV3<T>(byte[]? bytes) where T : class;
    Result<byte[]> SerializeAsByteV3<T>(T? item) where T : class;
    Result<string> SerializeAsStringV3<T>(T? item) where T : class;
    Result<XDocument> SerializeAsXDocumentV3<T>(T? item) where T : class;
}
