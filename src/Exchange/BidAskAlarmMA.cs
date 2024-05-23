using MetaFrm.Extensions;
using MetaFrm.Service;
using MetaFrm.Stock.Console;
using MetaFrm.Stock.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;
using System.Text.Json;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// BidAskAlarmMA
    /// </summary>
    public class BidAskAlarmMA : ICore
    {
        /// <summary>
        /// AuthState
        /// </summary>
        internal Task<AuthenticationState> AuthState { get; }

        /// <summary>
        /// Candles
        /// </summary>
        public static Dictionary<string, CandlesMinute> Candles { get; set; } = new();

        /// <summary>
        /// MinuteCandleType
        /// </summary>
        public MinuteCandleType MinuteCandleType { get; set; }

        /// <summary>
        /// Api
        /// </summary>
        public IApi Api { get; set; }

        /// <summary>
        /// LeftMA7
        /// </summary>
        public int LeftMA7 { get; set; }

        /// <summary>
        /// RightMA30
        /// </summary>
        public int RightMA30 { get; set; }

        /// <summary>
        /// RightMA60
        /// </summary>
        public int RightMA60 { get; set; }

        /// <summary>
        /// StopLossRate
        /// </summary>
        public decimal StopLossRate { get; set; }

        /// <summary>
        /// Rate
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// IsRunReciveData
        /// </summary>
        public bool IsRunReciveData { get; set; }

        /// <summary>
        /// StatusBidAskAlarmMA
        /// </summary>
        private StatusBidAskAlarmMA StatusBidAskAlarmMA { get; set; }

        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };

        /// <summary>
        /// BidAskAlarmMA
        /// </summary>
        /// <param name="authState"></param>
        /// <param name="api"></param>
        /// <param name="market"></param>
        /// <param name="unit"></param>
        /// <param name="leftMA7"></param>
        /// <param name="rightMA30"></param>
        /// <param name="rightMA60"></param>
        /// <param name="stopLossRate"></param>
        /// <param name="rate"></param>
        public BidAskAlarmMA(Task<AuthenticationState> authState, IApi api, string market, int unit, int leftMA7, int rightMA30, int rightMA60, decimal stopLossRate, decimal rate)
        {
            this.AuthState = authState;
            this.Api = api;

            if (!BidAskAlarmMA.Candles.ContainsKey($"{market}_{unit}"))
                BidAskAlarmMA.Candles.Add($"{market}_{unit}", new(market, api.ExchangeID, unit));

            this.LeftMA7 = leftMA7;
            this.RightMA30 = rightMA30;
            this.RightMA60 = rightMA60;
            this.StopLossRate = stopLossRate;
            this.Rate = rate;
            this.MinuteCandleType = $"_{unit}".EnumParse<MinuteCandleType>();

            this.StatusBidAskAlarmMA = ReadStatusBidAskAlarmMA(0, api.ExchangeID, unit, market, this.LeftMA7, this.RightMA30, this.RightMA60, this.StopLossRate, this.Rate) ?? new();

            if (this.StatusBidAskAlarmMA.TempInvest <= 0)
                this.StatusBidAskAlarmMA.TempInvest = 1000000M;
        }


        private DateTime lassRunDateTime = DateTime.MinValue;
        /// <summary>
        /// Run
        /// </summary>
        public void Run(string market)
        {
            this.IsRunReciveData = true;

            Task.Run(async () =>
            {
                DateTime dateTime;
                StringBuilder stringBuilder = new();
                CandlesMinute candles = BidAskAlarmMA.Candles[$"{market}_{(int)this.MinuteCandleType}"];

                while (this.IsRunReciveData)
                {
                    await Task.Delay(2000);

                    dateTime = DateTime.Now;

                    if (this.lassRunDateTime == DateTime.MinValue
                        || candles.CandlesMinuteList == null || candles.CandlesMinuteList.Count < this.RightMA60
                        || (this.lassRunDateTime.Minute != dateTime.Minute && dateTime.Minute % candles.Unit == 0))
                    {
                        this.lassRunDateTime = dateTime;

                        //다음 Unit에서 손절 평가
                        if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && this.StatusBidAskAlarmMA.IsBid)
                            this.StatusBidAskAlarmMA.IsBid = false;

                        //System.Console.WriteLine($"실행 {this.Api.ExchangeID} {market} {this.lassRunDateTime:yyyy-MM-dd HH:mm:ss}");

                        await Task.Delay(2000);

                        SecondaryIndicator(this.Api, candles, market, this.MinuteCandleType, this.LeftMA7, this.RightMA30, this.RightMA60);

                        this.MA_Test(candles, this.StatusBidAskAlarmMA, market, this.LeftMA7, this.RightMA30, this.RightMA60, this.StopLossRate, this.Rate);
                    }

                    if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && !this.StatusBidAskAlarmMA.IsBid)
                    {
                        Ticker? ticker = this.GetCurrentInfo(market);

                        //매수 상태이고 스탑로스 가격 보다 떨어지면 손절
                        if (ticker != null && this.StatusBidAskAlarmMA.StopLossPrice >= ticker.TradePrice)
                        {
                            this.StatusBidAskAlarmMA.CurrentStatus = "손절";
                            this.StatusBidAskAlarmMA.TempInvest = this.StatusBidAskAlarmMA.StopLossPrice * this.StatusBidAskAlarmMA.Qty * (1M - 0.0005M);

                            stringBuilder.Clear();
                            stringBuilder.AppendLine($"손절가격 : {this.StatusBidAskAlarmMA.StopLossPrice.PriceToString(this.Api.ExchangeID, market)} -{(this.StopLossRate * 100M):N2}%");
                            stringBuilder.AppendLine($"매수가격 : {this.StatusBidAskAlarmMA.BidPrice.PriceToString(this.Api.ExchangeID, market)}");
                            stringBuilder.AppendLine($"가상 투자금액 : {this.StatusBidAskAlarmMA.TempInvest:N0}");
                            stringBuilder.ToString().WriteMessage(this.Api.ExchangeID, null, null, market);

                            this.BidAskAlarm_MA(this.AuthState.Token(), market, "ask", "limit", this.StatusBidAskAlarmMA.StopLossPrice.PriceRound(this.Api.ExchangeID, market)
                                , $"{User.ExchangeName(this.Api.ExchangeID)} {market} 손절신호", stringBuilder.ToString());
                        }
                    }
                }

                SaveStatusBidAskAlarmMA(0, this.StatusBidAskAlarmMA, this.Api.ExchangeID, candles.Unit, market, this.LeftMA7, this.RightMA30, this.RightMA60, this.StopLossRate, this.Rate);
            });
        }
        internal Ticker? GetCurrentInfo(string market)
        {
            if (this.Api == null) return null;

            var ticker = this.Api.Ticker(market);
            if (ticker == null || ticker.TickerList == null || ticker.TickerList.Count < 1) return null;

            return ticker.TickerList[0];
        }



        private void MA_Test(CandlesMinute candles, StatusBidAskAlarmMA statusBidAskAlarmMA, string market, int leftMA7, int rightMA30, int rightMA60, decimal stopLossRate, decimal rate)
        {
            if (candles.CandlesMinuteList == null)
                return;

            var data = candles.CandlesMinuteList.OrderByDescending(o => o.CandleDateTimeKst).Take(2).OrderBy(o => o.CandleDateTimeKst);

            if (data == null)
                return;

            decimal? beforValue_7_30 = null;
            decimal? beforValue_7_60 = null;
            StringBuilder stringBuilder = new();

            foreach (var item in data)
            {
                if (item.SecondaryIndicator.TryGetValue($"{leftMA7} - {rightMA30}MA_TradePrice", out decimal? value_7_30) && value_7_30 != null
                    && item.SecondaryIndicator.TryGetValue($"{leftMA7} - {rightMA60}MA_TradePrice", out decimal? value_7_60) && value_7_60 != null)
                {
                    if (beforValue_7_30 == null || beforValue_7_60 == null)
                    {
                        beforValue_7_30 = value_7_30;
                        beforValue_7_60 = value_7_60;
                        continue;
                    }

                    if (value_7_30 > 0 && value_7_60 > 0)
                    {
                        //상승 중

                        //매수 포지션 일떄
                        if (statusBidAskAlarmMA.CurrentStatus == "매수")
                        {
                            //이전 값보다 작아 졌을떄 => 간격이 좁아지면 => 매도를 한다 (보정값으로으로 미세 조정 필요★)
                            // 5 < 6
                            if (value_7_30 < beforValue_7_30 && item.TradePrice > (statusBidAskAlarmMA.BidPrice * (1M + rate)))
                            {
                                statusBidAskAlarmMA.AskPrice = item.TradePrice;
                                statusBidAskAlarmMA.CurrentStatus = "";
                                statusBidAskAlarmMA.TempInvest = statusBidAskAlarmMA.AskPrice * statusBidAskAlarmMA.Qty * (1M - 0.0005M);

                                stringBuilder.Clear();
                                stringBuilder.AppendLine($"매수가격 : {statusBidAskAlarmMA.StopLossPrice.PriceToString(this.Api.ExchangeID, market)}");
                                stringBuilder.AppendLine($"매도가격 : {statusBidAskAlarmMA.AskPrice.PriceToString(this.Api.ExchangeID, market)}");
                                stringBuilder.AppendLine($"가상 투자금액 : {statusBidAskAlarmMA.TempInvest:N0}");
                                stringBuilder.ToString().WriteMessage(this.Api.ExchangeID, null, null, market);

                                this.BidAskAlarm_MA(this.AuthState.Token(), market, "ask", "limit", item.TradePrice.PriceRound(this.Api.ExchangeID, market)
                                    , $"{User.ExchangeName(this.Api.ExchangeID)} {market} 매도신호", stringBuilder.ToString());
                            }
                        }

                        //손절 상태가 되면 상숭 후에 다시 내려 갔을떄 진입
                        if (statusBidAskAlarmMA.CurrentStatus == "손절")
                            statusBidAskAlarmMA.CurrentStatus = "";
                    }
                    else if (value_7_30 < 0)
                    {
                        //하락 중

                        //포지션이 없을때
                        if (StatusBidAskAlarmMA.CurrentStatus == "")
                        {
                            //이전 값보다 커졌을떄 => 간격이 좁아지면 => 매수를 한다 (보정값으로으로 미세 조정 필요★)
                            // -5 > -6
                            if (value_7_30 > beforValue_7_30)
                            {
                                statusBidAskAlarmMA.CurrentStatus = "매수";
                                statusBidAskAlarmMA.BidPrice = item.TradePrice;
                                statusBidAskAlarmMA.Qty = (statusBidAskAlarmMA.TempInvest * (1M - 0.0005M)) / statusBidAskAlarmMA.BidPrice;

                                statusBidAskAlarmMA.StopLossPrice = (statusBidAskAlarmMA.BidPrice * (1M - stopLossRate)).PriceRound(this.Api.ExchangeID, market);//손절

                                statusBidAskAlarmMA.IsBid = true;

                                //StatusBidAskAlarmMA.StopLossPrice = item.TradePrice * (1M - stopLossRate);//손절

                                stringBuilder.Clear();
                                stringBuilder.AppendLine($"매수가격 : {statusBidAskAlarmMA.BidPrice.PriceToString(this.Api.ExchangeID, market)}");
                                stringBuilder.AppendLine($"목표가격 : {(statusBidAskAlarmMA.BidPrice * (1M + rate)).PriceRound(this.Api.ExchangeID, market).PriceToString(this.Api.ExchangeID, market)}");
                                stringBuilder.AppendLine($"손절가격 : {statusBidAskAlarmMA.StopLossPrice.PriceToString(this.Api.ExchangeID, market)} {(stopLossRate * 100M * -1M):N2}%");
                                stringBuilder.AppendLine($"가상 투자금액 : {statusBidAskAlarmMA.TempInvest:N0}");
                                stringBuilder.ToString().WriteMessage(this.Api.ExchangeID, null, null, market);

                                this.BidAskAlarm_MA(this.AuthState.Token(), market, "bid", "limit", item.TradePrice.PriceRound(this.Api.ExchangeID, market)
                                    , $"{User.ExchangeName(this.Api.ExchangeID)} {market} 매수신호", stringBuilder.ToString());
                            }
                        }
                    }

                    beforValue_7_30 = value_7_30;
                    beforValue_7_60 = value_7_60;
                }
            }
        }


        /// <summary>
        /// SecondaryIndicator
        /// </summary>
        /// <param name="api"></param>
        /// <param name="candles"></param>
        /// <param name="market"></param>
        /// <param name="minuteCandleType"></param>
        /// <param name="leftMA7"></param>
        /// <param name="rightMA30"></param>
        /// <param name="rightMA60"></param>
        public static void SecondaryIndicator(IApi api, CandlesMinute candles, string market, MinuteCandleType minuteCandleType, int leftMA7, int rightMA30, int rightMA60)
        {
            lock (BidAskAlarmMA.Candles)
            {
                CandlesMinute(api, candles, market, minuteCandleType, rightMA60);
                
                if (candles.CandlesMinuteList != null && candles.CandlesMinuteList.Count >= rightMA60)
                {
                    CandlesMinute_MA_TradePrice(candles, leftMA7);
                    CandlesMinute_MA_TradePrice(candles, rightMA30);
                    CandlesMinute_MA_TradePrice(candles, rightMA60);

                    CandlesMinute_MA_Diff_TradePrice(candles, leftMA7, rightMA30);
                    CandlesMinute_MA_Diff_TradePrice(candles, leftMA7, rightMA60);
                }
            }
        }

        private static void CandlesMinute_MA_Diff_TradePrice(CandlesMinute candles, int countMA_Left, int countMA_Right)
        {
            string keyLeft = $"{countMA_Left}MA_TradePrice";
            string keyRight = $"{countMA_Right}MA_TradePrice";
            string key = $"{countMA_Left} - {countMA_Right}MA_TradePrice";

            var aa = candles.CandlesMinuteList?
                .Where(x => x.SecondaryIndicator.ContainsKey(keyLeft)
                        && x.SecondaryIndicator.ContainsKey(keyRight)
                        && !x.SecondaryIndicator.ContainsKey(key));

            aa?.AsParallel().WithDegreeOfParallelism(2).ForAll(item =>
            {
                item.SecondaryIndicator.Add(key, item.SecondaryIndicator[keyLeft] - item.SecondaryIndicator[keyRight]);
            });
        }
        private static void CandlesMinute_MA_TradePrice(CandlesMinute candles, int countMA)
        {
            string key = $"{countMA}MA_TradePrice";

            var aa = candles.CandlesMinuteList?.Where(x => !x.SecondaryIndicator.ContainsKey(key));

            aa?.AsParallel().WithDegreeOfParallelism(2).ForAll(item =>
            {
                var sel = candles.CandlesMinuteList?.Where(x => x.CandleDateTimeKst <= item.CandleDateTimeKst).OrderByDescending(o => o.CandleDateTimeKst).Take(countMA);

                if (sel != null && sel.Count() == countMA)
                {
                    item.SecondaryIndicator.Add(key, sel.Average(a => a.TradePrice));
                }
            });
        }
        private static void CandlesMinute(IApi api, CandlesMinute candles, string market, MinuteCandleType minuteCandleType, int rightMA60)
        {
            CandlesMinute? candlesMinute = null;
            DateTime? candleDateTimeKstMax = null;
            rightMA60 += 1;

            candlesMinute = api.CandlesMinute(market, minuteCandleType, DateTime.Now.AddMinutes((int)minuteCandleType * -1), rightMA60);

            if (candlesMinute.CandlesMinuteList != null && candles.CandlesMinuteList != null)
            {
                if (candles.CandlesMinuteList.Count == 0)
                    candleDateTimeKstMax = DateTime.Now.AddMinutes(candles.Unit * rightMA60 * -1);
                else
                {
                    candleDateTimeKstMax = candles.CandlesMinuteList?.Max(x => x.CandleDateTimeKst);
                    candleDateTimeKstMax ??= DateTime.Now.AddMinutes(candles.Unit * rightMA60 * -1);
                }

                var list = candlesMinute.CandlesMinuteList.Where(x => x.CandleDateTimeKst > candleDateTimeKstMax).OrderByDescending(o => o.CandleDateTimeKst).ToList();

                if (list != null && list.Count > 0)
                    candles.CandlesMinuteList?.InsertRange(0, list);

                if (candles.CandlesMinuteList != null && candles.CandlesMinuteList.Count > rightMA60)
                    candles.CandlesMinuteList.RemoveRange(candles.CandlesMinuteList.Count - (candles.CandlesMinuteList.Count - rightMA60), candles.CandlesMinuteList.Count - rightMA60);
            }
        }

        /// <summary>
        /// ReadStatusBidAskAlarmMA
        /// </summary>
        /// <returns></returns>
        public static StatusBidAskAlarmMA? ReadStatusBidAskAlarmMA(int settingID, int exchangeID, int unit, string market, int leftMA7, int rightMA30, int rightMA60, decimal stopLossRate, decimal rate)
        {
            try
            {
                string path = $"Run_{settingID}_{exchangeID}_{market}_StatusBidAskAlarmMA_{unit}_{leftMA7}_{rightMA30}_{rightMA60}_{stopLossRate:N3}_{rate:N3}.txt";

                if (File.Exists(path))
                {
                    using StreamReader streamReader = File.OpenText(path);
                    return JsonSerializer.Deserialize<StatusBidAskAlarmMA>(streamReader.ReadToEnd(), JsonSerializerOptions);
                }
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, exchangeID, null, null, market);
            }

            return new();
        }
        /// <summary>
        /// SaveStatusBidAskAlarmMA
        /// </summary>
        public static void SaveStatusBidAskAlarmMA(int settingID, StatusBidAskAlarmMA statusBidAskAlarmMA, int exchangeID, int unit, string market, int leftMA7, int rightMA30, int rightMA60, decimal stopLossRate, decimal rate)
        {
            try
            {
                string path = $"Run_{settingID}_{exchangeID}_{market}_StatusBidAskAlarmMA_{unit}_{leftMA7}_{rightMA30}_{rightMA60}_{stopLossRate:N3}_{rate:N3}.txt";
                using StreamWriter streamWriter = File.CreateText(path);
                streamWriter.Write(JsonSerializer.Serialize(statusBidAskAlarmMA, JsonSerializerOptions));
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, exchangeID, null, null, market);
            }
        }


        internal void BidAskAlarm_MA(string token, string MARKET, string ORDER_SIDE, string ORDER_TYPE, decimal BASE_PRICE, string MESSAGE_TITLE, string MESSAGE_BODY)
        {
            ServiceData data = new()
            {
                TransactionScope = false,
                Token = token,
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute($"Service.BidAskAlarmUser");
            data["1"].AddParameter(nameof(MARKET), Database.DbType.NVarChar, 50, MARKET);

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.Api.ExchangeID, null, null, MARKET);
                else 
                {
                    if (response.DataSet != null && response.DataSet.DataTables.Count > 0)
                    {
                        MARKET = MARKET == "KRW-BTC" ? "BTC" : "ETH";
                        foreach (var item in response.DataSet.DataTables[0].DataRows)
                            this.BidAskAlarm_MA_Push(token, item.Int("USER_ID") ?? 0, MARKET, ORDER_SIDE, ORDER_TYPE, BASE_PRICE, MESSAGE_TITLE, MESSAGE_BODY);
                    }
                }
            });
        }

        internal void BidAskAlarm_MA_Push(string token, int USER_ID, string market, string ORDER_SIDE, string ORDER_TYPE, decimal BASE_PRICE, string MESSAGE_TITLE, string MESSAGE_BODY)
        {
            if (USER_ID <= 0)
                return;

            ServiceData data = new()
            {
                ServiceName = "",
                TransactionScope = false,
                Token = token,
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute($"Service.BidAskAlarm_MA_{market}");
            data["1"].AddParameter(nameof(USER_ID), Database.DbType.Int, 3, USER_ID);
            data["1"].AddParameter(nameof(ORDER_SIDE), Database.DbType.NVarChar, 50, ORDER_SIDE);
            data["1"].AddParameter(nameof(ORDER_TYPE), Database.DbType.NVarChar, 50, ORDER_TYPE);
            data["1"].AddParameter(nameof(BASE_PRICE), Database.DbType.Decimal, 25, BASE_PRICE);
            data["1"].AddParameter(nameof(MESSAGE_TITLE), Database.DbType.NVarChar, 4000, MESSAGE_TITLE);
            data["1"].AddParameter(nameof(MESSAGE_BODY), Database.DbType.NVarChar, 4000, MESSAGE_BODY);
            data["1"].AddParameter("IMAGE_URL", Database.DbType.NVarChar, 4000, "None");

            Task.Run(() =>
            {
                Response response;

                response = this.ServiceRequest(data);

                if (response.Status != Status.OK)
                    response.Message?.WriteMessage(this.Api.ExchangeID, USER_ID, null, market);
            });
        }
    }
}