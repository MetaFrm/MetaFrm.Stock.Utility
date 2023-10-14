namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 분(Minute) 캔들
    /// </summary>
    public class CandlesMinute : Candles
    {
        /// <summary>
        /// 분 단위(유닛)
        /// </summary>
        public int Unit { get; set; }

        /// <summary>
        /// CandlesMinuteList
        /// </summary>
        public List<CandlesMinute>? CandlesMinuteList { get; set; }
    }
}