using MetaFrm.Stock.Models;

namespace MetaFrm.Stock
{
    /// <summary>
    /// IApi
    /// </summary>
    public interface IApi : ICore
    {
        /// <summary>
        /// ExchangeID
        /// </summary>
        decimal ExchangeID { get; set; }

        /// <summary>
        /// Access Key
        /// </summary>
        string AccessKey { get; set; }

        /// <summary>
        /// Secret Key
        /// </summary>
        string SecretKey { get; set; }

        /// <summary>
        /// Timeout (Milliseconds)
        /// </summary>
        double TimeoutMilliseconds { get; set; }

        /// <summary>
        /// 전체 계좌 조회
        /// </summary>
        /// <returns></returns>
        Account Account();

        /// <summary>
        /// 현재가 정보
        /// </summary>
        /// <param name="markets"></param>
        /// <returns></returns>
        Ticker Ticker(string markets);

        /// <summary>
        /// 마켓 코드 조회
        /// </summary>
        /// <returns></returns>
        Markets Markets();

        /// <summary>
        /// 주문 리스트 조회
        /// </summary>
        /// <param name="market"></param>
        /// <param name="order_by">정렬 방식
        /// asc : 오름차순 (default)
        /// desc : 내림차순</param>
        /// <returns></returns>
        Order AllOrder(string market, string order_by);

        /// <summary>
        /// 주문 리스트 조회
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="page">page</param>
        /// <param name="order_by">정렬 방식
        /// asc : 오름차순 (default)
        /// desc : 내림차순</param>
        /// <returns></returns>
        Order AllOrder(string market, int page, string order_by);

        /// <summary>
        /// 개별 주문 조회
        /// </summary>
        /// <param name="market"></param>
        /// <param name="sideName"></param>
        /// <param name="uuid">주문 UUID</param>
        /// <returns></returns>
        //Order Order(string uuid);
        Order Order(string market, string sideName, string uuid);

        /// <summary>
        /// 주문하기
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="side">주문 종류</param>
        /// <param name="volume">주문 수량</param>
        /// <param name="price">유닛당 주문 가격</param>
        /// <param name="ord_type">주문 타입</param>
        Order MakeOrder(string market, OrderSide side, decimal volume, decimal price, OrderType ord_type = OrderType.limit);

        /// <summary>
        /// 주문 취소 접수
        /// </summary>
        /// <param name="market"></param>
        /// <param name="sideName"></param>
        /// <param name="uuid">주문 UUID</param>
        /// <returns></returns>
        //Order CancelOrder(string uuid);
        Order CancelOrder(string market, string sideName, string uuid);

        /// <summary>
        /// 호가 정보 조회
        /// </summary>
        /// <param name="markets">Market ID</param>
        /// <returns></returns>
        Orderbook Orderbook(string markets);

        /// <summary>
        /// 주문 가능 정보
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <returns></returns>
        OrderChance OrderChance(string market);

        /// <summary>
        /// 분(Minute) 캔들
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="unit">분 단위. 가능한 값 : 1, 3, 5, 15, 10, 30, 60, 240</param>
        /// <param name="to">마지막 캔들 시각 (exclusive). 포맷 : yyyy-MM-dd'T'HH:mm:ssXXX. 비워서 요청시 가장 최근 캔들</param>
        /// <param name="count">캔들 개수(최대 200개까지 요청 가능)</param>
        /// <returns></returns>
        CandlesMinute CandlesMinute(string market, MinuteCandleType unit, DateTime to = default, int count = 1);

        /// <summary>
        /// 일(Day) 캔들
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="to">마지막 캔들 시각 (exclusive). 포맷 : yyyy-MM-dd'T'HH:mm:ssXXX. 비워서 요청시 가장 최근 캔들</param>
        /// <param name="count">캔들 개수</param>
        /// <param name="convertingPriceUnit">종가 환산 화폐 단위 (생략 가능, KRW로 명시할 시 원화 환산 가격을 반환.)</param>
        /// <returns></returns>
        CandlesDay CandlesDay(string market, DateTime to = default, int count = 1, string convertingPriceUnit = "");

        /// <summary>
        /// 주(Week) 캔들
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="to">마지막 캔들 시각 (exclusive). 포맷 : yyyy-MM-dd'T'HH:mm:ssXXX. 비워서 요청시 가장 최근 캔들</param>
        /// <param name="count">캔들 개수</param>
        /// <returns></returns>
        CandlesWeek CandlesWeek(string market, DateTime to = default, int count = 1);

        /// <summary>
        /// 월(Month) 캔들
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="to">마지막 캔들 시각 (exclusive). 포맷 : yyyy-MM-dd'T'HH:mm:ssXXX. 비워서 요청시 가장 최근 캔들</param>
        /// <param name="count">캔들 개수</param>
        /// <returns></returns>
        CandlesMonth CandlesMonth(string market, DateTime to = default, int count = 1);

        /// <summary>
        /// 당일 체결 내역
        /// </summary>
        /// <param name="market">Market ID</param>
        /// <param name="to">마지막 체결 시각. 형식 : [HHmmss 또는 HH:mm:ss]. 비워서 요청시 가장 최근 데이터</param>
        /// <param name="count">체결 개수</param>
        /// <returns></returns>
        Ticks Ticks(string market, DateTime to = default, int count = 1);


        /// <summary>
        /// 입금 내역
        /// </summary>
        /// <param name="currency">Currency 코드</param>
        /// <param name="imit">페이지당 개수</param>
        /// <param name="page">페이지 번호</param>
        /// <param name="order_by">정렬 방식</param>
        /// <returns></returns>
        Deposits Deposits(string currency, int imit, int page, string order_by);

        /// <summary>
        /// API 키 리스트
        /// </summary>
        /// <returns></returns>
        ApiKyes ApiKyes();
    }
}