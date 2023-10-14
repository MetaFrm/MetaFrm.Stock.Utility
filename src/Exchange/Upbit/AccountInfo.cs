using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 전체 계좌 조회
    /// </summary>
    public class AccountInfo : ICore
    {
        /// <summary>
        /// 회원가입 일시 타임 스탬프
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// 회원 ID
        /// </summary>
        [JsonPropertyName("account_id")]
        public string? AccountID { get; set; }

        /// <summary>
        /// 주문 통화 (코인)
        /// </summary>
        [JsonPropertyName("order_currency")]
        public string? OrderCurrency { get; set; }

        /// <summary>
        /// 결제 통화 (마켓)
        /// </summary>
        [JsonPropertyName("payment_currency")]
        public string? PaymentCurrency { get; set; }

        /// <summary>
        /// 거래 수수료율
        /// </summary>
        [JsonPropertyName("trade_fee")]
        public decimal TradeFee { get; set; }

        /// <summary>
        /// 주문 가능 수량
        /// </summary>
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}