using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 주문 리스트
    /// </summary>
    public class Order : ICore
    {
        /// <summary>
        /// OrderList
        /// </summary>
        public Order[]? OrderList { get; set; }

        /// <summary>
        /// 주문의 고유 아이디
        /// </summary>
        [JsonPropertyName("uuid")]
        public string? UUID { get; set; }

        /// <summary>
        /// 주문 종류
        /// bid : 매수
        /// ask : 매도
        /// ask_sl : 손실 stop loss
        /// bid_sb : 이득 stop benefit
        /// </summary>
        [JsonPropertyName("side")]
        public string? Side { get; set; }

        /// <summary>
        /// 주문 방식
        /// </summary>
        [JsonPropertyName("ord_type")]
        public string? OrdType { get; set; }

        /// <summary>
        /// 주문 당시 화폐 가격
        /// </summary>
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 주문 상태
        /// wait : 체결 대기 (default)
        /// done : 전체 체결 완료
        /// cancel : 주문 취소
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market { get; set; }

        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 사용자가 입력한 주문 양
        /// </summary>
        [JsonPropertyName("volume")]
        public decimal? Volume { get; set; }

        /// <summary>
        /// 체결 후 남은 주문 양
        /// </summary>
        [JsonPropertyName("remaining_volume")]
        public decimal? RemainingVolume { get; set; }

        /// <summary>
        /// 수수료로 예약된 비용
        /// </summary>
        [JsonPropertyName("reserved_fee")]
        public decimal ReservedFee { get; set; }

        /// <summary>
        /// 남은 수수료
        /// </summary>
        [JsonPropertyName("remaining_fee")]
        public decimal RemainingFee { get; set; }

        /// <summary>
        /// 사용된 수수료
        /// </summary>
        [JsonPropertyName("paid_fee")]
        public decimal PaidFee { get; set; }

        /// <summary>
        /// 거래에 사용중인 비용
        /// </summary>
        [JsonPropertyName("locked")]
        public decimal Locked { get; set; }

        /// <summary>
        /// 체결된 양
        /// </summary>
        [JsonPropertyName("executed_volume")]
        public decimal ExecutedVolume { get; set; }

        /// <summary>
        /// 해당 주문에 걸린 체결 수
        /// </summary>
        [JsonPropertyName("trades_count")]
        public int TradesCount { get; set; }

        /// <summary>
        /// 체결
        /// </summary>
        [JsonPropertyName("trades")]
        public IList<Trade>? Trades { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}