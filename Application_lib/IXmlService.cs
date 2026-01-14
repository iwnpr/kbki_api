using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using Domain;
using Domain.QBCHModels.CommonTypes;

namespace Application_lib
{
    /// <summary>
    /// 
    /// </summary>
    public interface IXmlService
    {
        /// <summary>
        /// Десериализация из xDocument
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="stream">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        T? Deserialize<T>(XDocument? xml) where T : class;

        /// <summary>
        /// Десериализация из xDocument
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="stream">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        T? Deserialize<T>(byte[]? bytes) where T : class;

        /// <summary>
        /// Десериализация из xDocument
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="stream">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        T? Deserialize<T>(XElement? xml) where T : class;

        /// <summary>
        /// Десериализация из потока
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="stream">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        T? Deserialize<T>(Stream? stream) where T : class;

        /// <summary>
        /// Десериализация из XmlReader
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="reader">Поток</param>
        /// <returns>Десериалиованный объект класса</returns>
        T? Deserialize<T>(XmlReader? reader) where T : class;

        /// <summary>
        /// Сериализация в строку
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>Класс в виде xml string</returns>
        Task<string> SerializeAsync<T>(T item) where T : class;

        /// <summary>
        /// Сериализация в поток
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>Класс в виде xml string</returns>
        Stream SerializeAsStream<T>(T? item) where T : class;

        /// <summary>
        /// Сериалищзация в строку
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        string SerializeAsString<T>(T? item) where T : class;

        /// <summary>
        /// Сериализация в строку
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>Класс в виде xml string</returns>
        byte[] SerializeAsByte<T>(T? item) where T : class;

        /// <summary>
        /// Сериализация в строку
        /// </summary>
        /// <typeparam name="T">Тип</typeparam>
        /// <param name="item">Обект класса</param>
        /// <returns>Класс в виде xml string</returns>
        Task<XDocument> SerializeAsXDocumentAsync<T>(T item) where T : class;

        /// <summary>
        /// Валидация xml
        /// </summary>
        /// <param name="memStream">Xml</param>
        /// <param name="schemasFullPaths">Полный путь к схемам</param>
        /// <returns></returns>
        BaseResult? ValidateXml(MemoryStream memStream, string[] schemasFullPaths);

        ///// <summary>
        ///// Валидация xml
        ///// </summary>
        ///// <param name="xDoc">Xml</param>
        ///// <param name="schemasFullPaths">Полный путь к схемам</param>
        ///// <returns></returns>
        //BaseResult? ValidateXml(XDocument xDoc, string[] schemasFullPaths);

        /// <summary>
        /// Валидация xml
        /// </summary>
        /// <param name="memStream">Поток XDocument</param>
        /// <param name="nameOfController">Имя контроллера для поиска Xsd в MemoryCache</param>
        /// <returns></returns>
        bool ValidateXml(MemoryStream memStream, string nameOfController, string apiversion, [NotNullWhen(false)] out BaseResult? result);

        /// <summary>
        /// Валидация xml (перегрузка Result)
        /// </summary>
        /// <param name="memStream">Поток XDocument</param>
        /// <param name="nameOfController">Имя контроллера для поиска Xsd в MemoryCache</param>
        /// <returns></returns>
        public Result ValidateXml(MemoryStream memStream, string nameOfController, string apiversion);
    }
}
