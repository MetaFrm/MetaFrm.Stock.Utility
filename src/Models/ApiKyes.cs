namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// API 키 리스트
    /// </summary>
    public class ApiKyes : ICore
    {
        /// <summary>
        /// OrderList
        /// </summary>
        public List<ApiKyes>? ApiKyesList { get; set; }

        /// <summary>
        /// AccessKey
        /// </summary>
        public string? AccessKey { get; set; }

        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        public DateTime ExpireAt { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}