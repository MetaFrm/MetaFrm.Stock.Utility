using MetaFrm.Stock.Console;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// SettingGridMartingaleShortTrading
    /// </summary>
    public class SettingGridMartingaleShortTrading : Setting, ISettingAction
    {
        /// <summary>
        /// SettingGridTrading
        /// </summary>
        public SettingGridTrading SettingGridTrading { get; set; }

        /// <summary>
        /// SettingMartingaleShortTrading
        /// </summary>
        public SettingMartingaleShortTrading SettingMartingaleShortTrading { get; set; }

        private Setting? settingCurrent;
        /// <summary>
        /// SettingCurrent
        /// </summary>
        public Setting? SettingCurrent 
        {
            get 
            {
                return this.settingCurrent;
            }
            set
            {
                if (this.settingCurrent != null && value != null)
                    this.ChangeSettingMessage(this.settingCurrent, value);

                this.settingCurrent = value;
            }
        }

        /// <summary>
        /// SettingGridMartingaleShortTrading
        /// </summary>
        /// <param name="user"></param>
        /// <param name="settingGridTrading"></param>
        /// <param name="settingMartingaleShortTrading"></param>
        public SettingGridMartingaleShortTrading(User user, SettingGridTrading settingGridTrading, SettingMartingaleShortTrading settingMartingaleShortTrading) : base(user) 
        {
            this.SettingType = SettingType.GridMartingaleShort;

            this.SettingGridTrading = settingGridTrading;
            this.SettingMartingaleShortTrading = settingMartingaleShortTrading;

            this.SettingGridTrading.ParentSetting = this;
            this.SettingGridTrading.LossStack = this.LossStack;

            this.SettingMartingaleShortTrading.ParentSetting = this;
            this.SettingMartingaleShortTrading.LossStack = this.LossStack;
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="allOrder"></param>
        public new void Run(Models.Order? allOrder)
        {
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;

            this.CurrentInfo = this.GetCurrentInfo();

            if (this.CurrentInfo == null) return;

            //처음 시작시 그리드 매매로 시작
            if (this.SettingCurrent == null)
            {
                var lossStack = this.ReadLossStack();
                if (lossStack != null)
                {
                    this.LossStack = lossStack;
                    this.SettingGridTrading.LossStack = this.LossStack;
                    this.SettingMartingaleShortTrading.LossStack = this.LossStack;
                }

                ////this.SettingGridTrading.Market = this.Market;
                ////this.SettingGridTrading.SettingID = this.SettingID;
                ////this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
                ////this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);
                ////this.SettingGridTrading.Invest = this.Invest;
                //this.SetSettingGridTrading(this.CurrentInfo.TradePrice, this.Invest);

                //user.AddSetting(new SettingGridTrading(user)
                //{
                //    Market = "KRW-SBD",
                //    SettingID = 1,
                //    BasePrice = 5200M,
                //    TopPrice = 6000M,

                //    Rate = 2.5M,//*세팅시 입력
                //    ListMin = 5,//*세팅시 입력
                //    Invest = 200000,//*세팅시 입력
                //    SmartType = SmartType.TrailingMoveTopShorten,//*세팅시 입력
                //    IsBuying = true,//*세팅시 입력
                //});


                //this.SettingMartingaleShortTrading.Market = this.Market;
                //this.SettingMartingaleShortTrading.SettingID = this.SettingID;
                //this.SettingMartingaleShortTrading.BasePrice = this.CurrentInfo.TradePrice;
                //decimal tmp = this.SettingMartingaleShortTrading.BasePrice * (1 + (((this.SettingMartingaleShortTrading.Rate + Setting.DefaultFees(this.User.ExchangeID)) / 99.8M)) * this.SettingMartingaleShortTrading.ListMin);
                //tmp = Math.Round(tmp);
                //this.SettingMartingaleShortTrading.TopPrice = tmp;

                ////user.AddSetting(new SettingMartingaleShortTrading(user)
                ////{
                ////    Market = "KRW-NEAR",
                ////    SettingID = 3,
                ////    BasePrice = 2000M,
                ////    TopPrice = 3000M,
                ////    Rate = 2.0M,//*세팅시 입력
                ////    ListMin = 4,//*세팅시 입력
                ////    Invest = 0,

                ////    GapRate = 5.0M,//*세팅시 입력
                ////    IsProfitStop = false,
                ////    FirstFix = false,
                ////});

                if (this.LossStack.Count % 2 == 0)
                {
                    this.SetSettingGridTrading(this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);
                    this.SettingCurrent = this.SettingGridTrading;
                }
                else
                {
                    this.SetSettingMartingaleShortTrading(this.CurrentInfo.TradePrice, this.LossStack.Peek().Invest);
                    this.SettingCurrent = this.SettingMartingaleShortTrading;
                }
            }

            try
            {
                if (this.SettingCurrent.SettingType == SettingType.Grid && this.SettingCurrent is SettingGridTrading set1)
                    this.GridTradingRun(allOrder, set1);

                if (this.SettingCurrent.SettingType == SettingType.MartingaleShort && this.SettingCurrent is SettingMartingaleShortTrading set2)
                    this.MartingaleShortTradingRun(allOrder, set2);
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
        }
        private void GridTradingRun(Models.Order? allOrder, SettingGridTrading gridTrading)
        {
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;

            if (this.CurrentInfo == null) return;

            gridTrading.Run(allOrder);

            if (gridTrading.WorkDataList == null)
                return;

            decimal minBidPrice = gridTrading.WorkDataList.Min(x => x.BidPrice);

            //그리드 -> 마틴게일숏 전환 (마지막 매수 보다 아래로 하락하면)
            if (minBidPrice > this.CurrentInfo.TradePrice * (1 + (gridTrading.Rate / 99.8M) * 2))
            {
                this.GridTradingRun(gridTrading, true);
                return;
            }
            else
            {
                //손실 본 만큼 수익이 발생 했으면
                //그리드 -> 숏으로 전환
                if (this.LossStack.Count > 0 && this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, gridTrading.WorkDataList, gridTrading.Fees) > 0)
                {
                    this.GridTradingRun(gridTrading, false);
                    return;
                }
            }
        }
        private void GridTradingRun(SettingGridTrading gridTrading, bool isPush)
        {
            decimal qty = 0;
            decimal bidKrw = 0;
            
            if (gridTrading.WorkDataList == null) return;
            if (this.CurrentInfo == null) return;

            //일부 매도된 수량이 있으면 매도한 금액 가치만큼 시장가 매수를 한다(매도한 금액 만큼 현금이 있으므로)
            var askExecutedVolumeWorkData = gridTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.ExecutedVolume > 0);
            if (askExecutedVolumeWorkData.Any())
            {
                decimal askAmount = askExecutedVolumeWorkData.Sum(x => (x.AskOrder?.Price * x.AskOrder?.ExecutedVolume) - x.AskOrder?.PaidFee) ?? 0;
                if (askAmount > 0 && gridTrading.Market != null)
                    qty += this.MakeOrderBidPrice(gridTrading.Market, askAmount)?.ExecutedVolume ?? 0;
            }

            //남아 있는 매도 주문의 수량
            var askWorkData = gridTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.RemainingVolume > 0);
            if (askWorkData.Any())
                qty += askWorkData.Sum(x => x.AskOrder?.RemainingVolume) ?? 0;

            //일부 매수된 주문의 수량
            var bidWorkData = gridTrading.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);
            if (bidWorkData.Any())
                qty += bidWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;

            if (qty > 0 && gridTrading.Market != null)
            {
                //남아 있는 매수 주문의 금액
                bidWorkData = gridTrading.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.RemainingVolume > 0);
                if (bidWorkData.Any())
                    bidKrw = bidWorkData.Sum(x => (x.BidOrder?.Price * x.BidOrder?.RemainingVolume) + (x.BidOrder?.Price * x.BidOrder?.RemainingVolume * (gridTrading.Fees / 100M))) ?? 0;

                gridTrading.Organized(gridTrading.SettingID, false, true, false, false);

                if (bidKrw > 0)
                    qty += this.MakeOrderBidPrice(gridTrading.Market, bidKrw)?.ExecutedVolume ?? 0;

                //SettingMartingaleShortTrading으로 전환
                //this.SettingMartingaleShortTrading.Market = this.Market;
                //this.SettingMartingaleShortTrading.SettingID = this.SettingID;
                //this.SettingMartingaleShortTrading.BasePrice = this.CurrentInfo.TradePrice;
                //this.SettingMartingaleShortTrading.TopPrice = this.GetTopPrice(this.CurrentInfo.TradePrice, this.SettingMartingaleShortTrading.Rate, this.SettingMartingaleShortTrading.ListMin);
                //this.SettingMartingaleShortTrading.Invest = qty;
                this.SetSettingMartingaleShortTrading(this.CurrentInfo.TradePrice, qty);

                if (isPush)
                    this.LossStack.Push(new() { AccProfit = 0, Invest = qty });
                else
                    this.LossStack.Pop();

                $"전환 SettingGridTrading->SettingMartingaleShortTrading IsPush:{isPush} TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
                this.SettingCurrent = this.SettingMartingaleShortTrading;
                gridTrading.WorkDataList = null;
                return;
            }
        }

        private void MartingaleShortTradingRun(Models.Order? allOrder, SettingMartingaleShortTrading settingMartingaleShortTrading)
        {
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;

            if (this.CurrentInfo == null) return;

            settingMartingaleShortTrading.Run(allOrder);

            if (settingMartingaleShortTrading.WorkDataList == null)
                return;

            decimal maxAskPrice = settingMartingaleShortTrading.WorkDataList.Max(x => x.AskPrice);

            //마틴게일숏 -> 그리드 전환
            //매도한 총금액 보다 커야 전환 가능
            //모두 매도 되었는데..
            //매도된 전체 금액이 이전 Loss의 전체 금액보다 작을 수도 있고 클 수 도 있음
            if (maxAskPrice * (1 + (settingMartingaleShortTrading.Rate / 99.8M) * (settingMartingaleShortTrading.ListMin / 2.5M)) < this.CurrentInfo.TradePrice)
            {
                //이전 매매의 총금액 보다 작다면 손실
                //this.LossStack.Push해서 그리드로 전환하고 손실난 금액이 복구 되도록 해야 함
                if (this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, settingMartingaleShortTrading.WorkDataList, settingMartingaleShortTrading.Fees) < 0)
                {
                    this.MartingaleShortTradingRun(settingMartingaleShortTrading, true);
                    return;
                }
                else
                {
                    this.MartingaleShortTradingRun(settingMartingaleShortTrading, false);
                    return;
                }
            }
            else
            {
                //손실 만큼 복구를 했으면
                //그리드로 전환
                //this.LossStack 남은게 없다면???
                if (this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, settingMartingaleShortTrading.WorkDataList, settingMartingaleShortTrading.Fees) > 0)
                {
                    var askExecutedVolumeWorkData = settingMartingaleShortTrading.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);

                    if (askExecutedVolumeWorkData.Any())
                    {
                        //일부 매수된 수량이 있으면 시장가 매도를 한다
                        decimal qty = askExecutedVolumeWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;
                        if (qty > 0 && settingMartingaleShortTrading.Market != null)
                            this.MakeOrderAskMarket(settingMartingaleShortTrading.Market, qty);
                    }

                    settingMartingaleShortTrading.Organized(settingMartingaleShortTrading.SettingID, true, true, false, false);

                    //매도 안된 수량만큼 시장가로 매도 한다
                    var askWorkData = settingMartingaleShortTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.RemainingVolume > 0);
                    if (askWorkData.Any())
                    {
                        decimal qty = askWorkData.Sum(x => x.AskOrder?.RemainingVolume) ?? 0;

                        if (qty > 0 && settingMartingaleShortTrading.Market != null)
                            this.MakeOrderAskMarket(settingMartingaleShortTrading.Market, qty);
                    }

                    //this.SettingGridTrading.Market = this.Market;
                    //this.SettingGridTrading.SettingID = this.SettingID;
                    //this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
                    //this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);
                    //this.SettingGridTrading.Invest = this.Invest;

                    this.LossStack.Pop();

                    this.SetSettingGridTrading(this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);


                    $"전환 SettingMartingaleShortTrading->SettingGridTrading IsPush:{false}TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
                    this.SettingCurrent = this.SettingGridTrading;
                    settingMartingaleShortTrading.WorkDataList = null;
                    return;
                }
            }
        }
        private void MartingaleShortTradingRun(SettingMartingaleShortTrading settingMartingaleShortTrading, bool isPush)
        {
            decimal askAmount = 0;

            if (this.CurrentInfo == null) return;
            if (settingMartingaleShortTrading.WorkDataList == null) return;

            if (isPush)
            {
                var askExecutedVolumeWorkData = settingMartingaleShortTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "done" && x.AskOrder.ExecutedVolume > 0);
                askAmount = askExecutedVolumeWorkData.Sum(x => (x.AskOrder?.Price * x.AskOrder?.ExecutedVolume) - x.AskOrder?.PaidFee) ?? 0;
            }

            settingMartingaleShortTrading.Organized(settingMartingaleShortTrading.SettingID, true, false, false, false);

            //this.SettingGridTrading.Market = this.Market;
            //this.SettingGridTrading.SettingID = this.SettingID;
            //this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
            //this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);

            if (isPush)
            {
                //this.SettingGridTrading.Invest = askAmount;
                this.SetSettingGridTrading(this.CurrentInfo.TradePrice, askAmount);
                this.LossStack.Push(new() { AccProfit = 0, Invest = askAmount });
            }
            else
            {
                this.LossStack.Pop();

                //this.SettingGridTrading.Invest = this.Invest;
                this.SetSettingGridTrading(this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);
            }

            $"전환 SettingMartingaleShortTrading->SettingGridTrading IsPush:{isPush} TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
            this.SettingCurrent = this.SettingGridTrading;
            settingMartingaleShortTrading.WorkDataList = null;
            return;
        }

        private void SetSettingGridTrading(decimal tradePrice, decimal invest)
        {
            this.SettingGridTrading.Market = this.Market;
            this.SettingGridTrading.SettingID = this.SettingID;
            this.SettingGridTrading.TopPrice = tradePrice;
            this.SettingGridTrading.BasePrice = this.GetBasePrice(tradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);
            this.SettingGridTrading.Invest = invest;
        }
        private void SetSettingMartingaleShortTrading(decimal tradePrice, decimal invest)
        {
            this.SettingMartingaleShortTrading.Market = this.Market;
            this.SettingMartingaleShortTrading.SettingID = this.SettingID;
            this.SettingMartingaleShortTrading.BasePrice = tradePrice;
            this.SettingMartingaleShortTrading.TopPrice = this.GetTopPrice(tradePrice, this.SettingMartingaleShortTrading.Rate, this.SettingMartingaleShortTrading.ListMin);
            this.SettingMartingaleShortTrading.Invest = invest;
        }
    }
}