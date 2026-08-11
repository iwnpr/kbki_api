using ОтветНаЗапросСведенийV3 = QBCH.Lib.qcb_xml.v3_0.ОтветНаЗапросСведений;

namespace QBCHService_lib.Models
{
    /// <summary>
    /// Результат обработки ответа КБКИ.
    /// Legacy-свойство Answer сохранено для обратной совместимости,
    /// а основным контрактом для API 3.0 является AnswerV3.
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    /// <param name="psrn">огрн КБКИ</param>
    /// <param name="answer">Ответ</param>
    public class QBCHTaskResult(string? psrn, ОтветНаЗапросСведенийV3? answer3 = null)
    {

        /// <summary>
        /// ОГРН КБКИ из конфига
        /// </summary>
        public string? BureauPSRN { get; set; } = psrn;

        /// <summary>
        /// Ответ КБКИ API 3.0.
        /// </summary>
        public ОтветНаЗапросСведенийV3? Answer3 { get; set; } = answer3;
    }
}