using QBCH_lib.CommonTypes.Api;
using QBCH_lib.core;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;

namespace XmlService_lib.Services.Interfaces.V3;

public interface IXmlServiceV3
{
    /// <summary>
    /// Десериализация из xDocument
    /// </summary>
    /// <typeparam name="T">Тип</typeparam>
    /// <param name="stream">Поток</param>
    /// <returns>Десериалиованный объект класса</returns>
    T? DeserializeV3<T>(XDocument? xml) where T : class;

    /// <summary>
    /// Десериализация из xDocument
    /// </summary>
    /// <typeparam name="T">Тип</typeparam>
    /// <param name="stream">Поток</param>
    /// <returns>Десериалиованный объект класса</returns>
    T? DeserializeV3<T>(byte[]? bytes) where T : class;

    /// <summary>
    /// Сериалищзация в строку
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <returns></returns>
    byte[] SerializeAsByteV3<T>(T? item) where T : class;

    /// <summary>
    /// Сериализация в строку
    /// </summary>
    /// <typeparam name="T">Тип</typeparam>
    /// <param name="item">Обект класса</param>
    /// <returns>Класс в виде xml string</returns>
    string SerializeAsStringV3<T>(T? item) where T : class;

    /// <summary>
    /// Сериализация в строку
    /// </summary>
    /// <typeparam name="T">Тип</typeparam>
    /// <param name="item">Обект класса</param>
    /// <returns>Класс в виде xml string</returns>
    XDocument? SerializeAsXDocumentV3<T>(T? item) where T : class;

    /// <summary>
    /// Валидация xml
    /// </summary>
    /// <param name="memStream">Xml</param>
    /// <param name="schemasFullPaths">Полный путь к схемам</param>
    /// <returns></returns>
    BaseResult? ValidateXmlV3(MemoryStream memStream, string[] schemasFullPaths);

    /// <summary>
    /// Валидация xml
    /// </summary>
    /// <param name="memStream">Поток XDocument</param>
    /// <param name="nameOfController">Имя контроллера для поиска Xsd в MemoryCache</param>
    /// <returns></returns>
    bool ValidateXmlV3(MemoryStream memStream, string nameOfController, [NotNullWhen(false)] out BaseResult? result);

    /// <summary>
    /// Валидация xml (перегрузка Result)
    /// </summary>
    /// <param name="memStream">Поток XDocument</param>
    /// <param name="nameOfController">Имя контроллера для поиска Xsd в MemoryCache</param>
    /// <returns></returns>
    Result ValidateXmlV3(MemoryStream memStream, string nameOfController);
}
