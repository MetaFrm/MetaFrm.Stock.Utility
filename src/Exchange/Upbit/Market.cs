using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 마켓에 대한 정보
    /// </summary>
    public class Market : ICore
    {
        /// <summary>
        /// 마켓의 유일 키
        /// </summary>
        [JsonPropertyName("id")]
        public string? ID { get; set; }

        /// <summary>
        /// 마켓 이름
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 매도 주문 지원 방식
        /// </summary>
        [JsonPropertyName("ask_types")]
        public IList<string>? AskTypes { get; set; }

        /// <summary>
        /// 매수 주문 지원 방식
        /// </summary>
        [JsonPropertyName("bid_types")]
        public IList<string>? BidTypes { get; set; }

        /// <summary>
        /// 지원 주문 종류
        /// </summary>
        [JsonPropertyName("order_sides")]
        public IList<string>? OrderSides { get; set; }

        /// <summary>
        /// 매수 시 제약사항
        /// </summary>
        [JsonPropertyName("bid")]
        public Bid? Bid { get; set; }

        /// <summary>
        /// 매도 시 제약사항
        /// </summary>
        [JsonPropertyName("ask")]
        public Ask? Ask { get; set; }

        /// <summary>
        /// 최대 매도/매수 금액
        /// </summary>
        [JsonPropertyName("max_total")]
        public decimal MaxTotal { get; set; }

        /// <summary>
        /// 마켓 운영 상태
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// 이 인스턴스의 마켓 운영 상태 값을 해당하는 문자열 표현으로 변환합니다.
        /// </summary>
        /// <returns></returns>
        public override string? ToString()
        {
            return this.State;
        }
    }
}