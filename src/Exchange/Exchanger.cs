﻿using MetaFrm.Extensions;
using MetaFrm.Service;
using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// Exchanger
    /// </summary>
    public class Exchanger : ICore
    {
        /// <summary>
        /// AuthState
        /// </summary>
        private Task<AuthenticationState> AuthState { get; set; }

        /// <summary>
        /// ExchangeID
        /// </summary>
        public int ExchangeID { get; set; }

        /// <summary>
        /// Users
        /// </summary>
        public List<User> Users { get; set; } = new();

        /// <summary>
        /// IsUnLock
        /// </summary>
        public static bool IsUnLock { get; set; } = false;

        private BidAskAlarmMA? BidAskAlarmMA_BTC;
        private BidAskAlarmMA? BidAskAlarmMA_ETH;

        /// <summary>
        /// Exchange
        /// </summary>
        /// <param name="authState"></param>
        /// <param name="exchangeID"></param>
        /// <param name="isLoadDB"></param>
        public Exchanger(Task<AuthenticationState> authState, int exchangeID, bool isLoadDB)
        { 
            this.AuthState = authState;
            this.ExchangeID = exchangeID;

            if (isLoadDB)
                this.GetSetting();
        }

        /// <summary>
        /// AddUser
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        public User? AddUser(int userID, string accessKey, string secretKey)
        {
            User? user;

            lock (this.Users)
                user = this.Users.SingleOrDefault(x => x.UserID == userID);

            if (user != null)
                return user;

            bool isAdd = false;

            lock (this.Users)
            {
                int count = this.Users.Count;

                user = new(this.AuthState)
                {
                    ExchangeID = this.ExchangeID,
                    UserID = userID,
                    Api = CreateApi(count)
                };

                if (user.Api == null)
                    return null;

                user.Api.AccessKey = accessKey;
                user.Api.SecretKey = secretKey;

                user.IsFirstUser = (count == 0);

                this.Users.Add(user);
                isAdd = true;
            }

            if (isAdd)
            {
                user.Start();
                $"Added User".WriteMessage(this.ExchangeID, user.UserID);

                if (this.ExchangeID == 1 && user.IsFirstUser)
                {
                    this.BidAskAlarmMA_BTC = new(this.AuthState, user.Api, "KRW-BTC", 15, 7, 30, 60, 0.025M, 0.12M);
                    this.BidAskAlarmMA_BTC.Run("KRW-BTC");

                    this.BidAskAlarmMA_ETH = new(this.AuthState, user.Api, "KRW-ETH", 15, 5, 30, 60, 0.02M, 0.13M);
                    this.BidAskAlarmMA_ETH.Run("KRW-ETH");
                }
            }

            return user;
        }
        /// <summary>
        /// CreateApi
        /// </summary>
        /// <returns></returns>
        public IApi? CreateApi(int count)
        {
            return CreateApi(this.ExchangeID, count, true, this.AuthState);
        }
        /// <summary>
        /// CreateApi
        /// </summary>
        /// <param name="exchangeID"></param>
        /// <param name="userCount"></param>
        /// <param name="runOrderResultFromWebSocket"></param>
        /// <param name="authState"></param>
        /// <returns></returns>
        public static IApi? CreateApi(int exchangeID, int userCount, bool runOrderResultFromWebSocket, Task<AuthenticationState>? authState)
        {
            return exchangeID switch
            {
                1 => new Stock.Exchange.Upbit.UpbitApi(userCount == 0, runOrderResultFromWebSocket, Factory.Platform == Maui.Devices.DevicePlatform.Server ? authState : null),
                2 => new Stock.Exchange.Bithumb.BithumbApi(userCount == 0, runOrderResultFromWebSocket, Factory.Platform == Maui.Devices.DevicePlatform.Server ? authState : null),
                _ => null
            };
        }

        /// <summary>
        /// RemoveUser
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="saveWorkDataList"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUser(int userID, bool saveWorkDataList)
        {
            User? user = null;

            lock (this.Users)
                user = this.Users.SingleOrDefault(x => x.UserID == userID);

            if (user != null)
                return await this.RemoveUser(user, saveWorkDataList  );

            return false;
        }
        /// <summary>
        /// RemoveUser
        /// </summary>
        /// <param name="user"></param>
        /// <param name="saveWorkDataList"></param>
        /// <returns></returns>
        public async Task<bool> RemoveUser(User user, bool saveWorkDataList)
        {
            bool isContains = false;

            lock (this.Users)
                isContains = this.Users.Contains(user);

            if (isContains)
            {
                user.SaveWorkDataList = saveWorkDataList;
                user.IsStopped = true;

                while (true)
                {
                    await Task.Delay(3000);

                    lock (user.Settings)
                        if (user.Settings.Count != 0)
                            continue;

                    break;
                }

                if (!user.IsFirstUser)
                {
                    ((IDisposable?)user.Api)?.Dispose();
                    $"Removed User".WriteMessage(this.ExchangeID, user.UserID);

                    lock (this.Users)
                        return this.Users.Remove(user);
                }
                else
                {
                    $"FirstUser !!".WriteMessage(this.ExchangeID, user.UserID);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Exit
        /// </summary>
        /// <param name="saveWorkDataList"></param>
        /// <returns></returns>
        public async Task<bool> Exit(bool saveWorkDataList)
        {
            List<User> users = new();

            lock (this.Users)
                foreach (var user in this.Users)
                    users.Add(user);

            foreach (var user in users)
                if (this.RemoveUser(user, saveWorkDataList).Result)
                    continue;

            while (true)
            {
                if (this.BidAskAlarmMA_BTC != null)
                    this.BidAskAlarmMA_BTC.IsRunReciveData = false;
                if (this.BidAskAlarmMA_ETH != null)
                    this.BidAskAlarmMA_ETH.IsRunReciveData = false;

                await Task.Delay(10000);

                int count = 0;

                lock (this.Users)
                    count = this.Users.Count;

                if (count == 1)
                {
                    lock (this.Users[0].Settings)
                        count = this.Users[0].Settings.Count;

                    if (count == 0)
                    {
                        ((IDisposable?)this.Users[0].Api)?.Dispose();
                        $"Removed User".WriteMessage(this.ExchangeID, this.Users[0].UserID);

                        lock (this.Users)
                            this.Users.Remove(this.Users[0]);

                        return true;
                    }
                }

                if (count == 0)
                    return true;
            }
        }


        internal void GetSetting()
        {
            Response response;
            ServiceData data = new()
            {
                TransactionScope = false,
                Token = this.AuthState.Token(),
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Exchanger.GetSetting");
            data["1"].AddParameter("EXCHANGE_ID", Database.DbType.Int, 3, this.ExchangeID);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, this.AuthState.UserID());

            response = this.ServiceRequest(data);

            if (response.Status != Status.OK)
                response.Message?.WriteMessage(this.ExchangeID, this.AuthState.UserID());
            else
            {
                if (response.DataSet == null || response.DataSet.DataTables.Count != 2)
                    return;

                foreach (var user in response.DataSet.DataTables[0].DataRows)
                {
                    int? USER_ID = user.Int(nameof(USER_ID));
                    string? EMAIL = user.String(nameof(EMAIL));
                    string? ACCESS_KEY = user.String(nameof(ACCESS_KEY));
                    string? SECRET_KEY = user.String(nameof(SECRET_KEY));

                    if (USER_ID == null || EMAIL == null || ACCESS_KEY == null || SECRET_KEY == null)
                        continue;

                    User? user1 = this.AddUser((int)USER_ID
                        , ACCESS_KEY.AesDecryptorToBase64String(EMAIL, $"{USER_ID}")
                        , SECRET_KEY.AesDecryptorToBase64String(EMAIL, $"{USER_ID}"));

                    if (user1 == null)
                        continue;

                    var setting = response.DataSet.DataTables[1].DataRows.Where(x => x.Int("USER_ID") != null && x.Int("USER_ID") == USER_ID);

                    foreach (var item in setting)
                    {
                        string? SETTING_TYPE = item.String(nameof(SETTING_TYPE));
                        int? SETTING_ID = item.Int(nameof(SETTING_ID));
                        string? MARKET = item.String(nameof(MARKET));
                        decimal? INVEST = item.Decimal(nameof(INVEST));
                        decimal? BASE_INVEST = item.Decimal(nameof(BASE_INVEST));
                        decimal? BASE_PRICE = item.Decimal(nameof(BASE_PRICE));
                        decimal? TOP_PRICE = item.Decimal(nameof(TOP_PRICE));
                        decimal? RATE = item.Decimal(nameof(RATE));
                        int? LIST_MIN = item.Int(nameof(LIST_MIN));
                        decimal? FEES = item.Decimal(nameof(FEES));
                        bool TOP_STOP = item.String(nameof(TOP_STOP)) == "Y";
                        bool IS_PROFIT_STOP = item.String(nameof(IS_PROFIT_STOP)) == "Y";
                        string? MESSAGE = item.String(nameof(MESSAGE));

                        if (SETTING_TYPE == null || SETTING_ID == null || MARKET == null || BASE_INVEST == null || INVEST == null || BASE_PRICE == null || TOP_PRICE == null || RATE == null || LIST_MIN == null || FEES == null)
                            continue;

                        switch (SETTING_TYPE.EnumParse<SettingType>())
                        {
                            case SettingType.Grid:

                                string? SMART_TYPE = item.String(nameof(SMART_TYPE));
                                bool IS_BUYING = item.String(nameof(IS_BUYING)) == "Y";
                                bool ASK_FILL = item.String(nameof(ASK_FILL)) == "Y";
                                bool STOP_LOSS = item.String(nameof(STOP_LOSS)) == "Y";
                                bool BID_ORDER_ALL = item.String(nameof(BID_ORDER_ALL)) == "Y";

                                if (SMART_TYPE != null)
                                    user1.AddSetting(new Grid(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                        BasePrice = (decimal)BASE_PRICE,
                                        TopPrice = (decimal)TOP_PRICE,
                                        Rate = (decimal)RATE,
                                        ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        TopStop = TOP_STOP,
                                        IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        SmartType = SMART_TYPE.EnumParse<SmartType>(),
                                        IsBuying = IS_BUYING,
                                        AskFill = ASK_FILL,
                                        StopLoss = STOP_LOSS,
                                        BidOrderAll = BID_ORDER_ALL,
                                    });
                                break;


                            case SettingType.TraillingStop:
                                decimal? RETURN_RATE1 = item.Decimal("RETURN_RATE");
                                decimal? GAP_RATE1 = item.Decimal("GAP_RATE");
                                bool IS_USER_BID1 = item.String("IS_USER_BID") == "Y";

                                if (RETURN_RATE1 != null && GAP_RATE1 != null)
                                    user1.AddSetting(new TraillingStop(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                        BasePrice = (decimal)BASE_PRICE,
                                        //TopPrice = (decimal)TOP_PRICE,
                                        Rate = (decimal)RATE,
                                        ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        //TopStop = TOP_STOP,
                                        //IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        ReturnRate = (decimal)RETURN_RATE1,
                                        GapRate = (decimal)GAP_RATE1,
                                        IsUserBid = IS_USER_BID1,
                                    });

                                break;


                            case SettingType.MartingaleLong:
                                decimal? GAP_RATE2 = item.Decimal("GAP_RATE");
                                bool FIRST_FIX2 = item.String("FIRST_FIX") == "Y";

                                if (GAP_RATE2 != null)
                                    user1.AddSetting(new MartingaleLong(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                        BasePrice = (decimal)BASE_PRICE,
                                        TopPrice = (decimal)TOP_PRICE,
                                        Rate = (decimal)RATE,
                                        ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        TopStop = TOP_STOP,
                                        IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        GapRate = (decimal)GAP_RATE2,
                                        FirstFix = FIRST_FIX2,
                                    });

                                break;

                            case SettingType.MartingaleShort:
                                decimal? GAP_RATE3 = item.Decimal("GAP_RATE");
                                bool FIRST_FIX3 = item.String("FIRST_FIX") == "Y";

                                if (GAP_RATE3 != null)
                                    user1.AddSetting(new MartingaleShort(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                        BasePrice = (decimal)BASE_PRICE,
                                        TopPrice = (decimal)TOP_PRICE,
                                        Rate = (decimal)RATE,
                                        ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        TopStop = TOP_STOP,
                                        IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        GapRate = (decimal)GAP_RATE3,
                                        FirstFix = FIRST_FIX3,
                                    });

                                break;

                            case SettingType.GridMartingaleLong:
                                decimal? GM_G_RATE1 = item.Decimal("GM_G_RATE");
                                int? GM_G_LIST_MIN1 = item.Int("GM_G_LIST_MIN");

                                decimal? GM_M_RATE1 = item.Decimal("GM_M_RATE");
                                int? GM_M_LIST_MIN1 = item.Int("GM_M_LIST_MIN");
                                decimal? GM_M_GAP_RATE1 = item.Decimal("GM_M_GAP_RATE");

                                if (GM_G_RATE1 != null && GM_G_LIST_MIN1 != null && GM_M_RATE1 != null && GM_M_LIST_MIN1 != null && GM_M_GAP_RATE1 != null)
                                    user1.AddSetting(new GridMartingaleLong(user1
                                        , new(user1)
                                        {
                                            Rate = (decimal)GM_G_RATE1,
                                            ListMin = (int)GM_G_LIST_MIN1,
                                            TopStop = TOP_STOP,
                                            IsProfitStop = IS_PROFIT_STOP,
                                        }
                                        , new(user1)
                                        {
                                            Rate = (decimal)GM_M_RATE1,
                                            ListMin = (int)GM_M_LIST_MIN1,
                                            TopStop = TOP_STOP,
                                            IsProfitStop = IS_PROFIT_STOP,

                                            GapRate = (decimal)GM_M_GAP_RATE1,
                                        }
                                        )
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                    });

                                break;

                            case SettingType.GridMartingaleShort:
                                decimal? GM_G_RATE2 = item.Decimal("GM_G_RATE");
                                int? GM_G_LIST_MIN2 = item.Int("GM_G_LIST_MIN");

                                decimal? GM_M_RATE2 = item.Decimal("GM_M_RATE");
                                int? GM_M_LIST_MIN2 = item.Int("GM_M_LIST_MIN");
                                decimal? GM_M_GAP_RATE2 = item.Decimal("GM_M_GAP_RATE");

                                if (GM_G_RATE2 != null && GM_G_LIST_MIN2 != null && GM_M_RATE2 != null && GM_M_LIST_MIN2 != null && GM_M_GAP_RATE2 != null)
                                    user1.AddSetting(new GridMartingaleShort(user1
                                        , new(user1)
                                        {
                                            Rate = (decimal)GM_G_RATE2,
                                            ListMin = (int)GM_G_LIST_MIN2,
                                            TopStop = TOP_STOP,
                                            IsProfitStop = IS_PROFIT_STOP,
                                        }
                                        , new(user1)
                                        {
                                            Rate = (decimal)GM_M_RATE2,
                                            ListMin = (int)GM_M_LIST_MIN2,
                                            TopStop = TOP_STOP,
                                            IsProfitStop = IS_PROFIT_STOP,

                                            GapRate = (decimal)GM_M_GAP_RATE2,
                                        }
                                        )
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                    });

                                break;

                            case SettingType.Schedule:
                                string? ORDER_SIDE = item.String(nameof(ORDER_SIDE));
                                string? ORDER_TYPE1 = item.String("ORDER_TYPE");
                                int? INTERVAL = item.Int(nameof(INTERVAL));
                                DateTime? START_DATE = item.DateTime(nameof(START_DATE));
                                DateTime? END_DATE = item.DateTime(nameof(END_DATE));
                                DateTime? EXECUTE_DATE = item.DateTime(nameof(EXECUTE_DATE));

                                if (ORDER_SIDE != null && ORDER_TYPE1 != null && INTERVAL != null && START_DATE != null && END_DATE != null)
                                    user1.AddSetting(new Schedule(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                        BasePrice = (decimal)BASE_PRICE,
                                        //TopPrice = (decimal)TOP_PRICE,
                                        //Rate = (decimal)RATE,
                                        //ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        //TopStop = TOP_STOP,
                                        //IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        OrderSide = ORDER_SIDE.EnumParse<Models.OrderSide>(),
                                        OrderType = ORDER_TYPE1.EnumParse<Models.OrderType>(),
                                        Interval = (int)INTERVAL,
                                        StartDate = (DateTime)START_DATE,
                                        EndDate = (DateTime)END_DATE,
                                        ExecuteDate = EXECUTE_DATE,
                                    });

                                break;


                            case SettingType.BidAskMA:
                                string? ORDER_TYPE2 = item.String("ORDER_TYPE");
                                int? MINUTE_CANDLE_TYPE = item.Int(nameof(MINUTE_CANDLE_TYPE));
                                int? LEFT_MA7 = item.Int(nameof(LEFT_MA7));
                                int? RIGHT_MA30 = item.Int(nameof(RIGHT_MA30));
                                int? RIGHT_MA60 = item.Int(nameof(RIGHT_MA60));
                                decimal? STOP_LOSS_RATE = item.Decimal(nameof(STOP_LOSS_RATE));

                                if (MINUTE_CANDLE_TYPE != null && LEFT_MA7 != null && RIGHT_MA30 != null && RIGHT_MA60 != null && STOP_LOSS_RATE != null)
                                    user1.AddSetting(new BidAskMA(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BaseInvest = (decimal)BASE_INVEST,
                                        //BasePrice = (decimal)BASE_PRICE,
                                        //TopPrice = (decimal)TOP_PRICE,
                                        Rate = (decimal)RATE,
                                        //ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        //TopStop = TOP_STOP,
                                        //IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        OrderType = ORDER_TYPE2.IsNullOrEmpty() ? Models.OrderType.price_market : ORDER_TYPE2.EnumParse<Models.OrderType>(),
                                        MinuteCandleType = MINUTE_CANDLE_TYPE.EnumParse<Models.MinuteCandleType>(),
                                        LeftMA7 = (int)LEFT_MA7,
                                        RightMA30 = (int)RIGHT_MA30,
                                        RightMA60 = (int)RIGHT_MA60,
                                        StopLossRate = (decimal)STOP_LOSS_RATE,
                                    });

                                break;
                        }
                    }
                }
            }
        }
    }
}