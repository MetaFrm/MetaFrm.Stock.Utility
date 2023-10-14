namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 월(Month) 캔들
    /// </summary>
    public class CandlesMonth : Candles
    {
        /// <summary>
        /// 캔들 기간의 가장 첫 날
        /// </summary>
        public string? FirstDayOfPeriod { get; set; }

        /// <summary>
        /// CandlesMonthList
        /// </summary>
        public List<CandlesMonth>? CandlesMonthList { get; set; }
    }
}