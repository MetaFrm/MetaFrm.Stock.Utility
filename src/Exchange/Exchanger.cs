using MetaFrm.Extensions;
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
            {
                user = this.Users.SingleOrDefault(x => x.UserID == userID);

                if (user != null)
                    return user;

                user = new(this.AuthState)
                {
                    ExchangeID = this.ExchangeID,
                    UserID = userID,
                    Api = CreateApi()
                };

                if (user.Api == null)
                    return null;

                user.Api.AccessKey = accessKey;
                user.Api.SecretKey = secretKey;

                user.IsFirstUser = (this.Users.Count == 0);
                this.Users.Add(user);
            }

            user.Start();
            $"Added User".WriteMessage(this.ExchangeID, user.UserID);

            return user;
        }
        /// <summary>
        /// CreateApi
        /// </summary>
        /// <returns></returns>
        public IApi? CreateApi()
        {
            lock (this.Users)
                return CreateApi(this.ExchangeID, this.Users.Count, true);
        }
        /// <summary>
        /// CreateApi
        /// </summary>
        /// <param name="exchangeID"></param>
        /// <param name="userCount"></param>
        /// <param name="runOrderResultFromWebSocket"></param>
        /// <returns></returns>
        public static IApi? CreateApi(int exchangeID, int userCount, bool runOrderResultFromWebSocket)
        {
            return exchangeID switch
            {
                1 => new Stock.Exchange.Upbit.UpbitApi(userCount == 0, runOrderResultFromWebSocket),
                2 => new Stock.Exchange.Bithumb.BithumbApi(userCount == 0, runOrderResultFromWebSocket),
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
            var sel = this.Users.SingleOrDefault(x => x.UserID == userID);

            if (sel != null)
                return await this.RemoveUser(sel, saveWorkDataList  );

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
            if (this.Users.Contains(user))
            {
                user.SaveWorkDataList = saveWorkDataList;
                user.IsStopped = true;

                while (true)
                {
                    await Task.Delay(1000);

                    lock (user.Settings)
                        if (user.Settings.Count != 0)
                            continue;

                    break;
                }

                lock (Users)
                {
                    if (!user.IsFirstUser)
                    {
                        ((IDisposable?)user.Api)?.Dispose();
                        $"Removed User".WriteMessage(this.ExchangeID, user.UserID);
                        return this.Users.Remove(user);
                    }
                    else
                    {
                        $"FirstUser !!".WriteMessage(this.ExchangeID, user.UserID);
                        return false;
                    }
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
                await Task.Delay(2000);

                lock (this.Users)
                {
                    if (this.Users.Count == 1)
                        lock (this.Users[0].Settings)
                            if (this.Users[0].Settings.Count == 0)
                            {
                                ((IDisposable?)this.Users[0].Api)?.Dispose();
                                $"Removed User".WriteMessage(this.ExchangeID, this.Users[0].UserID);
                                this.Users.Remove(this.Users[0]);

                                return true;
                            }

                    if (this.Users.Count == 0)
                        return true;
                }
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
                        decimal? BASE_PRICE = item.Decimal(nameof(BASE_PRICE));
                        decimal? TOP_PRICE = item.Decimal(nameof(TOP_PRICE));
                        decimal? RATE = item.Decimal(nameof(RATE));
                        int? LIST_MIN = item.Int(nameof(LIST_MIN));
                        decimal? FEES = item.Decimal(nameof(FEES));
                        bool TOP_STOP = item.String(nameof(TOP_STOP)) == "Y";
                        bool IS_PROFIT_STOP = item.String(nameof(IS_PROFIT_STOP)) == "Y";
                        string? MESSAGE = item.String(nameof(MESSAGE));

                        if (SETTING_TYPE == null || SETTING_ID == null || MARKET == null || INVEST == null || BASE_PRICE == null || TOP_PRICE == null || RATE == null || LIST_MIN == null || FEES == null)
                            continue;

                        switch (SETTING_TYPE.EnumParse<SettingType>())
                        {
                            case SettingType.Grid:

                                string? SMART_TYPE = item.String(nameof(SMART_TYPE));
                                bool IS_BUYING = item.String(nameof(IS_BUYING)) == "Y";
                                bool STOP_LOSS = item.String(nameof(STOP_LOSS)) == "Y";
                                bool BID_ORDER_ALL = item.String(nameof(BID_ORDER_ALL)) == "Y";

                                if (SMART_TYPE != null)
                                    user1.AddSetting(new Grid(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
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
                                    });

                                break;

                            case SettingType.Schedule:
                                string? ORDER_SIDE = item.String(nameof(ORDER_SIDE));
                                string? ORDER_TYPE = item.String(nameof(ORDER_TYPE));
                                int? INTERVAL = item.Int(nameof(INTERVAL));
                                DateTime? START_DATE = item.DateTime(nameof(START_DATE));
                                DateTime? END_DATE = item.DateTime(nameof(END_DATE));
                                DateTime? EXECUTE_DATE = item.DateTime(nameof(EXECUTE_DATE));

                                if (ORDER_SIDE != null && ORDER_TYPE != null && INTERVAL != null && START_DATE != null && END_DATE != null)
                                    user1.AddSetting(new Schedule(user1)
                                    {
                                        SettingID = (int)SETTING_ID,
                                        Market = MARKET,
                                        Invest = (decimal)INVEST,
                                        BasePrice = (decimal)BASE_PRICE,
                                        //TopPrice = (decimal)TOP_PRICE,
                                        //Rate = (decimal)RATE,
                                        //ListMin = (int)LIST_MIN,
                                        Fees = (decimal)FEES,
                                        //TopStop = TOP_STOP,
                                        //IsProfitStop = IS_PROFIT_STOP,
                                        //Message = MESSAGE,

                                        OrderSide = ORDER_SIDE.EnumParse<Models.OrderSide>(),
                                        OrderType = ORDER_TYPE.EnumParse<Models.OrderType>(),
                                        Interval = (int)INTERVAL,
                                        StartDate = (DateTime)START_DATE,
                                        EndDate = (DateTime)END_DATE,
                                        ExecuteDate = EXECUTE_DATE,
                                    });

                                break;
                        }
                    }
                }
            }
        }
    }
}