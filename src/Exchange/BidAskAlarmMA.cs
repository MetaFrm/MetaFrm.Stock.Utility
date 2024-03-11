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
        public CandlesMinute Candles { get; set; }

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
        /// ProfitRate
        /// </summary>
        public decimal ProfitRate { get; set; }

        /// <summary>
        /// IsRunReciveData
        /// </summary>
        public bool IsRunReciveData { get; set; }

        /// <summary>
        /// StatusBidAskAlarmMA
        /// </summary>
        private StatusBidAskAlarmMA StatusBidAskAlarmMA { get; set; } = new();

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };

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
        /// <param name="profitRate"></param>
        public BidAskAlarmMA(Task<AuthenticationState> authState, IApi api, string market, int unit, int leftMA7, int rightMA30, int rightMA60, decimal stopLossRate, decimal profitRate)
        {
            this.AuthState = authState;
            this.Api = api;
            this.Candles = new(market, api.ExchangeID, unit);
            this.LeftMA7 = leftMA7;
            this.RightMA30 = rightMA30;
            this.RightMA60 = rightMA60;
            this.StopLossRate = stopLossRate;
            this.ProfitRate = profitRate;
            this.MinuteCandleType = $"_{unit}".EnumParse<MinuteCandleType>();

            this.ReadStatusBidAskAlarmMA();
        }


        private DateTime lassRunDateTime = DateTime.MinValue;
        /// <summary>
        /// Run
        /// </summary>
        public void Run()
        {
            this.IsRunReciveData = true;

            Task.Run(async () =>
            {
                DateTime dateTime;
                StringBuilder stringBuilder = new();

                while (this.IsRunReciveData)
                {
                    await Task.Delay(2000);

                    dateTime = DateTime.Now;

                    if (this.lassRunDateTime == DateTime.MinValue
                        || this.Candles.CandlesMinuteList == null || this.Candles.CandlesMinuteList.Count < this.RightMA60
                        || (this.lassRunDateTime.Minute != dateTime.Minute && dateTime.Minute % this.Candles.Unit == 0))
                    {
                        this.lassRunDateTime = dateTime;

                        //다음 Unit에서 손절 평가
                        if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && this.StatusBidAskAlarmMA.IsBid)
                            this.StatusBidAskAlarmMA.IsBid = false;

                        System.Console.WriteLine($"실행 {this.Api.ExchangeID} {this.Candles.Market} {this.lassRunDateTime:yyyy-MM-dd HH:mm:ss}");

                        await Task.Delay(2000);

                        this.CandlesMinute();
                        this.Simulation();
                    }

                    if (this.StatusBidAskAlarmMA.CurrentStatus == "매수" && !this.StatusBidAskAlarmMA.IsBid)
                    {
                        Ticker? ticker = this.GetCurrentInfo();

                        //매수 상태이고 스탑로스 가격 보다 떨어지면 손절
                        if (ticker != null && this.StatusBidAskAlarmMA.StopLossPrice >= ticker.TradePrice)
                        {
                            this.StatusBidAskAlarmMA.CurrentStatus = "손절";

                            stringBuilder.Clear();
                            stringBuilder.AppendLine($"손절가격 : {this.StatusBidAskAlarmMA.StopLossPrice.PriceToString(this.Api.ExchangeID, this.Candles.Market ?? "")} -{(this.StopLossRate * 100M):N2}%");
                            stringBuilder.AppendLine($"매수가격 : {this.StatusBidAskAlarmMA.BidPrice.PriceToString(this.Api.ExchangeID, this.Candles.Market ?? "")}");
                            stringBuilder.ToString().WriteMessage(this.Api.ExchangeID, null, null, this.Candles.Market);

                            this.BidAskAlarm_MA(this.AuthState.Token(), this.Candles.Market ?? "", "ask", "limit", this.StatusBidAskAlarmMA.StopLossPrice.PriceRound(this.Api.ExchangeID, this.Candles.Market ?? "")
                                , $"{User.ExchangeName(this.Api.ExchangeID)} {this.Candles.Market} 손절신호", stringBuilder.ToString());
                        }
                    }
                }

                this.SaveStatusBidAskAlarmMA();
            });
        }
        internal Ticker? GetCurrentInfo()
        {
            if (this.Api == null) return null;
            if (this.Candles.Market == null) return null;

            var ticker = this.Api.Ticker(this.Candles.Market);
            if (ticker == null || ticker.TickerList == null || ticker.TickerList.Count < 1) return null;

            return ticker.TickerList[0];
        }

        private void CandlesMinute()
        {
            CandlesMinute? candlesMinute = null;
            DateTime? candleDateTimeKstMax = null;
            int rightMA60 = this.RightMA60 + 1;

            candlesMinute = this.Api.CandlesMinute(this.Candles.Market ?? "", this.MinuteCandleType, DateTime.Now.AddMinutes((int)this.MinuteCandleType * -1), rightMA60);

            if (this.Candles == null)
                this.Candles = candlesMinute;
            else
            {
                if (candlesMinute.CandlesMinuteList != null && this.Candles.CandlesMinuteList != null)
                {
                    if (this.Candles.CandlesMinuteList.Count == 0)
                        candleDateTimeKstMax = DateTime.Now.AddMinutes(this.Candles.Unit * rightMA60 * -1);
                    else
                    {
                        candleDateTimeKstMax = this.Candles.CandlesMinuteList?.Max(x => x.CandleDateTimeKst);
                        candleDateTimeKstMax ??= DateTime.Now.AddMinutes(this.Candles.Unit * rightMA60 * -1);
                    }

                    var list = candlesMinute.CandlesMinuteList.Where(x => x.CandleDateTimeKst > candleDateTimeKstMax).OrderByDescending(o => o.CandleDateTimeKst).ToList();

                    if (list != null && list.Count > 0)
                        this.Candles.CandlesMinuteList?.InsertRange(0, list);

                    if (this.Candles.CandlesMinuteList != null && this.Candles.CandlesMinuteList.Count > rightMA60)
                        this.Candles.CandlesMinuteList.RemoveRange(this.Candles.CandlesMinuteList.Count - (this.Candles.CandlesMinuteList.Count - rightMA60), this.Candles.CandlesMinuteList.Count - rightMA60);
                }
            }
        }

        private void Simulation()
        {
            if (this.Candles == null || this.Candles.CandlesMinuteList == null || this.Candles.CandlesMinuteList.Count < this.RightMA60)
                return;

            this.CandlesMinute_MA_TradePrice(this.LeftMA7);
            this.CandlesMinute_MA_TradePrice(this.RightMA30);
            this.CandlesMinute_MA_TradePrice(this.RightMA60);

            this.CandlesMinute_MA_Diff_TradePrice(this.LeftMA7, this.RightMA30);
            this.CandlesMinute_MA_Diff_TradePrice(this.LeftMA7, this.RightMA60);

            this.MA_Test(this.LeftMA7, this.RightMA30, this.RightMA60, this.StopLossRate, this.ProfitRate);
        }

        private void MA_Test(int leftMA7, int rightMA30, int rightMA60, decimal stopLossRate, decimal profitRate)
        {
            if (this.Candles.CandlesMinuteList == null)
                return;

            var data = this.Candles.CandlesMinuteList.OrderByDescending(o => o.CandleDateTimeKst).Take(2).OrderBy(o => o.CandleDateTimeKst);

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
                        if (this.StatusBidAskAlarmMA.CurrentStatus == "매수")
                        {
                            //이전 값보다 작아 졌을떄 => 간격이 좁아지면 => 매도를 한다 (보정값으로으로 미세 조정 필요★)
                            // 5 < 6
                            if (value_7_30 < beforValue_7_30 && item.TradePrice > (this.StatusBidAskAlarmMA.BidPrice * (1M + profitRate)))
                            {
                                this.StatusBidAskAlarmMA.AskPrice = item.TradePrice;
                                this.StatusBidAskAlarmMA.CurrentStatus = "";

                                stringBuilder.Clear();
                                stringBuilder.AppendLine($"매수가격 : {this.StatusBidAskAlarmMA.StopLossPrice.PriceToString(this.Api.ExchangeID, this.Candles.Market ?? "")}");
                                stringBuilder.AppendLine($"매도가격 : {this.StatusBidAskAlarmMA.AskPrice.PriceToString(this.Api.ExchangeID, this.Candles.Market ?? "")}");
                                stringBuilder.ToString().WriteMessage(this.Api.ExchangeID, null, null, this.Candles.Market);

                                this.BidAskAlarm_MA(this.AuthState.Token(), this.Candles.Market ?? "", "ask", "limit", item.TradePrice.PriceRound(this.Api.ExchangeID, this.Candles.Market ?? "")
                                    , $"{User.ExchangeName(this.Api.ExchangeID)} {this.Candles.Market} 매도신호", stringBuilder.ToString());
                            }
                        }

                        //손절 상태가 되면 상숭 후에 다시 내려 갔을떄 진입
                        if (this.StatusBidAskAlarmMA.CurrentStatus == "손절")
                            this.StatusBidAskAlarmMA.CurrentStatus = "";
                    }
                    else if (value_7_30 < 0)
                    {
                        //하락 중

                        //포지션이 없을때
                        if (this.StatusBidAskAlarmMA.CurrentStatus == "")
                        {
                            //이전 값보다 커졌을떄 => 간격이 좁아지면 => 매수를 한다 (보정값으로으로 미세 조정 필요★)
                            // -5 > -6
                            if (value_7_30 > beforValue_7_30)
                            {
                                this.StatusBidAskAlarmMA.CurrentStatus = "매수";
                                this.StatusBidAskAlarmMA.BidPrice = item.TradePrice;

                                this.StatusBidAskAlarmMA.StopLossPrice = (this.StatusBidAskAlarmMA.BidPrice * (1M - stopLossRate)).PriceRound(this.Api.ExchangeID, this.Candles.Market ?? "");//손절

                                this.StatusBidAskAlarmMA.IsBid = true;

                                this.StatusBidAskAlarmMA.StopLossPrice = item.TradePrice * (1M - stopLossRate);//손절

                                stringBuilder.Clear();
                                stringBuilder.AppendLine($"매수가격 : {this.StatusBidAskAlarmMA.BidPrice.PriceToString(this.Api.ExchangeID, this.Candles.Market ?? "")}");
                                stringBuilder.AppendLine($"손절가격 : {this.StatusBidAskAlarmMA.StopLossPrice.PriceToString(this.Api.ExchangeID, this.Candles.Market ?? "")} {(stopLossRate * 100M * -1M):N2}%");
                                stringBuilder.ToString().WriteMessage(this.Api.ExchangeID, null, null, this.Candles.Market);

                                this.BidAskAlarm_MA(this.AuthState.Token(), this.Candles.Market ?? "", "bid", "limit", item.TradePrice.PriceRound(this.Api.ExchangeID, this.Candles.Market ?? "")
                                    , $"{User.ExchangeName(this.Api.ExchangeID)} {this.Candles.Market} 매수신호", stringBuilder.ToString());
                            }
                        }
                    }

                    beforValue_7_30 = value_7_30;
                    beforValue_7_60 = value_7_60;
                }
            }
        }



        private void CandlesMinute_MA_Diff_TradePrice(int countMA_Left, int countMA_Right)
        {
            string keyLeft = $"{countMA_Left}MA_TradePrice";
            string keyRight = $"{countMA_Right}MA_TradePrice";
            string key = $"{countMA_Left} - {countMA_Right}MA_TradePrice";

            var aa = this.Candles?.CandlesMinuteList?
                .Where(x => x.SecondaryIndicator.ContainsKey(keyLeft)
                        && x.SecondaryIndicator.ContainsKey(keyRight)
                        && !x.SecondaryIndicator.ContainsKey(key));

            aa?.AsParallel().WithDegreeOfParallelism(2).ForAll(item =>
            {
                item.SecondaryIndicator.Add(key, item.SecondaryIndicator[keyLeft] - item.SecondaryIndicator[keyRight]);
            });
        }
        private void CandlesMinute_MA_TradePrice(int countMA)
        {
            string key = $"{countMA}MA_TradePrice";

            var aa = this.Candles?.CandlesMinuteList?.Where(x => !x.SecondaryIndicator.ContainsKey(key));

            aa?.AsParallel().WithDegreeOfParallelism(2).ForAll(item =>
            {
                var sel = this.Candles?.CandlesMinuteList?.Where(x => x.CandleDateTimeKst <= item.CandleDateTimeKst).OrderByDescending(o => o.CandleDateTimeKst).Take(countMA);

                if (sel != null && sel.Count() == countMA)
                {
                    item.SecondaryIndicator.Add(key, sel.Average(a => a.TradePrice));
                }
            });
        }


        /// <summary>
        /// ReadStatusBidAskAlarmMA
        /// </summary>
        /// <returns></returns>
        public void ReadStatusBidAskAlarmMA()
        {
            try
            {
                string path = $"Run_{this.Api.ExchangeID}_{this.Candles.Market}_StatusBidAskAlarmMA_{this.Candles.Unit}_{this.LeftMA7}_{this.RightMA30}_{this.RightMA60}_{this.StopLossRate}_{this.ProfitRate}.txt";

                if (File.Exists(path))
                {
                    using StreamReader streamReader = File.OpenText(path);
                    StatusBidAskAlarmMA? result = JsonSerializer.Deserialize<StatusBidAskAlarmMA>(streamReader.ReadToEnd(), this.jsonSerializerOptions);

                    if (result != null)
                        this.StatusBidAskAlarmMA = result;
                }
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, this.Api.ExchangeID, null, null, this.Candles.Market);
            }
        }
        /// <summary>
        /// SaveStatusBidAskAlarmMA
        /// </summary>
        public void SaveStatusBidAskAlarmMA()
        {
            try
            {
                string path = $"Run_{this.Api.ExchangeID}_{this.Candles.Market}_StatusBidAskAlarmMA_{this.Candles.Unit}_{this.LeftMA7}_{this.RightMA30}_{this.RightMA60}_{this.StopLossRate}_{this.ProfitRate}.txt";
                using StreamWriter streamWriter = File.CreateText(path);
                streamWriter.Write(JsonSerializer.Serialize(this.StatusBidAskAlarmMA, this.jsonSerializerOptions));
            }
            catch (Exception ex)
            {
                ex.WriteMessage(true, this.Api.ExchangeID, null, null, this.Candles.Market);
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