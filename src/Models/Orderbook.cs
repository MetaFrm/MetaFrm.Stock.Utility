namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 호가 정보 조회
    /// </summary>
    public class Orderbook : ICore
    {
        /// <summary>
        /// OrderbookList
        /// </summary>
        public List<Orderbook>? OrderbookList { get; set; }

        /// <summary>
        /// 마켓 코드
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// 호가 생성 시각
        /// </summary>
        public long? TimeStamp { get; set; }

        /// <summary>
        /// 호가 매도 총 잔량
        /// </summary>
        public decimal TotalAskSize { get; set; }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        public decimal TotalBidSize { get; set; }


        /// <summary>
        /// TotalAskKrw
        /// </summary>
        public decimal TotalAskKrw { get; set; }

        /// <summary>
        /// TotalBidKrw
        /// </summary>
        public decimal TotalBidKrw { get; set; }

        /// <summary>
        /// 호가
        /// </summary>
        public List<OrderbookUnit>? OrderbookUnits { get; set; }

        /// <summary>
        /// 호가 모아보기 단위 (default: 0, 기본 호가단위)
        /// </summary>
        public decimal Level { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}