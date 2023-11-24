using MetaFrm.Stock.Console;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// GridMartingaleLong
    /// </summary>
    public class GridMartingaleLong : Setting, ISettingAction
    {
        /// <summary>
        /// Grid
        /// </summary>
        public Grid Grid { get; set; }

        /// <summary>
        /// MartingaleLong
        /// </summary>
        public MartingaleLong MartingaleLong { get; set; }

        private Setting? current;
        /// <summary>
        /// Current
        /// </summary>
        public Setting? Current 
        {
            get 
            {
                return this.current;
            }
            set
            {
                if (this.current != null && value != null)
                    this.ChangeSettingMessage(this.current, value);

                this.current = value;
            }
        }

        /// <summary>
        /// GridMartingaleLong
        /// </summary>
        /// <param name="user"></param>
        /// <param name="grid"></param>
        /// <param name="martingaleLong"></param>
        public GridMartingaleLong(User user, Grid grid, MartingaleLong martingaleLong) : base(user) 
        {
            this.SettingType = SettingType.GridMartingaleLong;

            this.Grid = grid;
            this.MartingaleLong = martingaleLong;

            this.Grid.ParentSetting = this;
            this.Grid.LossStack = this.LossStack;

            this.MartingaleLong.ParentSetting = this;
            this.MartingaleLong.LossStack = this.LossStack;
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
                var lossStack = this.ReadLossStack();
                if (lossStack != null)
                {
                    this.LossStack = lossStack;
                    this.Grid.LossStack = this.LossStack;
                    this.MartingaleLong.LossStack = this.LossStack;
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


                //this.SettingMartingaleLongTrading.Market = this.Market;
                //this.SettingMartingaleLongTrading.SettingID = this.SettingID;
                //this.SettingMartingaleLongTrading.BasePrice = this.CurrentInfo.TradePrice;
                //decimal tmp = this.SettingMartingaleLongTrading.BasePrice * (1 + (((this.SettingMartingaleLongTrading.Rate + Setting.DefaultFees(this.User.ExchangeID)) / 99.8M)) * this.SettingMartingaleLongTrading.ListMin);
                //tmp = Math.Round(tmp);
                //this.SettingMartingaleLongTrading.TopPrice = tmp;

                ////user.AddSetting(new SettingMartingaleLongTrading(user)
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
                    this.SetGrid(this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);
                    this.Current = this.Grid;
                }
                else
                {
                    this.SetMartingaleLong(this.CurrentInfo.TradePrice, this.LossStack.Peek().Invest);
                    this.Current = this.MartingaleLong;
                }
            }

            try
            {
                if (this.Current.SettingType == SettingType.Grid && this.Current is Grid set1)
                    this.GridRun(allOrder, set1);

                if (this.Current.SettingType == SettingType.MartingaleLong && this.Current is MartingaleLong set2)
                    this.MartingaleLongRun(allOrder, set2);
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
        private void GridRun(Grid gridTrading, bool isPush)
        {
            decimal krw = 0;
            decimal askKrw = 0;
            
            if (gridTrading.WorkDataList == null) return;
            if (this.CurrentInfo == null) return;

            //총 매도 금액
            var askVolumeWorkData = gridTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait");
            if (askVolumeWorkData.Any())
                askKrw += askVolumeWorkData.Sum(x => (x.AskOrder?.Price * x.AskOrder?.Volume) - (x.AskOrder?.Price * x.AskOrder?.Volume * (gridTrading.Fees / 100M))) ?? 0;

            //일부 매도된 수량이 있으면 매도한 금액 가치만큼 누적
            var askExecutedVolumeWorkData = gridTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.ExecutedVolume > 0);
            if (askExecutedVolumeWorkData.Any())
                krw += askExecutedVolumeWorkData.Sum(x => (x.AskOrder?.Price * x.AskOrder?.ExecutedVolume) - x.AskOrder?.PaidFee) ?? 0;

            //남아 있는 매도 주문의 수량 만큼을 시장가로 매도 한다
            var askWorkData = gridTrading.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.RemainingVolume > 0);
            if (askWorkData.Any())
            {
                var askAmount = askWorkData.Sum(x => x.AskOrder?.RemainingVolume) ?? 0;
                if (askAmount > 0 && gridTrading.Market != null)
                {
                    gridTrading.Organized(gridTrading.SettingID, true, true, false, false);

                    var order = this.MakeOrderAskMarket(gridTrading.Market, askAmount);

                    if (order != null)
                        krw += order.Trades != null && order.Trades.Count > 0 ? order.Trades.Sum(x => x.Price * x.Volume) - order.Trades.Sum(x => x.Fee) : order.Price * askAmount - (order.Price * askAmount * (gridTrading.Fees / 100M));
                }
            }

            //일부 매수된 주문의 수량 만큼을 시장가로 매도 한다
            var bidWorkData = gridTrading.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);
            if (bidWorkData.Any())
            {
                var askAmount = bidWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;

                if (askAmount > 0 && gridTrading.Market != null)
                {
                    var order = this.MakeOrderAskMarket(gridTrading.Market, askAmount);

                    if (order != null)
                        krw += order.Trades != null && order.Trades.Count > 0 ? order.Trades.Sum(x => x.Price * x.Volume) - order.Trades.Sum(x => x.Fee) : order.Price * askAmount - (order.Price * askAmount * (gridTrading.Fees / 100M));
                }
            }

            if (krw > 0 && gridTrading.Market != null)
            {
                //남아 있는 매수 주문의 금액
                bidWorkData = gridTrading.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.RemainingVolume > 0);
                if (bidWorkData.Any())
                    krw = bidWorkData.Sum(x => (x.BidOrder?.Price * x.BidOrder?.RemainingVolume) + (x.BidOrder?.Price * x.BidOrder?.RemainingVolume * (gridTrading.Fees / 100M))) ?? 0;

                //SettingMartingaleLongTrading으로 전환
                //this.SettingMartingaleLongTrading.Market = this.Market;
                //this.SettingMartingaleLongTrading.SettingID = this.SettingID;
                //this.SettingMartingaleLongTrading.BasePrice = this.CurrentInfo.TradePrice;
                //this.SettingMartingaleLongTrading.TopPrice = this.GetTopPrice(this.CurrentInfo.TradePrice, this.SettingMartingaleLongTrading.Rate, this.SettingMartingaleLongTrading.ListMin);
                //this.SettingMartingaleLongTrading.Invest = qty;
                this.SetMartingaleLong(this.CurrentInfo.TradePrice, krw);

                if (isPush)
                    this.LossStack.Push(new() { AccProfit = 0, Invest = askKrw });
                else
                    this.LossStack.Pop();

                $"전환 SettingGridTrading->SettingMartingaleLongTrading IsPush:{isPush} TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
                this.Current = this.MartingaleLong;
                gridTrading.WorkDataList = null;
                return;
            }
        }

        private void MartingaleLongRun(Models.Order? allOrder, MartingaleLong martingaleLong)
        {
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;

            if (this.CurrentInfo == null) return;

            martingaleLong.Run(allOrder);

            if (martingaleLong.WorkDataList == null)
                return;

            decimal minBidPrice = martingaleLong.WorkDataList.Max(x => x.BidPrice);

            //마틴게일롱 -> 그리드 전환
            //매수한 총금액 보다 커야 전환 가능
            //모두 매수 되었는데..
            //매수된 전체 금액이 이전 Loss의 전체 금액보다 작을 수도 있고 클 수 도 있음
            if (minBidPrice * (1 - (martingaleLong.Rate / 99.8M) * (martingaleLong.ListMin / 2.5M)) > this.CurrentInfo.TradePrice)
            {
                //이전 매매의 총금액 보다 작다면 손실
                //this.LossStack.Push해서 그리드로 전환하고 손실난 금액이 복구 되도록 해야 함
                if (this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, martingaleLong.WorkDataList, martingaleLong.Fees) < 0)
                {
                    this.MartingaleLongRun(martingaleLong, true);
                    return;
                }
                else
                {
                    this.MartingaleLongRun(martingaleLong, false);
                    return;
                }
            }
            else
            {
                //손실 만큼 복구를 했으면
                //그리드로 전환
                //this.LossStack 남은게 없다면???
                if (this.LossStack.Peek().RemainLoss(this.Invest, this.CurrentInfo, martingaleLong.WorkDataList, martingaleLong.Fees) > 0)
                {
                    var askExecutedVolumeWorkData = martingaleLong.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);

                    if (askExecutedVolumeWorkData.Any())
                    {
                        //일부 매수된 수량이 있으면 시장가 매도를 한다
                        decimal qty = askExecutedVolumeWorkData.Sum(x => x.BidOrder?.ExecutedVolume) ?? 0;
                        if (qty > 0 && martingaleLong.Market != null)
                            this.MakeOrderAskMarket(martingaleLong.Market, qty);
                    }

                    martingaleLong.Organized(martingaleLong.SettingID, true, true, false, false);

                    //매도 안된 수량만큼 시장가로 매도 한다
                    var askWorkData = martingaleLong.WorkDataList.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.RemainingVolume > 0);
                    if (askWorkData.Any())
                    {
                        decimal qty = askWorkData.Sum(x => x.AskOrder?.RemainingVolume) ?? 0;

                        if (qty > 0 && martingaleLong.Market != null)
                            this.MakeOrderAskMarket(martingaleLong.Market, qty);
                    }

                    //this.SettingGridTrading.Market = this.Market;
                    //this.SettingGridTrading.SettingID = this.SettingID;
                    //this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
                    //this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);
                    //this.SettingGridTrading.Invest = this.Invest;

                    this.LossStack.Pop();

                    this.SetGrid(this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);


                    $"전환 SettingMartingaleLongTrading->SettingGridTrading IsPush:{false}TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
                    this.Current = this.Grid;
                    martingaleLong.WorkDataList = null;
                    return;
                }
            }
        }
        private void MartingaleLongRun(MartingaleLong martingaleLong, bool isPush)
        {
            decimal bidQty = 0;
            decimal krw = 0;
            decimal bidKrw = 0;


            if (this.CurrentInfo == null) return;
            if (martingaleLong.WorkDataList == null) return;

            if (isPush)
            {
                var askExecutedVolumeWorkData = martingaleLong.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "done" && x.BidOrder.ExecutedVolume > 0);
                bidQty = askExecutedVolumeWorkData.Sum(x => x.AskOrder?.ExecutedVolume) ?? 0;
            }

            martingaleLong.Organized(martingaleLong.SettingID, true, true, false, false);

            //매수된 수량 시장가 매도를 한다
            if (bidQty > 0 && martingaleLong.Market != null)
            {
                var order = this.MakeOrderAskMarket(martingaleLong.Market, bidQty);

                if (order != null)
                    krw += order.Trades != null && order.Trades.Count > 0 ? order.Trades.Sum(x => x.Price * x.Volume) - order.Trades.Sum(x => x.Fee) : order.Price * bidQty - (order.Price * bidQty * (martingaleLong.Fees / 100M));
            }

            //this.SettingGridTrading.Market = this.Market;
            //this.SettingGridTrading.SettingID = this.SettingID;
            //this.SettingGridTrading.TopPrice = this.CurrentInfo.TradePrice;
            //this.SettingGridTrading.BasePrice = this.GetBasePrice(this.CurrentInfo.TradePrice, this.SettingGridTrading.Rate, this.SettingGridTrading.ListMin);

            if (isPush)
            {
                //총 매수 금액
                var askVolumeWorkData = martingaleLong.WorkDataList.Where(x => x.BidOrder != null && x.BidOrder.State == "done");
                if (askVolumeWorkData.Any())
                    bidKrw += askVolumeWorkData.Sum(x => (x.BidOrder?.Price * x.BidOrder?.Volume) - (x.BidOrder?.Price * x.BidOrder?.Volume * (martingaleLong.Fees / 100M))) ?? 0;

                //this.SettingGridTrading.Invest = askAmount;
                this.SetGrid(this.CurrentInfo.TradePrice, krw);
                this.LossStack.Push(new() { AccProfit = 0, Invest = bidKrw });
            }
            else
            {
                this.LossStack.Pop();

                //this.SettingGridTrading.Invest = this.Invest;
                this.SetGrid(this.CurrentInfo.TradePrice, this.LossStack.Count == 0 ? this.Invest : this.LossStack.Peek().Invest);
            }

            $"전환 SettingMartingaleLongTrading->SettingGridTrading IsPush:{isPush} TradePrice:{this.CurrentInfo.TradePrice}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market, ConsoleColor.DarkGreen);
            this.Current = this.Grid;
            martingaleLong.WorkDataList = null;
            return;
        }

        private void SetGrid(decimal tradePrice, decimal invest)
        {
            this.Grid.Market = this.Market;
            this.Grid.SettingID = this.SettingID;
            this.Grid.TopPrice = tradePrice;
            this.Grid.BasePrice = this.GetBasePrice(tradePrice, this.Grid.Rate, this.Grid.ListMin);
            this.Grid.Invest = invest;
        }
        private void SetMartingaleLong(decimal tradePrice, decimal invest)
        {
            this.MartingaleLong.Market = this.Market;
            this.MartingaleLong.SettingID = this.SettingID;
            this.MartingaleLong.BasePrice = tradePrice;
            this.MartingaleLong.TopPrice = this.GetTopPrice(tradePrice, this.MartingaleLong.Rate, this.MartingaleLong.ListMin);
            this.MartingaleLong.Invest = invest;
        }
    }
}