namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 주(Week) 캔들
    /// </summary>
    public class CandlesWeek : Candles
    {
        /// <summary>
        /// 캔들 기간의 가장 첫 날
        /// </summary>
        public string? FirstDayOfPeriod { get; set; }

        /// <summary>
        /// CandlesWeekList
        /// </summary>
        public List<CandlesWeek>? CandlesWeekList { get; set; }
    }
}