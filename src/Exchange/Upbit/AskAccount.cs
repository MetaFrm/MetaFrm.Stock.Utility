using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 매도 시 사용하는 화폐의 계좌 상태
    /// </summary>
    public class AskAccount : ICore
    {
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
        public decimal AvgBuyPrice { get; set; }

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
        /// 이 인스턴스의 매도 시 사용하는 화폐의 계좌 상태 값을 해당하는 문자열 표현으로 변환합니다.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Locked.ToString(); ;
        }
    }
}