using MetaFrm.Service;
using MetaFrm.Stock.Console;
using MetaFrm.Stock.Models;
using System.Text;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// TraillingStop
    /// </summary>
    public class BidAskMA : Setting, ISettingAction
    {
        /// <summary>
        /// MinuteCandleType
        /// </summary>
        public MinuteCandleType MinuteCandleType { get; set; }

        /// <summary>
        /// LeftMA7
        /// </summary>
        public int LeftMA7 { get; set; }

        /// <summary>
        /// RightMA30
        /// </summary>
        public int RightMA30 { get; set; }

        /// <summary>
        /// RightMA60
        /// </summary>
        public int RightMA60 { get; set; }

        /// <summary>
        /// StopLossRate
        /// </summary>
        public decimal StopLossRate { get; set; }

        /// <summary>
        /// StatusBidAskAlarmMA
        /// </summary>
        public StatusBidAskAlarmMA? StatusBidAskAlarmMA { get; set; }


        /// <summary>
        /// TraillingStop
        /// </summary>
        /// <param name="user"></param>
        public BidAskMA(User? user) : base(user)
        {
            this.SettingType = SettingType.BidAskMA;
        }

        CandlesMinute? Candles;
        private DateTime lassRunDateTime = DateTime.MinValue;
        /// <summary>
        /// Run
        /// TrailingMoveTop 일때 ListMin의 값이 있어야 함
        /// </summary>
        /// <param name="allOrder"></param>
        public new void Run(Models.Order? allOrder)
        {
            DateTime dateTime;

            if (this.User == null) return;

            try
            {
                if (this.User.Api == null) return;
                if (this.Market == null) return;
                if (this.Invest < this.User.ExchangeID switch
                {
                    1 => 5000,
                    2 => 500,
                    _ => 5000,
                }) return;
                if (this.BaseInvest < this.User.ExchangeID switch
                {
                    1 => 5000,
                    2 => 500,
                    _ => 5000,
                }) return;
                if (this.Rate <= 0.1M) return;


                allOrder ??= this.User.Api.AllOrder(this.Market, "desc");

                if (allOrder.OrderList == null) return;

                var allOrderList = allOrder.OrderList.Where(x => x.Market == this.Market);


                this.StatusBidAskAlarmMA ??= BidAskAlarmMA.ReadStatusBidAskAlarmMA(this.SettingID, this.ExchangeID, (int)this.MinuteCandleType, this.Market, this.LeftMA7, this.RightMA30, this.RightMA60, this.StopLossRate, this.Rate) ?? new();

                this.Candles ??= BidAskAlarmMA.Candles[$"{this.Market}_{(int)this.MinuteCandleType}"];


                //세팅 처음 시작시 매수 주문을 미리 생성해 놓는다 -> KRW lock
                if (this.StatusBidAskAlarmMA.CurrentStatus == "" || this.StatusBidAskAlarmMA.CurrentStatus == "손절")
                {
                    if (this.StatusBidAskAlarmMA.TempBidOrder == null)
                        this.BidOrder(this.Market, this.User.Api, this.StatusBidAskAlarmMA, this.User.UserID);
                    else
                    {
                        var bidOrder = allOrderList.SingleOrDefault(x => x.UUID == this.StatusBidAskAlarmMA.TempBidOrder.UUID);

                        if (bidOrder != null)
                        {
                            if (bidOrder.State == "done")
                            {
                                this.StatusBidAskAlarmMA.CurrentStatus = "매수";
                                this.StatusBidAskAlarmMA.BidOrder = bidOrder;

                                this.StatusBidAskAlarmMA.StopLossPrice = (this.StatusBidAskAlarmMA.BidOrder.Price * (1M - (this.StopLossRate / 100M))).PriceRound(this.ExchangeID, this.Market);//손절

                                this.StatusBidAskAlarmMA.IsBid = true;

                                this.StatusBidAskAlarmMA.TempBidOrder = null;
                                return;
                            }
                            else if (bidOrder.State == "cancel")
                            {
                                this.StatusBidAskAlarmMA.TempBidOrder = null;
                                this.BidOrder(this.Market, this.User.Api, this.StatusBidAskAlarmMA, this.User.UserID);
                                return;
                            }
                        }
                        else
                        {
                            this.StatusBidAskAlarmMA.TempBidOrder = null;
                            this.BidOrder(this.Market, this.User.Api, this.StatusBidAskAlarmMA, this.User.UserID);
                            return;
                        }
                    }
                }

                //매수 상태이면 매도 주문을 미리 생성해 놓는다 -> QTY lock
                if (this.StatusBidAskAlarmMA.CurrentStatus == "매수")
                {
                    if (this.StatusBidAskAlarmMA.TempAskOrder == null)
                        this.AskOrder(this.Market, this.User.Api, this.StatusBidAskAlarmMA, this.User.UserID);
                    else
                    {
                        var askOrder = allOrderList.SingleOrDefault(x => x.UUID == this.StatusBidAskAlarmMA.TempAskOrder.UUID);

                        if (askOrder != null && this.StatusBidAskAlarmMA.BidOrder != null)
                        {
                            if (askOrder.State == "done")
                            {
                                this.StatusBidAskAlarmMA.CurrentStatus = "";

                                decimal ASK = (askOrder.Volume * askOrder.Price) - askOrder.PaidFee;
                                decimal BID = (this.StatusBidAskAlarmMA.BidOrder.Trades != null ? this.StatusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Price * x.Volume) : this.StatusBidAskAlarmMA.BidOrder.Price * this.StatusBidAskAlarmMA.BidOrder.Volume) + this.StatusBidAskAlarmMA.BidOrder.PaidFee;

                                this.Invest = ASK;

                                this.Profit(this.User, this.SettingID, this.User.UserID
                                    , this.StatusBidAskAlarmMA.BidOrder.Price, this.StatusBidAskAlarmMA.BidOrder.Volume, this.StatusBidAskAlarmMA.BidOrder.PaidFee
                                    , askOrder.Price, askOrder.Volume, askOrder.PaidFee
                                    , ASK - BID
                                    , this.Market);

                                this.StatusBidAskAlarmMA.TempAskOrder = null;
                                this.StatusBidAskAlarmMA.BidOrder = null;
                                this.StatusBidAskAlarmMA.TempBidOrder = null;

                                return;
                            }
                            else if (askOrder.State == "cancel")
                            {
                                this.StatusBidAskAlarmMA.TempAskOrder = null;
                                this.AskOrder(this.Market, this.User.Api, this.StatusBidAskAlarmMA, this.User.UserID);
                                return;
                            }
                        }
                        else
                        {
                            this.StatusBidAskAlarmMA.TempAskOrder = null;
                            this.AskOrder(this.Market, this.User.Api, this.StatusBidAskAlarmMA, this.User.UserID);
                            return;
                        }
                    }
                }



                dateTime = DateTime.Now;

                if (this.lassRunDateTime == DateTime.MinValue
                    || this.Candles.CandlesMinuteList == null || this.Candles.CandlesMinuteList.Count < this.RightMA60
                    || (this.lassRunDateTime.Minute != dateTime.Minute && dateTime.Minute % this.Candles.Unit == 0))
                {
                    this.lassRunDateTime = dateTime;

                    //다음 Unit에서 손절 평가
                    if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && this.StatusBidAskAlarmMA.IsBid)
                        this.StatusBidAskAlarmMA.IsBid = false;

                    //System.Console.WriteLine($"실행 {this.SettingTypeString} {this.ExchangeID} {this.Market} {this.lassRunDateTime:yyyy-MM-dd HH:mm:ss}");

                    BidAskAlarmMA.SecondaryIndicator(this.User.Api, this.Candles, this.Market, this.MinuteCandleType, this.LeftMA7, this.RightMA30, this.RightMA60);

                    CandlesMinute candles = BidAskAlarmMA.Candles[$"{this.Market}_{(int)this.MinuteCandleType}"];



                    if (candles.CandlesMinuteList == null)
                        return;

                    var data = candles.CandlesMinuteList.OrderByDescending(o => o.CandleDateTimeKst).Take(2).OrderBy(o => o.CandleDateTimeKst);

                    if (data == null)
                        return;


                    decimal? beforValue_7_30 = null;
                    decimal? beforValue_7_60 = null;
                    StringBuilder stringBuilder = new();

                    foreach (var item in data)
                    {
                        if (item.SecondaryIndicator.TryGetValue($"{this.LeftMA7} - {this.RightMA30}MA_TradePrice", out decimal? value_7_30) && value_7_30 != null
                            && item.SecondaryIndicator.TryGetValue($"{this.LeftMA7} - {this.RightMA60}MA_TradePrice", out decimal? value_7_60) && value_7_60 != null)
                        {
                            if (beforValue_7_30 == null || beforValue_7_60 == null)
                            {
                                beforValue_7_30 = value_7_30;
                                beforValue_7_60 = value_7_60;
                                continue;
                            }

                            if (value_7_30 > 0 && value_7_60 > 0)
                            {
                                //상승 중

                                //매수 포지션 일떄
                                if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && this.StatusBidAskAlarmMA.BidOrder != null && this.StatusBidAskAlarmMA.BidOrder != null && this.StatusBidAskAlarmMA.BidOrder.Trades != null)
                                {
                                    //이전 값보다 작아 졌을떄 => 간격이 좁아지면 => 매도를 한다 (보정값으로으로 미세 조정 필요★)
                                    // 5 < 6
                                    decimal bidQty = this.StatusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Volume);
                                    decimal bidPrice = this.StatusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Volume * x.Price) / bidQty;

                                    if (value_7_30 < beforValue_7_30 && this.StatusBidAskAlarmMA.TempAskOrder != null && this.StatusBidAskAlarmMA.TempAskOrder.UUID != null && item.TradePrice > (bidPrice * (1M + (this.Rate / 100M))))
                                    {
                                        var order = this.User.Api.CancelOrder(this.Market, "ask", this.StatusBidAskAlarmMA.TempAskOrder.UUID);

                                        if (order.Error != null)
                                        {
                                            order = this.User.Api.CancelOrder(this.Market, "ask", this.StatusBidAskAlarmMA.TempAskOrder.UUID);

                                            if (order.Error != null)
                                            {
                                                this.Message = order.Error.Message;
                                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                                return;
                                            }
                                        }

                                        if (order.Error == null && this.StatusBidAskAlarmMA.BidOrder != null)
                                        {
                                            order = this.MakeOrderAskMarket(this.Market, bidQty);

                                            if (order != null && order.Error == null && order.Trades != null && order.Trades.Count > 0)
                                            {
                                                this.StatusBidAskAlarmMA.CurrentStatus = "";

                                                decimal askQty = order.Trades.Sum(x => x.Volume);
                                                decimal askPrice = order.Trades.Sum(x => x.Volume * x.Price) / askQty;

                                                decimal ASK = (askPrice * askQty) - order.PaidFee;
                                                decimal BID = (bidPrice * bidQty) + this.StatusBidAskAlarmMA.BidOrder.PaidFee;

                                                this.Invest = Math.Truncate(ASK);

                                                this.Profit(this.User, this.SettingID, this.User.UserID
                                                    , bidPrice, bidQty, this.StatusBidAskAlarmMA.BidOrder.PaidFee
                                                    , askPrice, askQty, order.PaidFee
                                                    , ASK - BID
                                                    , this.Market);

                                                this.StatusBidAskAlarmMA.TempAskOrder = null;
                                                this.StatusBidAskAlarmMA.BidOrder = null;
                                                this.StatusBidAskAlarmMA.TempBidOrder = null;

                                                return;
                                            }
                                        }
                                    }
                                }

                                //손절 상태가 되면 상숭 후에 다시 내려 갔을떄 진입
                                if (this.StatusBidAskAlarmMA.CurrentStatus == "손절")
                                    this.StatusBidAskAlarmMA.CurrentStatus = "";
                            }
                            else if (value_7_30 < 0)
                            {
                                //하락 중

                                //포지션이 없을때
                                if (this.StatusBidAskAlarmMA.CurrentStatus == "")
                                {
                                    //이전 값보다 커졌을떄 => 간격이 좁아지면 => 매수를 한다 (보정값으로으로 미세 조정 필요★)
                                    // -5 > -6
                                    if (value_7_30 > beforValue_7_30 && this.StatusBidAskAlarmMA.TempBidOrder != null && this.StatusBidAskAlarmMA.TempBidOrder.UUID != null)
                                    {
                                        var order = this.User.Api.CancelOrder(this.Market, "bid", this.StatusBidAskAlarmMA.TempBidOrder.UUID);

                                        if (order.Error != null)
                                        {
                                            order = this.User.Api.CancelOrder(this.Market, "bid", this.StatusBidAskAlarmMA.TempBidOrder.UUID);

                                            if (order.Error != null)
                                            {
                                                this.Message = order.Error.Message;
                                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                                return;
                                            }
                                        }

                                        if (order.Error == null)
                                        {
                                            order = this.MakeOrderBidPrice(this.Market, this.Invest * (1M - (this.Fees / 100M)));

                                            //order = this.User.Api.Order(this.Market, Models.OrderSide.bid.ToString(), "ff0ca8b9-4df9-4413-b95f-d4f99af3c79d");

                                            if (order != null && order.Error == null && order.Trades != null && order.Trades.Count > 0)
                                            {
                                                this.StatusBidAskAlarmMA.CurrentStatus = "매수";
                                                this.StatusBidAskAlarmMA.BidOrder = order;
                                                this.StatusBidAskAlarmMA.StopLossPrice = ((order.Trades.Sum(x => x.Volume * x.Price) / order.Trades.Sum(x => x.Volume)) * (1M - (this.StopLossRate / 100M))).PriceRound(this.ExchangeID, this.Market);//손절
                                                this.StatusBidAskAlarmMA.IsBid = true;
                                                this.StatusBidAskAlarmMA.TempBidOrder = null;
                                            }
                                        }
                                    }
                                }
                            }

                            beforValue_7_30 = value_7_30;
                            beforValue_7_60 = value_7_60;
                        }
                    }
                }

                if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && !this.StatusBidAskAlarmMA.IsBid)
                {
                    Ticker? ticker = this.GetCurrentInfo(this.Market);

                    //매수 상태이고 스탑로스 가격 보다 떨어지면 손절
                    if (ticker != null && this.StatusBidAskAlarmMA.StopLossPrice >= ticker.TradePrice && this.StatusBidAskAlarmMA.TempAskOrder != null && this.StatusBidAskAlarmMA.TempAskOrder.UUID != null)
                    {
                        this.StatusBidAskAlarmMA.CurrentStatus = "손절";

                        var order = this.User.Api.CancelOrder(this.Market, "ask", this.StatusBidAskAlarmMA.TempAskOrder.UUID);

                        if (order.Error != null)
                        {
                            order = this.User.Api.CancelOrder(this.Market, "ask", this.StatusBidAskAlarmMA.TempAskOrder.UUID);

                            if (order.Error != null)
                            {
                                this.Message = order.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                return;
                            }
                        }

                        if (order.Error == null && this.StatusBidAskAlarmMA.BidOrder != null)
                        {
                            order = this.MakeOrderAskMarket(this.Market
                                , this.StatusBidAskAlarmMA.BidOrder.Trades != null ? this.StatusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Volume) : this.StatusBidAskAlarmMA.BidOrder.Volume
                                );

                            if (order != null && order.Error == null && order.Trades != null && order.Trades.Count > 0 && this.StatusBidAskAlarmMA.BidOrder.Trades != null)
                            {
                                decimal bidQty = this.StatusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Volume);
                                decimal bidPrice = this.StatusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Volume * x.Price) / bidQty;

                                decimal askQty = order.Trades.Sum(x => x.Volume);
                                decimal askPrice = order.Trades.Sum(x => x.Volume * x.Price) / askQty;

                                decimal ASK = (askPrice * askQty) - order.PaidFee;
                                decimal BID = (bidPrice * bidQty) + this.StatusBidAskAlarmMA.BidOrder.PaidFee;

                                this.Invest = Math.Truncate(ASK);

                                this.Loss(this.User, this.SettingID, this.User.UserID
                                    , bidPrice, bidQty, this.StatusBidAskAlarmMA.BidOrder.PaidFee
                                    , askPrice, askQty, order.PaidFee
                                    , ASK - BID
                                    , this.Market);

                                this.StatusBidAskAlarmMA.TempAskOrder = null;
                                this.StatusBidAskAlarmMA.BidOrder = null;
                                this.StatusBidAskAlarmMA.TempBidOrder = null;
                                this.StatusBidAskAlarmMA.StopLossAskOrder = order;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
            finally
            {
                this.UpdateMessage(this.User, this.SettingID, this.Message ?? "");
            }
        }
        private void BidOrder(string market, IApi api, StatusBidAskAlarmMA statusBidAskAlarmMA, int userID)
        {
            Ticker? ticker = this.GetCurrentInfo(market);

            if (ticker != null)
            {
                this.Fees = DefaultFees(this.ExchangeID);

                decimal price = (ticker.TradePrice * 0.4M).PriceRound(this.ExchangeID, market);
                decimal qty = Math.Truncate(((this.Invest * (1M - (this.Fees / 100M))) / price) * Point(this.ExchangeID)) / Point(this.ExchangeID);

                var order = api.MakeOrder(market, Models.OrderSide.bid, qty, price);

                if (order == null)
                    return;
                else if (order != null && order.Error != null)
                {
                    this.Message = order.Error.Message;
                    this.Message?.WriteMessage(this.ExchangeID, userID, this.SettingID, market);
                    return;
                }

                statusBidAskAlarmMA.TempBidOrder = order;
            }
        }
        private void AskOrder(string market, IApi api, StatusBidAskAlarmMA statusBidAskAlarmMA, int userID)
        {
            Ticker? ticker = this.GetCurrentInfo(market);

            if (ticker != null && statusBidAskAlarmMA.BidOrder != null && statusBidAskAlarmMA.BidOrder.Trades != null)
            {
                this.Fees = DefaultFees(this.ExchangeID);

                decimal price = (ticker.TradePrice * 2.0M).PriceRound(this.ExchangeID, market);

                var order = api.MakeOrder(market, Models.OrderSide.ask, statusBidAskAlarmMA.BidOrder.Trades.Sum(x => x.Volume), price);

                if (order == null)//에러가 아니면
                    return;
                else if (order != null && order.Error != null)
                {
                    this.Message = order.Error.Message;
                    this.Message?.WriteMessage(this.ExchangeID, userID, this.SettingID, market);
                    return;
                }

                statusBidAskAlarmMA.TempAskOrder = order;
            }
        }
        internal Ticker? GetCurrentInfo(string market)
        {
            if (this.User?.Api == null) return null;

            var ticker = this.User.Api.Ticker(market);
            if (ticker == null || ticker.TickerList == null || ticker.TickerList.Count < 1) return null;

            return ticker.TickerList[0];
        }
    }
}