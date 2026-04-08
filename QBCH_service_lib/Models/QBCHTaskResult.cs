using QBCH_lib.qcb_xml.v1_3.qcb_answer;
using QBCH_lib.qcb_xml.v2_0.qcb_answer;

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
    public class QBCHTaskResult(string? psrn, СведенияОПлатежах? answer = null, ОтветНаЗапросСведений? answer2 = null)
    {

        /// <summary>
        /// ОГРН КБКИ из конфига
        /// </summary>
        public string? BureauPSRN { get; set; } = psrn;

        /// <summary>
        /// Ответ КБКИ
        /// </summary>
        public СведенияОПлатежах? Answer { get; set; } = answer;

        /// <summary>
        /// 
        /// </summary>
        public ОтветНаЗапросСведений? Answer2 { get; set; } = answer2;
    }
}