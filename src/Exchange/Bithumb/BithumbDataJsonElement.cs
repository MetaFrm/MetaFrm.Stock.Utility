using System.Text.Json.Serialization;
using System.Text.Json;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// BithumbDataJsonElement
    /// </summary>
    public class BithumbDataJsonElement : ResultStatus
    {
        /// <summary>
        /// Data
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, JsonElement>? Data { get; set; }
    }
}