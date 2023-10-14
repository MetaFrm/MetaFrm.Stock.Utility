namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 체결
    /// </summary>
    public class Trade : ICore
    {
        /// <summary>
        /// 마켓의 유일 키
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// 체결의 고유 아이디
        /// </summary>
        public string? UUID { get; set; }

        /// <summary>
        /// 체결 가격
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 체결 양
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 체결된 총 가격
        /// </summary>
        public decimal Funds { get; set; }

        /// <summary>
        /// 체결 종류
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 이 인스턴스의 체결의 고유 아이디(UUID) 값을 해당하는 문자열 표현으로 변환합니다.
        /// </summary>
        /// <returns></returns>
        public override string? ToString()
        {
            return this.UUID;
        }
    }
}