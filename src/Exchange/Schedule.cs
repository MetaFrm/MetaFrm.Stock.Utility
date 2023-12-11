using MetaFrm.Service;
using MetaFrm.Stock.Console;
using MetaFrm.Stock.Models;
using System.Text;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// Schedule
    /// </summary>
    public class Schedule : Setting, ISettingAction
    {
        /// <summary>
        /// OrderSide
        /// </summary>
        public OrderSide OrderSide { get; set; } = OrderSide.bid;

        /// <summary>
        /// OrderSide
        /// </summary>
        public OrderType OrderType { get; set; } = OrderType.limit;

        /// <summary>
        /// Interval
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// StartDate
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// EndDate
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// ExecuteDate
        /// </summary>
        public DateTime? ExecuteDate { get; set; }


        /// <summary>
        /// Schedule
        /// </summary>
        /// <param name="user"></param>
        public Schedule(User? user) : base(user)
        {
            this.SettingType = SettingType.Schedule;
        }

        /// <summary>
        /// Run
        /// TrailingMoveTop 일때 ListMin의 값이 있어야 함
        /// </summary>
        /// <param name="allOrder"></param>
        public new void Run(Models.Order? allOrder)
        {
            DateTime dateTime = DateTime.Now;
            Models.Order? order;

            if (this.User == null) return;

            try
            {
                if (this.User.Api == null) return;
                if (this.Market == null) return;
                if (this.Invest <= 0) return;
                if (this.StartDate > dateTime) return;

                //if (allOrder != null && allOrder.OrderList != null)
                //    $"OCNT:{allOrder.OrderList.Where(x => x.Market == this.Market).Count()} - {nameof(SettingGridTrading)}".WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);

                if (this.EndDate < dateTime)
                {
                    this.BidCancel = true;
                    this.AskCancel = true;
                    this.User.RemoveSetting(this);
                    return;
                }

                if (this.ExecuteDate != null && ((DateTime)this.ExecuteDate).AddMinutes(this.Interval) > dateTime) return;

                if (this.OrderSide == OrderSide.bid)
                {
                    if (this.Invest < this.User.ExchangeID switch
                    {
                        1 => 5000,
                        2 => 500,
                        _ => 5000,
                    }) return;

                    if (this.OrderType == OrderType.limit)
                    {
                        if (this.BasePrice <= 0) return;

                        decimal bidQty = (this.Invest / this.BasePrice) - (this.Invest / this.BasePrice * (this.Fees / 100M));

                        bidQty = Math.Ceiling(bidQty * Point(this.User.ExchangeID)) / Point(this.User.ExchangeID);

                        order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.bid, bidQty, this.BasePrice);
                    }
                    else if (this.OrderType == OrderType.price)
                    {
                        order = this.MakeOrderBidPrice(this.Market, this.Invest);
                    }
                    else
                        return;
                }
                else
                {
                    if (this.OrderType == OrderType.limit)
                    {
                        if (this.BasePrice <= 0) return;

                        order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, this.Invest, this.BasePrice);
                    }
                    else if (this.OrderType == OrderType.market)
                    {
                        order = this.MakeOrderAskMarket(this.Market, this.Invest);
                    }
                    else
                        return;
                }

                if (order != null && order.Error == null)//에러가 아니면
                {
                    this.ExecuteDate = this.ExecuteDate == null ? dateTime : ((DateTime)this.ExecuteDate).AddMinutes(this.Interval);
                    this.Update(this.User, this.SettingID, order, this.ExecuteDate);
                }
                else if (order != null && order.Error != null)
                {
                    this.Message = order.Error.Message;
                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                }
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
            finally
            {
                this.UpdateMessage(this.User, this.SettingID, this.Message ??"");
            }
        }

        private void Update(User user, int SETTING_ID, Models.Order order, DateTime? executeDate)
        {
            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "Batch.[dbo].[USP_WORK_DATA_SCHEDULE_TRADING_UPD]";
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter("EXECUTE_DATE", Database.DbType.DateTime, 0, executeDate);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, user.UserID);

            stringBuilder.Append($"{user.ExchangeName()} {this.SettingTypeString} {(this.OrderType != OrderType.limit ? "시장가 " : "")}{(this.OrderSide == OrderSide.bid ? "매수" : "매도")} 주문 알림");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.AppendLine($"{order.Market}");

            string[]? tmps = order.Market?.Split('-');

            decimal volume;
            decimal krw;
            decimal priceAvg;

            if (order.Volume == 0 && order.Trades != null && order.Trades.Count > 0)
            {
                volume = order.Trades.Sum(x => x.Volume);
                krw = order.Trades.Sum(x => x.Volume * x.Price);
                priceAvg = krw / volume;
                priceAvg = Math.Ceiling(priceAvg * Point(user.ExchangeID)) / Point(user.ExchangeID);
            }
            else
            {
                volume = order.Volume;
                krw = order.Volume * order.Price;
                priceAvg = order.Price;
            }

            stringBuilder.Append($"{volume:N4} {tmps?[1]}");

            if (priceAvg >= 100)
                stringBuilder.Append($" | {priceAvg:N0} {tmps?[0]}");
            else if (priceAvg >= 1)
                stringBuilder.Append($" | {priceAvg:N2} {tmps?[0]}");
            else
                stringBuilder.Append($" | {priceAvg:N4} {tmps?[0]}");

            stringBuilder.Append($" | {krw + krw * (order.Side == "bid" ? this.Fees / 100M : -this.Fees / 100M):N0}원");

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            data["1"].AddParameter("QTY", Database.DbType.Decimal, 25, krw);

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