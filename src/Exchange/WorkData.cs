namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// WorkData
    /// </summary>
    public class WorkData
    {
        /// <summary>
        /// BidPrice
        /// </summary>
        public decimal BidPrice { get; set; } = 0;
        /// <summary>
        /// BidQty
        /// </summary>
        public decimal BidQty { get; set; } = 0;
        /// <summary>
        /// BidAvg
        /// </summary>
        public decimal BidAvgPrice { get; set; } = 0;
        /// <summary>
        /// BidTotalFee
        /// </summary>
        public decimal BidTotalFee { get; set; } = 0;
        /// <summary>
        /// BidOrderChecked
        /// </summary>
        public bool BidOrderChecked { get; set; }
        /// <summary>
        /// BidOrder
        /// </summary>
        public Models.Order? BidOrder { get; set; }

        /// <summary>
        /// AskPrice
        /// </summary>
        public decimal AskPrice { get; set; } = 0;
        /// <summary>
        /// AskQty
        /// </summary>
        public decimal AskQty { get; set; } = 0;
        /// <summary>
        /// AskAvg
        /// </summary>
        public decimal AskAvgPrice { get; set; } = 0;
        /// <summary>
        /// AskTotalFee
        /// </summary>
        public decimal AskTotalFee { get; set; } = 0;
        /// <summary>
        /// AskOrderChecked
        /// </summary>
        public bool AskOrderChecked { get; set; }
        /// <summary>
        /// AskOrder
        /// </summary>
        public Models.Order? AskOrder { get; set; }
    }
}