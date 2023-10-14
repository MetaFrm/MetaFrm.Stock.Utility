namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 마켓에 대한 정보
    /// </summary>
    public class Market : ICore
    {
        /// <summary>
        /// 마켓의 유일 키
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// 마켓 이름
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 매도 주문 지원 방식
        /// </summary>
        public IList<string>? AskTypes { get; set; }

        /// <summary>
        /// 매도 주문 지원 방식
        /// </summary>
        public IList<string>? BidTypes { get; set; }

        /// <summary>
        /// 매수 주문 지원 방식
        /// </summary>
        public IList<string>? OrderSides { get; set; }

        /// <summary>
        /// 매수 시 제약사항
        /// </summary>
        public Bid? Bid { get; set; }

        /// <summary>
        /// 매도 시 제약사항
        /// </summary>
        public Ask? Ask { get; set; }

        /// <summary>
        /// 최대 매도/매수 금액
        /// </summary>
        public decimal MaxTotal { get; set; }

        /// <summary>
        /// 마켓 운영 상태
        /// </summary>
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