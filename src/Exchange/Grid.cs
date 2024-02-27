using MetaFrm.Extensions;
using MetaFrm.Service;
using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// Grid
    /// </summary>
    public class Grid : Setting, ISettingAction
    {
        /// <summary>
        /// SmartType
        /// </summary>
        public SmartType SmartType { get; set; } = SmartType.None;

        /// <summary>
        /// SmartTypeString
        /// </summary>
        public string SmartTypeString 
        {
            get
            {
                return this.SmartType.ToString();
            }
            set
            {
                this.SmartType = value.EnumParse<SmartType>();
            }
        }

        /// <summary>
        /// IsBuying
        /// 매집 여부
        /// </summary>
        public bool IsBuying { get; set; }

        /// <summary>
        /// AskFill
        /// 매도 채우기
        /// </summary>
        public bool AskFill { get; set; }

        /// <summary>
        /// StopLoss
        /// 손절
        /// </summary>
        public bool StopLoss { get; set; }

        /// <summary>
        /// 모든 매수 주문 생성
        /// </summary>
        public bool BidOrderAll { get; set; }

        /// <summary>
        /// SettingGridTrading
        /// </summary>
        /// <param name="user"></param>
        public Grid(User? user) : base(user)
        {
            this.SettingType = SettingType.Grid;
        }

        private decimal Amount = 0;
        /// <summary>
        /// Run
        /// TrailingMoveTop 일때 ListMin의 값이 있어야 함
        /// </summary>
        /// <param name="allOrder"></param>
        public new void Run(Models.Order? allOrder)
        {
            List<WorkData>? tmpWorkDataList = null;
            Models.OrderChance? orderChance = null;

            if (this.User == null) return;

            try
            {
                if (this.User.Api == null) return;
                if (this.Market == null) return;
                if ((this.Invest + this.BaseInvest) < this.User.ExchangeID switch
                {
                    1 => 5000,
                    2 => 500,
                    _ => 5000,
                }) return;
                if (this.BasePrice <= 0) return;
                if (this.TopPrice <= 0 || this.BasePrice >= this.TopPrice) return;
                if (this.Rate <= 0.1M) return;

                //if (allOrder != null && allOrder.OrderList != null)
                //    $"OCNT:{allOrder.OrderList.Where(x => x.Market == this.Market).Count()} - {nameof(SettingGridTrading)}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                this.WorkDataList ??= this.ReadWorkDataList(this.User);
                this.WorkDataList ??= this.GetWorkData(null, null, null);

                if (this.WorkDataList == null || this.WorkDataList.Count < 1) return;

                this.CurrentInfo = this.GetCurrentInfo();
                if (this.CurrentInfo == null)
                    return;

                if ((this.SmartType == SmartType.TrailingMoveTop || this.SmartType == SmartType.TrailingMoveTopShorten)
                    && this.WorkDataList != null && this.WorkDataList.Count > 0 && this.CurrentInfo.TradePrice > this.WorkDataList[0].BidPrice * (1 + (this.Rate / 200)))
                {
                    int idx;

                    if (this.SmartType == SmartType.TrailingMoveTop || this.WorkDataList.Count <= this.ListMin)
                        idx = this.WorkDataList.Count - 1;
                    else
                        idx = this.WorkDataList.Count - 2;

                    if (this.Amount <= 0)
                    {
                        decimal amount = (this.WorkDataList[0].BidPrice * this.WorkDataList[0].BidQty);
                        amount += amount * (this.Fees / 99.9M);
                        this.Amount = Math.Round(amount, 2);
                    }

                    tmpWorkDataList = this.GetWorkData(this.Amount, this.WorkDataList[idx].AskPrice, this.WorkDataList[0].AskPrice);

                    if (tmpWorkDataList == null || tmpWorkDataList.Count < 1)
                        return;

                    var aa = (from sel in tmpWorkDataList
                              where sel.AskPrice > this.WorkDataList[0].AskPrice
                              select sel).OrderBy(x => x.AskPrice);

                    if (aa != null && aa.Any())
                        foreach (WorkData dataRow in aa)
                        {
                            WorkData dataRowNew = new()
                            {
                                BidPrice = dataRow.BidPrice,
                                BidQty = dataRow.BidQty,
                                AskPrice = dataRow.AskPrice,
                                AskQty = dataRow.AskQty
                            };

                            this.WorkDataList.Insert(0, dataRowNew);
                        }

                    List<WorkData> deleteDataRow = new();

                    foreach (WorkData dataRow in this.WorkDataList)
                    {
                        if (dataRow.BidPrice < tmpWorkDataList[^1].BidPrice)
                            deleteDataRow.Add(dataRow);
                    }

                    foreach (WorkData dataRow in deleteDataRow)
                    {
                        if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && !dataRow.BidOrder.UUID.Contains("임시") && dataRow.BidOrder.UUID != ""
                            && dataRow.BidOrder.Market != null && dataRow.BidOrder.Side != null)
                        {
                            Models.Order order = this.User.Api.CancelOrder(dataRow.BidOrder.Market, dataRow.BidOrder.Side, dataRow.BidOrder.UUID);

                            if (order.Error == null)//에러가 아니면
                            {
                                dataRow.BidOrder.UUID = order.UUID;
                                dataRow.BidOrder.State = order.State;
                            }
                            else
                            {
                                this.Message = order.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            }
                        }

                        this.WorkDataList.Remove(dataRow);
                    }

                    //세팅 업데이트
                    this.Update(this.User, this.SettingID, this.WorkDataList[^1].BidPrice, this.WorkDataList[0].BidPrice);
                }

                if (this.WorkDataList == null) return;

                allOrder ??= this.User.Api.AllOrder(this.Market, "desc");

                if (allOrder.OrderList == null) return;

                var allOrderList = allOrder.OrderList.Where(x => x.Market == this.Market);

                if (this.AskFill)
                    orderChance = this.User.Api.OrderChance(this.Market);

                //★ 주문 리스트에서 반영되지 않은 내역 반영 Start
                foreach (WorkData dataRow in this.WorkDataList)
                {
                    var bidOrder = allOrderList.SingleOrDefault(x => x.Side == "bid" && x.Price == dataRow.BidPrice && x.Volume == dataRow.BidQty);
                    if (bidOrder != null)
                        dataRow.BidOrder = bidOrder;

                    var askOrder = allOrderList.SingleOrDefault(x => x.Side == "ask" && x.Price == dataRow.AskPrice
                                                                && x.Volume == dataRow.AskQty);
                    if (askOrder != null)
                        dataRow.AskOrder = askOrder;
                    else if (this.AskFill && orderChance != null)//매도 채우기
                    {
                        if (orderChance.Error != null)
                        {
                            this.Message = orderChance.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            continue;
                        }

                        if (orderChance.AskAccount != null && orderChance.AskAccount.Balance >= dataRow.AskQty
                            && this.CurrentInfo.TradePrice <= dataRow.AskPrice
                            && (dataRow.AskOrder == null || dataRow.AskOrder.UUID == ""))//매도할 수량이 있으면(현재 가격까지 채움)
                        {
                            var order1 = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, dataRow.AskQty, dataRow.AskPrice);

                            //오류가 발생하면 한번 더 시도
                            if (order1.Error != null)
                                order1 = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, dataRow.AskQty, dataRow.AskPrice);

                            if (order1.Error != null)
                            {
                                this.Message = order1.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                continue;
                            }

                            dataRow.AskOrder = order1;

                            orderChance.AskAccount.Balance -= dataRow.AskQty;
                            orderChance.AskAccount.Locked += dataRow.AskQty;
                        }
                    }
                }
                //★ 주문 리스트에서 반영되지 않은 내역 반영 End

                this.AskFill = false;

                foreach (WorkData dataRow in this.WorkDataList)
                {
                    //UUID가 있고 매수 체결 여부 확인
                    if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && dataRow.BidOrder.UUID != "" && !dataRow.BidOrder.UUID.Contains("임시"))//매수는 임시가 있을 수 있음
                    {
                        var bidOrder = allOrder.OrderList.SingleOrDefault(x => x.UUID == dataRow.BidOrder.UUID && x.Side == "bid");

                        if (bidOrder != null)
                            dataRow.BidOrder = bidOrder;
                        else
                        {
                            var order1 = this.User.Api.Order(this.Market, Models.OrderSide.bid.ToString(), dataRow.BidOrder.UUID);

                            if (order1.Error == null)//에러가 아니면
                                dataRow.BidOrder = order1;
                            else
                            {
                                this.Message = order1.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            }
                        }
                    }

                    //UUID가 있고 매도 체결 여부 확인
                    if (dataRow.AskOrder != null && dataRow.AskOrder.UUID != null && dataRow.AskOrder.UUID != "")
                    {
                        var askOrder = allOrder.OrderList.SingleOrDefault(x => x.UUID == dataRow.AskOrder.UUID && x.Side == "ask");

                        if (askOrder != null)
                            dataRow.AskOrder = askOrder;
                        else
                        {
                            var order1 = this.User.Api.Order(this.Market, Models.OrderSide.ask.ToString(), dataRow.AskOrder.UUID);

                            if (order1.Error == null)//에러가 아니면
                                dataRow.AskOrder = order1;
                            else
                            {
                                this.Message = order1.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            }
                        }
                    }

                    //매수/ 매도 체결 모두 되었으면 매수/매도 초기화
                    if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != "" && dataRow.BidOrder.State == "done"
                        && dataRow.AskOrder != null && dataRow.AskOrder.UUID != "" && dataRow.AskOrder.State == "done")
                    {
                        try
                        {
                            decimal ASK;

                            if (this.IsBuying)
                                ASK = (dataRow.BidOrder.Volume * dataRow.AskOrder.Price) - dataRow.AskOrder.PaidFee;
                            else
                                ASK = (dataRow.AskOrder.Volume * dataRow.AskOrder.Price) - dataRow.AskOrder.PaidFee;

                            decimal BID = (dataRow.BidOrder.Volume * dataRow.BidOrder.Price) + dataRow.BidOrder.PaidFee;

                            this.Profit(this.User, this.SettingID, this.User.UserID
                                , dataRow.BidOrder.Price, dataRow.BidOrder.Volume, dataRow.BidOrder.PaidFee
                                , dataRow.AskOrder.Price, dataRow.AskOrder.Volume, dataRow.AskOrder.PaidFee
                                , ASK - BID
                                , this.Market);

                            dataRow.AskOrder = null;
                            dataRow.BidOrder = null;
                        }
                        catch (Exception ex)
                        {
                            this.Message = ex.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                    }

                    //매수만 있고 상태가 cancel 이면 초기화
                    if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != "" && dataRow.BidOrder.State == "cancel"
                        && (dataRow.AskOrder == null || dataRow.AskOrder.UUID == "" || dataRow.AskOrder.State == ""))
                    {
                        dataRow.BidOrder = null;
                        dataRow.AskOrder = null;
                    }


                    //매수 체결 되었고 매도가 cancel이면 매도 초기화
                    if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != "" && dataRow.BidOrder.State == "done"
                        && (dataRow.AskOrder != null && dataRow.AskOrder.UUID != "" && dataRow.AskOrder.State == "cancel"))
                    {
                        dataRow.AskOrder = null;
                    }


                    //매수가 ""인데 매도가 wait 이면
                    //매수를 done로 만들고 UUID를 임시번호로 할당
                    if ((dataRow.BidOrder == null || dataRow.BidOrder.UUID == "" || dataRow.BidOrder.State == "")
                        && dataRow.AskOrder != null && dataRow.AskOrder.UUID != "" && dataRow.AskOrder.State == "wait")
                    {
                        dataRow.BidOrder = new()
                        {
                            UUID = "임시" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Side = "bid",
                            OrdType = "limit",
                            Price = dataRow.BidPrice,
                            AvgPrice = dataRow.BidPrice,
                            State = "done",
                            Market = this.Market,
                            CreatedAt = DateTime.Now,
                            Volume = dataRow.BidQty,
                            RemainingVolume = 0,
                            ReservedFee = 0,
                            RemainingFee = 0,
                            PaidFee = (dataRow.BidPrice * dataRow.BidQty) * (this.Fees / 100),
                            Locked = 0,
                            ExecutedVolume = dataRow.BidQty
                        };
                    }
                }

                //setting.LastTradePrice = this.currentInfo.TradePrice;

                var minPrice = this.WorkDataList.Min(x => x.BidPrice);

                //현재가가 시작호가 미만이고 손실제한 체크가 되어 있으면
                //자동매매 종료 및 손실제한 처리
                if (this.CurrentInfo.TradePrice < minPrice && this.StopLoss)
                {
                    this.BidCancel = true;
                    this.AskCancel = true;
                    this.AskCurrentPrice = true;
                    this.User.RemoveSetting(this);
                    //this.Organized(this.SettingID, true, true, true, false, false, true, true);
                    return;
                }

                //종료호가 터치 중지
                if (this.CurrentInfo.TradePrice > this.TopPrice && this.TopStop)
                {
                    this.BidCancel = true;
                    this.User.RemoveSetting(this);
                    //this.Organized(this.SettingID, true, false, false, false, false, true, true);
                    return;
                }


                decimal priceTickChange;

                priceTickChange = 0;
                if (this.WorkDataList.Count > 0)
                    priceTickChange = this.WorkDataList[0].AskPrice - this.WorkDataList[0].BidPrice;

                //매수 없는 항목 매수 / 범위를 벗어난 매수 취소
                //매수가 있고 매수 완료 이면, 매도 없고, 매도 상태가 클리어 이면 매도 주문
                foreach (WorkData dataRow in this.WorkDataList)
                {
                    //현재가 보다 작고, 매수/매도 상태가 클리어, 매수/매도 UUID 없으면 매수 주문
                    //********핵심 부분*******
                    //이 부분을 상승장? 하락장? 보합? 상태에 따라서 변경 해야 함
                    if ((dataRow.BidPrice < (this.CurrentInfo.TradePrice * (1M + 0.003M))
                        && (dataRow.BidPrice > (this.CurrentInfo.TradePrice * (1M - 0.10M))
                        || this.BidOrderAll)
                        && (dataRow.BidOrder == null || (dataRow.BidOrder.UUID == "" && dataRow.BidOrder.State == ""))
                        && (dataRow.AskOrder == null || (dataRow.AskOrder.UUID == "" && dataRow.AskOrder.State == ""))))
                    {
                        var bidOrder = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid, dataRow.BidQty, dataRow.BidPrice);

                        if (bidOrder.Error == null)//에러가 아니면
                            dataRow.BidOrder = bidOrder;
                        else
                        {
                            this.Message = bidOrder.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                    }


                    //기준 단가 보다 아래에 있는 매수 예약 취소(매수요청량 == 매수잔량)
                    if (dataRow.BidPrice <= (this.CurrentInfo.TradePrice * (1M - 0.10M))
                        && dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && dataRow.BidOrder.UUID != "" && dataRow.BidOrder.State == "wait"
                        && (dataRow.AskOrder == null || (dataRow.AskOrder.UUID == "" && dataRow.AskOrder.State == ""))
                        && dataRow.BidQty == dataRow.BidOrder.RemainingVolume && !this.BidOrderAll)
                    {
                        var bidOrder = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), dataRow.BidOrder.UUID);

                        if (bidOrder.Error == null)//에러가 아니면
                            dataRow.BidOrder = null;
                        else
                        {
                            this.Message = bidOrder.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                    }


                    //매수가 있고 매수 완료 이면, 매도 없고, 매도 상태가 클리어 이면 매도 주문 생성
                    if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && dataRow.BidOrder.UUID != "" && dataRow.BidOrder.State == "done"
                        && (dataRow.AskOrder == null || (dataRow.AskOrder.UUID == "" && dataRow.AskOrder.State == "")))
                    {
                        Models.Order order;

                        order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, dataRow.AskQty, dataRow.AskPrice);

                        if (order.Error == null)//에러가 없으면
                            dataRow.AskOrder = order;
                        else
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }

                        //setting.LastOrder = null;
                    }

                    //틱 변화량
                    //매수 주문이 있고
                    //임시가 아니고
                    //매수 완료가 아니고
                    //현재가가 3틱 높고
                    //매수요청량 != 매수잔량 이면
                    //매수잔량 만큼 현재행 매수 가격의 매도가격으로 매도 주문
                    if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && !dataRow.BidOrder.UUID.Contains("임시") && dataRow.BidOrder.State == "wait"
                        && priceTickChange > 0
                        && this.CurrentInfo.TradePrice >= (dataRow.BidPrice + (priceTickChange * 3))
                        && dataRow.BidQty > dataRow.BidOrder.RemainingVolume)
                    {
                        var order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, dataRow.BidQty - dataRow.BidOrder.RemainingVolume, dataRow.AskPrice);

                        if (order.Error == null && order.UUID != null)//에러가 없으면
                        {
                            order = this.User.Api.Order(this.Market, Models.OrderSide.ask.ToString(), order.UUID);

                            if (order.Error == null)//매도가 정상이면 매수 취소
                            {
                                order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), dataRow.BidOrder.UUID);

                                if (order.Error != null)//에러가 있으면 취소 한번더
                                    order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), dataRow.BidOrder.UUID);

                                if (order.Error != null)
                                {
                                    this.Message = order.Error.Message;
                                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                }
                            }
                            else
                            {
                                this.Message = order.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                                if (order.UUID != null)
                                {
                                    //매도 주문 취소(혹시나 잘못된 매도 주문을 할 수도 있기 때문에
                                    order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.ask.ToString(), order.UUID);

                                    if (order.Error != null)
                                    {
                                        this.Message = order.Error.Message;
                                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.Message = order.Error?.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
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

        /// <summary>
        /// GetWorkData
        /// </summary>
        /// <param name="BASE_INVEST"></param>
        /// <param name="BASE_PRICE"></param>
        /// <param name="TOP_PRICE"></param>
        /// <param name="authState"></param>
        /// <returns></returns>
        public List<WorkData>? GetWorkData(decimal? BASE_INVEST, decimal? BASE_PRICE, decimal? TOP_PRICE, Task<AuthenticationState>? authState = null)
        {
            Response response;

            this.Fees = DefaultFees(this.ExchangeID);

            ServiceData data = new()
            {
                TransactionScope = false,
                Token = authState != null ? authState.Token() : this.User?.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Grid.GetWorkData");
            data["1"].AddParameter("EXCHANGE_ID", Database.DbType.Int, 3, this.ExchangeID);
            data["1"].AddParameter("INVEST", Database.DbType.Decimal, 25, this.Invest);

            if (BASE_INVEST == null)
                data["1"].AddParameter("BASE_INVEST", Database.DbType.Decimal, 25, this.BaseInvest);
            else
                data["1"].AddParameter("BASE_INVEST", Database.DbType.Decimal, 25, BASE_INVEST);

            if (BASE_PRICE == null)
                data["1"].AddParameter("BASE_PRICE", Database.DbType.Decimal, 25, this.BasePrice);
            else
                data["1"].AddParameter("BASE_PRICE", Database.DbType.Decimal, 25, BASE_PRICE);

            if (TOP_PRICE == null)
                data["1"].AddParameter("TOP_PRICE", Database.DbType.Decimal, 25, this.TopPrice);
            else
                data["1"].AddParameter("TOP_PRICE", Database.DbType.Decimal, 25, TOP_PRICE);

            data["1"].AddParameter("RATE", Database.DbType.Decimal, 25, this.Rate);
            data["1"].AddParameter("FEES", Database.DbType.Decimal, 25, this.Fees);
            data["1"].AddParameter("IS_BUYING", Database.DbType.NVarChar, 1, this.IsBuying ? "Y" : "N");
            data["1"].AddParameter("MARKET_ID", Database.DbType.NVarChar, 20, this.Market);

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
                    });
                }

                if (workDatas.Any(x => x.BidPrice == 0 || x.BidQty == 0 || x.AskPrice == 0 || x.AskQty == 0))
                    return null;
                else
                {
                    //$"SettingGrid".WriteMessage(this.ExchangeID, this.User.UserID, this.SettingID, this.Market);
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
        private void Update(User user, int SETTING_ID, decimal BID_PRICE_MIN, decimal BID_PRICE_MAX)
        {
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Grid.Update");
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(BID_PRICE_MIN), Database.DbType.Decimal, 25, BID_PRICE_MIN);
            data["1"].AddParameter(nameof(BID_PRICE_MAX), Database.DbType.Decimal, 25, BID_PRICE_MAX);

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