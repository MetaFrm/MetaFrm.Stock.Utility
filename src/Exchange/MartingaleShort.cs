using MetaFrm.Service;
using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// MartingaleShort
    /// </summary>
    public class MartingaleShort : Setting, ISettingAction
    {
        /// <summary>
        /// GapRate
        /// </summary>
        public decimal GapRate { get; set; } = 5M;

        /// <summary>
        /// FirstFix
        /// </summary>
        public bool FirstFix { get; set; }

        /// <summary>
        /// SettingMartingaleLongTrading
        /// </summary>
        /// <param name="user"></param>
        public MartingaleShort(User? user) : base(user)
        {
            this.SettingType = SettingType.MartingaleShort;
        }

        /// <summary>
        /// Run
        /// TrailingMoveTop 일때 ListMin의 값이 있어야 함
        /// 매집 테스트
        /// 매도 채우기 테스트
        /// </summary>
        /// <param name="allOrder"></param>
        public new void Run(Models.Order? allOrder)
        {
            if (this.User == null) return;

            try
            {
                if (this.User.Api == null) return;
                if (this.Market == null) return;
                if (this.BasePrice <= 0) return;
                if (this.TopPrice <= 0 || this.BasePrice >= this.TopPrice) return;
                if (this.Rate <= 0.1M) return;

                //if (allOrder != null && allOrder.OrderList != null)
                //    $"OCNT:{allOrder.OrderList.Where(x => x.Market == this.Market).Count()} - {nameof(SettingMartingaleShortTrading)}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                this.CurrentInfo = this.GetCurrentInfo();
                if (this.CurrentInfo == null)
                    return;

                if (this.WorkDataList == null || this.Invest == 0)
                {
                    if (this.WorkDataList == null && this.IsFirstReadWorkDataList)
                    {
                        this.IsFirstReadWorkDataList = false;
                        this.WorkDataList = ReadWorkDataList(this);
                    }

                    if (this.WorkDataList == null)
                        if (this.BaseInvest == 0)
                        {
                            var orderChance = this.User.Api.OrderChance(this.Market);
                            if (orderChance != null && orderChance.AskAccount != null)
                                this.Invest = orderChance.AskAccount.Balance;

                            this.WorkDataList = null;

                            if (this.Invest < this.User.ExchangeID switch
                            {
                                1 => 5000 / this.CurrentInfo.TradePrice,
                                2 => 500 / this.CurrentInfo.TradePrice,
                                _ => 5000 / this.CurrentInfo.TradePrice,
                            }) return;
                        }

                    this.WorkDataList ??= this.GetWorkData(this.CurrentInfo.TradePrice);
                }
                //else
                //{
                //    this.CurrentInfo = this.GetCurrentInfo();
                //    if (this.CurrentInfo == null)
                //        return;
                //}

                if (this.WorkDataList == null || this.WorkDataList.Count < 1) return;

                if (this.WorkDataList == null) return;

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

                    var askOrder = allOrderList.FirstOrDefault(x => x.Side == "ask" && x.Price == dataRow.AskPrice && x.Volume == dataRow.AskQty);
                    if (askOrder != null)
                    {
                        dataRow.AskOrder = askOrder;
                        dataRow.AskOrderChecked = true;
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
                        item.BidOrder = order1;
                        item.BidOrderChecked = true;
                    }
                    else if (order1 != null && order1.Error != null)
                    {
                        this.Message = order1.Error.Message;
                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }

                workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && !x.AskOrder.UUID.Contains("임시") && !x.AskOrderChecked);

                foreach (var item in workDataList)
                {
                    if (item.AskOrder == null || item.AskOrder.UUID == null) continue;

                    var order1 = this.User.Api.Order(this.Market, Models.OrderSide.ask.ToString(), item.AskOrder.UUID);

                    if (order1 != null && order1.Error == null)//에러가 아니면
                    {
                        item.AskOrder = order1;
                        item.AskOrderChecked = true;
                    }
                    else if (order1 != null && order1.Error != null)
                    {
                        this.Message = order1.Error.Message;
                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }

                //매도만 있고 상태가 cancel 이면 초기화
                workDataList = this.WorkDataList.Where(x => (x.BidOrder == null || x.BidOrder.UUID == null || x.BidOrder.UUID == "")
                                                            && x.AskOrder != null && x.AskOrder.UUID != "" && x.AskOrder.State == "cancel");
                foreach (var item in workDataList)
                {
                    item.BidOrder = null;
                    item.AskOrder = null;
                }

                //매도 체결 되었고 매수가 cancel이면 매수 초기화
                workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && x.BidOrder.State == "cancel"
                                                            && x.AskOrder != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done");

                foreach (var item in workDataList)
                {
                    item.BidOrder = null;
                }


                //매도가 ""인데 매수가 wait 이면
                //매도를 done로 만들고 UUID를 임시번호로 할당
                workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && x.BidOrder.State == "wait"
                                                            && (x.AskOrder == null || x.AskOrder.UUID == null || x.AskOrder.UUID == "" || x.AskOrder.State == ""));
                foreach (var item in workDataList)
                {
                    item.AskOrder = new()
                    {
                        UUID = "임시" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Side = "ask",
                        OrdType = "limit",
                        Price = item.AskPrice,
                        AvgPrice = item.AskPrice,
                        State = "done",
                        Market = this.Market,
                        CreatedAt = DateTime.Now,
                        Volume = item.AskQty,
                        RemainingVolume = 0,
                        ReservedFee = 0,
                        RemainingFee = 0,
                        PaidFee = (item.AskPrice * item.AskQty) * (this.Fees / 100),
                        Locked = 0,
                        ExecutedVolume = item.AskQty
                    };
                }


                //익절 처리
                workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && x.BidOrder.State == "done"
                                                            && x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done");
                if (workDataList.Count() == 1)
                {
                    try
                    {
                        var item = workDataList.ToList()[0];

                        if (item.BidOrder == null || item.AskOrder == null) return;

                        decimal ASK = (item.BidOrder.Volume * item.AskAvgPrice) - item.AskTotalFee - item.AskOrder.PaidFee;
                        decimal BID = (item.BidOrder.Volume * item.BidOrder.Price) + item.BidOrder.PaidFee;

                        this.Profit(this.User, this.SettingID, this.User.UserID
                            , item.BidOrder.Price, item.BidOrder.Volume, item.BidOrder.PaidFee
                            , item.AskAvgPrice, item.BidOrder.Volume, item.AskTotalFee + item.AskOrder.PaidFee
                            , ASK - BID
                            , this.Market);

                        //일부만 매도된 주문이 있으면 다시 매수
                        workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State != "done" && x.AskOrder.Volume != x.AskOrder.RemainingVolume);
                        if (workDataList.Any())
                        {
                            ASK = 0;
                            foreach (var item1 in workDataList)
                            {
                                if (item1.AskOrder == null) continue;

                                ASK += item1.AskOrder.RemainingVolume;
                            }

                            if (ASK > 0)
                            {
                                var order1 = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid, ASK, workDataList.ToArray().Max(x => x.AskPrice));

                                if (order1.Error != null)//에러가 아니면
                                {
                                    this.Message = order1.Error.Message;
                                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                                }
                            }
                        }

                        if (this.IsProfitStop)
                        {
                            this.BidCancel = true;
                            this.AskCancel = true;
                            this.User.RemoveSetting(this);
                        }
                        else
                        {
                            this.Organized(this.SettingID, true, true, false, false, false, this.IsProfitStop, this.IsProfitStop);
                            this.WorkDataList = null;
                        }

                        this.FirstFix = false;
                        return;
                    }
                    catch (Exception ex)
                    {
                        this.Message = ex.Message;
                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        return;
                    }
                }


                //종료호가 터치 중지
                if (this.CurrentInfo.TradePrice > this.TopPrice && this.TopStop)
                {
                    this.AskCancel= true;
                    this.User.RemoveSetting(this);
                    //this.Organized(this.SettingID, false, true, false, false, false, true, true);
                    //this.WorkDataList = null;
                    return;
                }


                //매수는 없고 매도만 done 인 경우(1개 이상 일 수 있음)
                //매수 주문 생성
                workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done"
                                                        && (x.BidOrder == null || x.BidOrder.UUID == null || x.BidOrder.UUID == ""));

                if (workDataList.Any())
                {
                    var maxAskPrice = workDataList.Max(y => y.AskPrice);

                    workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done" && x.AskPrice != maxAskPrice);

                    //매도가 된게 있다면(가장 비싼 가격은 뺴고)
                    List<WorkData> workDataDelete;
                    workDataDelete = [.. workDataList];

                    foreach (var item in workDataDelete)
                    {
                        //$"Remove".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                        if (item.BidOrder != null && item.BidOrder.UUID != null && item.BidOrder.UUID != "")
                        {
                            //$"CancelOrder BidUUID:{item.BidOrder.UUID}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                            var order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), item.BidOrder.UUID);

                            if (order.Error != null)
                            {
                                this.Message = order.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            }

                            if (item.BidQty != item.BidOrder.RemainingVolume)
                            {
                                $"CancelOrder BidUUID item.BidQty != item.BidRemainingVolume".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                                foreach (var item1 in this.WorkDataList)
                                    item1.BidQty -= (item.BidQty - item.BidOrder.RemainingVolume);
                            }
                        }

                        if (item.AskOrder != null && item.AskOrder.UUID != null && item.AskOrder.UUID != "" && item.AskOrder.State == "done")
                            this.WorkDataList.ForEach(x => x.AskTotalFee += item.AskOrder.PaidFee);

                        this.WorkDataList.Remove(item);
                    }


                    //매도 완료 되었으면 매수 주문
                    workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done"
                                                            && (x.BidOrder == null || x.BidOrder.UUID == null || x.BidOrder.UUID == ""));
                    if (workDataList.Count() == 1)
                    {
                        var workDataListArray = workDataList.ToArray()[0];

                        var order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid, workDataListArray.BidQty, workDataListArray.BidPrice);

                        if (order != null && order.Error == null)//에러가 없으면
                        {
                            workDataListArray.BidOrder = order;
                            return;
                        }
                        else if (order != null && order.Error != null)
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }

                        if (workDataListArray.AskOrder != null && workDataListArray.AskOrder.UUID != null && workDataListArray.AskOrder.UUID != "" && workDataListArray.AskOrder.State == "done")
                            this.WorkDataList.ForEach(x => x.AskTotalFee += workDataListArray.AskOrder.PaidFee);
                    }
                }

                //일부만 매수되어 있는데
                //  매도 done이 1개 보다 많으면
                //  일부만 매수된 주문 취소
                //  매수 주문 모두를 일부만 매수된 수량만큼 모두 줄임
                workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "" && x.BidOrder.State == "wait" && x.BidOrder.Volume != x.BidOrder.RemainingVolume);
                if (workDataList.Count() == 1)
                {
                    var askOrder = workDataList.ToArray()[0];

                    workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done");
                    if (workDataList.Count() > 1 && askOrder.BidOrder != null && askOrder.BidOrder.UUID != null && askOrder.BidOrder.UUID != "")
                    {
                        var order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), askOrder.BidOrder.UUID);

                        if (order.Error != null)
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            return;
                        }

                        foreach (var item in this.WorkDataList)
                            item.BidQty -= (askOrder.BidQty - askOrder.BidOrder.RemainingVolume);

                        var workDataListArray = workDataList.ToArray()[0];

                        if (workDataListArray.AskOrder != null && workDataListArray.AskOrder.UUID != null && workDataListArray.AskOrder.UUID != "" && workDataListArray.AskOrder.State == "done")
                            this.WorkDataList.ForEach(x => x.AskTotalFee += workDataListArray.AskOrder.PaidFee);

                        this.WorkDataList.Remove(askOrder);
                    }
                }

                //일부만 매도 있고, 매도 done가 없고
                //  매수 주문이 하나도 없고
                //  매수가격 보다 작으면
                //  일부만 매도된 수량 만큼 매수
                //  매도된 주문 모두 취소 포지션 종료
                workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State != "done" && x.AskOrder.Volume != x.AskOrder.RemainingVolume);
                if (workDataList.Count() == 1)
                {
                    var bidOrder = workDataList.ToArray()[0];

                    workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done");
                    if (!workDataList.Any() && bidOrder.AskOrder != null)
                    {
                        var minBidPrice = this.WorkDataList.Min(x => x.BidPrice);

                        workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "");
                        if (minBidPrice > this.CurrentInfo.TradePrice && !workDataList.Any())
                        {
                            var order1 = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid
                            , bidOrder.AskOrder.Volume - bidOrder.AskOrder.RemainingVolume
                            , minBidPrice, Models.OrderType.limit);

                            if (order1 != null && order1.Error == null)//매도 주문 정상이면 포지션 종료
                            {
                                if (this.IsProfitStop)
                                {
                                    this.AskCancel = true;
                                    this.User.RemoveSetting(this);
                                }
                                else
                                {
                                    this.Organized(this.SettingID, false, true, false, false, false, this.IsProfitStop, this.IsProfitStop);
                                    this.WorkDataList = null;
                                }
                                return;
                            }
                        }
                    }
                }

                //일부만 매도 있고, 매도 done가 하나 있고
                //  매수 주문이 하나 있고
                //  매수 주문 가격 보다 작으면
                //  일부만 매도된 수량 만큼 매수
                //  매도된 주문 모두 취소 포지션 종료
                workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State != "done" && x.AskOrder.Volume != x.AskOrder.RemainingVolume);
                if (workDataList.Count() == 1)
                {
                    var bidOrder = workDataList.ToArray()[0];

                    workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done");
                    if (workDataList.Count() == 1)
                    {
                        var minBidPrice = this.WorkDataList.Min(x => x.BidPrice);

                        if (minBidPrice > this.CurrentInfo.TradePrice && bidOrder.AskOrder != null)
                        {
                            var order1 = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid
                             , bidOrder.AskOrder.Volume - bidOrder.AskOrder.RemainingVolume
                             , minBidPrice, Models.OrderType.limit);

                            if (order1 != null && order1.Error == null)//매도 주문 정상이면 포지션 종료
                            {
                                if (this.IsProfitStop)
                                {
                                    this.AskCancel = true;
                                    this.User.RemoveSetting(this);
                                }
                                else
                                {
                                    this.Organized(this.SettingID, false, true, false, false, false, this.IsProfitStop, this.IsProfitStop);
                                    this.WorkDataList = null;
                                }
                                return;
                            }
                        }
                    }
                }

                //매수/매도 주문이 하나도 없으면 매도 주문
                workDataList = this.WorkDataList.Where(x => (x.AskOrder == null || x.AskOrder.UUID == null || x.AskOrder.UUID == "")
                                                        && (x.BidOrder == null || x.BidOrder.UUID == null || x.BidOrder.UUID == "")).OrderBy(y => y.AskPrice);
                foreach (var item in workDataList)
                {
                    var order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, item.AskQty, item.AskPrice);

                    if (order != null && order.Error == null)//에러가 아니면
                        item.AskOrder = order;
                    else if (order != null && order.Error != null)
                    {
                        this.Message = order.Error.Message;
                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }

                //매도가 있고 매도 완료 이면, 매수 없고, 매수 상태가 클리어 이면 매수 주문 생성
                workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.State == "done"
                                                        && (x.BidOrder == null || x.BidOrder.UUID == null || x.BidOrder.UUID == ""));
                if (workDataList.Count() == 1)
                {
                    var bidOrder = workDataList.ToArray()[0];

                    var order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid, bidOrder.BidQty, bidOrder.BidPrice);

                    if (order != null && order.Error == null)//에러가 없으면
                        bidOrder.BidOrder = order;
                    else if (order != null && order.Error != null)
                    {
                        this.Message = order.Error.Message;
                        this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }


                //현재 가격이 최소 매수 가격 보다 작다면
                //매수 주문이 없고
                //매도 잔량이 모두 그대로 있다면
                //박스권 재설정
                if (!this.FirstFix)
                {
                    var bidMinPrice = this.WorkDataList.Min(x => x.BidPrice);

                    if (bidMinPrice > this.CurrentInfo.TradePrice)
                    {
                        workDataList = this.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.UUID != null && x.BidOrder.UUID != "");
                        if (!workDataList.Any())
                        {
                            workDataList = this.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.UUID != null && x.AskOrder.UUID != "" && x.AskOrder.Volume != x.AskOrder.RemainingVolume);
                            if (!workDataList.Any())
                            {
                                this.Organized(this.SettingID, false, true, false, false, false, false, false);
                                this.WorkDataList = null;
                                return;
                            }
                        }
                    }
                }


                //1개만 남은 상황에서
                //if (setting.WorkDataList.Count == 1 && setting.IsOutChange && setting.OutPrice == 0 && setting.OutQty == 0 && setting.WorkDataList[0].BidQty == setting.WorkDataList[0].BidRemainingVolume)
                //{
                //    //마지막 매도가격에서 차이가격 보다 더 올라가면
                //    if (setting.LastTradePrice > (setting.WorkDataList[0].AskPrice + (setting.WorkDataList[0].AskPrice - setting.WorkDataList[0].BidPrice)))
                //    {
                //        //매수 주문 취소하고 롱으로 전환
                //        System.Threading.Thread.Sleep(60);
                //        lock (setting.User.LockSettingList)
                //            order1 = setting.Api.CancelOrder(setting.Market, OrderSide.bid.ToString(), setting.WorkDataList[0].BidUUID);

                //        if (order1.Error != null)
                //        {
                //            System.Console.WriteLine(order1.Error.message);
                //            //return;
                //        }
                //        else
                //        {
                //            response = this.UpdateOut(setting.SettingID, setting.WorkDataList[0].BidPrice, setting.WorkDataList[0].BidQty, setting.SettingUserID);

                //            if (response.Status != Status.OK)
                //            {
                //                System.Console.WriteLine(response.Message);
                //            }
                //            else
                //            {
                //                lock (setting.User.LockSettingList)
                //                    if (setting.User.Settings.Contains(setting))
                //                    {
                //                        if (setting.Api is IDisposable aaa)
                //                            aaa.Dispose();

                //                        setting.User.Settings.Remove(setting);
                //                    }

                //                return;
                //            }
                //        }
                //    }
                //}
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
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("MartingaleShort.GetWorkData");
            data["1"].AddParameter("EXCHANGE_ID", Database.DbType.Int, 3, this.ExchangeID);
            data["1"].AddParameter("BASE_PRICE", Database.DbType.Decimal, 25, this.FirstFix ? this.BasePrice : TRADE_PRICE);
            data["1"].AddParameter("GAP_RATE", Database.DbType.Decimal, 25, this.GapRate);
            data["1"].AddParameter("RATE", Database.DbType.Decimal, 25, this.Rate);
            data["1"].AddParameter("FEES", Database.DbType.Decimal, 25, this.Fees);
            data["1"].AddParameter("LIST_MIN", Database.DbType.Int, 3, this.ListMin);
            data["1"].AddParameter("MARKET_ID", Database.DbType.NVarChar, 20, this.Market);
            data["1"].AddParameter("INVEST", Database.DbType.Decimal, 25, this.Invest);

            response = this.ServiceRequest(data);

            if (response.Status == Status.OK && response.DataSet != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
            {
                List<WorkData> workDatas = [];
                foreach (var item in response.DataSet.DataTables[0].DataRows)
                {
                    workDatas.Add(new()
                    {
                        BidPrice = item.Decimal("BID_PRICE") ?? 0,
                        BidQty = item.Decimal("BID_QTY") ?? 0,
                        AskPrice = item.Decimal("ASK_PRICE") ?? 0,
                        AskQty = item.Decimal("ASK_QTY") ?? 0,
                        AskAvgPrice = item.Decimal("ASK_KRW_AVG") ?? 0,
                    });
                }

                if (workDatas.Any(x => x.BidPrice == 0 || x.BidQty == 0 || x.AskPrice == 0 || x.AskQty == 0))
                    return null;
                else
                {
                    //$"SettingMartingaleShort".WriteMessage(this.ExchangeID, this.User.UserID, this.SettingID, this.Market);
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
    }
}