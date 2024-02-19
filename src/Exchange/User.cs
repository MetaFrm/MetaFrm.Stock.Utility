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
            if (!SettingValidation(this.ExchangeID, this.UserID, setting, out _))
                return;

            lock (this.Settings)
                if (this.IsFirstUser && this.IsStopped && this.Settings.Count == 0)
                    this.Start();

            if (!Settings.Any(x => x.SettingID == setting.SettingID))
                this.AddSettingQueue.Enqueue(setting);
        }

        /// <summary>
        /// SettingValidation
        /// </summary>
        /// <param name="ExchangeID"></param>
        /// <param name="UserID"></param>
        /// <param name="setting"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool SettingValidation(int ExchangeID, int UserID, Setting setting, out string? message)
        {
            if (setting.Market == null || setting.Market.IsNullOrEmpty() || setting.Market.Length < 4)
            {
                message = "'종목'을 입력하세요.";
                $"종목을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                return false;
            }

            var investMin = ExchangeID switch { 1 => 10000, 2 => 2000, _ => 10000 };

            if (setting.Invest < investMin && setting.SettingType != SettingType.MartingaleShort && setting.SettingType != SettingType.Schedule)
            {
                message = $"'투자금액'({investMin:N}이상)을 입력하세요.";
                $"'투자금액'({investMin:N}이상)을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                return false;
            }

            switch (setting.SettingType)
            {
                case SettingType.Grid:
                    Grid grid = (Grid)setting;

                    if (setting.BasePrice <= 0.0M)
                    {
                        message = "'시작호가'를 입력하세요.";
                        $"'시작호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.TopPrice <= 0.0M)
                    {
                        message = "'종료호가'를 입력하세요.";
                        $"'종료호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.BasePrice >= setting.TopPrice)
                    {
                        message = "'종료호가'는 '시작호가'와 같거나 작을 수 없습니다.";
                        $"'종료호가'는 '시작호가'와 같거나 작을 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.Rate < 0.11M)
                    {
                        message = "'호가변화%'는 최소 0.11%";
                        $"'호가변화%'는 최소 0.11%".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.ListMin < 4 || setting.ListMin > 100)
                    {
                        message = "'최소 리스트 수'는 4이상 입니다.";
                        $"'최소 리스트 수'는 4이상 입니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    decimal tmp = setting.BasePrice * (1 + ((setting.Rate + Setting.DefaultFees(ExchangeID)) / 100) * setting.ListMin);
                    if (tmp > setting.TopPrice)
                    {
                        message = $"'종료호가'는 {tmp} 보다 커야 합니다.";
                        $"'종료호가'는 {tmp} 보다 커야 합니다. BasePrice:{setting.BasePrice}\tRate{setting.Rate}\tFees:{Setting.DefaultFees(ExchangeID)}\tListMin{setting.ListMin}\tBasePrice*(((1+Rate+Fees)/100)*ListMin):{tmp}\tTopPrice:{setting.TopPrice}".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.TopStop && (grid.SmartType == SmartType.TrailingMoveTop || grid.SmartType == SmartType.TrailingMoveTopShorten))
                    {
                        message = "'종료호가 터치 중지' 와 '트레일링'을 동시에 사용할 수 없습니다.";
                        $"'종료호가 터치 중지' 와 '트레일링'을 동시에 사용할 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;

                case SettingType.TraillingStop:
                    TraillingStop settingTraillingStop = (TraillingStop)setting;

                    if (setting.BasePrice <= 0.0M)
                    {
                        message = "'매수호가'를 입력하세요.";
                        $"'매수호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.Rate <= 0.0M)
                    {
                        message = "'목표%'를 입력하세요.";
                        $"'목표%'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    //if (settingTraillingStop.ReturnRate <= 0.0M)
                    //{
                    //    message = "'리턴%'(양수)를 입력하세요.";
                    //    $"'리턴%'(양수)를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    //    return false;
                    //}
                    if (Math.Abs(settingTraillingStop.ReturnRate) > settingTraillingStop.Rate)
                    {
                        message = "'목표%' 값 너무 낮거나 '리턴%' 값니 너무 작습니다.";
                        $"'목표%' 값 너무 낮거나 '리턴%' 값니 너무 작습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingTraillingStop.ListMin > 1 && settingTraillingStop.GapRate < 5M)
                    {
                        message = "'갭%'은 5.0 이상만 됩니다.";
                        $"SettingTraillingStop.GapRate 는 5.0 이상만 됩니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;

                case SettingType.MartingaleLong:
                    MartingaleLong martingaleLong = (MartingaleLong)setting;

                    if (setting.BasePrice <= 0.0M)
                    {
                        message = "'시작호가'를 입력하세요.";
                        $"'시작호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.TopPrice <= 0.0M)
                    {
                        message = "'종료호가'를 입력하세요.";
                        $"'종료호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.BasePrice >= setting.TopPrice)
                    {
                        message = "'종료호가'는 '시작호가'와 같거나 작을 수 없습니다.";
                        $"'종료호가'는 '시작호가'와 같거나 작을 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (martingaleLong.GapRate < 3M)
                    {
                        message = "'갭%'은 3.0 이상만 됩니다.";
                        $"SettingTraillingStop.GapRate 는 3.0 이상만 됩니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.ListMin < 2)
                    {
                        message = "'리스트 수'는 2 보다 커야 합니다.";
                        $"'리스트 수'는 2 보다 커야 합니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;


                case SettingType.MartingaleShort:
                    MartingaleShort martingaleShort = (MartingaleShort)setting;

                    if (setting.BasePrice <= 0.0M)
                    {
                        message = "'시작호가'를 입력하세요.";
                        $"'시작호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.TopPrice <= 0.0M)
                    {
                        message = "'종료호가'를 입력하세요.";
                        $"'종료호가'를 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.BasePrice >= setting.TopPrice)
                    {
                        message = "'종료호가'는 '시작호가'와 같거나 작을 수 없습니다.";
                        $"'종료호가'는 '시작호가'와 같거나 작을 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (martingaleShort.GapRate < 3M)
                    {
                        message = "'갭%'은 3.0 이상만 됩니다.";
                        $"SettingTraillingStop.GapRate 는 3.0 이상만 됩니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.ListMin < 2)
                    {
                        message = "'리스트 수'는 2 보다 커야 합니다.";
                        $"'리스트 수'는 2 보다 커야 합니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;

                case SettingType.GridMartingaleLong:
                    GridMartingaleLong settingGridMartingaleLongTrading = (GridMartingaleLong)setting;

                    if (settingGridMartingaleLongTrading.Grid.Rate < 0.8M)
                    {
                        message = "'그리드 호가변화%'는 최소 0.8%";
                        $"'그리드 호가변화%'는 최소 0.8%".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.ListMin < 4 || setting.ListMin > 100)
                    {
                        message = "'그리드 최소 리스트 수'는 4이상 입니다.";
                        $"'그리드 최소 리스트 수'는 4이상 입니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleLongTrading.Grid.StopLoss)
                    {
                        message = "'그리드 손절'은 사용 할 수 없습니다.";
                        $"'그리드 손절'은 사용 할 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleLongTrading.Grid.SmartType != SmartType.TrailingMoveTop)
                    {
                        message = "'그리드 트레일링'만 사용 할 수 있습니다.";
                        $"'그리드 트레일링'만 사용 할 수 있습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }

                    if (settingGridMartingaleLongTrading.MartingaleLong.GapRate < 3M)
                    {
                        message = "'마틴게일 롱 갭%'은 3.0 이상만 됩니다.";
                        $"'마틴게일 롱 갭%'은 3.0 이상만 됩니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleLongTrading.MartingaleLong.ListMin < 4)
                    {
                        message = "'마틴게일 롱 리스트 수'는 4이상 입니다.";
                        $"'마틴게일 롱 리스트 수'는 4이상 입니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleLongTrading.MartingaleLong.IsProfitStop)
                    {
                        message = "'마틴게일 롱 익절중지'를 사용 할 수 없습니다.";
                        $"'마틴게일 롱 익절중지'를 사용 할 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;

                case SettingType.GridMartingaleShort:
                    GridMartingaleShort settingGridMartingaleShortTrading = (GridMartingaleShort)setting;

                    if (settingGridMartingaleShortTrading.Grid.Rate < 0.8M)
                    {
                        message = "'그리드 호가변화%'는 최소 0.8%";
                        $"'그리드 호가변화%'는 최소 0.8%".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (setting.ListMin < 4 || setting.ListMin > 100)
                    {
                        message = "'그리드 최소 리스트 수'는 4이상 입니다.";
                        $"'그리드 최소 리스트 수'는 4이상 입니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleShortTrading.Grid.StopLoss)
                    {
                        message = "'그리드 손절'은 사용 할 수 없습니다.";
                        $"'그리드 손절'은 사용 할 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleShortTrading.Grid.SmartType != SmartType.TrailingMoveTop)
                    {
                        message = "'그리드 트레일링'만 사용 할 수 있습니다.";
                        $"'그리드 트레일링'만 사용 할 수 있습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }

                    if (settingGridMartingaleShortTrading.MartingaleShort.GapRate < 3M)
                    {
                        message = "'마틴게일 숏 갭%'은 3.0 이상만 됩니다.";
                        $"'마틴게일 숏 갭%'은 3.0 이상만 됩니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleShortTrading.MartingaleShort.ListMin < 4)
                    {
                        message = "'마틴게일 숏 리스트 수'는 4이상 입니다.";
                        $"'마틴게일 숏 리스트 수'는 4이상 입니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingGridMartingaleShortTrading.MartingaleShort.IsProfitStop)
                    {
                        message = "'마틴게일 숏 익절중지'를 사용 할 수 없습니다.";
                        $"'마틴게일 숏 익절중지'를 사용 할 수 없습니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;

                case SettingType.Schedule:
                    Schedule settingSchedule = (Schedule)setting;

                    if (settingSchedule.OrderSide == Models.OrderSide.bid)
                    {
                        if (setting.Invest < investMin)
                        {
                            message = $"'매수금액'({investMin:N}이상)을 입력하세요.";
                            $"'매수금액'({investMin:N}이상)을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                            return false;
                        }
                        if (settingSchedule.OrderType == Models.OrderType.limit)
                        {
                            if (setting.BasePrice <= 0.0M)
                            {
                                message = "'매수가격'을 입력하세요.";
                                $"'매수가격'을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                                return false;
                            }
                        }
                    }
                    if (settingSchedule.OrderSide == Models.OrderSide.ask)
                    {
                        if (setting.Invest <= 0)
                        {
                            message = $"'매도수량'을 입력하세요.";
                            $"'매도수량'을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                            return false;
                        }
                        if (settingSchedule.OrderType == Models.OrderType.limit)
                        {
                            if (setting.BasePrice <= 0.0M)
                            {
                                message = "'매도가격'을 입력하세요.";
                                $"'매도가격'을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                                return false;
                            }
                        }
                    }
                    //if (settingSchedule.StartDate < DateTime.Now)
                    //{
                    //    message = "'시작일시'는 현재 일시 보다 커야 합니다.";
                    //    $"'시작일시'는 현재 일시 보다 커야 합니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                    //    return false;
                    //}
                    if (settingSchedule.EndDate < DateTime.Now)
                    {
                        message = "'종료일시'는 현재 일시 보다 커야 합니다.";
                        $"'종료일시'는 현재 일시 보다 커야 합니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingSchedule.StartDate >= settingSchedule.EndDate)
                    {
                        message = "'종료일시'는 '시작일시' 보다 커야 합니다.";
                        $"'종료일시'는 '시작일시' 보다 커야 합니다.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    if (settingSchedule.Invest <= 0)
                    {
                        message = "'간격(분)'을 입력하세요.";
                        $"'간격(분)'을 입력하세요.".WriteMessage(ExchangeID, UserID, setting.SettingID, setting.Market, ConsoleColor.Red);
                        return false;
                    }
                    break;
            }

            message = "";
            return true;
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
                //await Task.Delay(30000);

                int cnt = 0;
                int cntPoint = 0;
                int mod = 0;
                bool isUploadOrder = false;
                bool isUploadAccount = false;
                while (true)
                {
                    try
                    {
                        List<Models.Order> addSettingsOrder = new();

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

                                    if (this.Settings.Contains(setting))
                                    {
                                        if (this.SaveWorkDataList)//서버 프로그램 종료시 저장
                                        {
                                            if (setting.SettingType == SettingType.GridMartingaleShort)
                                            {
                                                setting.SaveLossStack(this);

                                                (((GridMartingaleShort)setting).Current as ISettingAction)?.Organized(setting.SettingID, false, false, false, false, true, true, false);
                                            }
                                            else if (setting.SettingType == SettingType.GridMartingaleLong)
                                            {
                                                setting.SaveLossStack(this);

                                                (((GridMartingaleLong)setting).Current as ISettingAction)?.Organized(setting.SettingID, false, false, false, false, true, true, false);
                                            }
                                            else
                                                (setting as ISettingAction).Organized(setting.SettingID, false, false, false, false, true, true, false);
                                        }
                                        else
                                        {
                                            //사용자 세팅 중지 요청
                                            if (setting.BidCancel || setting.AskCancel || setting.AskCurrentPrice || setting.BidCurrentPrice)
                                            {
                                                if (setting.SettingType == SettingType.GridMartingaleShort)
                                                {
                                                    GridMartingaleShort set2 = (GridMartingaleShort)setting;

                                                    if (set2.Current?.SettingType == SettingType.MartingaleShort)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, setting.BidCancel, setting.AskCancel, setting.AskCurrentPrice, setting.BidCurrentPrice, false, true, true);
                                                    else if (set2.Current?.SettingType == SettingType.Grid)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, setting.BidCancel, setting.AskCancel, setting.AskCurrentPrice, setting.BidCurrentPrice, false, true, true);
                                                }
                                                else if (setting.SettingType == SettingType.GridMartingaleLong)
                                                {
                                                    GridMartingaleLong set2 = (GridMartingaleLong)setting;

                                                    if (set2.Current?.SettingType == SettingType.MartingaleLong)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, setting.BidCancel, setting.AskCancel, setting.AskCurrentPrice, setting.BidCurrentPrice, false, true, true);
                                                    else if (set2.Current?.SettingType == SettingType.Grid)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, setting.BidCancel, setting.AskCancel, setting.AskCurrentPrice, setting.BidCurrentPrice, false, true, true);
                                                }
                                                else
                                                    (setting as ISettingAction).Organized(setting.SettingID, setting.BidCancel, setting.AskCancel, setting.AskCurrentPrice, setting.BidCurrentPrice, false, true, true);
                                            }
                                            else
                                            {
                                                //서버 프로그램에서 개별 세팅 중지
                                                if (setting.SettingType == SettingType.MartingaleShort)
                                                    (setting as ISettingAction).Organized(setting.SettingID, false, true, false, false, false, true, true);

                                                else if (setting.SettingType == SettingType.GridMartingaleShort)
                                                {
                                                    GridMartingaleShort set2 = (GridMartingaleShort)setting;

                                                    if (set2.Current?.SettingType == SettingType.MartingaleShort)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, false, true, false, false, false, true, true);
                                                    else if (set2.Current?.SettingType == SettingType.Grid)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, true, false, false, false, false, true, true);
                                                }
                                                else if (setting.SettingType == SettingType.GridMartingaleLong)
                                                {
                                                    GridMartingaleLong set2 = (GridMartingaleLong)setting;

                                                    if (set2.Current?.SettingType == SettingType.MartingaleLong)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, true, false, false, false, false, true, true);
                                                    else if (set2.Current?.SettingType == SettingType.Grid)
                                                        (set2.Current as ISettingAction).Organized(setting.SettingID, true, false, false, false, false, true, true);
                                                }
                                                else
                                                    (setting as ISettingAction).Organized(setting.SettingID, true, false, false, false, false, true, true);
                                            }
                                        }

                                        this.Settings.Remove(setting);
                                        $"Removed setting".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Yellow);
                                    }
                                }
                        }

                        if (this.AddSettingQueue.Count > 0)
                            lock (this.Settings)
                                while (this.AddSettingQueue.Count > 0)
                                {
                                    var setting = this.AddSettingQueue.Dequeue();

                                    setting.User = this;
                                    this.Settings.Add(setting);
                                    $"Added {setting.SettingTypeString}".WriteMessage(this.ExchangeID, this.UserID, setting.SettingID, setting.Market, ConsoleColor.Yellow);

                                    if (Exchanger.IsUnLock)
                                        setting.SettingInOut(setting.User, setting.SettingID, true);

                                    if (setting.Market != null)
                                        addSettingsOrder.Add(new() { Market = setting.Market });
                                }

                        //중지 요청이고 세팅이 모두 제거 되었다면 while 빠져나가기
                        if (this.IsStopped && this.Settings.Count == 0)
                            break;

                        if (this.Api != null && this.Settings.Any())
                        {
                            Models.Order order;

                            order = this.Api.AllOrder("ALL", "desc");
                            if (!isUploadOrder)
                            {
                                Upload(this, order);
                                isUploadOrder = true;
                            }

                            if (addSettingsOrder.Count > 0)
                            {
                                $"addOrderSettings:{addSettingsOrder.Count} ".WriteMessage(this.ExchangeID, this.UserID);
                                foreach (var item in addSettingsOrder)
                                    $"{item.Market}".WriteMessage(this.ExchangeID, this.UserID);
                                ;
                                this.Run(addSettingsOrder, order);
                                //cnt = 0;
                                cnt = 60000;
                                isUploadOrder = false;
                                isUploadAccount = false;
                            }
                            else if (cnt >= 60000)
                            {
                                //$"cnt >= 60:{cnt} ".WriteMessage(this.ExchangeID, this.UserID);

                                Models.Markets markets = this.Api.Markets();
                                List<Models.Order> orders = new();

                                if (markets != null && markets.MarketList != null)
                                {
                                    foreach (var item in markets.MarketList)
                                        orders.Add(new() { Market = item.Market });

                                    this.Run(orders, order);
                                    isUploadOrder = false;
                                    isUploadAccount = false;
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
                                    if (!isUploadAccount)
                                    {
                                        Upload(this, account);
                                        isUploadAccount = true;
                                    }

                                    this.Accounts ??= account;

                                    var a1 = this.Accounts.AccountList.Except(account.AccountList);
                                    var b1 = account.AccountList.Except(this.Accounts.AccountList);
                                    var c1 = a1.Union(b1);

                                    if (c1.Any())
                                    {
                                        //$"Accounts Except:{c1.Count()} ".WriteMessage(this.ExchangeID, this.UserID);
                                        //foreach (var item in c1)
                                        //    $"{item.Currency} Balance:{item.Balance}\tLocked:{item.Locked}".WriteMessage(this.ExchangeID, this.UserID);

                                        Models.Markets markets = this.Api.Markets();
                                        List<Models.Order> orders = new();

                                        if (markets != null && markets.MarketList != null)
                                        {
                                            foreach (var item in markets.MarketList)
                                                orders.Add(new() { Market = item.Market });

                                            this.Run(orders, order);
                                            cnt = 0;
                                            isUploadOrder = false;
                                            isUploadAccount = false;
                                        }
                                    }
                                    else
                                    {
                                        var a2 = this.Settings.Where(x => x.SettingType == SettingType.TraillingStop || x.SettingType == SettingType.Schedule);

                                        if (a2.Any())
                                        {
                                            List<Models.Order> orders = new();

                                            foreach (var item in a2)
                                                orders.Add(new() { Market = item.Market });

                                            this.Run(orders, order);
                                            //isUploadOrder = false;
                                            //isUploadAccount = false;
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
                                        var cc = aa.Union(bb);

                                        if (cc.Any())
                                        {
                                            this.Run(cc, order);
                                            isUploadOrder = false;
                                            isUploadAccount = false;
                                        }
                                        //cnt = 0;
                                    }
                                }
                            }

                            this.Orders = order;

                            if (cntPoint >= 2880000)
                            {
                                cntPoint = 0;
                                this.PointCheck();
                            }
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
                        cntPoint += this.StartRunDelay;
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

        private void Exchange_Action(ICore sender, MetaFrmEventArgs e)
        {
            if (e.Action == "OrderExecution" && e.Value != null && e.Value is Models.Order order)
            {
                //$"OrderExecution {order.Side} {order.Price} {order.ExecutedVolume} {order.UUID}".WriteMessage(this.ExchangeID, this.UserID, null, order.Market, ConsoleColor.Cyan);

                lock (this.Settings)
                    if (this.Api != null && this.Settings.Any(x => x.Market == order.Market) && !this.RemoveSettingQueue.Any(x => x.Market == order.Market))
                    {
                        try
                        {
                            var a1 = this.Settings.Where(x => x.SettingType != SettingType.GridMartingaleShort && x.SettingType != SettingType.GridMartingaleLong);
                            var a2 = this.Settings.Where(x => x.SettingType == SettingType.GridMartingaleShort).Select(x => ((GridMartingaleShort)x).Current);
                            var a3 = this.Settings.Where(x => x.SettingType == SettingType.GridMartingaleLong).Select(x => ((GridMartingaleLong)x).Current);

                            var aa = a1.Union(a2).Union(a3).Where(x => x != null && x.WorkDataList != null).SingleOrDefault(z => z != null && z.WorkDataList != null && z.WorkDataList.SingleOrDefault(y => (y.BidOrder != null && y.BidOrder.UUID == order.UUID) || (y.AskOrder != null && y.AskOrder.UUID == order.UUID)) != null);

                            if (aa != null)
                            {
                                if (aa.SettingType == SettingType.Grid && order.Side == "bid")
                                    this.OrderExecution(aa, order);

                                if (aa.SettingType == SettingType.MartingaleLong && order.Side == "bid")
                                    this.OrderExecution(aa, order);

                                if (aa.SettingType == SettingType.MartingaleShort && order.Side == "ask")
                                    this.OrderExecution(aa, order);

                                if (aa.SettingType == SettingType.GridMartingaleShort && aa is GridMartingaleShort set1 && set1.Current?.SettingType == SettingType.Grid && order.Side == "bid")
                                    this.OrderExecution(set1.Current,order);
                                if (aa.SettingType == SettingType.GridMartingaleShort && aa is GridMartingaleShort set2 && set2.Current?.SettingType == SettingType.MartingaleShort && order.Side == "ask")
                                    this.OrderExecution(set2.Current, order);

                                if (aa.SettingType == SettingType.GridMartingaleLong && aa is GridMartingaleLong set3 && set3.Current?.SettingType == SettingType.Grid && order.Side == "bid")
                                    this.OrderExecution(set3.Current, order);
                                if (aa.SettingType == SettingType.GridMartingaleLong && aa is GridMartingaleLong set4 && set4.Current?.SettingType == SettingType.MartingaleLong && order.Side == "bid")
                                    this.OrderExecution(set4.Current, order);

                                if (aa.SettingType == SettingType.TraillingStop && order.Side == "bid")
                                    this.OrderExecution(aa, order);
                            }
                            else
                            {
                                //$"if (aa != null)".WriteMessage(this.ExchangeID, this.UserID, null, order.Market, ConsoleColor.Red);
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
                    else
                    {
                        //$"if (this.Api != null && this.Settings.Any(x => x.Market == order.Market) && !this.RemoveSettingQueue.Any(x => x.Market == order.Market))".WriteMessage(this.ExchangeID, this.UserID, null, order.Market, ConsoleColor.Red);
                    }
            }

            this.StartRunDelay = 10000;
        }

        internal void OrderExecution(Setting setting, Models.Order order)
        {
            if (setting.User == null) return;

            StringBuilder stringBuilder = new();
            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = this.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("User.OrderExecution");
            data["1"].AddParameter("SETTING_ID", Database.DbType.Int, 3, setting.SettingID);
            data["1"].AddParameter("MARKET_ID", Database.DbType.NVarChar, 20, order.Market);
            data["1"].AddParameter("SIDE", Database.DbType.NVarChar, 20, order.Side);
            data["1"].AddParameter("ORDER_TYPE", Database.DbType.NVarChar, 20, order.OrdType);
            data["1"].AddParameter("PRICE", Database.DbType.Decimal, 25, order.Price);
            data["1"].AddParameter("EXECUTE_QTY", Database.DbType.Decimal, 25, order.ExecutedVolume);
            data["1"].AddParameter(nameof(order.UUID), Database.DbType.NVarChar, 100, order.UUID);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, setting.User.UserID);

            stringBuilder.Append($"{this.ExchangeName()} {setting.SettingTypeString} {(order.Side == "bid" ? "매수" : "매도")} 체결");
            data["1"].AddParameter("MESSAGE_TITLE", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            stringBuilder.Clear();
            stringBuilder.AppendLine($"{order.Market}");

            string[]? tmps = order.Market?.Split('-');

            stringBuilder.Append($"{order.ExecutedVolume:N4} {tmps?[1]}");

            //if (order.Price >= 100)
            //    stringBuilder.Append($" | {order.Price:N0} {tmps?[0]}");
            //else if (order.Price >= 1)  
            //    stringBuilder.Append($" | {order.Price:N2} {tmps?[0]}");
            //else                        
            //    stringBuilder.Append($" | {order.Price:N4} {tmps?[0]}");
            stringBuilder.Append($" | {order.Price.PriceToString(setting.ExchangeID, order.Market ?? "")} {tmps?[0]}");

            decimal tmp = order.Price * order.ExecutedVolume;
            stringBuilder.Append($" | {tmp + tmp * (order.Side == "bid" ? setting.Fees / 100M : -setting.Fees / 100M):N0}원");

            data["1"].AddParameter("MESSAGE_BODY", Database.DbType.NVarChar, 4000, stringBuilder.ToString());

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(setting.User.ExchangeID, setting.User.UserID, setting.SettingID, order.Market);
            });
        }

        internal void PointCheck()
        {
            ServiceData data = new()
            {
                TransactionScope = false,
                Token = this.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("User.PointCheck");
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, this.UserID);

            Response response;

            response = this.ServiceRequest(data);

            if (response.Status != Status.OK)
                response.Message?.WriteMessage();
            else
            {
                if (response.DataSet != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count >= 1)
                    if (response.DataSet.DataTables[0].DataRows[0].String("IS_STOP") == "Y")
                    {
                        lock (this.Settings)
                            foreach (var item in this.Settings)
                            {
                                item.BidCancel = true;
                                this.RemoveSetting(item);
                            }
                    }
            }
        }


        /// <summary>
        /// Upload
        /// </summary>
        /// <param name="user"></param>
        /// <param name="account"></param>
        public static void Upload(User user, Models.Account account)
        {
            Upload(user, user.AuthState, user.ExchangeID, user.UserID, account);
        }
        /// <summary>
        /// Upload
        /// </summary>
        /// <param name="core"></param>
        /// <param name="authenticationState"></param>
        /// <param name="exchangeID"></param>
        /// <param name="userID"></param>
        /// <param name="account"></param>
        public static void Upload(ICore core, Task<AuthenticationState> authenticationState, int exchangeID, int userID, Models.Account account)
        {
            MemoryServiceSet(core, authenticationState, $"{exchangeID}_{userID}_Accounts", System.Text.Json.JsonSerializer.Serialize(account));
        }
        /// <summary>
        /// Upload
        /// </summary>
        /// <param name="user"></param>
        /// <param name="order"></param>
        public static void Upload(User user, Models.Order order)
        {
            Upload(user, user.AuthState, user.ExchangeID, user.UserID, order);
        }
        /// <summary>
        /// Upload
        /// </summary>
        /// <param name="core"></param>
        /// <param name="authenticationState"></param>
        /// <param name="exchangeID"></param>
        /// <param name="userID"></param>
        /// <param name="order"></param>
        public static void Upload(ICore core, Task<AuthenticationState> authenticationState, int exchangeID, int userID, Models.Order order)
        {
            MemoryServiceSet(core, authenticationState, $"{exchangeID}_{userID}_Orders", System.Text.Json.JsonSerializer.Serialize(order));
        }
        private static void MemoryServiceSet(ICore core, Task<AuthenticationState> authenticationState, string key, string value)
        {
            Response response;
            ServiceData data = new()
            {
                ServiceName = "MetaFrm.Service.MemoryService",
                TransactionScope = false,
                Token = authenticationState.Token(),
            };
            data["1"].CommandText = "Set";
            data["1"].AddParameter("KEY", Database.DbType.NVarChar, 0, key);
            data["1"].AddParameter("VALUE", Database.DbType.NVarChar, 0, value);

            response = core.ServiceRequest(data);

            Task.Run(() =>
            {
                Response response;

                response = core.ServiceRequest(data);

                if (response.Status != Status.OK)
                    $"{key} : {response.Message}"?.WriteMessage();
            });
        }

        /// <summary>
        /// DownloadAccount
        /// </summary>
        /// <param name="core"></param>
        /// <param name="authenticationState"></param>
        /// <param name="exchangeID"></param>
        /// <returns></returns>
        public static async Task<Models.Account?> DownloadAccount(ICore core, Task<AuthenticationState> authenticationState, int exchangeID)
        {
            var result = await MemoryServiceGet(core, authenticationState, $"{exchangeID}_{authenticationState.UserID()}_Accounts");

            if (result != null)
                return System.Text.Json.JsonSerializer.Deserialize<Models.Account?>(result);
            else
                return null;
        }
        /// <summary>
        /// DownloadAccount
        /// </summary>
        /// <param name="core"></param>
        /// <param name="authenticationState"></param>
        /// <param name="exchangeID"></param>
        /// <returns></returns>
        public static async Task<Models.Order?> DownloadOrder(ICore core, Task<AuthenticationState> authenticationState, int exchangeID)
        {
            var result = await MemoryServiceGet(core, authenticationState, $"{exchangeID}_{authenticationState.UserID()}_Orders");

            if (result != null)
                return System.Text.Json.JsonSerializer.Deserialize<Models.Order?>(result);
            else
                return null;
        }
        private static async Task<string?> MemoryServiceGet(ICore core, Task<AuthenticationState> authenticationState, string key)
        {
            Response response;
            ServiceData data = new()
            {
                ServiceName = "MetaFrm.Service.MemoryService",
                TransactionScope = false,
                Token = authenticationState.Token(),
            };
            data["1"].CommandText = "Get";
            data["1"].AddParameter("KEY", Database.DbType.NVarChar, 0, key);

            response = await core.ServiceRequestAsync(data);

            if (response.Status != Status.OK || response.DataSet == null || response.DataSet.DataTables.Count < 1 || response.DataSet.DataTables[0].DataRows.Count < 1)
                return null;
            else
                return response.DataSet.DataTables[0].DataRows[0].String("VALUE");
        }


        /// <summary>
        /// ExchangeName
        /// </summary>
        /// <returns></returns>
        public string ExchangeName()
        {
            return ExchangeName(this.ExchangeID);
        }

        /// <summary>
        /// ExchangeName
        /// </summary>
        /// <returns></returns>
        public static string ExchangeName(int exchangeID)
        {
            return exchangeID switch
            {
                1 => "Upbit",
                2 => "Bithumb",
                3 => "Binance",
                _ => ""
            };
        }
    }
}