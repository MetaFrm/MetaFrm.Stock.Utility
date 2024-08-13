namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// OrderbookSupportedLevel
    /// </summary>
    public class OrderbookSupportedLevel : ICore
    {
        /// <summary>
        /// OrderbookSupportedLevelList
        /// </summary>
        public List<OrderbookSupportedLevel>? OrderbookSupportedLevelList { get; set; }

        /// <summary>
        /// Market
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// SupportedLevels
        /// </summary>
        public decimal[]? SupportedLevels { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}