using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 에러
    /// </summary>
    public class ErrorRoot : ICore
    {
        /// <summary>
        /// 에러
        /// </summary>
        [JsonPropertyName("error")]
        public Error? Error { get; set; }
    }
}