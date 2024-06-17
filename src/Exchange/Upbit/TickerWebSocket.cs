using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// API 키 리스트
    /// {
    ///  "type": "ticker",
    ///  "code": "KRW-BTC",
    ///  "opening_price": 31883000,
    ///  "high_price": 32310000,
    ///  "low_price": 31855000,
    ///  "trade_price": 32287000,
    ///  "prev_closing_price": 31883000.00000000,
    ///  "acc_trade_price": 78039261076.51241000,
    ///  "change": "RISE",
    ///  "change_price": 404000.00000000,
    ///  "signed_change_price": 404000.00000000,
    ///  "change_rate": 0.0126713295,
    ///  "signed_change_rate": 0.0126713295,
    ///  "ask_bid": "ASK",
    ///  "trade_volume": 0.03103806,
    ///  "acc_trade_volume": 2429.58834336,
    ///  "trade_date": "20230221",
    ///  "trade_time": "074102",
    ///  "trade_timestamp": 1676965262139,
    ///  "acc_ask_volume": 1146.25573608,
    ///  "acc_bid_volume": 1283.33260728,
    ///  "highest_52_week_price": 57678000.00000000,
    ///  "highest_52_week_date": "2022-03-28",
    ///  "lowest_52_week_price": 20700000.00000000,
    ///  "lowest_52_week_date": "2022-12-30",
    ///  "market_state": "ACTIVE",
    ///  "is_trading_suspended": false,
    ///  "delisting_date": null,
    ///  "market_warning": "NONE",
    ///  "timestamp": 1676965262177,
    ///  "acc_trade_price_24h": 228827082483.70729000,
    ///  "acc_trade_volume_24h": 7158.80283560,
    ///  "stream_type": "REALTIME"
    ///}
    /// </summary>
    public class TickerWebSocket : ICore
    {
        /// <summary>
        /// OrderList
        /// </summary>
        public TickerWebSocket[]? TickerWebSocketList { get; set; }

        /// <summary>
        /// 타입
        /// ticker : 현재가
        /// </summary>
        [JsonPropertyName("ty")]
        public string? Type { get; set; }

        /// <summary>
        /// 마켓 코드 (ex. KRW-BTC)
        /// </summary>
        [JsonPropertyName("cd")]
        public string? Code { get; set; }

        /// <summary>
        /// 시가
        /// </summary>
        [JsonPropertyName("op")]
        public decimal? OpeningPrice { get; set; }

        /// <summary>
        /// 고가
        /// </summary>
        [JsonPropertyName("hp")]
        public decimal? HighPrice { get; set; }

        /// <summary>
        /// 저가
        /// </summary>
        [JsonPropertyName("lp")]
        public decimal? LowPrice { get; set; }

        /// <summary>
        /// 현재가
        /// </summary>
        [JsonPropertyName("tp")]
        public decimal? TradePrice { get; set; }

        /// <summary>
        /// 전일 종가
        /// </summary>
        [JsonPropertyName("pcp")]
        public decimal? PrevClosingPrice { get; set; }

        /// <summary>
        /// 누적 거래대금(UTC 0시 기준)
        /// </summary>
        [JsonPropertyName("atp")]
        public decimal AccTradePrice { get; set; }

        /// <summary>
        /// 전일 대비
        /// RISE : 상승
        /// EVEN : 보합
        /// FALL : 하락
        /// </summary>
        [JsonPropertyName("c")]
        public string? Change { get; set; }

        /// <summary>
        /// 부호 없는 전일 대비 값
        /// </summary>
        [JsonPropertyName("cp")]
        public decimal? ChangePrice { get; set; }

        /// <summary>
        /// 전일 대비 값
        /// </summary>
        [JsonPropertyName("scp")]
        public decimal? SignedChangePrice { get; set; }

        /// <summary>
        /// 부호 없는 전일 대비 등락율
        /// </summary>
        [JsonPropertyName("cr")]
        public decimal ChangeRate { get; set; }

        /// <summary>
        /// 전일 대비 등락율
        /// </summary>
        [JsonPropertyName("scr")]
        public decimal SignedChangeRate { get; set; }

        /// <summary>
        /// 매수/매도 구분
        /// ASK : 매도
        /// BID : 매수
        /// </summary>
        [JsonPropertyName("ab")]
        public string? AskBid { get; set; }

        /// <summary>
        /// 가장 최근 거래량
        /// </summary>
        [JsonPropertyName("tv")]
        public decimal? TradeVolume { get; set; }

        /// <summary>
        /// 누적 거래량(UTC 0시 기준)
        /// </summary>
        [JsonPropertyName("atv")]
        public decimal AccTradeVolume { get; set; }

        /// <summary>
        /// 최근 거래 일자(UTC)
        /// </summary>
        [JsonPropertyName("tdt")]
        public string? TradeDate { get; set; }

        /// <summary>
        /// 최근 거래 시각(UTC)
        /// </summary>
        [JsonPropertyName("ttm")]
        public string? TradeTime { get; set; }

        /// <summary>
        /// 체결 타임스탬프 (milliseconds)
        /// </summary>
        [JsonPropertyName("ttms")]
        public long? TradeTimeStamp { get; set; }

        /// <summary>
        /// 누적 매도량
        /// </summary>
        [JsonPropertyName("aav")]
        public decimal? AccAskVolume { get; set; }

        /// <summary>
        /// 누적 매수량
        /// </summary>
        [JsonPropertyName("abv")]
        public decimal? AccBidVolume { get; set; }

        /// <summary>
        /// 52주 최고가
        /// </summary>
        [JsonPropertyName("h52wp")]
        public decimal? Highest52WeekPrice { get; set; }

        /// <summary>
        /// 52주 최고가 달성일
        /// </summary>
        [JsonPropertyName("h52wdt")]
        public string? Highest52WeekDate { get; set; }

        /// <summary>
        /// 52주 최저가
        /// </summary>
        [JsonPropertyName("l52wp")]
        public decimal? Lowest52WeekPrice { get; set; }

        /// <summary>
        /// 52주 최저가 달성일
        /// </summary>
        [JsonPropertyName("l52wdt")]
        public string? Lowest52WeekDate { get; set; }

        /// <summary>
        /// 거래상태*deprecated
        /// </summary>
        [JsonPropertyName("ms")]
        public string? MarketState { get; set; }

        /// <summary>
        /// 거래 정지 여부
        /// </summary>
        [JsonPropertyName("its")]
        public bool IsTradingSuspended { get; set; }

        /// <summary>
        /// 상장폐지일
        /// </summary>
        [JsonPropertyName("dd")]
        public object? DelistingDate { get; set; }

        /// <summary>
        /// 유의 종목 여부
        /// NONE : 해당없음
        /// CAUTION : 투자유의
        /// </summary>
        [JsonPropertyName("mw")]
        public string? MarketWarning { get; set; }

        /// <summary>
        /// 타임스탬프 (millisecond)
        /// </summary>
        [JsonPropertyName("tms")]
        public long TimeStamp { get; set; }

        /// <summary>
        /// 24시간 누적 거래대금
        /// </summary>
        [JsonPropertyName("atp24h")]
        public decimal AccTradePrice24h { get; set; }

        /// <summary>
        /// 24시간 누적 거래량
        /// </summary>
        [JsonPropertyName("atv24h")]
        public decimal AccTradeVolume24h { get; set; }

        /// <summary>
        /// 스트림 타입
        /// </summary>
        [JsonPropertyName("st")]
        public string? StreamType { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}