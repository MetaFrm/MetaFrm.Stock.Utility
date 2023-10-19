using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// 전체 계좌 조회
    /// </summary>
    public class BithumbDatas : ResultStatus
    {
        /// <summary>
        /// Data
        /// </summary>
        [JsonPropertyName("data")]
        public List<Dictionary<string, string>?>? Datas { get; set; }
    }
}