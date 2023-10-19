using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// 전체 계좌 조회
    /// </summary>
    public class BithumbData : ResultStatus
    {
        /// <summary>
        /// Data
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, string>? Data { get; set; }

        /// <summary>
        /// OrderID
        /// </summary>
        [JsonPropertyName("order_id")]
        public string? OrderID { get; set; }
    }
}