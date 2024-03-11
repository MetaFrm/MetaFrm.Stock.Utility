namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// StatusBidAskAlarmMA
    /// </summary>
    public class StatusBidAskAlarmMA : ICore
    {
        /// <summary>
        /// CurrentStatus
        /// </summary>
        public string CurrentStatus { get; set; } = "";

        /// <summary>
        /// BidPrice
        /// </summary>
        public decimal BidPrice { get; set; }

        /// <summary>
        /// StopLossPrice
        /// </summary>
        public decimal StopLossPrice { get; set; }

        /// <summary>
        /// AskPrice
        /// </summary>
        public decimal AskPrice { get; set; }

        /// <summary>
        /// IsBid
        /// </summary>
        public bool IsBid { get; set; }
    }
}