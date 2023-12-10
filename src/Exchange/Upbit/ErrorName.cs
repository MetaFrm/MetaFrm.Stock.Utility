using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 에러 Name
    /// </summary>
    public class ErrorName : ICore
    {
        /// <summary>
        /// 메시지 이름
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}