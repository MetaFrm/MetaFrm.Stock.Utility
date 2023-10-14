namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 당일 체결 내역
    /// </summary>
    public class Ticks : ICore
    {
        /// <summary>
        /// TicksList
        /// </summary>
        public List<Ticks>? TicksList { get; set; }

        /// <summary>
        /// 마켓 구분 코드
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// 체결 일자(UTC 기준)
        /// </summary>
        public string? TradeDateUtc { get; set; }

        /// <summary>
        /// 체결 시각(UTC 기준)
        /// </summary>
        public string? TradeTimeUtc { get; set; }

        /// <summary>
        /// 체결 타임스탬프
        /// </summary>
        public long TimeStamp { get; set; }

        /// <summary>
        /// 체결 가격
        /// </summary>
        public decimal TradePrice { get; set; }

        /// <summary>
        /// 체결량
        /// </summary>
        public decimal TradeVolume { get; set; }

        /// <summary>
        /// 전일 종가
        /// </summary>
        public decimal PrevClosingPrice { get; set; }

        /// <summary>
        /// 변화량
        /// </summary>
        public decimal ChangePrice { get; set; }

        /// <summary>
        /// 매도(ask)/매수(bid)
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// 체결 번호(Unique)
        /// </summary>
        public long Sequential_ID { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}