using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 매수 시 제약사항
    /// </summary>
    public class Bid : ICore
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// 주문금액 단위
        /// </summary>
        [JsonPropertyName("price_unit")]
        public string? PriceUnit { get; set; }

        /// <summary>
        /// 최소 매도/매수 금액
        /// </summary>
        [JsonPropertyName("min_total")]
        public decimal MinTotal { get; set; }

        /// <summary>
        /// 이 인스턴스의 최소 매도/매수 금액 값을 해당하는 문자열 표현으로 변환합니다.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.MinTotal.ToString();
        }
    }
}