using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 분(Minute) 캔들
    /// </summary>
    public class CandlesMinute : Candles
    {
        /// <summary>
        /// 분 단위(유닛)
        /// </summary>
        [JsonPropertyName("unit")]
        public int Unit { get; set; }

        /// <summary>
        /// CandlesMinuteList
        /// </summary>
        public CandlesMinute[]? CandlesMinuteList { get; set; }
    }
}