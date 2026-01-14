namespace QBCH_lib.CommonTypes.Api
{
    /// <summary>
    /// Ошибка
    /// </summary>
    /// <remarks>
    /// Конструктор
    /// </remarks>
    /// <param name="message">Сообщение</param>
    public class Error(string? message = null)
    {

        /// <summary>
        ///  Текст ошибки
        /// </summary>
        public string? Message { get; set; } = message;
    }
}