using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// ResultStatus
    /// </summary>
    public class ResultStatus : ICore
    {
        /// <summary>
        /// status
        /// </summary>
        [JsonPropertyName("status")]
        public string? Code { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
