using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 내 체결 (MyTrade)
    /// {
    ///  "type": "myTrade",
    ///  "code": "KRW-BTC",
    ///  "ask_bid": "BID",
    ///  "price": 55660000,
    ///  "volume": 0.5389867,
    ///  "order_uuid": "e5cec6f9-6a15-4c95-ae76-6d7dcb3a00e0",
    ///  "order_type": "price",
    ///  "trade_uuid": "cd955522-c9d8-4f06-b86d-54a09a25e707",
    ///  "trade_timestamp": 1677487182655,
    ///  "stream_type": "REALTIME"
    ///}
    /// </summary>
    public class OrderWebSocket : ICore
    {
        /// <summary>
        /// 타입
        /// </summary>
        [JsonPropertyName("ty")]
        public string? Type { get; set; }

        /// <summary>
        /// 마켓 코드 (ex. KRW-BTC)
        /// </summary>
        [JsonPropertyName("cd")]
        public string? Code { get; set; }

        /// <summary>
        /// 매수/매도 구분
        /// </summary>
        [JsonPropertyName("ab")]
        public string? AskBid { get; set; }

        /// <summary>
        /// 체결 가격
        /// </summary>
        [JsonPropertyName("p")]
        public decimal Price { get; set; }

        /// <summary>
        /// 체결량
        /// </summary>
        [JsonPropertyName("v")]
        public decimal Volume { get; set; }

        /// <summary>
        /// 주문의 고유 아이디
        /// </summary>
        [JsonPropertyName("ouid")]
        public string? OrderUUID { get; set; }

        /// <summary>
        /// 주문 타입
        /// </summary>
        [JsonPropertyName("ot")]
        public string? OrderType { get; set; }

        /// <summary>
        /// 체결의 고유 아이디
        /// </summary>
        [JsonPropertyName("tuid")]
        public string? TradeUUID { get; set; }

        /// <summary>
        /// 체결 타임스탬프 (millisecond)
        /// </summary>
        [JsonPropertyName("ttms")]
        public long TradeTimeStamp { get; set; }

        /// <summary>
        /// 스트림 타입
        /// </summary>
        [JsonPropertyName("st")]
        public string? StreamType { get; set; }
    }
}