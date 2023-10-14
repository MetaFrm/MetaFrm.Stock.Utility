namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 매도 시 제약사항
    /// </summary>
    public class Ask : ICore
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 주문금액 단위
        /// </summary>
        public string? PriceUnit { get; set; }

        /// <summary>
        /// 최소 매도/매수 금액
        /// </summary>
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