namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 주문 가능 정보
    /// </summary>
    public class OrderChance : ICore
    {
        /// <summary>
        /// 매수 수수료 비율
        /// </summary>
        public decimal BidFee { get; set; }

        /// <summary>
        /// 매도 수수료 비율
        /// </summary>
        public decimal AskFee { get; set; }

        /// <summary>
        /// 마켓에 대한 정보
        /// </summary>
        public Market? Market { get; set; }

        /// <summary>
        /// 매수 시 사용하는 화폐의 계좌 상태
        /// </summary>
        public BidAccount? BidAccount { get; set; }

        /// <summary>
        /// 매도 시 사용하는 화폐의 계좌 상태
        /// </summary>
        public AskAccount? AskAccount { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}