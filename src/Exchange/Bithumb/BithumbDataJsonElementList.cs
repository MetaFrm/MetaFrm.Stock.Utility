using System.Text.Json.Serialization;
using System.Text.Json;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// BithumbDataJsonElementList
    /// </summary>
    public class BithumbDataJsonElementList : ResultStatus
    {
        /// <summary>
        /// Data
        /// </summary>
        [JsonPropertyName("data")]
        public List<JsonElement>? Data { get; set; }
    }
}