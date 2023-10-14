using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 전체 계좌 조회
    /// </summary>
    public class Account : ICore
    {
        /// <summary>
        /// AccountList
        /// </summary>
        public Account[]? AccountList { get; set; }

        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// 주문가능 금액/수량
        /// </summary>
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// 주문 중 묶여있는 금액/수량
        /// </summary>
        [JsonPropertyName("locked")]
        public decimal Locked { get; set; }

        /// <summary>
        /// 매수평균가
        /// </summary>
        [JsonPropertyName("avg_buy_price")]
        public decimal AvgKrwBuyPrice { get; set; }

        /// <summary>
        /// 매수금액KRW
        /// </summary>
        public decimal KrwBuyPrice {
            get
            {
                return (this.Balance  + this.Locked) * this.AvgKrwBuyPrice;
                //return (this.Balance == 0.0M ? 1 : this.Balance) * (this.Locked == 0.0M ? 1 : this.Locked) * this.AvgKrwBuyPrice;
            }
        }

        /// <summary>
        /// 매수평균가 수정 여부
        /// </summary>
        [JsonPropertyName("avg_buy_price_modified")]
        public bool Modified { get; set; }

        /// <summary>
        /// 평단가 기준 화폐
        /// </summary>
        [JsonPropertyName("unit_currency")]
        public string? UnitCurrency { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}