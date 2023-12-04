using MetaFrm.Stock.Console;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// GridMartingaleShort
    /// </summary>
    public class GridMartingaleShort : Setting, ISettingAction
    {
        /// <summary>
        /// Grid
        /// </summary>
        public Grid Grid { get; set; }

        /// <summary>
        /// MartingaleShort
        /// </summary>
        public MartingaleShort MartingaleShort { get; set; }

        /// <summary>
        /// GridMartingaleShort
        /// </summary>
        /// <param name="user"></param>
        /// <param name="grid"></param>
        /// <param name="martingaleShort"></param>
        public GridMartingaleShort(User? user, Grid grid, MartingaleShort martingaleShort) : base(user) 
        {
            this.SettingType = SettingType.GridMartingaleShort;

            this.Grid = grid;
            this.Grid.SmartType = SmartType.TrailingMoveTop;
            this.Grid.IsBuying = false;
            this.Grid.BidOrderAll = true;

            this.MartingaleShort = martingaleShort;

            this.Grid.ParentSetting = this;
            this.Grid.LossStack = this.LossStack;

            this.MartingaleShort.ParentSetting = this;
            this.MartingaleShort.LossStack = this.LossStack;
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
            if (this.Current == null)
            {
                var lossStack = this.ReadLossStack(this.User);
                if (lossStack != null)
                {
                    this.LossStack = lossStack;
                    this.Grid.LossStack = this.LossStack;
                    this.MartingaleShort.LossStack = this.LossStack;
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
                    this.SetGrid(this.User.ExchangeID, this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().CurrentInvest);
                    this.Current = this.Grid;
                }
                else
                {
                    this.SetMartingaleShort(this.User.ExchangeID, this.CurrentInfo.TradePrice, this.LossStack.Peek().CurrentInvest);
                    this.Current = this.MartingaleShort;
                }
            }

            try
            {
                if (this.Current.SettingType == SettingType.Grid && this.Current is Grid set1)
                    this.GridRun(allOrder, set1);

                if (this.Current.SettingType == SettingType.MartingaleShort && this.Current is MartingaleShort set2)
                    this.MartingaleShortRun(allOrder, set2);
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
        }
        private void GridRun(Models.Order? allOrder, Grid grid)
        {
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;

            if (this.CurrentInfo == null) return;

            grid.Run(allOrder);

            if (grid.WorkDataList == null)
                return;

            decimal minBidPrice = grid.WorkDataList.Min(x => x.BidPrice);

            //그리드 -> 마틴게일숏 전환 (마지막 매수 보다 아래로 하락하면)
            if (minBidPrice > this.CurrentInfo.TradePrice * (1 + (grid.Rate / 99.8M) * 2))
            {
                this.GridRun(grid, true);
                return;
            }
            else
            {
                //손실 본 만큼 수익이 발생 했으면
                //그리드 -> 숏으로 전환
                if (this.LossStack.Count > 0 && this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, grid.WorkDataList, grid.Fees) > 0)
                {
                    this.GridRun(grid, false);
                    return;
                }
            }
        }
        private void GridRun(Grid grid, bool isPush)
        {
            decimal qty = 0;
            decimal bidKrw = 0;

            if (this.User == null) return;
            if (grid.WorkDataList == null) return;
            if (this.CurrentInfo == null) return;

            //일부 매도된 수량이 있으면 매도한 금액 가치만큼 시장가 매수를 한다(매도한 금액 만큼 현금이 있으므로)
            var askExecutedVolumeWorkData = grid.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.ExecutedVolume > 0);
            if (askExecutedVolumeWorkData.Any())
            {
                decimal askAmount = askExecutedVolumeWorkData.Sum(x => (x.AskOrder?.Price * x.AskOrder?.ExecutedVolume) - x.AskOrder?.PaidFee) ?? 0;
                if (askAmount > 0 && grid.Market != null)
                    qty += this.MakeOrderBidPrice(grid.Market, askAmount)?.ExecutedVolume ?? 0;
            }

            //남아 있는 매도 주문의 수량
            var askWorkData = grid.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.RemainingVolume > 0);
            if (askWorkData.Any())
                qty += askWorkData.Sum(x => x.AskOrder?.RemainingVolume) ?? 0;

            //일부 매수된 주문의 수량
            var bidWorkData = grid.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);
            if (bidWorkData.Any())
                qty += bidWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;

            if (qty > 0 && grid.Market != null)
            {
                //남아 있는 매수 주문의 금액
                bidWorkData = grid.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.RemainingVolume > 0);
                if (bidWorkData.Any())
                    bidKrw = bidWorkData.Sum(x => (x.BidOrder?.Price * x.BidOrder?.RemainingVolume) + (x.BidOrder?.Price * x.BidOrder?.RemainingVolume * (grid.Fees / 100M))) ?? 0;

                grid.Organized(grid.SettingID, true, true, false, false, false, false);

                if (bidKrw > 0)
                    qty += this.MakeOrderBidPrice(grid.Market, bidKrw)?.ExecutedVolume ?? 0;

                //매도 주문 총 금액
                bidWorkData = grid.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.Volume > 0);
                if (bidWorkData.Any())
                    bidKrw = bidWorkData.Sum(x => (x.BidOrder?.Price * x.BidOrder?.Volume) + (x.BidOrder?.Price * x.BidOrder?.Volume * (grid.Fees / 100M))) ?? 0;

                //SettingMartingaleShortTrading으로 전환
                //this.SettingMartingaleShortTrading.Market = this.Market;
                //this.SettingMartingaleShortTrading.SettingID = this.SettingID;
                //this.SettingMartingaleShortTrading.BasePrice = this.CurrentInfo.TradePrice;
                //this.SettingMartingaleShortTrading.TopPrice = this.GetTopPrice(this.CurrentInfo.TradePrice, this.SettingMartingaleShortTrading.Rate, this.SettingMartingaleShortTrading.ListMin);
                //this.SettingMartingaleShortTrading.Invest = qty;
                this.SetMartingaleShort(this.User.ExchangeID, this.CurrentInfo.TradePrice, qty);

                if (isPush)
                    this.LossStack.Push(new() { AccProfit = 0, Invest = bidKrw, CurrentInvest = qty });
                else
                    this.LossStack.Pop();

                $"전환 SettingGridTrading->SettingMartingaleShortTrading IsPush:{isPush} TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
                this.Current = this.MartingaleShort;
                grid.WorkDataList = null;
                return;
            }
        }

        private void MartingaleShortRun(Models.Order? allOrder, MartingaleShort MartingaleShort)
        {
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;

            if (this.CurrentInfo == null) return;

            MartingaleShort.Run(allOrder);

            if (MartingaleShort.WorkDataList == null)
                return;

            decimal maxAskPrice = MartingaleShort.WorkDataList.Max(x => x.AskPrice);

            //마틴게일숏 -> 그리드 전환
            //매도한 총금액 보다 커야 전환 가능
            //모두 매도 되었는데..
            //매도된 전체 금액이 이전 Loss의 전체 금액보다 작을 수도 있고 클 수 도 있음
            if (maxAskPrice * (1 + (MartingaleShort.Rate / 99.8M) * (MartingaleShort.ListMin / 2.5M)) < this.CurrentInfo.TradePrice)
            {
                //이전 매매의 총금액 보다 작다면 손실
                //this.LossStack.Push해서 그리드로 전환하고 손실난 금액이 복구 되도록 해야 함
                if (this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, MartingaleShort.WorkDataList, MartingaleShort.Fees) < 0)
                {
                    this.MartingaleShortRun(MartingaleShort, true);
                    return;
                }
                else
                {
                    this.MartingaleShortRun(MartingaleShort, false);
                    return;
                }
            }
            else
            {
                //손실 만큼 복구를 했으면
                //그리드로 전환
                //this.LossStack 남은게 없다면???
                if (this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, MartingaleShort.WorkDataList, MartingaleShort.Fees) > 0)
                {
                    var askExecutedVolumeWorkData = MartingaleShort.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);

                    if (askExecutedVolumeWorkData.Any())
                    {
                        //일부 매수된 수량이 있으면 시장가 매도를 한다
                        decimal qty = askExecutedVolumeWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;
                        if (qty > 0 && MartingaleShort.Market != null)
                            this.MakeOrderAskMarket(MartingaleShort.Market, qty);
                    }

                    MartingaleShort.Organized(MartingaleShort.SettingID, true, true, false, false, false, false);

                    //매도 안된 수량만큼 시장가로 매도 한다
                    var askWorkData = MartingaleShort.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.RemainingVolume > 0);
                    if (askWorkData.Any())
                    {
                        decimal qty = askWorkData.Sum(x => x.AskOrder?.RemainingVolume) ?? 0;

                        if (qty > 0 && MartingaleShort.Market != null)
                            this.MakeOrderAskMarket(MartingaleShort.Market, qty);
                    }

                    //this.SettingGridTrading.Market = this.Market;
                    //this.SettingGridTrading.SettingID = this.SettingID;
                    //this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
                    //this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);
                    //this.SettingGridTrading.Invest = this.Invest;

                    this.LossStack.Pop();

                    this.SetGrid(this.User.ExchangeID, this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);


                    $"전환 SettingMartingaleShortTrading->SettingGridTrading IsPush:{false}TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
                    this.Current = this.Grid;
                    MartingaleShort.WorkDataList = null;
                    return;
                }
            }
        }
        private void MartingaleShortRun(MartingaleShort MartingaleShort, bool isPush)
        {
            decimal askAmount = 0;

            if (this.User == null) return;
            if (this.CurrentInfo == null) return;
            if (MartingaleShort.WorkDataList == null) return;

            if (isPush)
            {
                var askExecutedVolumeWorkData = MartingaleShort.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "done" && x.AskOrder.ExecutedVolume > 0);
                askAmount = askExecutedVolumeWorkData.Sum(x => (x.AskOrder?.Price * x.AskOrder?.ExecutedVolume) - x.AskOrder?.PaidFee) ?? 0;
            }

            //일부 매수된 주문의 수량 만큼을 시장가로 매도 한다
            var bidWorkData = MartingaleShort.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);
            if (bidWorkData.Any())
            {
                var bidAmount = bidWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;

                if (bidAmount > 0 && MartingaleShort.Market != null)
                {
                    var order = this.MakeOrderAskMarket(MartingaleShort.Market, bidAmount);

                    if (order != null)
                        askAmount += order.Trades != null && order.Trades.Count > 0 ? order.Trades.Sum(x => x.Price * x.Volume) - order.Trades.Sum(x => x.Fee) : order.Price * bidAmount - (order.Price * bidAmount * (MartingaleShort.Fees / 100M));
                }
            }

            MartingaleShort.Organized(MartingaleShort.SettingID, true, true, false, false, false, false);

            //this.SettingGridTrading.Market = this.Market;
            //this.SettingGridTrading.SettingID = this.SettingID;
            //this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
            //this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);

            if (isPush)
            {
                //this.SettingGridTrading.Invest = askAmount;
                this.SetGrid(this.User.ExchangeID, this.CurrentInfo.TradePrice, askAmount);
                this.LossStack.Push(new() { AccProfit = 0, Invest = this.Invest, CurrentInvest = askAmount });
            }
            else
            {
                this.LossStack.Pop();

                //this.SettingGridTrading.Invest = this.Invest;
                this.SetGrid(this.User.ExchangeID, this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);
            }

            $"전환 SettingMartingaleShortTrading->SettingGridTrading IsPush:{isPush} TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
            this.Current = this.Grid;
            MartingaleShort.WorkDataList = null;
            return;
        }

        private void SetGrid(int exchangeID, decimal tradePrice, decimal invest)
        {
            this.Grid.Market = this.Market;
            this.Grid.SettingID = this.SettingID;
            this.Grid.TopPrice = tradePrice;
            this.Grid.BasePrice = GetBasePrice(exchangeID, tradePrice, this.Grid.Rate, this.Grid.ListMin);
            this.Grid.Invest = invest;
        }
        private void SetMartingaleShort(int exchangeID, decimal tradePrice, decimal invest)
        {
            this.MartingaleShort.Market = this.Market;
            this.MartingaleShort.SettingID = this.SettingID;
            this.MartingaleShort.BasePrice = tradePrice;
            this.MartingaleShort.TopPrice = GetTopPrice(exchangeID, tradePrice, this.MartingaleShort.Rate, this.MartingaleShort.ListMin);
            this.MartingaleShort.Invest = invest;
        }
    }
}