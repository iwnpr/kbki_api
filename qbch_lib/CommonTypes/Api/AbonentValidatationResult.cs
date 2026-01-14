using System.Xml.Linq;

namespace QBCH_lib.CommonTypes.Api
{
    /// <summary>
    /// Результат обработки
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    public class AbonentValidatationResult(XElement? info) : BaseResult
    {

        /// <summary>
        /// ИНН
        /// </summary>
        public string? Inn { get; set; } = info?.Element("inn")?.Value;

        /// <summary>
        /// ОГРН
        /// </summary>
        public string? Ogrn { get; set; } = info?.Element("ogrn")?.Value;

        /// <summary>
        /// Полное имя
        /// </summary>
        public string? FullName { get; set; } = info?.Element("full_name")?.Value;

        /// <summary>
        /// Короткое имя
        /// </summary>
        public string? ShortName { get; set; } = info?.Element("short_name")?.Value;
    }
}
