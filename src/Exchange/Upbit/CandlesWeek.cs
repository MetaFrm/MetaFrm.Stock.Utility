using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 주(Week) 캔들
    /// </summary>
    public class CandlesWeek : Candles
    {
        /// <summary>
        /// 캔들 기간의 가장 첫 날
        /// </summary>
        [JsonPropertyName("first_day_of_period")]
        public string? FirstDayOfPeriod { get; set; }

        /// <summary>
        /// CandlesWeekList
        /// </summary>
        public CandlesWeek[]? CandlesWeekList { get; set; }
    }
}