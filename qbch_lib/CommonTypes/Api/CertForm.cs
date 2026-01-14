using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QBCH_lib.CommonTypes.Api
{
    /// <summary>
    /// Форма для запросов с сертификатами
    /// </summary>
    [BindProperties]
    /// <summary>
    /// 
    /// </summary>
    public class CertForm
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// Сертификат
        /// </summary>
        public IFormFile? cert { get; set; }

        /// <summary>
        /// Отсоединенная подпись
        /// </summary>
        public IFormFile? sign { get; set; }
    }
}
