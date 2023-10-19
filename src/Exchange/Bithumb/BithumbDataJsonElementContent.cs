using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// BithumbDataJsonElementContent
    /// </summary>
    public class BithumbDataJsonElementContent : ResultStatus
    {
        /// <summary>
        /// Type
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Content
        /// </summary>
        [JsonPropertyName("content")]
        public Dictionary<string, string>? Content { get; set; }
    }
}