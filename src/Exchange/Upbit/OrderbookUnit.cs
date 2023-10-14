using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 호가
    /// </summary>
    public class OrderbookUnit : ICore
    {
        /// <summary>
        /// 매도호가
        /// </summary>
        [JsonPropertyName("ask_price")]
        public decimal AskPrice { get; set; }

        /// <summary>
        /// 매수호가
        /// </summary>
        [JsonPropertyName("bid_price")]
        public decimal BidPrice { get; set; }

        /// <summary>
        /// 매도 잔량
        /// </summary>
        [JsonPropertyName("ask_size")]
        public decimal AskSize { get; set; }

        /// <summary>
        /// 매수 잔량
        /// </summary>
        [JsonPropertyName("bid_size")]
        public decimal BidSize { get; set; }
    }
}