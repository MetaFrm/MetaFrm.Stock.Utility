namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 현재가 정보
    /// </summary>
    public class Ticker : ICore
    {
        /// <summary>
        /// TickerList
        /// </summary>
        public List<Ticker> TickerList { get; set; } = new();

        /// <summary>
        /// ExchangeID
        /// </summary>
        public int ExchangeID { get; set; }

        /// <summary>
        /// 종목 구분 코드
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// Icon
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 최근 거래 일자(UTC)
        /// </summary>
        public string? TradeDate { get; set; }
        /// <summary>
        /// 최근 거래 시각(UTC)
        /// </summary>
        public string? TradeTime { get; set; }

        /// <summary>
        /// 최근 거래 일자(KST)
        /// </summary>
        public string? TradeDateKst { get; set; }
        /// <summary>
        /// 최근 거래 시각(KST)
        /// </summary>
        public string? TradeTimeKst { get; set; }

        /// <summary>
        /// TradeTimeStamp
        /// </summary>
        public long? TradeTimeStamp { get; set; }

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
        /// 종가(현재가)
        /// </summary>
        public decimal TradePrice { get; set; }

        /// <summary>
        /// 전일 종가
        /// </summary>
        public decimal PrevClosingPrice { get; set; }

        /// <summary>
        /// 보합/상승/하락
        /// </summary>
        public string? Change { get; set; }

        /// <summary>
        /// 변화액의 절대값
        /// </summary>
        public decimal ChangePrice { get; set; }

        /// <summary>
        /// 변화율의 절대값
        /// </summary>
        public decimal ChangeRate { get; set; }

        /// <summary>
        /// 부호가 있는 변화액
        /// </summary>
        public decimal SignedChangePrice { get; set; }

        /// <summary>
        /// 부호가 있는 변화율
        /// </summary>
        public decimal SignedChangeRate { get; set; }

        /// <summary>
        /// 가장 최근 거래량
        /// </summary>
        public decimal TradeVolume { get; set; }

        /// <summary>
        /// 누적 거래대금(UTC 0시 기준)
        /// </summary>
        public decimal AccTradePrice { get; set; }

        /// <summary>
        /// 24시간 누적 거래대금
        /// </summary>
        public decimal AccTradePrice24h { get; set; }

        /// <summary>
        /// 누적 거래량(UTC 0시 기준)
        /// </summary>
        public decimal AccTradeVolume { get; set; }

        /// <summary>
        /// 24시간 누적 거래대금
        /// </summary>
        public decimal AccTradeVolume24h { get; set; }

        /// <summary>
        /// 52주 신고가
        /// </summary>
        public decimal Highest52WeekPrice { get; set; }

        /// <summary>
        /// 52주 신고가 달성일
        /// </summary>
        public string? Highest52WeekDate { get; set; }

        /// <summary>
        /// 52주 신저가
        /// </summary>
        public decimal Lowest52WeekPrice { get; set; }

        /// <summary>
        /// 52주 신저가 달성일
        /// </summary>
        public string? Lowest52WeekDate { get; set; }

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public long? TimeStamp { get; set; }

        /// <summary>
        /// LastDateTime
        /// </summary>
        public DateTime LastDateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}