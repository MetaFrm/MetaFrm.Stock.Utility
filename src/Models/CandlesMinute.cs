namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 분(Minute) 캔들
    /// </summary>
    public class CandlesMinute : Candles
    {
        /// <summary>
        /// ExchangeID
        /// </summary>
        public int ExchangeID { get; set; }

        /// <summary>
        /// 분 단위(유닛)
        /// </summary>
        public int Unit { get; set; }

        /// <summary>
        /// CandlesMinuteList
        /// </summary>
        public List<CandlesMinute>? CandlesMinuteList { get; set; } = [];

        /// <summary>
        /// CandlesMinute
        /// </summary>
        /// <param name="market"></param>
        /// <param name="exchangeID"></param>
        /// <param name="unit"></param>
        public CandlesMinute(string market, int exchangeID, int unit)
        {
            this.Market = market;
            this.ExchangeID = exchangeID;
            this.Unit = unit;
        }
    }
}