using MetaFrm.Service;
using MetaFrm.Stock.Console;
using System.Text;
using System.Text.Json;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// Setting
    /// </summary>
    public class Setting : ISettingAction
    {
        /// <summary>
        /// SettingType
        /// </summary>
        public SettingType SettingType { get; set; } = SettingType.None;

        /// <summary>
        /// User
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// SettingID
        /// </summary>
        public int SettingID { get; set; }

        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// StopType
        /// </summary>
        public string? StopType { get; set; }

        /// <summary>
        /// Fees
        /// </summary>
        public decimal Fees { get; set; }


        private string? message;
        /// <summary>
        /// Message
        /// </summary>
        public string? Message 
        {
            get
            { 
                return message;
            }
            set
            {
                message = $"{DateTime.Now:dd HH:mm:ss} {value}";
            }
        }

        /// <summary>
        /// Invest
        /// </summary>
        public decimal Invest { get; set; }

        /// <summary>
        /// BasePrice
        /// </summary>
        public decimal BasePrice { get; set; }

        /// <summary>
        /// TopPrice
        /// </summary>
        public decimal TopPrice { get; set; }

        /// <summary>
        /// Rate
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// ListMin
        /// </summary>
        public int ListMin { get; set; } = 4;

        /// <summary>
        /// TopStop
        /// 종료호가 터치 중지
        /// </summary>
        public bool TopStop { get; set; }

        /// <summary>
        /// IsProfitStop
        /// 익절 중지
        /// </summary>
        public bool IsProfitStop { get; set; }


        internal List<WorkData>? WorkDataList { get; set; }

        /// <summary>
        /// GetWorkDataList
        /// </summary>
        public List<WorkData>? GetWorkDataList => this.WorkDataList;

        /// <summary>
        /// CurrentInfo
        /// </summary>
        public Models.Ticker? CurrentInfo { get; set; }

        /// <summary>
        /// LossStack
        /// </summary>
        public Stack<Loss> LossStack { get; set; } = new();

        /// <summary>
        /// ParentSetting
        /// </summary>
        public Setting? ParentSetting { get; set; }

        /// <summary>
        /// Setting
        /// </summary>
        /// <param name="user"></param>
        public Setting(User user)
        { 
            this.User = user;
        }

        /// <summary>
        /// Organized
        /// </summary>
        /// <param name="SETTING_ID"></param>
        /// <param name="BID_CANCEL"></param>
        /// <param name="ASK_CANCEL"></param>
        /// <param name="ASK_CURRENT_PRICE"></param>
        /// <param name="REMOVE_SETTING"></param>
        public void Organized(int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool REMOVE_SETTING)
        {
            this.CurrentInfo ??= this.GetCurrentInfo();

            if (!BID_CANCEL && !ASK_CANCEL && !ASK_CURRENT_PRICE)
                this.SaveWorkDataList();

            this.OrganizedRun(BID_CANCEL, ASK_CANCEL, ASK_CURRENT_PRICE, this.CurrentInfo);

            if (REMOVE_SETTING)
                this.Clear(SETTING_ID, BID_CANCEL, ASK_CANCEL, ASK_CURRENT_PRICE, REMOVE_SETTING);

            if (this.ParentSetting != null)
            {
                if (this.User.Settings.Contains(this.ParentSetting) && REMOVE_SETTING)
                    this.User.RemoveSetting(this.ParentSetting);
            }
            else
            {
                if (this.User.Settings.Contains(this) && REMOVE_SETTING)
                    this.User.RemoveSetting(this);
            }
        }
        private void OrganizedRun(bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, Models.Ticker? ticker)
        {
            Models.Order order;
            decimal ASK;
            decimal RemainingVolume;

            if (this.WorkDataList == null || this.WorkDataList.Count < 1) return;
            if (this.Market == "") return;
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;
            if (ticker == null) return;

            ASK = 0;

            foreach (WorkData dataRow in this.WorkDataList)
            {
                if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && !dataRow.BidOrder.UUID.Contains("임시") && dataRow.BidOrder.State == "wait" && BID_CANCEL)
                {
                    try
                    {
                        order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), dataRow.BidOrder.UUID);

                        if (order.Error != null)
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.WriteMessage(true, this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }

                if (dataRow.AskOrder != null && dataRow.AskOrder.UUID != null && dataRow.AskOrder.State == "wait" && ASK_CANCEL)
                {
                    try
                    {
                        order = this.User.Api.Order(this.Market, Models.OrderSide.ask.ToString(), dataRow.AskOrder.UUID);

                        if (order.Error != null)
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                        else
                        {
                            RemainingVolume = order.RemainingVolume;

                            order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.ask.ToString(), dataRow.AskOrder.UUID);

                            if (order.Error != null)
                            {
                                this.Message = order.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            }
                            else
                                ASK += RemainingVolume;
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.WriteMessage(true, this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                    }
                }
            }

            if (ASK > 0 && ASK_CURRENT_PRICE)
            {
                order = this.User.Api.MakeOrder(this.Market, Models.OrderSide.ask, ASK, ticker.TradePrice, Models.OrderType.market);

                if (order.Error != null)
                {
                    this.Message = order.Error.Message;
                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                }
            }
        }
        internal Models.Ticker? GetCurrentInfo()
        {
            if (this.User == null) return null;
            if (this.User.Api == null) return null;
            if (this.Market == null) return null;

            var ticker = this.User.Api.Ticker(this.Market);
            if (ticker == null || ticker.TickerList == null || ticker.TickerList.Count < 1) return null;

            return ticker.TickerList[0];
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="allOrder"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Run(Models.Order? allOrder)
        {
            throw new NotImplementedException();
        }

        internal static decimal Point(int exchangeID)
        {
            return exchangeID switch { 1 => 100000000M, 2 => 10000M, _ => 10000M };
        }
        internal static decimal DefaultFees(int exchangeID)
        {
            return exchangeID switch
            {
                1 => 0.05M,
                2 => 0.04M,
                _ => 0.05M
            };
        }

        /// <summary>
        /// GetBasePrice
        /// </summary>
        /// <param name="tradePrice"></param>
        /// <param name="rate"></param>
        /// <param name="listMin"></param>
        /// <returns></returns>
        internal decimal GetBasePrice(decimal tradePrice, decimal rate, decimal listMin)
        {
            decimal basePrice = tradePrice * (1 - ((rate + Setting.DefaultFees(this.User.ExchangeID)) / 99.8M) * listMin);
            return Math.Round(basePrice);
        }
        internal decimal GetTopPrice(decimal tradePrice, decimal rate, decimal listMin)
        {
            decimal basePrice = tradePrice * (1 + ((rate + Setting.DefaultFees(this.User.ExchangeID)) / 99.8M) * listMin);
            return Math.Round(basePrice);
        }


        private string LastMessage = "";
        internal void UpdateMessage(int SETTING_ID, string MESSAGE)
        {
            if (this.LastMessage.Equals(MESSAGE)) return;

            this.LastMessage = MESSAGE;

            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = this.User.AuthState.Token(),
            };
            data["1"].CommandText = "Batch.[dbo].[USP_TRADING_MESSAGE_UPD]";
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(MESSAGE), Database.DbType.NVarChar, 4000, MESSAGE);

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            });
        }
        internal void Profit(int SETTING_ID, int USER_ID, decimal BID_PRICE, decimal BID_QTY, decimal BID_FEE, decimal ASK_PRICE, decimal ASK_QTY, decimal ASK_FEE, decimal PROFIT, string MARKET_ID)
        {
            if (this.LossStack.Count > 0)
                this.LossStack.Peek().AccProfit += PROFIT;

            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = this.User.AuthState.Token(),
            };
            data["1"].CommandText = "Batch.[dbo].[USP_TRADING_PROFIT_UPD]";
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(BID_PRICE), Database.DbType.Decimal, 18, BID_PRICE);
            data["1"].AddParameter(nameof(BID_QTY), Database.DbType.Decimal, 18, BID_QTY);
            data["1"].AddParameter(nameof(BID_FEE), Database.DbType.Decimal, 18, BID_FEE);
            data["1"].AddParameter(nameof(ASK_PRICE), Database.DbType.Decimal, 18, ASK_PRICE);
            data["1"].AddParameter(nameof(ASK_QTY), Database.DbType.Decimal, 18, ASK_QTY);
            data["1"].AddParameter(nameof(ASK_FEE), Database.DbType.Decimal, 18, ASK_FEE);
            data["1"].AddParameter(nameof(PROFIT), Database.DbType.Decimal, 18, PROFIT);
            data["1"].AddParameter(nameof(USER_ID), Database.DbType.Int, 3, USER_ID);

            stringBuilder.Append($"{this.User.ExchangeName()} 수익 발생");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.Append($"{MARKET_ID}");
            stringBuilder.AppendLine(PROFIT >= 1 ? $" {PROFIT:N0}원 수익" : $" {PROFIT:N2}원 수익");

            string[]? tmps = MARKET_ID?.Split('-');

            //stringBuilder.Append("S ");
            stringBuilder.Append($"{ASK_QTY:N4} {tmps?[1]}");

            if (ASK_PRICE >= 100)
                stringBuilder.Append($" | {ASK_PRICE:N0} {tmps?[0]}");
            else if (ASK_PRICE >= 1)    
                stringBuilder.Append($" | {ASK_PRICE:N2} {tmps?[0]}");
            else                        
                stringBuilder.Append($" | {ASK_PRICE:N4} {tmps?[0]}");
                                        
            stringBuilder.AppendLine($" | {(ASK_PRICE * ASK_QTY) - ASK_FEE:N0}원");


            //stringBuilder.Append("B ");
            stringBuilder.Append($"{BID_QTY:N4} {tmps?[1]}");

            if (BID_PRICE >= 100)
                stringBuilder.Append($" | {BID_PRICE:N0} {tmps?[0]}");
            else if (BID_PRICE >= 1)    
                stringBuilder.Append($" | {BID_PRICE:N2} {tmps?[0]}");
            else                        
                stringBuilder.Append($" | {BID_PRICE:N4} {tmps?[0]}");

            stringBuilder.Append($" | {(BID_PRICE * BID_QTY) + BID_FEE:N0}원");


            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            });
        }
        internal void ChangeSettingMessage(Setting before, Setting after)
        {
            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = this.User.AuthState.Token(),
            };
            data["1"].CommandText = "Batch.[dbo].[USP_TRADING_CHANGE_SETTING]";
            data["1"].AddParameter("SETTING_ID", Database.DbType.Int, 3, before.SettingID);
            data["1"].AddParameter("BEFORE", Database.DbType.NVarChar, 50, before.SettingType == SettingType.Grid ? "Grid" : "MartingaleShort");
            data["1"].AddParameter("AFTER", Database.DbType.NVarChar, 50, after.SettingType == SettingType.Grid ? "Grid" : "MartingaleShort");
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, before.User.UserID);

            stringBuilder.Append($"{this.User.ExchangeName()} 세팅 전환");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.Append($"{before.Market}");
            stringBuilder.AppendLine($" {(before.SettingType == SettingType.Grid ? "그리드" : "마틴게일 숏")} -> {(after.SettingType == SettingType.Grid ? "그리드" : "마틴게일 숏")}");

            if (this.LossStack.Count > 0)
            {
                int i = this.LossStack.Count - 1;
                foreach(var item in this.LossStack)
                {
                    if (i % 2 == 0)
                        stringBuilder.Append($"마틴게일 숏({item.DateTime:MM-dd HH:mm}) ");
                    else
                        stringBuilder.Append($"그리드({item.DateTime:MM-dd HH:mm}) ");
                }
            }

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            });
        }
        internal void Clear(int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool IS_PROFIT_STOP)
        {
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = this.User.AuthState.Token(),
            };
            data["1"].CommandText = "Batch.[dbo].[USP_TRADING_CLEAR_UPD]";
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(BID_CANCEL), Database.DbType.NVarChar, 1, BID_CANCEL ? "Y" : "N");
            data["1"].AddParameter(nameof(ASK_CANCEL), Database.DbType.NVarChar, 1, ASK_CANCEL ? "Y" : "N");
            data["1"].AddParameter(nameof(ASK_CURRENT_PRICE), Database.DbType.NVarChar, 1, ASK_CURRENT_PRICE ? "Y" : "N");
            data["1"].AddParameter(nameof(IS_PROFIT_STOP), Database.DbType.NVarChar, 1, IS_PROFIT_STOP ? "Y" : "N");

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            });
        }

        internal void SaveWorkDataList()
        {
            try
            {
                if (this.WorkDataList != null)
                {
                    string path = $"S_WDL_{this.SettingID}.txt";
                    using StreamWriter streamWriter = File.CreateText(path);
                    streamWriter.Write(JsonSerializer.Serialize(this.WorkDataList, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString }));
                }
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
        }
        internal List<WorkData>? ReadWorkDataList()
        {
            try
            {
                List<WorkData>? result = null;
                string path = $"S_WDL_{this.SettingID}.txt";

                if (File.Exists(path))
                {
                    using (StreamReader streamReader = File.OpenText(path))
                        result = JsonSerializer.Deserialize<List<WorkData>>(streamReader.ReadToEnd(), new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                    File.Delete(path);
                }

                return result;
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }

            return null;
        }
        internal void SaveLossStack()
        {
            try
            {
                string path = $"S_LS_{this.SettingID}.txt";
                using StreamWriter streamWriter = File.CreateText(path);
                streamWriter.Write(JsonSerializer.Serialize(this.LossStack, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString }));
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }
        }
        internal Stack<Loss>? ReadLossStack()
        {
            try
            {
                Stack<Loss>? result = null;
                string path = $"S_LS_{this.SettingID}.txt";

                if (File.Exists(path))
                {
                    using (StreamReader streamReader = File.OpenText(path))
                        result = JsonSerializer.Deserialize<Stack<Loss>>(streamReader.ReadToEnd(), new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                    File.Delete(path);
                }

                return result;
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }

            return null;
        }


        /// <summary>
        /// 시장가 매수
        /// </summary>
        /// <returns></returns>
        internal Models.Order? MakeOrderBidPrice(string market, decimal bidAmount)
        {
            if (this.User.Api == null) return null;

            var order = this.User.Api.MakeOrder(market, Models.OrderSide.bid, 0, (decimal)bidAmount, Models.OrderType.price);

            //오류가 발생하면 한번 더 시도
            if (order.Error != null)
                order = this.User.Api.MakeOrder(market, Models.OrderSide.bid, 0, (decimal)bidAmount, Models.OrderType.price);

            if (order.Error != null)
            {
                this.Message = order.Error.Message;
                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }

            if (order.UUID != null)
            {
                order = this.User.Api.Order(market, Models.OrderSide.bid.ToString(), order.UUID);

                if (order.Error == null)//에러가 아니면
                    return order;
                else
                {
                    this.Message = order.Error.Message;
                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                }
            }

            return null;
        }
        /// <summary>
        ///  시장가 매도
        /// </summary>
        /// <returns></returns>
        internal Models.Order? MakeOrderAskMarket(string market, decimal qty)
        {
            if (this.User.Api == null) return null;

            var order = this.User.Api.MakeOrder(market, Models.OrderSide.ask, qty, 0, Models.OrderType.market);

            //오류가 발생하면 한번 더 시도
            if (order.Error != null)
                order = this.User.Api.MakeOrder(market, Models.OrderSide.ask, qty, 0, Models.OrderType.market);

            if (order.Error != null)
            {
                this.Message = order.Error.Message;
                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
            }

            if (order.UUID != null)
            {
                order = this.User.Api.Order(market, Models.OrderSide.bid.ToString(), order.UUID);

                if (order.Error == null)//에러가 아니면
                    return order;
                else
                {
                    this.Message = order.Error.Message;
                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                }
            }

            return null;
        }
    }
}