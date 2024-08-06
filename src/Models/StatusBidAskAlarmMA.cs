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
        /// Qty
        /// </summary>
        public decimal Qty { get; set; }

        /// <summary>
        /// IsBid
        /// </summary>
        public bool IsBid { get; set; }

        /// <summary>
        /// TempBidOrder
        /// </summary>
        public Models.Order? TempBidOrder { get; set; }
        /// <summary>
        /// TempAskOrder
        /// </summary>
        public Models.Order? TempAskOrder { get; set; }

        /// <summary>
        /// BidOrder
        /// </summary>
        public Models.Order? BidOrder { get; set; }

        /// <summary>
        /// StopLossAskOrder
        /// </summary>
        public Models.Order? StopLossAskOrder { get; set; }

        /// <summary>
        /// TempInvest
        /// </summary>
        public decimal TempInvest { get; set; }

        /// <summary>
        /// EnterCount1
        /// </summary>
        public decimal EnterCount1 { get; set; }

        /// <summary>
        /// EnterCount2
        /// </summary>
        public decimal EnterCount2 { get; set; }

        /// <summary>
        /// EnterCount3
        /// </summary>
        public decimal EnterCount3 { get; set; }
    }
}