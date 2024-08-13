using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// OrderbookSupportedLevel
    /// </summary>
    public class OrderbookSupportedLevel : ICore
    {
        /// <summary>
        /// OrderbookSupportedLevel
        /// </summary>
        public OrderbookSupportedLevel[]? ApiKyesList { get; set; }

        /// <summary>
        /// Market
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market { get; set; }

        /// <summary>
        /// SupportedLevels
        /// </summary>
        [JsonPropertyName("supported_levels")]
        public decimal[]? SupportedLevels { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}