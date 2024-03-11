namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 일(Day) 캔들
    /// </summary>
    public class Candles : ICore
    {
        /// <summary>
        /// 마켓명
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// 캔들 기준 시각(UTC 기준)
        /// </summary>
        public DateTime CandleDateTimeUtc { get; set; }

        /// <summary>
        /// 캔들 기준 시각(KST 기준)
        /// </summary>
        public DateTime CandleDateTimeKst { get; set; }

        /// <summary>
        /// 시가
        /// </summary>
        public decimal OpeningPrice { get; set; }

        /// <summary>
        /// 고가
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// 저가
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// 종가
        /// </summary>
        public decimal TradePrice { get; set; }

        /// <summary>
        /// 마지막 틱이 저장된 시각
        /// </summary>
        public long? TimeStamp { get; set; }

        /// <summary>
        /// 누적 거래 금액
        /// </summary>
        public decimal CandleAccTradePrice { get; set; }

        /// <summary>
        /// 누적 거래량
        /// </summary>
        public decimal CandleAccTradeVolume { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }

        /// <summary>
        /// TradePrice1
        /// 차트용
        /// </summary>
        public decimal TradePrice1 { get; set; }

        /// <summary>
        /// CandleAccTradeVolume1
        /// 차트용
        /// </summary>
        public decimal CandleAccTradeVolume1 { get; set; }

        /// <summary>
        /// 보조지표
        /// </summary>
        public Dictionary<string, decimal?> SecondaryIndicator { get; set; } = new();

        /// <summary>
        /// SecondaryIndicatorCount
        /// </summary>
        public int SecondaryIndicatorCount => this.SecondaryIndicator.Count;
    }
}