using MetaFrm.Control;
using MetaFrm.Service;
using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// User
    /// </summary>
    public class User : ICore
    {
        /// <summary>
        /// IsFirstUser
        /// </summary>
        public bool IsFirstUser { get; set; }

        /// <summary>
        /// AuthState
        /// </summary>
        internal Task<AuthenticationState> AuthState { get; }

        /// <summary>
        /// ExchangeID
        /// </summary>
        public int ExchangeID { get; set; }

        /// <summary>
        /// UserID
        /// </summary>
        public int UserID { get; set; }
        /// <summary>
        /// Api
        /// </summary>
        public IApi? Api { get; set; }
        /// <summary>
        /// IsStopped
        /// </summary>
        public bool IsStopped { get; set; } = false;
        /// <summary>
        /// SaveWorkDataList
        /// </summary>
        public bool SaveWorkDataList { get; set; } = false;

        /// <summary>
        /// Settings
        /// </summary>
        public List<Setting> Settings { get; set; } = new();
        private readonly Queue<Setting> AddSettingQueue = new();
        private readonly Queue<Setting> RemoveSettingQueue = new();

        /// <summary>
        /// Orders
        /// </summary>
        public Models.Order? Orders { get; set; }
        /// <summary>
        /// Account
        /// </summary>
        public Models.Account? Accounts { get; set; }

        /// <summary>
        /// User
        /// </summary>
        /// <param name="authState"></param>
        public User(Task<AuthenticationState> authState)
        { 
            this.AuthState = authState;

            this.SetAction();
        }
        private async void SetAction()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000);

                    if (this.Api != null && !this.Api.AccessKey.IsNullOrEmpty() && !this.Api.SecretKey.IsNullOrEmpty())
                        break;
                }

                if (this.Api != null)
                {
                    ((IAction)this.Api).Action -= Exchange_Action;
                    ((IAction)this.Api).Action += Exchange_Action;
                }
            });
        }

        /// <summary>
        /// AddSetting
        /// </summary>
        /// <param name="setting"></param>
        public void AddSetting(Setting setting)
        {
            if (setting.ListMin < 4)
            {
                $"ListMin 값이 4 보다 작음".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                return;
            }

            if (setting is SettingGridTrading settingGrid)
            {
                decimal tmp = settingGrid.BasePrice * (1 + (((settingGrid.Rate + Setting.DefaultFees(this.ExchangeID)) / 100)) * setting.ListMin);
                if (tmp > settingGrid.TopPrice)
                {
                    $"TopPrice 값이 너무 낮음 BasePrice:{settingGrid.BasePrice}\tRate{settingGrid.Rate}\tFees:{Setting.DefaultFees(this.ExchangeID)}\tListMin{setting.ListMin}\tBasePrice*(((1+Rate+Fees)/100)*ListMin):{tmp}\tTopPrice:{settingGrid.TopPrice}".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }
            }

            if (setting is SettingGridMartingaleShortTrading settingGridMartingaleShortTrading)
            {
                if (settingGridMartingaleShortTrading.SettingGridTrading.StopLoss)
                {
                    $"Grid.StopLoss 는 False만 됩니다.".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }
                if (settingGridMartingaleShortTrading.SettingGridTrading.Rate < 0.8M)
                {
                    $"Grid.Rate 0.8 이상만 됩니다.".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }
                if (settingGridMartingaleShortTrading.SettingGridTrading.SmartType != SmartType.TrailingMoveTop)
                {
                    $"Grid.SmartType 는 TrailingMoveTop만 됩니다.".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }

                if (settingGridMartingaleShortTrading.SettingMartingaleShortTrading.IsProfitStop)
                {
                    $"MartingaleShort.IsProfitStop 는 False만 됩니다.".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }
                if (settingGridMartingaleShortTrading.SettingMartingaleShortTrading.Rate < 0.8M)
                {
                    $"MartingaleShort.Rate 는 0.8 이상만 됩니다.".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }
                if (settingGridMartingaleShortTrading.SettingMartingaleShortTrading.GapRate < 1.6M)
                {
                    $"MartingaleShort.GapRate 는 1.6 이상만 됩니다.".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    return;
                }
            }

            lock (this.Settings)
                if (this.IsFirstUser && this.IsStopped && this.Settings.Count == 0)
                    this.Start();

            if (!Settings.Any(x => x.SettingID == setting.SettingID))
                this.AddSettingQueue.Enqueue(setting);
        }
        /// <summary>
        /// RemoveSetting
        /// </summary>
        /// <param name="settingID"></param>
        public void RemoveSetting(int settingID)
        {
            var sel = Settings.SingleOrDefault(x => x.SettingID == settingID);

            if (sel != null)
                this.RemoveSetting(sel);
        }
        /// <summary>
        /// RemoveSetting
        /// </summary>
        /// <param name="setting"></param>
        public void RemoveSetting(Setting setting)
        {
            if (this.Settings.Contains(setting))
                this.RemoveSettingQueue.Enqueue(setting);
        }

        private int StartRunDelay = 1000;
        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            Task.Run(async () =>
            {
                int cnt = 0;
                int mod = 0;
                while (true)
                {
                    try
                    {
                        List<Models.Order> addOrderSettings = new();

                        //중지 요청이면 기존 세팅을 모두 제거 큐에 입력
                        if (this.IsStopped)
                            foreach (var item in this.Settings)
                                this.RemoveSettingQueue.Enqueue(item);

                        if (this.RemoveSettingQueue.Count > 0)
                        {
                            lock (this.Settings)
                                while (this.RemoveSettingQueue.Count > 0)
                                {
                                    var setting = this.RemoveSettingQueue.Dequeue();

                                    //세팅 제거 사전 작업
                                    switch (setting.StopType)
                                    {
                                        case "":
                                            break;
                                    }

                                    if (this.Settings.Contains(setting))
                                    {
                                        if (this.SaveWorkDataList)
                                        {
                                            if (setting is SettingGridMartingaleShortTrading set)
                                            {
                                                setting.SaveLossStack();
                                                (set.SettingCurrent as ISettingAction)?.Organized(setting.SettingID, false, false, false, true);
                                            }
                                            else
                                                (setting as ISettingAction).Organized(setting.SettingID, false, false, false, true);
                                        }
                                        else
                                        {
                                            if (setting is SettingMartingaleShortTrading)
                                                (setting as ISettingAction).Organized(setting.SettingID, false, true, false, true);
                                            else
                                                (setting as ISettingAction).Organized(setting.SettingID, true, false, false, true);
                                        }

                                        this.Settings.Remove(setting);
                                        $"Removed setting".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Yellow);
                                    }
                                }

                            //중지 요청이고 세팅이 모두 제거 되었다면 while 빠져나가기
                            if (this.IsStopped && this.Settings.Count == 0)
                                break;
                        }

                        if (this.AddSettingQueue.Count > 0)
                            lock (this.Settings)
                                while (this.AddSettingQueue.Count > 0)
                                {
                                    var setting = this.AddSettingQueue.Dequeue();

                                    //세팅 추가 사전 작업
                                    switch (setting.StopType)
                                    {
                                        case "":
                                            break;
                                    }

                                    setting.User = this;
                                    this.Settings.Add(setting);
                                    $"Added setting".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Yellow);

                                    if (setting.Market != null)
                                        addOrderSettings.Add(new() { Market = setting.Market });
                                }

                        if (this.Api != null && this.Settings.Any())
                        {
                            Models.Order order;

                            order = this.Api.AllOrder("ALL", "desc");

                            if (addOrderSettings.Count > 0)
                            {
                                $"addOrderSettings:{addOrderSettings.Count} ".WriteMessage(this.ExchangeID, this.UserID);
                                foreach (var item in addOrderSettings)
                                    $"{item.Market}".WriteMessage(this.ExchangeID, this.UserID);
                                ;
                                this.Run(addOrderSettings, order);
                                cnt = 0;
                            }
                            else if (cnt >= 60000)
                            {
                                $"cnt >= 60:{cnt} ".WriteMessage(this.ExchangeID, this.UserID);
                                //foreach (var item in c1)
                                //    $"{item.Currency} Balance:{item.Balance}\tLocked:{item.Locked}".WriteMessage(this.ExchangeID, this.UserID);

                                Models.Markets markets = this.Api.Markets();
                                List<Models.Order> orders = new();

                                if (markets != null && markets.MarketList != null)
                                {
                                    foreach (var item in markets.MarketList)
                                        orders.Add(new() { Market = item.Market });

                                    this.Run(orders, order);
                                }

                                if (cnt >= 60000)
                                    cnt = 0;
                            }
                            else
                            {
                                if (mod == 0)
                                {
                                    Models.Account account;

                                    account = this.Api.Account();

                                    this.Accounts ??= account;

                                    var a1 = this.Accounts.AccountList.Except(account.AccountList);
                                    var b1 = account.AccountList.Except(this.Accounts.AccountList);
                                    var c1 = a1.Union(b1);

                                    if (c1.Any())
                                    {
                                        $"Accounts Except:{c1.Count()} ".WriteMessage(this.ExchangeID, this.UserID);
                                        foreach (var item in c1)
                                            $"{item.Currency} Balance:{item.Balance}\tLocked:{item.Locked}".WriteMessage(this.ExchangeID, this.UserID);

                                        Models.Markets markets = this.Api.Markets();
                                        List<Models.Order> orders = new();

                                        if (markets != null && markets.MarketList != null)
                                        {
                                            foreach (var item in markets.MarketList)
                                                orders.Add(new() { Market = item.Market });

                                            this.Run(orders, order);
                                            cnt = 0;
                                        }
                                    }

                                    this.Accounts = account;
                                }
                                else
                                {
                                    if (this.Orders != null && this.Orders.OrderList != null && order != null && order.OrderList != null)
                                    {
                                        var aa = this.Orders.OrderList.Except(order.OrderList);
                                        var bb = order.OrderList.Except(this.Orders.OrderList);

                                        this.Run(aa.Union(bb), order);
                                        //cnt = 0;
                                    }
                                }
                            }

                            this.Orders = order;
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.WriteMessage(true, this.ExchangeID, this.UserID);
                    }
                    finally
                    {
                        await Task.Delay(this.StartRunDelay);

                        cnt += this.StartRunDelay;
                        mod = (mod == 0 ? 1 : 0);
                    }
                }
            });
        }
        private bool IsRun = false;
        private void Run(IEnumerable<Models.Order>? orders, Models.Order? allOrder)
        {
            if (this.Api != null)
                try
                {
                    if (this.IsRun) return;
                    this.IsRun = true;

                    lock (this.Settings)
                        foreach (var item in this.Settings)
                        {
                            if (item.Market != null && item is ISettingAction action && (orders == null || orders.Any(x => x.Market == item.Market)))
                            {
                                action.Run(allOrder);
                            }
                        }
                }
                catch (Exception ex)
                {
                    ex.WriteMessage(true, this.ExchangeID, this.UserID);
                }
                finally
                {
                    this.IsRun = false;
                }
        }
        //private void Run(IEnumerable<Models.Order>? orders, Models.Order? allOrder)
        //{
        //    if (this.Api != null)
        //        lock (this.Settings)
        //            Task.WhenAll(this.Settings.Select(setting => Task.Run(() =>
        //            {
        //                if (setting.Market != null && setting is ISettingAction action && (orders == null || orders.Any(x => x.Market == setting.Market)))
        //                {
        //                    action.Run(allOrder);
        //                    //Stock.Models.Order order = this.Api.AllOrder(settingGridTrading.Market, "");
        //                    //Console.WriteLine($"{DateTime.Now:MM-dd HH:mm:ss} ExchangeID:{this.Api.ExchangeID}, UserID:{this.UserID}, SettingID:{setting.SettingID} Market:{settingGridTrading.Market} OCNT:{order.OrderList?.Count} - {settingGridTrading.GetType()}");
        //                }
        //            })));
        //}

        private void Exchange_Action(ICore sender, MetaFrmEventArgs e)
        {
            if (e.Action == "OrderExecution" && e.Value != null && e.Value is Models.Order order)
            {
                $"OrderExecution {order.Side} {order.Price} {order.Volume} {order.UUID}".WriteMessage(this.ExchangeID, this.UserID, null, order.Market, ConsoleColor.Cyan);
                //this.Run(new List<Models.Order>() { { new() { UUID = order.UUID, Side = order.Side, OrdType = order.OrdType, Price = order.Price, State = order.State, Market = order.Market, Volume = order.Volume } } });

                lock (this.Settings)
                    if (this.Api != null && this.Settings.Any(x => x.Market == order.Market) && !this.RemoveSettingQueue.Any(x => x.Market == order.Market))
                    {
                        try
                        {
                            var aa = this.Settings.Where(x => x.WorkDataList != null).SingleOrDefault(z => z.WorkDataList != null && z.WorkDataList.SingleOrDefault(y => (y.BidOrder != null && y.BidOrder.UUID == order.UUID) || (y.AskOrder != null && y.AskOrder.UUID == order.UUID)) != null);

                            if (aa != null)
                            {
                                if (aa is SettingGridTrading settingGrid && order.Side == "bid")
                                    this.OrderExecution(null, order);

                                if (aa is SettingMartingaleLongTrading settingMartingaleLong && order.Side == "bid")
                                    this.OrderExecution(null, order);

                                if (aa is SettingMartingaleShortTrading settingMartingaleShort && order.Side == "ask")
                                    this.OrderExecution(null, order);

                                if (aa is SettingGridMartingaleShortTrading set1 && set1.SettingCurrent is SettingGridTrading && order.Side == "bid")
                                    this.OrderExecution(null, order);

                                if (aa is SettingGridMartingaleShortTrading set2 && set2.SettingCurrent is SettingMartingaleShortTrading && order.Side == "ask")
                                    this.OrderExecution(null, order);
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.WriteMessage(true, this.ExchangeID, this.UserID);
                        }
                        //this.OrderExecution(null, order);

                        this.Orders = this.Api.AllOrder("ALL", "");
                        this.Run(new List<Models.Order>() { { new() { Market = order.Market } } }, this.Orders);
                    }
            }

            this.StartRunDelay = 10000;
        }

        internal void OrderExecution(int? SETTING_ID, Models.Order order)
        {
            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = this.AuthState.Token(),
            };
            data["1"].CommandText = "Batch.[dbo].[USP_TRADING_ORDER_EXECUTION]";
            data["1"].AddParameter(nameof(SETTING_ID), Database.DbType.Int, 3, SETTING_ID);
            data["1"].AddParameter("MARKET_ID", Database.DbType.NVarChar, 20, order.Market);
            data["1"].AddParameter("SIDE", Database.DbType.NVarChar, 20, order.Side);
            data["1"].AddParameter("ORDER_TYPE", Database.DbType.NVarChar, 20, order.OrdType);
            data["1"].AddParameter("PRICE", Database.DbType.Decimal, 18, order.Price);
            data["1"].AddParameter("EXECUTE_QTY", Database.DbType.Decimal, 18, order.ExecutedVolume);
            data["1"].AddParameter(nameof(order.UUID), Database.DbType.NVarChar, 100, order.UUID);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, this.AuthState.UserID());

            stringBuilder.Append($"{this.ExchangeName()} {(order.Side == "bid" ? "매수" : "매도")} 체결");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine($"{order.Market}");

            //if (order.ExecutedVolume >= 100)
            //    stringBuilder.Append($"거래수량: {order.ExecutedVolume:N0}");
            //else if (order.ExecutedVolume >= 10)
            //    stringBuilder.Append($"거래수량: {order.ExecutedVolume:N2}");
            //else
            stringBuilder.Append($"거래수량: {order.ExecutedVolume:N4}");

            if (order.Price >= 100)
                stringBuilder.AppendLine($" / 거래단가: {order.Price:N0}");
            else if (order.Price >= 1)
                stringBuilder.AppendLine($" / 거래단가: {order.Price:N2}");
            else
                stringBuilder.AppendLine($" / 거래단가: {order.Price:N4}");

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.ExchangeID, this.UserID, SETTING_ID, order.Market);
            });
        }

        /// <summary>
        /// ExchangeName
        /// </summary>
        /// <returns></returns>
        public string ExchangeName()
        {
            return this.ExchangeID switch
            {
                1 => "Upbit",
                2 => "Bithumb",
                3 => "Binance",
                _ => ""
            };
        }
    }
}