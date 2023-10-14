using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 일(Day) 캔들
    /// </summary>
    public class CandlesDay : Candles
    {
        /// <summary>
        /// 전일 종가(UTC 0시 기준)
        /// </summary>
        [JsonPropertyName("prev_closing_price")]
        public decimal PrevClosingPrice { get; set; }

        /// <summary>
        /// 전일 종가 대비 변화 금액
        /// </summary>
        [JsonPropertyName("change_price")]
        public decimal ChangePrice { get; set; } = 0;

        /// <summary>
        /// 전일 종가 대비 변화량
        /// </summary>
        [JsonPropertyName("change_rate")]
        public decimal ChangeRate { get; set; }

        /// <summary>
        /// 종가 환산 화폐 단위로 환산된 가격(요청에 convertingPriceUnit 파라미터 없을 시 해당 필드 포함되지 않음.)
        /// </summary>
        [JsonPropertyName("converted_trade_price")]
        public decimal ConvertedTradePrice { get; set; }

        /// <summary>
        /// CandlesDayList
        /// </summary>
        public CandlesDay[]? CandlesDayList { get; set; }
    }
}