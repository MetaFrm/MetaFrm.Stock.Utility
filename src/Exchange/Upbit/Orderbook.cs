using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 호가 정보 조회
    /// </summary>
    public class Orderbook : ICore
    {
        /// <summary>
        /// OrderbookList
        /// </summary>
        public Orderbook[]? OrderbookList { get; set; }

        /// <summary>
        /// 마켓 코드
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market { get; set; }

        /// <summary>
        /// 호가 생성 시각
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long? TimeStamp { get; set; }

        /// <summary>
        /// 호가 매도 총 잔량
        /// </summary>
        [JsonPropertyName("total_ask_size")]
        public decimal TotalAskSize { get; set; }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        [JsonPropertyName("total_bid_size")]
        public decimal TotalBidSize { get; set; }

        /// <summary>
        /// 호가
        /// </summary>
        [JsonPropertyName("orderbook_units")]
        public IList<OrderbookUnit>? OrderbookUnits { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}