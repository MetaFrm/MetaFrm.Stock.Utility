using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 에러
    /// </summary>
    public class Error : ICore
    {
        /// <summary>
        /// 메시지
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// 메시지 이름
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}