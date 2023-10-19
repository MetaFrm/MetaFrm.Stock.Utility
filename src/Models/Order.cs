namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// Order
    /// </summary>
    public class Order : ICore
    {
        /// <summary>
        /// OrderList
        /// </summary>
        public List<Order>? OrderList { get; set; }

        /// <summary>
        /// 주문의 고유 아이디
        /// </summary>
        public string? UUID { get; set; }

        /// <summary>
        /// 주문 종류
        /// bid : 매수
        /// ask : 매도
        /// ask_sl : 손실 stop loss
        /// bid_sb : 이득 stop benefit
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// 주문 방식
        /// </summary>
        public string? OrdType { get; set; }

        /// <summary>
        /// 주문 당시 화폐 가격
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 체결 가격의 평균가
        /// </summary>
        public decimal AvgPrice { get; set; }

        /// <summary>
        /// 주문 상태
        /// wait : 체결 대기 (default)
        /// done : 전체 체결 완료
        /// cancel : 주문 취소
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 사용자가 입력한 주문 양
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 체결 후 남은 주문 양
        /// </summary>
        public decimal RemainingVolume { get; set; }

        /// <summary>
        /// 수수료로 예약된 비용
        /// </summary>
        public decimal ReservedFee { get; set; }

        /// <summary>
        /// 남은 수수료
        /// </summary>
        public decimal RemainingFee { get; set; }

        /// <summary>
        /// 사용된 수수료
        /// </summary>
        public decimal PaidFee { get; set; }

        /// <summary>
        /// 거래에 사용중인 비용
        /// </summary>
        public decimal Locked { get; set; }

        /// <summary>
        /// 체결된 양
        /// </summary>
        public decimal ExecutedVolume { get; set; }

        /// <summary>
        /// 해당 주문에 걸린 체결 수
        /// </summary>
        public int TradesCount { get; set; }

        /// <summary>
        /// 체결
        /// </summary>
        public List<Trade>? Trades { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}