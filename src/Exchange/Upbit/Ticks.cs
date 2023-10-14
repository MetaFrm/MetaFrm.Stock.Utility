using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 당일 체결 내역
    /// </summary>
    public class Ticks : ICore
    {
        /// <summary>
        /// TicksList
        /// </summary>
        public Ticks[]? TicksList { get; set; }

        /// <summary>
        /// 마켓 구분 코드
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market { get; set; }

        /// <summary>
        /// 체결 일자(UTC 기준)
        /// </summary>
        [JsonPropertyName("trade_date_utc")]
        public string? TradeDateUtc { get; set; }

        /// <summary>
        /// 체결 시각(UTC 기준)
        /// </summary>
        [JsonPropertyName("trade_time_utc")]
        public string? TradeTimeUtc { get; set; }

        /// <summary>
        /// 체결 타임스탬프
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long TimeStamp { get; set; }

        /// <summary>
        /// 체결 가격
        /// </summary>
        [JsonPropertyName("trade_price")]
        public decimal TradePrice { get; set; }

        /// <summary>
        /// 체결량
        /// </summary>
        [JsonPropertyName("trade_volume")]
        public decimal TradeVolume { get; set; }

        /// <summary>
        /// 전일 종가
        /// </summary>
        [JsonPropertyName("prev_closing_price")]
        public decimal PrevClosingPrice { get; set; }

        /// <summary>
        /// 변화량
        /// </summary>
        [JsonPropertyName("change_price")]
        public decimal ChangePrice { get; set; }

        /// <summary>
        /// 매도(ask)/매수(bid)
        /// </summary>
        [JsonPropertyName("ask_bid")]
        public string? Side { get; set; }

        /// <summary>
        /// 체결 번호(Unique)
        /// </summary>
        [JsonPropertyName("sequential_id")]
        public long Sequential_ID { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}