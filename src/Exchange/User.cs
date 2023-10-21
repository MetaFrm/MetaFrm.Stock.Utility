using MetaFrm.Control;
using MetaFrm.Stock.Console;

namespace MetaFrm.Stock
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
        /// Settings
        /// </summary>
        public List<Setting> Settings { get; set; } = new();
        private readonly Queue<Setting> AddSettingQueue = new();
        private readonly Queue<Setting> RemoveSettingQueue = new();

        /// <summary>
        /// AddSetting
        /// </summary>
        /// <param name="setting"></param>
        public void AddSetting(Setting setting)
        {
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

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            Models.Order? AllOrder = null;

            if (this.Api != null)
                ((IAction)this.Api).Action += Exchange_Action;

            Task.Run(async () =>
            {
                while (true)
                {
                    Models.Order order;

                    await Task.Delay(2000);

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

                                this.Settings.Remove(setting);
                                $"Removed setting".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market);
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

                                this.Settings.Add(setting);
                                $"Added setting".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market);
                            }

                    if (this.Api != null)
                    {
                        order = this.Api.AllOrder("ALL", "");

                        if (AllOrder != null && AllOrder.OrderList != null && order != null && order.OrderList != null)
                        {
                            var aa = AllOrder.OrderList.Except(order.OrderList);
                            var bb = order.OrderList.Except(AllOrder.OrderList);

                            AllOrder = order;

                            this.Run(aa.Union(bb));
                        }
                        else
                            this.Run(null);

                        AllOrder ??= this.Api.AllOrder("ALL", "");
                    }
                }
            });
        }
        private void Run(IEnumerable<Models.Order>? orders)
        {
            if (this.Api != null)
                Task.WhenAll(this.Settings.Select(setting => Task.Run(() =>
                {
                    if (setting is SettingGridTrading settingGridTrading && settingGridTrading.Market != null
                    && (orders == null || orders.Any(x => x.Market == settingGridTrading.Market)))
                    {
                        this.GridTrading(settingGridTrading);
                        //Stock.Models.Order order = this.Api.AllOrder(settingGridTrading.Market, "");
                        //Console.WriteLine($"{DateTime.Now:MM-dd HH:mm:ss} ExchangeID:{this.Api.ExchangeID}, UserID:{this.UserID}, SettingID:{setting.SettingID} Market:{settingGridTrading.Market} OCNT:{order.OrderList?.Count} - {settingGridTrading.GetType()}");
                    }
                })));
        }

        private void Exchange_Action(ICore sender, MetaFrmEventArgs e)
        {
            if (e.Action == "OrderExecution" && e.Value != null && e.Value is Models.Order order)
            {
                $"OrderExecution {order.Side} {order.Price} {order.UUID}".WriteMessage(this.ExchangeID, this.UserID, null, order.Market);
            }
        }

        private void GridTrading(SettingGridTrading settingGridTrading)
        {
            if (this.Api == null) return;
            if (settingGridTrading.Market == null) return;

            Models.Order order = this.Api.AllOrder(settingGridTrading.Market, "");
            $"OCNT:{order.OrderList?.Count} - {nameof(SettingGridTrading)}".WriteMessage(this.ExchangeID, this.UserID, settingGridTrading.SettingID, settingGridTrading.Market);
        }
    }
}