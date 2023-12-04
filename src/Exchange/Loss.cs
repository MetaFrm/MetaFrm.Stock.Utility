namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// Loss
    /// </summary>
    public class Loss
    {
        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; } = DateTime.Now;

        /// <summary>
        /// 투자 수량
        /// </summary>
        public decimal Invest { get; set; }

        /// <summary>
        /// CurrentInvest
        /// </summary>
        public decimal CurrentInvest { get; set; }

        /// <summary>
        /// 누적 수익금
        /// </summary>
        public decimal AccProfit { get; set; }

        private decimal currentKrwValue;
        /// <summary>
        /// 현재 평가금액
        /// </summary>
        public decimal CurrentKrwValue => this.currentKrwValue;

        /// <summary>
        /// 남은 Loss 금액
        /// </summary>
        /// <param name="Invest"></param>
        /// <param name="currentInfo"></param>
        /// <param name="workDatas"></param>
        /// <param name="fees"></param>
        /// <returns></returns>
        public decimal RemainLoss(decimal Invest, Models.Ticker currentInfo, List<WorkData> workDatas, decimal fees)
        {
            //총금액 조금 더 늘린다(시장가로 처리할떄 손실을 보전) 
            return GetCurrentKrwValue(currentInfo, workDatas, fees) + this.AccProfit - (Invest * 1.001M);
        }

        /// <summary>
        /// 현재 상태의 가치 금액
        /// </summary>
        /// <param name="currentInfo"></param>
        /// <param name="workDatas"></param>
        /// <param name="fees"></param>
        /// <returns></returns>
        public decimal GetCurrentKrwValue(Models.Ticker currentInfo, List<WorkData> workDatas, decimal fees)
        {
            this.currentKrwValue = 0;

            //매수 대기 물량 가치
            var bid = workDatas.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.RemainingVolume > 0);
            if (bid.Any())
                this.currentKrwValue += bid.Sum(x => (x.BidOrder?.Price * x.BidOrder?.RemainingVolume) + ((x.BidOrder?.Price * x.BidOrder?.RemainingVolume) * (fees / 100M))) ?? 0;

            //일부 매수된 물량은 현재 가격으로 가치
            bid = workDatas.Where(x => x.BidOrder != null && x.BidOrder.State == "wait" && x.BidOrder.ExecutedVolume > 0);
            if (bid.Any())
                this.currentKrwValue += bid.Sum(x => (currentInfo.TradePrice * x.BidOrder?.ExecutedVolume) + ((currentInfo.TradePrice * x.BidOrder?.ExecutedVolume) * (fees / 100M))) ?? 0;

            //매도 대기 물량은 현재 가격으로 가치(남아 있는 물량도 현재 가격에 매도을 하기 때문에 일부 매도 된 물량을 따로 계산할 필요 없음)
            var ask = workDatas.Where(x => x.AskOrder != null && x.AskOrder.State == "wait" && x.AskOrder.Volume > 0);
            if (ask.Any())
                this.currentKrwValue += ask.Sum(x => (currentInfo.TradePrice * x.AskOrder?.Volume) - ((currentInfo.TradePrice * x.AskOrder?.Volume) * (fees / 100M))) ?? 0;

            return this.currentKrwValue;
        }
    }
}