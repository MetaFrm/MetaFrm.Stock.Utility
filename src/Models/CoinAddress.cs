namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// CoinAddress
    /// </summary>
    public class CoinAddress : ICore
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 입금 주소
        /// </summary>
        public string? DepositAddress { get; set; }

        /// <summary>
        /// 입금 주소
        /// </summary>
        public string? SecondaryAddress { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}