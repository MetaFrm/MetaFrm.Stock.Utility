namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 주문타입
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// 지정가 주문
        /// </summary>
        limit,

        /// <summary>
        /// 시장가 주문(매수)
        /// </summary>
        /// 
        price,

        /// <summary>
        /// 시장가 주문(매도)
        /// </summary>
        market
    }
}