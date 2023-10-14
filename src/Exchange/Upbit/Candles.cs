using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 일(Day) 캔들
    /// </summary>
    public class Candles : ICore
    {
        /// <summary>
        /// 마켓명
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market { get; set; }

        /// <summary>
        /// 캔들 기준 시각(UTC 기준)
        /// </summary>
        [JsonPropertyName("candle_date_time_utc")]
        public DateTime CandleDateTimeUtc { get; set; }

        /// <summary>
        /// 캔들 기준 시각(KST 기준)
        /// </summary>
        [JsonPropertyName("candle_date_time_kst")]
        public DateTime CandleDateTimeKst { get; set; }

        /// <summary>
        /// 시가
        /// </summary>
        [JsonPropertyName("opening_price")]
        public decimal OpeningPrice { get; set; }

        /// <summary>
        /// 고가
        /// </summary>
        [JsonPropertyName("high_price")]
        public decimal HighPrice { get; set; }

        /// <summary>
        /// 저가
        /// </summary>
        [JsonPropertyName("low_price")]
        public decimal LowPrice { get; set; }

        /// <summary>
        /// 종가
        /// </summary>
        [JsonPropertyName("trade_price")]
        public decimal TradePrice { get; set; }

        /// <summary>
        /// 마지막 틱이 저장된 시각
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long? TimeStamp { get; set; }

        /// <summary>
        /// 누적 거래 금액
        /// </summary>
        [JsonPropertyName("candle_acc_trade_price")]
        public decimal CandleAccTradePrice { get; set; }

        /// <summary>
        /// 누적 거래량
        /// </summary>
        [JsonPropertyName("candle_acc_trade_volume")]
        public decimal CandleAccTradeVolume { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}