namespace Domain.QBCHModels.CommonTypes
{
    /// <summary>
    /// 
    /// </summary>
    public class QBCHResult : BaseResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string? QBCHPSRN { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Body { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public bool IsValid { get; set; }
    }
}
