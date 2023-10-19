using MetaFrm.Stock.Models;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// 전체 계좌 조회
    /// </summary>
    public class AccountInfo : ICore
    {
        /// <summary>
        /// 회원가입 일시 타임 스탬프
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// 회원 ID
        /// </summary>
        public string? AccountID { get; set; }

        /// <summary>
        /// 주문 통화 (코인)
        /// </summary>
        public string? OrderCurrency { get; set; }

        /// <summary>
        /// 결제 통화 (마켓)
        /// </summary>
        public string? PaymentCurrency { get; set; }

        /// <summary>
        /// 거래 수수료율
        /// </summary>
        public decimal TradeFee { get; set; }

        /// <summary>
        /// 주문 가능 수량
        /// </summary>
        public decimal Balance { get; set; }
    }
}