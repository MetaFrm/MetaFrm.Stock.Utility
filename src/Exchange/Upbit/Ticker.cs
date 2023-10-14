using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 현재가 정보
    /// </summary>
    public class Ticker : ICore
    {
        private string? cnage;

        /// <summary>
        /// TickerList
        /// </summary>
        public Ticker[]? TickerList { get; set; }

        /// <summary>
        /// 종목 구분 코드
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market { get; set; }

        /// <summary>
        /// 최근 거래 일자(UTC)
        /// </summary>
        [JsonPropertyName("trade_date")]
        public string? TradeDate { get; set; }

        /// <summary>
        /// 최근 거래 시각(UTC)
        /// </summary>
        [JsonPropertyName("trade_time")]
        public string? TradeTime { get; set; }

        /// <summary>
        /// 최근 거래 일자(KST)
        /// </summary>
        [JsonPropertyName("trade_date_kst")]
        public string? TradeDateKst { get; set; }

        /// <summary>
        /// 최근 거래 시각(KST)
        /// </summary>
        [JsonPropertyName("trade_time_kst")]
        public string? TradeTimeKst { get; set; }

        /// <summary>
        /// trade_timestamp
        /// </summary>
        [JsonPropertyName("trade_timestamp")]
        public long? TradeTimeStamp { get; set; }

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
        /// 종가(현재가)
        /// </summary>
        [JsonPropertyName("trade_price")]
        public decimal TradePrice { get; set; }

        /// <summary>
        /// 전일 종가
        /// </summary>
        [JsonPropertyName("prev_closing_price")]
        public decimal PrevClosingPrice { get; set; }

        /// <summary>
        /// 보합/상승/하락
        /// </summary>
        [JsonPropertyName("change")]
        public string? Change {
            get
            {
                return this.cnage;
            }
            set
            {
                if (value == "EVEN")
                    this.cnage = "보합";
                else if (value == "RISE")
                    this.cnage = "상승";
                else
                    this.cnage = "하락";
            }
        }

        /// <summary>
        /// 변화액의 절대값
        /// </summary>
        [JsonPropertyName("change_price")]
        public decimal ChangePrice { get; set; }

        /// <summary>
        /// 변화율의 절대값
        /// </summary>
        [JsonPropertyName("change_rate")]
        public decimal ChangeRate { get; set; }

        /// <summary>
        /// 부호가 있는 변화액
        /// </summary>
        [JsonPropertyName("signed_change_price")]
        public decimal SignedChangePrice { get; set; }

        /// <summary>
        /// 부호가 있는 변화율
        /// </summary>
        [JsonPropertyName("signed_change_rate")]
        public decimal SignedChangeRate { get; set; }

        /// <summary>
        /// 가장 최근 거래량
        /// </summary>
        [JsonPropertyName("trade_volume")]
        public decimal TradeVolume { get; set; }

        /// <summary>
        /// 누적 거래대금(UTC 0시 기준)
        /// </summary>
        [JsonPropertyName("acc_trade_price")]
        public decimal AccTradePrice { get; set; }

        /// <summary>
        /// 24시간 누적 거래대금
        /// </summary>
        [JsonPropertyName("acc_trade_price_24h")]
        public decimal AccTradePrice24h { get; set; }

        /// <summary>
        /// 누적 거래량(UTC 0시 기준)
        /// </summary>
        [JsonPropertyName("acc_trade_volume")]
        public decimal AccTradeVolume { get; set; }

        /// <summary>
        /// 24시간 누적 거래대금
        /// </summary>
        [JsonPropertyName("acc_trade_volume_24h")]
        public decimal AccTradeVolume24h { get; set; }

        /// <summary>
        /// 52주 신고가
        /// </summary>
        [JsonPropertyName("highest_52_week_price")]
        public decimal Highest52WeekPrice { get; set; }

        /// <summary>
        /// 52주 신고가 달성일
        /// </summary>
        [JsonPropertyName("highest_52_week_date")]
        public string? Highest52WeekDate { get; set; }

        /// <summary>
        /// 52주 신저가
        /// </summary>
        [JsonPropertyName("lowest_52_week_price")]
        public decimal Lowest52WeekPrice { get; set; }

        /// <summary>
        /// 52주 신저가 달성일
        /// </summary>
        [JsonPropertyName("lowest_52_week_date")]
        public string? Lowest52WeekDate { get; set; }

        /// <summary>
        /// 타임스탬프
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long? TimeStamp { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }

    ///// <summary>
    ///// 보합/상승/하락
    ///// </summary>
    //internal enum Cnage
    //{
    //    /// <summary>
    //    /// 보합
    //    /// </summary>
    //    EVEN,
    //    /// <summary>
    //    /// 상승
    //    /// </summary>
    //    RISE,
    //    /// <summary>
    //    /// 하락
    //    /// </summary>
    //    FALL
    //}
}