using MetaFrm.Service;
using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// TraillingStop
    /// </summary>
    public class TraillingStop : Setting, ISettingAction
    {
        /// <summary>
        /// GapRate
        /// </summary>
        public decimal GapRate { get; set; } = 5M;

        /// <summary>
        /// ReturnRate
        /// </summary>
        public decimal ReturnRate { get; set; }

        /// <summary>
        /// ReturnRateString
        /// </summary>
        public string ReturnRateString
        {
            get
            {
               return $"{this.ReturnRate}";
            }
            set
            {
                if (value.ToTryDecimal(out decimal result))
                {
                    this.ReturnRate = result;
                }
            }
        }

        /// <summary>
        /// 사용자 매수
        /// </summary>
        public bool IsUserBid { get; set; } = false;

        /// <summary>
        /// IsTarget
        /// </summary>
        public bool IsTarget { get; set; } = false;

        /// <summary>
        /// ReturnPrice
        /// </summary>
        public decimal ReturnPrice { get; set; }

        /// <summary>
        /// TraillingStop
        /// </summary>
        /// <param name="user"></param>
        public TraillingStop(User? user) : base(user)
        {
            this.SettingType = SettingType.TraillingStop;
        }

        /// <summary>
        /// Run
        /// TrailingMoveTop 일때 ListMin의 값이 있어야 함
        /// </summary>
        /// <param name="allOrder"></param>
        public new void Run(Models.Order? allOrder)
        {
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
                if (this.BasePrice <= 0) return;
                //if (this.TopPrice <= 0 || this.BasePrice >= this.TopPrice) return;
                if (this.Rate <= 0.1M) return;

                //if (allOrder != null && allOrder.OrderList != null)
                //    $"OCNT:{allOrder.OrderList.Where(x => x.Market == this.Market).Count()} - {nameof(SettingGridTrading)}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                if (this.WorkDataList == null && this.IsFirstReadWorkDataList)
                {
                    this.IsFirstReadWorkDataList = false;
                    this.WorkDataList = ReadWorkDataList(this);
                }

                this.CurrentInfo = this.GetCurrentInfo();
                if (this.CurrentInfo == null)
                    return;

                this.WorkDataList ??= this.GetWorkData(!this.IsUserBid && this.BasePrice > this.CurrentInfo.TradePrice ? this.CurrentInfo.TradePrice : this.BasePrice);

                if (this.WorkDataList == null || this.WorkDataList.Count < 1) return;

                allOrder ??= this.User.Api.AllOrder(this.Market, "desc");

                if (allOrder.OrderList == null) return;

                var allOrderList = allOrder.OrderList.Where(x => x.Market == this.Market);

                foreach (var item in this.WorkDataList)
                {
                    item.BidOrderChecked = false;
                    item.AskOrderChecked = false;
                }

                //★ 주문 리스트에서 반영되지 않은 내역 반영 Start
                foreach (WorkData dataRow in this.WorkDataList)
                {
                    var bidOrder = allOrderList.FirstOrDefault(x => x.Side == "bid" && x.Price == dataRow.BidPrice && x.Volume == dataRow.BidQty);
                    if (bidOrder != null)
                    {
                        dataRow.BidOrder = bidOrder;
                        dataRow.BidOrderChecked = true;
                    }
                }
                //★ 주문 리스트에서 반영되지 않은 내역 반영 End


                var workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && !x.BidOrder.UUID.Contains("임시") && !x.BidOrderChecked);

                foreach (var item in workDataList)
                {
                    if (item.BidOrder == null || item.BidOrder.UUID == null) continue;

                    var order1 = this.User.Api.Order(this.Market, Models.OrderSide.bid.ToString(), item.BidOrder.UUID);

                    if (order1 != null && order1.Error == null)//에러가 아니면
                    {
                        if (order1.State == "cancel")
                        {
                            item.BidOrder = null;
                        }
                        else
                        {
                            item.BidOrder = order1;
                            item.BidOrderChecked = true;
                        }
                    }
                    else if (order1 != null && order1.Error != null)
                    {
                        this.Message = order1.Error.Message;
                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }

                //1. 매수주문 Start
                workDataList = this.WorkDataList.Where(x => (x.BidOrder == null || x.BidOrder.UUID == null && x.BidOrder.UUID == "")).OrderByDescending(y => y.BidPrice);
                if (this.IsUserBid)
                {
                    foreach (var item in workDataList)
                    {
                        item.BidOrder = new()
                        {
                            UUID = "임시" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Side = "bid",
                            OrdType = "limit",
                            Price = item.BidPrice,
                            AvgPrice = item.BidPrice,
                            State = "done",
                            Market = this.Market,
                            CreatedAt = DateTime.Now,
                            Volume = item.BidQty,
                            RemainingVolume = 0,
                            ReservedFee = 0,
                            RemainingFee = 0,
                            PaidFee = (item.BidPrice * item.BidQty) * (this.Fees / 100),
                            Locked = 0,
                            ExecutedVolume = item.BidQty
                        };
                    }
                }
                else
                {
                    foreach (var item in workDataList)
                    {
                        var order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid, item.BidQty, item.BidPrice);

                        if (order != null && order.Error == null)//에러가 아니면
                            item.BidOrder = order;
                        else if (order != null && order.Error != null)
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                    }
                }
                //1. 매수주문 End


                workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && x.BidOrder.State == "done").OrderByDescending(y => y.BidPrice);

                if (workDataList != null && workDataList.Any())
                {
                    var minBidPrice = workDataList.Min(x => x.BidPrice);

                    var workDataListMin = workDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && x.BidOrder.State == "done" && x.BidPrice == minBidPrice);

                    if (workDataListMin != null && workDataListMin.Count() == 1)
                    {
                        var item = workDataListMin.ToArray()[0];

                        if (this.CurrentInfo.TradePrice > item.TargetAskPrice)
                        {
                            this.IsTarget = true;
                            item.TargetAskPrice = this.CurrentInfo.TradePrice;
                            this.ReturnPrice = (item.TargetAskPrice * (1 + (this.ReturnRate / 100M))).PriceRound(this.ExchangeID, this.Market);

                            this.Update(this.User, this.SettingID, item.BidAvgPrice, item.TargetAskPrice, this.ReturnPrice);
                        }

                        //목표 터치 후 리턴 가격에 오면 익절 !!
                        //익절 후 세팅 중지
                        if (this.IsTarget && this.CurrentInfo.TradePrice <= this.ReturnPrice)
                        {
                            var order = this.MakeOrderAskMarket(this.Market, item.AskQty);

                            if (order != null && order.Error == null && order.Trades != null && order.Trades.Count > 0 && item.BidOrder != null)
                            {
                                foreach (var bidOrder in workDataList)
                                    this.WorkDataList.ForEach(x => x.BidTotalFee += bidOrder.BidOrder?.PaidFee ?? 0);

                                decimal ASK = order.Trades != null && order.Trades.Count > 0 ? order.Trades.Sum(x => x.Volume * x.Price) - order.PaidFee : (order.Price * order.ExecutedVolume) - order.PaidFee;
                                decimal BID = (workDataList.Sum(x => x.BidOrder?.ExecutedVolume ?? 0) * item.BidAvgPrice) + item.BidTotalFee;
                                decimal ASK_AvgPrice = order.Trades != null && order.Trades.Count > 0 ? order.Trades.Sum(x => x.Volume * x.Price) / order.Trades.Sum(x => x.Volume) : order.Price;

                                this.Profit(this.User, this.SettingID, this.User.UserID
                                    , item.BidAvgPrice, item.BidOrder.Volume, item.BidTotalFee
                                    , ASK_AvgPrice, order.ExecutedVolume, order.PaidFee
                                    , ASK - BID
                                    , this.Market);

                                this.BidCancel = true;
                                this.User.RemoveSetting(this);
                                //this.Organized(this.SettingID, true, false, false, false, false, true, true);
                                this.WorkDataList = null;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                ex.ToString()?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
            finally
            {
                if (this.IsChangeWorkDataList())
                    SaveWorkDataList(this);

                this.UpdateMessage(this.User, this.SettingID, this.Message ??"");
            }
        }
        /// <summary>
        /// GetWorkData
        /// </summary>
        /// <param name="TRADE_PRICE"></param>
        /// <param name="authState"></param>
        /// <returns></returns>
        public List<WorkData>? GetWorkData(decimal? TRADE_PRICE, Task<AuthenticationState>? authState = null)
        {
            Response response;

            this.Fees = DefaultFees(this.ExchangeID);

            ServiceData data = new()
            {
                TransactionScope = false,
                Token = authState != null ? authState.Token() : this.User?.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("TraillingStop.GetWorkData");
            data["1"].AddParameter("EXCHANGE_ID", Database.DbType.Int, 3, this.ExchangeID);
            data["1"].AddParameter("BASE_PRICE", Database.DbType.Decimal, 25, TRADE_PRICE);
            data["1"].AddParameter("GAP_RATE", Database.DbType.Decimal, 25, this.GapRate);
            data["1"].AddParameter("RATE", Database.DbType.Decimal, 25, this.Rate);
            data["1"].AddParameter("RETURN_RATE", Database.DbType.Decimal, 25, this.ReturnRate);
            data["1"].AddParameter("FEES", Database.DbType.Decimal, 25, this.Fees);
            data["1"].AddParameter("LIST_MIN", Database.DbType.Int, 3, this.ListMin);
            data["1"].AddParameter("MARKET_ID", Database.DbType.NVarChar, 20, this.Market);
            data["1"].AddParameter("INVEST", Database.DbType.Decimal, 25, this.Invest);

            response = this.ServiceRequest(data);

            if (response.Status == Status.OK && response.DataSet != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
            {
                List<WorkData> workDatas = new();
                foreach (var item in response.DataSet.DataTables[0].DataRows)
                {
                    workDatas.Add(new()
                    {
                        BidPrice = item.Decimal("BID_PRICE") ?? 0,
                        BidQty = item.Decimal("BID_QTY") ?? 0,
                        AskPrice = item.Decimal("ASK_PRICE") ?? 0,
                        AskQty = item.Decimal("ASK_QTY") ?? 0,
                        TargetAskPrice = item.Decimal("TARGET_ASK_PRICE") ?? 0,
                        BidAvgPrice = item.Decimal("BID_KRW_AVG") ?? 0,
                    });
                }

                if (workDatas.Any(x => x.BidPrice == 0 || x.BidQty == 0 || x.AskPrice == 0 || x.AskQty == 0))
                    return null;
                else
                {
                    //$"SettingTraillingStop".WriteMessage(this.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    //foreach (var workData in workDatas)
                    //{
                    //    if (workData == null) continue;

                    //    $"B {workData.BidPrice,20:N8} {workData.BidQty,23:N8}\tA {workData.AskPrice,20:N8} {workData.AskQty,23:N8}".WriteMessage();
                    //}
                    //System.Console.WriteLine();

                    return workDatas;
                }
            }
            else
            {
                response.Message?.WriteMessage(this.ExchangeID, this.User?.UserID, this.SettingID, this.Market);
                return null;
            }
        }
        private void Update(User user, int SETTING_ID, decimal BID_PRICE_AVG, decimal TARGET_PRICE, decimal RETURN_PRICE)
        {
            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("TraillingStop.Update");
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(BID_PRICE_AVG), Database.DbType.Decimal, 25, BID_PRICE_AVG);
            data["1"].AddParameter(nameof(TARGET_PRICE), Database.DbType.Decimal, 25, TARGET_PRICE);
            data["1"].AddParameter(nameof(RETURN_PRICE), Database.DbType.Decimal, 25, RETURN_PRICE);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, user.UserID);

            stringBuilder.Append($"{user.ExchangeName()} 트레일링 스탑 타겟 알림");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.AppendLine($"{this.Market}");

            string[]? tmps = this.Market?.Split('-');

            //if (TARGET_PRICE >= 100)
            //    stringBuilder.Append($"타겟: {TARGET_PRICE:N0} {tmps?[0]}");
            //else if (TARGET_PRICE >= 1)
            //    stringBuilder.Append($"타겟: {TARGET_PRICE:N2} {tmps?[0]}");
            //else
            //    stringBuilder.Append($"타겟: {TARGET_PRICE:N4} {tmps?[0]}");
            stringBuilder.Append($"타겟: {TARGET_PRICE.PriceToString(SETTING_ID, this.Market ?? "")} {tmps?[0]}");

            //if (RETURN_PRICE >= 100)
            //    stringBuilder.Append($" | 리턴: {RETURN_PRICE:N0} {tmps?[0]}");
            //else if (RETURN_PRICE >= 1)
            //    stringBuilder.Append($" | 리턴: {RETURN_PRICE:N2} {tmps?[0]}");
            //else
            //    stringBuilder.Append($" | 리턴: {RETURN_PRICE:N4} {tmps?[0]}");
            stringBuilder.Append($" | 리턴: {RETURN_PRICE.PriceToString(SETTING_ID, this.Market ?? "")} {tmps?[0]}");

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(user.ExchangeID, user.UserID, this.SettingID, this.Market);
            });
        }
    }
}