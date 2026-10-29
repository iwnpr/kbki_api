using QBCH_lib.core;
using System.Xml.Linq;

namespace XmlService_lib.Services.Interfaces.V3;

/// <summary>Преобразование объектов в XML и обратно, без XSD и бизнес-проверок.</summary>
public interface IXmlSerializerV3
{
    /// <summary>Десериализует XML-элемент в объект.</summary>
    /// <typeparam name="T">Тип объекта XML.</typeparam>
    /// <param name="xml">XML-элемент для десериализации.</param>
    /// <returns>Объект или ошибка чтения XML; для отсутствующего элемента — ошибка пустого тела.</returns>
    Result<T> DeserializeV3<T>(XElement? xml) where T : class;
    /// <summary>Десериализует XML из массива байтов в объект.</summary>
    /// <typeparam name="T">Тип объекта XML.</typeparam>
    /// <param name="bytes">Байты XML-документа.</param>
    /// <returns>Объект или ошибка чтения XML; для пустого массива — ошибка пустого тела.</returns>
    Result<T> DeserializeV3<T>(byte[]? bytes) where T : class;
    /// <summary>Сериализует объект в XML в кодировке UTF-8 без BOM.</summary>
    /// <typeparam name="T">Тип объекта XML.</typeparam>
    /// <param name="item">Объект для сериализации.</param>
    /// <returns>Байты XML или ошибка сериализации, в том числе при отсутствии объекта.</returns>
    Result<byte[]> SerializeAsByteV3<T>(T? item) where T : class;
    /// <summary>Сериализует объект в строку XML.</summary>
    /// <typeparam name="T">Тип объекта XML.</typeparam>
    /// <param name="item">Объект для сериализации.</param>
    /// <returns>Строка XML или ошибка сериализации.</returns>
    Result<string> SerializeAsStringV3<T>(T? item) where T : class;
    /// <summary>Сериализует объект в XML-документ.</summary>
    /// <typeparam name="T">Тип объекта XML.</typeparam>
    /// <param name="item">Объект для сериализации.</param>
    /// <returns>XML-документ или ошибка сериализации либо чтения XML.</returns>
    Result<XDocument> SerializeAsXDocumentV3<T>(T? item) where T : class;
}
