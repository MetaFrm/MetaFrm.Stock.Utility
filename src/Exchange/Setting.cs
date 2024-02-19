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
        /// User
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// SettingID
        /// </summary>
        public int SettingID { get; set; }

        /// <summary>
        /// SettingType
        /// </summary>
        public SettingType SettingType { get; set; } = SettingType.None;

        /// <summary>
        /// SettingTypeString
        /// </summary>
        public string SettingTypeString 
        {
            get
            {
                string tmp1;
                string tmp2 = "";

                tmp1 = GetSettingTypeString(this.SettingType);

                if (this.Current != null)
                    tmp2 = GetSettingTypeString(this.Current.SettingType);

                return $"{tmp1}{(this.Current != null ? $"-{tmp2}" : "")}";
            }
        }
        /// <summary>
        /// GetSettingTypeString
        /// </summary>
        /// <param name="settingType"></param>
        /// <returns></returns>
        public static string GetSettingTypeString(SettingType settingType)
        {
            return settingType switch
            {
                SettingType.Grid => "그리드",
                SettingType.TraillingStop => "트레일링 스탑",
                SettingType.MartingaleLong => "마틴 롱",
                SettingType.MartingaleShort => "마틴 숏",
                SettingType.GridMartingaleLong => "그리드+마틴 롱",
                SettingType.GridMartingaleShort => "그리드+마틴 숏",
                SettingType.Schedule => "스케줄러",
                _ => $"{settingType}"
            };
        }

        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        public string? Market { get; set; }

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
        public decimal Rate { get; set; } = 0.8M;

        /// <summary>
        /// ListMin
        /// </summary>
        public int ListMin { get; set; } = 4;

        /// <summary>
        /// Fees
        /// </summary>
        public decimal Fees { get; set; }

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

        /// <summary>
        /// AccProfit
        /// </summary>
        public decimal AccProfit { get; set; }

        private string? message;
        private DateTime messageDateTime;
        /// <summary>
        /// Message
        /// </summary>
        public string? Message
        {
            get
            {
                return $"{messageDateTime:dd HH:mm:ss} {this.message}";
            }
            set
            {
                this.message = value;
                this.messageDateTime = DateTime.Now;
            }
        }
        /// <summary>
        /// MessageString
        /// </summary>
        public string? MessageString
        {
            get
            {
                return this.message;
            }
            set
            {
                this.message = value;
            }
        }

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
                if (this.current != null && value != null && this.User != null)
                    this.ChangeSettingMessage(this.User, this.current, value);

                this.current = value;
            }
        }

        /// <summary>
        /// BidCancel
        /// </summary>
        public bool BidCancel { get; set; }
        /// <summary>
        /// AskCancel
        /// </summary>
        public bool AskCancel { get; set; }
        /// <summary>
        /// AskCurrentPrice
        /// </summary>
        public bool AskCurrentPrice { get; set; }
        /// <summary>
        /// BidCurrentPrice
        /// </summary>
        public bool BidCurrentPrice { get; set; }


        /// <summary>
        /// Ticker
        /// </summary>
        public Models.Ticker? Ticker { get; set; }
        private int exID;
        /// <summary>
        /// ExchangeID
        /// </summary>
        public int ExchangeID
        {
            get
            {
                return this.User != null ? this.User.ExchangeID : this.exID;
            }
            set
            {
                if (this.User == null)
                    this.exID = value;
            }
        }
        /// <summary>
        /// TradePrice
        /// </summary>
        public decimal? TradePrice => this.Ticker?.TradePrice;


        /// <summary>
        /// Setting
        /// </summary>
        /// <param name="user"></param>
        public Setting(User? user)
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
        /// <param name="BID_CURRENT_PRICE"></param>
        /// <param name="SAVE_WORKDATA">서버 프로그램 중지 할떄</param>
        /// <param name="REMOVE_SETTING">서버에서 세팅을 제거 할때</param>
        /// <param name="IS_PROFIT_STOP">수익/StopLoss/TopStop 발생 해서 중지 할때</param>
        public void Organized(int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool BID_CURRENT_PRICE, bool SAVE_WORKDATA, bool REMOVE_SETTING, bool IS_PROFIT_STOP)
        {
            this.CurrentInfo ??= this.GetCurrentInfo();

            if (this.User == null)
                return;

            if (!BID_CANCEL && !ASK_CANCEL && SAVE_WORKDATA)
                this.SaveWorkDataList(this.User);

            this.OrganizedRun(BID_CANCEL, ASK_CANCEL, ASK_CURRENT_PRICE, BID_CURRENT_PRICE, this.CurrentInfo);

            if (REMOVE_SETTING)
                this.Clear(this.User, SETTING_ID, BID_CANCEL, ASK_CANCEL, ASK_CURRENT_PRICE, BID_CURRENT_PRICE, SAVE_WORKDATA, REMOVE_SETTING, IS_PROFIT_STOP);

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
        private void OrganizedRun(bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool BID_CURRENT_PRICE, Models.Ticker? ticker)
        {
            Models.Order order;
            decimal ASK;
            decimal BID_KRW;
            decimal Price;
            decimal RemainingFee;
            decimal RemainingVolume;

            if (this.WorkDataList == null || this.WorkDataList.Count < 1) return;
            if (this.Market == "") return;
            if (this.User == null) return;
            if (this.User.Api == null) return;
            if (this.Market == null) return;
            if (ticker == null) return;

            ASK = 0;
            BID_KRW = 0;

            foreach (WorkData dataRow in this.WorkDataList)
            {
                if (dataRow.BidOrder != null && dataRow.BidOrder.UUID != null && !dataRow.BidOrder.UUID.Contains("임시") && dataRow.BidOrder.State == "wait" && BID_CANCEL)
                {
                    try
                    {
                        order = this.User.Api.Order(this.Market, Models.OrderSide.ask.ToString(), dataRow.BidOrder.UUID);

                        if (order.Error != null)
                        {
                            this.Message = order.Error.Message;
                            this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                        }
                        else
                        {
                            RemainingVolume = order.RemainingVolume;
                            Price = order.Price;
                            RemainingFee = order.RemainingFee;

                            order = this.User.Api.CancelOrder(this.Market, Models.OrderSide.bid.ToString(), dataRow.BidOrder.UUID);

                            if (order.Error != null)
                            {
                                this.Message = order.Error.Message;
                                this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                            }
                            else
                                BID_KRW += (RemainingVolume * Price) + RemainingFee;
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
                var order1 = this.MakeOrderAskMarket(this.Market, ASK);

                if (order1 != null && order1.Error != null)
                {
                    this.Message = order1.Error.Message;
                    this.Message?.WriteMessage(this.User.ExchangeID, this.User.UserID, this.SettingID, this.Market);
                }
            }

            if (BID_KRW > 0 && BID_CURRENT_PRICE)
            {
                var order1 = this.MakeOrderBidPrice(this.Market, BID_KRW);

                if (order1 != null && order1.Error != null)
                {
                    this.Message = order1.Error.Message;
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
        /// <param name="exchangeID"></param>
        /// <param name="market"></param>
        /// <param name="tradePrice"></param>
        /// <param name="rate"></param>
        /// <param name="listMin"></param>
        /// <returns></returns>
        public static decimal GetBasePrice(int exchangeID, string market, decimal tradePrice, decimal rate, decimal listMin)
        {
            decimal basePrice = tradePrice * (1 - ((rate + Setting.DefaultFees(exchangeID)) / 99.8M) * listMin);

            return basePrice.PriceRound(exchangeID, market);
            //return RoundPrice(basePrice);
        }
        /// <summary>
        /// GetTopPrice
        /// </summary>
        /// <param name="exchangeID"></param>
        /// <param name="market"></param>
        /// <param name="tradePrice"></param>
        /// <param name="rate"></param>
        /// <param name="listMin"></param>
        /// <returns></returns>
        public static decimal GetTopPrice(int exchangeID, string market, decimal tradePrice, decimal rate, decimal listMin)
        {
            decimal basePrice = tradePrice * (1 + ((rate + Setting.DefaultFees(exchangeID)) / 99.8M) * listMin);
            return basePrice.PriceRound(exchangeID, market);
            //return RoundPrice(basePrice);
        }

        ///// <summary>
        ///// RoundPrice
        ///// </summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        ///// <exception cref="Exception"></exception>
        //public static decimal RoundPrice(decimal value)
        //{
        //    if (value <= 0M)
        //        throw new Exception($"RoundPrice({value})");
        //    else if (value < 1M)
        //        return Math.Round(value * 10000M) / 10000M;
        //    else if (value < 100M)
        //        return Math.Round(value * 100M) / 100M;
        //    else
        //        return Math.Round(value);
        //}


        private string LastMessage = "";
        private string LastMessageTmp = "";
        internal void UpdateMessage(User user, int SETTING_ID, string MESSAGE)
        {
            //09 21:24:00 주문가능한 금액(KRW)이 부족합니다.
            //1234567890123

            if (MESSAGE.Length > 12)
            {
                string tmp = MESSAGE;

                //System.Console.WriteLine(tmp);
                tmp = $"{tmp[..7]} {tmp[12..]}";
                //System.Console.WriteLine(tmp);

                if (this.LastMessageTmp.Equals(tmp)) return;

                this.LastMessageTmp = tmp;
            }
            else
                if (this.LastMessage.Equals(MESSAGE)) return;

            this.LastMessage = MESSAGE;

            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Setting.UpdateMessage");
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(MESSAGE), Database.DbType.NVarChar, 4000, MESSAGE);

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(user.ExchangeID, user.UserID, this.SettingID, this.Market);
            });
        }
        internal void Profit(User user, int SETTING_ID, int USER_ID, decimal BID_PRICE, decimal BID_QTY, decimal BID_FEE, decimal ASK_PRICE, decimal ASK_QTY, decimal ASK_FEE, decimal PROFIT, string MARKET_ID)
        {
            if (this.LossStack.Count > 0)
                this.LossStack.Peek().AccProfit += PROFIT;

            this.AccProfit += PROFIT;

            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Setting.Profit");
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(BID_PRICE), Database.DbType.Decimal, 25, BID_PRICE);
            data["1"].AddParameter(nameof(BID_QTY), Database.DbType.Decimal, 25, BID_QTY);
            data["1"].AddParameter(nameof(BID_FEE), Database.DbType.Decimal, 25, BID_FEE);
            data["1"].AddParameter(nameof(ASK_PRICE), Database.DbType.Decimal, 25, ASK_PRICE);
            data["1"].AddParameter(nameof(ASK_QTY), Database.DbType.Decimal, 25, ASK_QTY);
            data["1"].AddParameter(nameof(ASK_FEE), Database.DbType.Decimal, 25, ASK_FEE);
            data["1"].AddParameter(nameof(PROFIT), Database.DbType.Decimal, 25, PROFIT);
            data["1"].AddParameter(nameof(USER_ID), Database.DbType.Int, 3, USER_ID);

            stringBuilder.Append($"{user.ExchangeName()} {this.SettingTypeString} 수익 발생");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.Append($"{MARKET_ID}");
            stringBuilder.AppendLine(PROFIT >= 1 ? $" {PROFIT:N0}원 수익" : $" {PROFIT:N2}원 수익");

            string[]? tmps = MARKET_ID?.Split('-');

            //stringBuilder.Append("S ");
            stringBuilder.Append($"{ASK_QTY:N4} {tmps?[1]}");

            //if (ASK_PRICE >= 100)
            //    stringBuilder.Append($" | {ASK_PRICE:N0} {tmps?[0]}");
            //else if (ASK_PRICE >= 1)
            //    stringBuilder.Append($" | {ASK_PRICE:N2} {tmps?[0]}");
            //else
            //    stringBuilder.Append($" | {ASK_PRICE:N4} {tmps?[0]}");
            stringBuilder.Append($" | {ASK_PRICE.PriceToString(this.ExchangeID, this.Market ?? "")} {tmps?[0]}");

            stringBuilder.AppendLine($" | {(ASK_PRICE * ASK_QTY) - ASK_FEE:N0}원");


            //stringBuilder.Append("B ");
            stringBuilder.Append($"{BID_QTY:N4} {tmps?[1]}");

            //if (BID_PRICE >= 100)
            //    stringBuilder.Append($" | {BID_PRICE:N0} {tmps?[0]}");
            //else if (BID_PRICE >= 1)
            //    stringBuilder.Append($" | {BID_PRICE:N2} {tmps?[0]}");
            //else
            //    stringBuilder.Append($" | {BID_PRICE:N4} {tmps?[0]}");
            stringBuilder.Append($" | {BID_PRICE.PriceToString(this.SettingID, this.Market ?? "")} {tmps?[0]}");

            stringBuilder.Append($" | {(BID_PRICE * BID_QTY) + BID_FEE:N0}원");


            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(user.ExchangeID, user.UserID, this.SettingID, this.Market);
            });
        }
        internal void ChangeSettingMessage(User user, Setting before, Setting after)
        {
            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Setting.ChangeSettingMessage");
            data["1"].AddParameter("SETTING_ID", Database.DbType.Int, 3, before.SettingID);
            data["1"].AddParameter("BEFORE", Database.DbType.NVarChar, 50, before.SettingType.ToString());
            data["1"].AddParameter("AFTER", Database.DbType.NVarChar, 50, after.SettingType.ToString());
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, user.UserID);

            stringBuilder.Append($"{user.ExchangeName()} 세팅 전환");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.Append($"{before.Market}");
            stringBuilder.AppendLine($" {(before.SettingType == SettingType.Grid ? "그리드" : (before.SettingType == SettingType.MartingaleLong ? "마틴게일 롱" : "마틴게일 숏"))} -> {(after.SettingType == SettingType.Grid ? "그리드" : (after.SettingType == SettingType.MartingaleLong ? "마틴게일 롱" : "마틴게일 숏"))}");

            if (this.LossStack.Count > 0)
            {
                int i = this.LossStack.Count - 1;
                foreach(var item in this.LossStack)
                {
                    if (i % 2 == 0)
                        stringBuilder.Append($"마틴게일({item.DateTime:MM-dd HH:mm}) ");
                    else
                        stringBuilder.Append($"그리드({item.DateTime:MM-dd HH:mm}) ");
                    i++;
                }
            }

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(user.ExchangeID, user.UserID, this.SettingID, this.Market);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="SETTING_ID"></param>
        /// <param name="BID_CANCEL"></param>
        /// <param name="ASK_CANCEL"></param>
        /// <param name="ASK_CURRENT_PRICE"></param>
        /// <param name="BID_CURRENT_PRICE"></param>
        /// <param name="SAVE_WORKDATA">서버 프로그램 중지 할떄</param>
        /// <param name="REMOVE_SETTING">서버에서 세팅을 제거 할때</param>
        /// <param name="IS_PROFIT_STOP">수익이 발생 해서 중지 할때</param>
        internal void Clear(User user, int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool BID_CURRENT_PRICE, bool SAVE_WORKDATA, bool REMOVE_SETTING, bool IS_PROFIT_STOP)
        {
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Setting.Clear");
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter(nameof(BID_CANCEL), Database.DbType.NVarChar, 1, BID_CANCEL ? "Y" : "N");
            data["1"].AddParameter(nameof(ASK_CANCEL), Database.DbType.NVarChar, 1, ASK_CANCEL ? "Y" : "N");
            data["1"].AddParameter(nameof(ASK_CURRENT_PRICE), Database.DbType.NVarChar, 1, ASK_CURRENT_PRICE ? "Y" : "N");
            data["1"].AddParameter(nameof(BID_CURRENT_PRICE), Database.DbType.NVarChar, 1, BID_CURRENT_PRICE ? "Y" : "N");
            data["1"].AddParameter(nameof(SAVE_WORKDATA), Database.DbType.NVarChar, 1, SAVE_WORKDATA ? "Y" : "N");
            data["1"].AddParameter(nameof(REMOVE_SETTING), Database.DbType.NVarChar, 1, REMOVE_SETTING ? "Y" : "N");
            data["1"].AddParameter(nameof(IS_PROFIT_STOP), Database.DbType.NVarChar, 1, IS_PROFIT_STOP ? "Y" : "N");
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, user.UserID);

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(user.ExchangeID, user.UserID, this.SettingID, this.Market);

                if (REMOVE_SETTING && IS_PROFIT_STOP)
                    this.SettingInOut(user, this.SettingID, false);
            });
        }
        internal void SettingInOut(User user, int SETTING_ID, bool isIn)
        {
            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = user.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Setting.SettingInOut");
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, user.UserID);

            stringBuilder.Append($"{user.ExchangeName()} {(this.ParentSetting != null ? this.ParentSetting.SettingTypeString : this.SettingTypeString)} 세팅 {(isIn ? "추가" : "제거")}");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.Append($"{this.Market}");

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(user.ExchangeID, user.UserID, this.SettingID, this.Market);
            });
        }

        internal void SaveWorkDataList(User user)
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
                ex.WriteMessage(true, user.ExchangeID, user.UserID, this.SettingID, this.Market);
            }
        }
        internal List<WorkData>? ReadWorkDataList(User user)
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
                ex.WriteMessage(true, user.ExchangeID, user.UserID, this.SettingID, this.Market);
            }

            return null;
        }
        internal void SaveLossStack(User user)
        {
            try
            {
                string path = $"S_LS_{this.SettingID}.txt";
                using StreamWriter streamWriter = File.CreateText(path);
                streamWriter.Write(JsonSerializer.Serialize(this.LossStack, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString }));
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, user.ExchangeID, user.UserID, this.SettingID, this.Market);
            }
        }
        internal Stack<Loss>? ReadLossStack(User user)
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
                ex.WriteMessage(true, user.ExchangeID, user.UserID, this.SettingID, this.Market);
            }

            return null;
        }


        /// <summary>
        /// 시장가 매수
        /// </summary>
        /// <returns></returns>
        internal Models.Order? MakeOrderBidPrice(string market, decimal bidAmount)
        {
            if (this.User == null) return null;
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
            if (this.User == null) return null;
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