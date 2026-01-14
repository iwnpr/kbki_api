using Domain.QBCHModels.qcb_xml.v2_0.qcb_answer;

namespace Domain.QBCHServiceModels
{
    /// <summary>
    /// Результат обработки КБКИ
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    /// <param name="psrn">огрн КБКИ</param>
    /// <param name="answer">Ответ</param>
    public class QBCHTaskResult(string? psrn, ОтветНаЗапросСведений? answer2 = null)
    {

        /// <summary>
        /// ОГРН КБКИ из конфига
        /// </summary>
        public string? BureauPSRN { get; set; } = psrn;

        /// <summary>
        /// 
        /// </summary>
        public ОтветНаЗапросСведений? Answer2 { get; set; } = answer2;
    }
}
