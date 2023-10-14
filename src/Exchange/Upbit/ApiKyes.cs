using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// API 키 리스트
    /// </summary>
    public class ApiKyes : ICore
    {
        /// <summary>
        /// OrderList
        /// </summary>
        public ApiKyes[]? ApiKyesList { get; set; }

        /// <summary>
        /// AccessKey
        /// </summary>
        [JsonPropertyName("access_key")]
        public string? AccessKey { get; set; }

        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        [JsonPropertyName("expire_at")]
        public DateTime ExpireAt { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}