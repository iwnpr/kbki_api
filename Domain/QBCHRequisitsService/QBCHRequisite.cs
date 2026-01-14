namespace Domain.QBCHRequisitsService
{
    /// <summary>
    /// Реквизиты БКИ
    /// </summary>
    public class QBCHRequisite : Requisites
    {
        /// <summary>
        /// Наименование Бюро
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Id бюро
        /// </summary>
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Базовый для запросов
        /// </summary>
        public string? Url { get; set; }
    }
}
