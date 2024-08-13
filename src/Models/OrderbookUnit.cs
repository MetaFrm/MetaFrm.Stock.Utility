namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 호가
    /// </summary>
    public class OrderbookUnit : ICore
    {
        /// <summary>
        /// 매도호가
        /// </summary>
        public decimal AskPrice { get; set; }

        /// <summary>
        /// 매수호가
        /// </summary>
        public decimal BidPrice { get; set; }

        /// <summary>
        /// 매도 잔량
        /// </summary>
        public decimal AskSize { get; set; }

        /// <summary>
        /// 매수 잔량
        /// </summary>
        public decimal BidSize { get; set; }

        /// <summary>
        /// AskKrw
        /// </summary>
        public decimal AskKrw { get; set; }

        /// <summary>
        /// BidKrw
        /// </summary>
        public decimal BidKrw { get; set; }
    }
}