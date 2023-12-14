using MetaFrm.Control;
using MetaFrm.Stock.Console;
using System.Collections.Specialized;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MetaFrm.Stock.Exchange.Bithumb
{
    /// <summary>
    /// BithumbApi
    /// </summary>
    public class BithumbApi : IApi, IAction, IDisposable
    {
        private readonly object _lock = new();
        private HttpClient? HttpClient { get; set; }
        double IApi.TimeoutMilliseconds
        {
            get
            {
                if (this.HttpClient != null)
                    return this.HttpClient.Timeout.TotalMilliseconds;
                else
                    return 0;
            }
            set
            {
                if (this.HttpClient != null)
                    this.HttpClient.Timeout = TimeSpan.FromMilliseconds(value);
            }
        }

        int IApi.ExchangeID { get; set; } = 2;
        string IApi.AccessKey { get; set; } = "";
        string IApi.SecretKey { get; set; } = "";
        internal string BaseUrl { get; set; } = "https://api.bithumb.com";
        private string BaseWebSocketUrl { get; set; } = "wss://pubwss.bithumb.com/pub/ws";

        private double BaseTimeoutMin { get; set; } = 1000;
        private int CallCount = 0;
        private int BaseTimeoutDecreaseMod { get; set; } = 200;

        private readonly int SocketCloseTimeOutSeconds = 60 + 30;

        /// <summary>
        /// Action event Handler입니다.
        /// </summary>
        public event MetaFrmEventHandler? Action;

        private static bool IsRunTickerFromWebSocket = false;

        private bool IsDispose = false;
        private readonly bool IsRunOrderResultFromWebSocket = false;

        /// <summary>
        /// BithumbAPI
        /// </summary>
        public BithumbApi(bool runTickerFromWebSocket, bool runOrderResultFromWebSocket)
        {
            this.CreateHttpClient(null);

            if (runTickerFromWebSocket && !IsRunTickerFromWebSocket)
            {
                IsRunTickerFromWebSocket = true;
                this.RunTickerFromWebSocket();
            }

            this.IsRunOrderResultFromWebSocket = runOrderResultFromWebSocket;

            if (runOrderResultFromWebSocket && false)
                this.RunOrderResultFromWebSocket();
        }
        private void CreateHttpClient(TimeSpan? timeSpan)
        {
            this.HttpClient?.Dispose();
            this.HttpClient = null;
            this.HttpClient = new(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            if (timeSpan != null)
                this.HttpClient.Timeout = (TimeSpan)timeSpan;
        }

        private string CallAPI_Public(string url, NameValueCollection? nvc, int reTryCount = 2)
        {
            string? result = "";

            try
            {
                lock (this._lock)
                    if (this.HttpClient != null)
                    {
                        Thread.Sleep(20);

                        var response = this.HttpClient.GetAsync(url + (nvc == null ? "" : ("?" + ToQueryString(nvc)))).Result;
                        result = response.Content.ReadAsStringAsync().Result;

                        //최소보다 크거나 연속으로 정상적인 호출 횟수가 BaseTimeoutDecreaseMod에 도달하면
                        //100 Milliseconds 감소
                        if (this.HttpClient.Timeout.TotalMilliseconds > this.BaseTimeoutMin && this.CallCount % this.BaseTimeoutDecreaseMod == 0)
                        {
                            this.CreateHttpClient(TimeSpan.FromMilliseconds(this.HttpClient.Timeout.TotalMilliseconds - 100));
                            this.CallCount = 0;
                        }
                    }

                if (result.Contains("빗썸 서비스 점검 중"))
                    return "";
                else
                    return result;
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);

                if (this.HttpClient != null)
                    this.CreateHttpClient(TimeSpan.FromMilliseconds(this.HttpClient.Timeout.TotalMilliseconds + 100));//오류 발생하면 100 Milliseconds 증가

                this.CallCount = 0;

                if (reTryCount > 0)
                    return this.CallAPI_Public(url, nvc, reTryCount - 1);
                else
                    return "";
            }
        }
        private string CallAPI_Private_WithParam(string url, NameValueCollection nvc, int reTryCount = 2)
        {
            string? result = "";

            try
            {
                lock (this._lock)
                    if (this.HttpClient != null)
                    {
                        Thread.Sleep(70);

                        var requestMessage = this.BuildHttpRequestMessage(url, nvc);
                        var response = this.HttpClient.SendAsync(requestMessage).Result;
                        result = response.Content.ReadAsStringAsync().Result;

                        //최소보다 크거나 연속으로 정상적인 호출 횟수가 BaseTimeoutDecreaseMod에 도달하면
                        //100 Milliseconds 감소
                        if (this.HttpClient.Timeout.TotalMilliseconds > this.BaseTimeoutMin && this.CallCount % this.BaseTimeoutDecreaseMod == 0)
                        {
                            this.CreateHttpClient(TimeSpan.FromMilliseconds(this.HttpClient.Timeout.TotalMilliseconds - 100));
                            this.CallCount = 0;
                        }
                    }

                if (result.Contains("빗썸 서비스 점검 중"))
                    return "";
                else
                    return result;
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);

                if (this.HttpClient != null)
                    this.CreateHttpClient(TimeSpan.FromMilliseconds(this.HttpClient.Timeout.TotalMilliseconds + 100));//오류 발생하면 100 Milliseconds 증가

                this.CallCount = 0;

                if (reTryCount > 0)
                    return this.CallAPI_Private_WithParam(url, nvc, reTryCount - 1);
                else
                    return "";
            }
        }
        private static string ToQueryString(NameValueCollection? nvc)
        {
            if (nvc == null) { return ""; }

            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key) ?? Array.Empty<string>()
                         select string.Format("{0}={1}", key, value))
                .ToArray();

            return string.Join("&", array);
        }
        private HttpRequestMessage BuildHttpRequestMessage(string url, NameValueCollection? nvc = null)
        {
            string endPoint = url.Replace(this.BaseUrl, "");
            string postData = (nvc == null) ? "" : ToQueryString(nvc) + "&endpoint=" + Uri.EscapeDataString(endPoint); ;
            long nonce = MicroSecTime();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            requestMessage.Headers.Add("Api-Key", ((IApi)this).AccessKey);
            requestMessage.Headers.Add("Api-Sign", Convert.ToBase64String(StringToByte(Hash_HMAC(((IApi)this).SecretKey, endPoint + (char)0 + postData + (char)0 + nonce.ToString()))));
            requestMessage.Headers.Add("Api-Nonce", nonce.ToString());
            requestMessage.Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

            return requestMessage;
        }
        private static long MicroSecTime()
        {
            long nEpochTicks = new DateTime(1970, 1, 1).Ticks;
            DateTime DateTimeNow = DateTime.UtcNow;
            long nNowTicks = DateTimeNow.Ticks;
            long nowMiliseconds = DateTimeNow.Millisecond;
            long nUnixTimeStamp = ((nNowTicks - nEpochTicks) / TimeSpan.TicksPerSecond);
            string sNonce = nUnixTimeStamp.ToString() + nowMiliseconds.ToString("D03");
            return (Convert.ToInt64(sNonce));
        }
        private static string Hash_HMAC(string sKey, string sData)
        {
            byte[] rgbyKey = Encoding.UTF8.GetBytes(sKey);

            using var hmacsha512 = new HMACSHA512(rgbyKey);
            hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(sData));

            if (hmacsha512.Hash != null)
                return (ByteToString(hmacsha512.Hash));
            else
                return "";
        }
        private static string ByteToString(byte[] rgbyBuff)
        {
            string sHexStr = "";

            for (int nCnt = 0; nCnt < rgbyBuff.Length; nCnt++)
            {
                sHexStr += rgbyBuff[nCnt].ToString("x2"); // Hex format
            }
            return (sHexStr);
        }
        private static byte[] StringToByte(string sStr)
        {
            return Encoding.UTF8.GetBytes(sStr);
        }
        //public enum BithumbOrderType { bid, ask }
        //public enum BithumbTransactionType { all = 0, bid_executed = 1, ask_executed = 2, withdrawal_processing = 3, deposit = 4, withdrawal = 5, KRW_deposit = 9 }
        static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            dtDateTime = dtDateTime.AddMilliseconds(timestamp / 1000).ToLocalTime();

            return dtDateTime;
        }
        private static Models.Error? GetError(string? code, string? customMessage, [CallerMemberName] string methodName = "")
        {
            customMessage = code switch
            {
                "5100" => "Bad Request",
                "5200" => "Not Member",
                "5300" => "Invalid Apikey",
                "5302" => "Method Not Allowed",
                "5400" => "Database Fail",
                "5500" => "Invalid Parameter",
                "5600" => customMessage,
                "5900" => "Unknown Error",
                _ => "",
            };

            $"{methodName} : {code} {customMessage}".WriteMessage();

            return new Models.Error()
            {
                Code = code,
                Message = code switch
                {
                    "5100" => "Bad Request",
                    "5200" => "Not Member",
                    "5300" => "Invalid Apikey",
                    "5302" => "Method Not Allowed",
                    "5400" => "Database Fail",
                    "5500" => "Invalid Parameter",
                    "5600" => customMessage,
                    "5900" => "Unknown Error",
                    _ => "",
                }
            };
        }
        private Models.Error GetError(Exception ex, bool isDetail)
        {
            ex.WriteMessage(isDetail, ((IApi)this).ExchangeID);

            return new() { Message = ex.Message, Code = ex.ToString() };
        }


        #region "자산"
        //internal List<AccountInfo>? AccountInfoCurrent { get; set; }
        //bool IsFirstAccount = true;
        //private void GetAccountInfo(string order_currency)
        //{
        //    string? tmp;
        //    BithumbData? list;
        //    Models.Account result = new();
        //    AccountInfo? accountInfo = null;

        //    this.AccountInfoCurrent ??= new();

        //    if (this.AccountInfoCurrent.Any(x => x.OrderCurrency == order_currency))
        //        return;

        //    tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/info/account", new NameValueCollection { { "order_currency", order_currency } });

        //    if (string.IsNullOrEmpty(tmp)) return;

        //    list = JsonSerializer.Deserialize<BithumbData>(tmp);

        //    if (list == null) return;
        //    if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return; }
        //    if (list.Data == null) return;

        //    accountInfo = new();
        //    foreach (var item in list.Data)
        //    {
        //        switch (item.Key)
        //        {
        //            case "created":
        //                if (item.Value.ToTryLong(out long long_1))
        //                    accountInfo.Created = long_1;
        //                break;
        //            case "account_id":
        //                accountInfo.AccountID = item.Value;
        //                break;
        //            case "order_currency":
        //                accountInfo.OrderCurrency = item.Value;
        //                break;
        //            case "payment_currency":
        //                accountInfo.PaymentCurrency = item.Value;
        //                break;
        //            case "trade_fee":
        //                if (item.Value.ToTryDecimal(out decimal decimal_1))
        //                    accountInfo.TradeFee = decimal_1;
        //                break;
        //            case "balance":
        //                if (item.Value.ToTryDecimal(out decimal decimal_2))
        //                    accountInfo.Balance = decimal_2;
        //                break;
        //        }
        //    }
        //    this.AccountInfoCurrent.Add(accountInfo);
        //}

        /// <summary>
        /// 전체 계좌 조회
        /// </summary>
        /// <returns></returns>
        Models.Account IApi.Account()
        {
            string? tmp;
            string[]? tmps;
            string currency = "";
            BithumbData? list;
            Models.Account result = new();
            Models.Account? account = null;

            try
            {
                //if (this.IsFirstAccount)
                //{
                //    this.GetAccountInfo("KRW");
                //    this.GetAccountInfo("BTC");
                //    this.IsFirstAccount = false;
                //}

                tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/info/balance", new NameValueCollection { { "currency", "ALL" } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbData>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                foreach (var item in list.Data)
                {
                    tmps = item.Key.Split('_');

                    if (tmps.Length <= 0 || tmps[0] == "xcoin" || item.Key.EndsWith("_old"))
                        continue;

                    if (tmps[0] == "total")
                    {
                        if (tmps.Length == 3)
                            continue;

                        currency = tmps[1].ToUpper();
                        account = new() { Currency = currency };
                    }

                    if (account != null)
                    {
                        if (tmps.Length == 2)
                        {
                            switch (tmps[0])
                            {
                                case "available":
                                    if (item.Value.ToTryDecimal(out decimal decimal_2))
                                        account.Balance = decimal_2;

                                    result.AccountList.Add(account);
                                    account = null;
                                    break;
                            }
                        }
                        else if (tmps.Length == 3)
                        {
                            switch ($"{tmps[0]}_{tmps[1]}")
                            {
                                case "in_use":
                                    if (item.Value.ToTryDecimal(out decimal decimalTmp))
                                        account.Locked = decimalTmp;
                                    break;
                            }
                        }
                    }

                    //result.AccountList.Add(new()
                    //{
                    //    Currency = item.Currency,
                    //    Balance = item.Balance,
                    //    Locked = item.Locked,
                    //    AvgKrwBuyPrice = item.AvgKrwBuyPrice,
                    //    Modified = item.Modified,
                    //    UnitCurrency = item.UnitCurrency,
                    //});
                }

                result.AccountList = result.AccountList.Where(x => x.Balance + x.Locked > 0).ToList();
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }
        #endregion


        #region "주문"
        Models.OrderChance IApi.OrderChance(string market)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.OrderChance result = new();
            BithumbData? list;
            AccountInfo? accountInfo = null;
            Models.Account account;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/info/account", new NameValueCollection { { "order_currency", market }, { "payment_currency", marketGroup } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbData>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                accountInfo = new();
                foreach (var item in list.Data)
                {
                    switch (item.Key)
                    {
                        case "created":
                            if (item.Value.ToTryLong(out long long_1))
                                accountInfo.Created = long_1;
                            break;
                        case "account_id":
                            accountInfo.AccountID = item.Value;
                            break;
                        case "order_currency":
                            accountInfo.OrderCurrency = item.Value;
                            break;
                        case "payment_currency":
                            accountInfo.PaymentCurrency = item.Value;
                            break;
                        case "trade_fee":
                            if (item.Value.ToTryDecimal(out decimal decimal_1))
                                accountInfo.TradeFee = decimal_1;
                            break;
                        case "balance":
                            if (item.Value.ToTryDecimal(out decimal decimal_2))
                                accountInfo.Balance = decimal_2;
                            break;
                    }
                }

                result.BidFee = accountInfo.TradeFee;
                result.AskFee = accountInfo.TradeFee;
                result.Market = new()
                {
                    Bid = new(),
                    Ask = new()
                };

                account = ((IApi)this).Account();

                if (account != null && account.AccountList != null)
                {
                    var a = account.AccountList.SingleOrDefault(x => x.Currency == marketGroup);

                    if (a != null)
                        result.BidAccount = new()
                        {
                            Currency = marketGroup,
                            Balance = a.Balance,
                            Locked = a.Locked,
                        };

                    var b = account.AccountList.SingleOrDefault(x => x.Currency == market);

                    if (b != null)
                        result.AskAccount = new()
                        {
                            Currency = market,
                            Balance = b.Balance,
                            Locked = b.Locked,
                        };
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        private readonly List<BithumbOrder> BithumbOrders = new();
        Models.Order IApi.Order(string market, string sideName, string uuid)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.Order result = new();
            BithumbDataJsonElement? list;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/info/order_detail", new NameValueCollection { { "order_id", uuid }, { "order_currency", market }, { "payment_currency", marketGroup } });

//                tmp = @"{""status"":""0000"",""data"":{""order_date"":""1697525084971851"",""type"":""ask"",""order_status"":""Pending"",""order_currency"":""SSX"",""payment_currency"":""KRW"",""watch_price"":""0"",""order_price"":""26.86"",""order_qty"":""100"",""cancel_date"":"""",""cancel_type"":"""",""contract"":[
//        {
//          ""transaction_date"": ""1572497603902030"",
//          ""price"": ""8601000"",
//          ""units"": ""0.005"",
//          ""fee_currency"": ""KRW"",
//          ""fee"": ""107.51"",
//          ""total"": ""43005""
//        },
//        {
//          ""transaction_date"": ""1572497603850325"",
//          ""price"": ""8600000"",
//          ""units"": ""0.002"",
//          ""fee_currency"": ""KRW"",
//          ""fee"": ""43"",
//          ""total"": ""17200""
//        }
//      ]}}
//";
                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDataJsonElement>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                result.UUID = uuid;
                foreach (var item in list.Data)
                {
                    switch (item.Key)
                    {
                        case "type":
                            result.Side = item.Value.GetString();
                            break;
                        case "order_price":
                            tmp = item.Value.GetString();
                            if (tmp != null)
                                result.Price = tmp.ToDecimal();
                            break;
                        case "order_status":
                            switch (item.Value.GetString())
                            {
                                case "Pending":
                                    result.State = "wait";
                                    break;
                                case "Cancel":
                                    result.State = "cancel";
                                    break;
                                case "Completed":
                                    result.State = "done";
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case "order_currency":
                            result.Market = item.Value.GetString();
                            break;
                        case "payment_currency":
                            result.Market = $"{item.Value.GetString()}-{result.Market}";
                            break;
                        case "order_date":
                            tmp = item.Value.GetString();
                            if (tmp != null)
                                result.CreatedAt = ConvertFromUnixTimestamp(tmp.ToDouble());
                            break;
                        case "order_qty":
                            tmp = item.Value.GetString();
                            if (tmp != null)
                                result.Volume = tmp.ToDecimal();
                            result.RemainingVolume = result.Volume;
                            break;
                        case "contract":
                            if (item.Value.ValueKind == JsonValueKind.Array && item.Value is JsonElement element)
                            {
                                tmp = element.GetRawText();

                                if (!tmp.IsNullOrEmpty())
                                {
                                    var contract = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(tmp);

                                    if (contract != null)
                                    {
                                        result.Trades = new();
                                        foreach (var contractItem in contract)
                                        {
                                            Models.Trade trade = new()
                                            {
                                                Market = result.Market,
                                                Side = result.Side
                                            };
                                            foreach (var contractDetail in contractItem)
                                            {
                                                switch (contractDetail.Key)
                                                {
                                                    case "transaction_date":
                                                        trade.UUID = contractDetail.Value;
                                                        trade.CreatedAt = ConvertFromUnixTimestamp(contractDetail.Value.ToDouble());
                                                        break;
                                                    case "price":
                                                        trade.Price = contractDetail.Value.ToDecimal();
                                                        trade.Funds = contractDetail.Value.ToDecimal();
                                                        break;
                                                    case "units":
                                                        trade.Volume = contractDetail.Value.ToDecimal();
                                                        break;
                                                    case "fee":
                                                        trade.Fee = contractDetail.Value.ToDecimal();
                                                        break;
                                                }
                                            }
                                            result.Trades.Add(trade);
                                        }
                                    }
                                }
                            }
                            result.RemainingVolume -= result.Trades != null && result.Trades.Any() ? result.Trades.Sum(x => x.Volume) : 0;
                            result.ExecutedVolume = result.Volume - result.RemainingVolume;
                            result.PaidFee = result.Trades != null && result.Trades.Any() ? result.Trades.Sum(x => x.Fee) : 0;
                            result.RemainingFee = result.ExecutedVolume > 0 ? (result.PaidFee * result.RemainingVolume) / result.ExecutedVolume : 0;
                            break;
                    }
                }

                this.OrderExecuteActionInvoke(result);
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }


        Models.Order IApi.AllOrder(string market, string order_by)
        {
            Models.Account account;
            List<Models.Order> orderList;
            Models.Order order;
            Models.Markets? markets;

            if (market == "ALL")
            {
                account = (this as IApi).Account();

                orderList = new List<Models.Order>();

                if (account.AccountList != null)
                {
                    markets = ((IApi)this).Markets();

                    foreach (Models.Account account1 in account.AccountList)
                    {
                        if (account1.Currency != "KRW" && (markets.MarketList != null && markets.MarketList.Any(x=> x.Market == $"KRW-{account1.Currency}")))
                        {
                            order = ((IApi)this).AllOrder($"KRW-{account1.Currency}", 0, "");

                            if (order != null && order.OrderList != null && order.OrderList.Count > 0)
                                orderList.AddRange(order.OrderList);
                        }
                    }
                }

                return new Models.Order()
                {
                    OrderList = orderList
                };
            }
            else
            {
                return ((IApi)this).AllOrder(market, 0, "");
            }
        }
        Models.Order IApi.AllOrder(string market, int page, string order_by)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.Order result = new();
            BithumbDatas? list;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/info/orders", new NameValueCollection { { "order_id", "" }, { "count", "1000" }, { "order_currency", market }, { "payment_currency", marketGroup } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDatas>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if ((list.Code != "5600" && list.Message != "거래 진행중인 내역이 존재하지 않습니다.") && list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Datas == null) return result;

                result.OrderList = new();

                foreach (var itemOrder in list.Datas)
                {
                    if (itemOrder == null)
                        continue;

                    Models.Order order = new();
                    foreach (var item in itemOrder)
                    {
                        switch (item.Key)
                        {
                            case "order_id":
                                order.UUID = item.Value; break;
                            case "type":
                                order.Side = item.Value; break;
                            case "price":
                                if (item.Value.ToTryDecimal(out decimal decimal_1))
                                    order.Price = decimal_1; break;
                            case "order_currency":
                                order.Market = item.Value; break;
                            case "payment_currency":
                                order.Market = $"{item.Value}-{order.Market}"; break;
                            case "units":
                                if (item.Value.ToTryDecimal(out decimal decimal_2))
                                    order.Volume = decimal_2; break;
                            case "units_remaining":
                                if (item.Value.ToTryDecimal(out decimal decimal_3))
                                {
                                    order.RemainingVolume = decimal_3;
                                    order.ExecutedVolume = order.Volume - decimal_3;
                                }
                                break;
                        }
                    }
                    order.State = "wait";
                    result.OrderList.Add(order);

                    this.OrderExecuteActionInvoke(order);
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        private void OrderExecuteActionInvoke(Models.Order result)
        {
            if (result.ExecutedVolume > 0 && this.IsRunOrderResultFromWebSocket)
            {
                lock (this.BithumbOrders)
                {
                    if (result.State != "cancel")
                        if (!this.BithumbOrders.Any(x => x.Order.UUID == result.UUID) && result.State != "done")
                        {
                            this.BithumbOrders.Add(new(result));
                            this.Action?.Invoke(this, new() { Action = "OrderExecution", Value = result });
                            System.Console.ForegroundColor = ConsoleColor.Red;
                            System.Console.WriteLine($"if (!this.BithumbOrders.Any(x => x.Order.UUID == result.UUID)) {result.State} {result.UUID} {result.ExecutedVolume}");
                            System.Console.ResetColor();
                        }
                        else
                        {
                            var item = this.BithumbOrders.SingleOrDefault(x => x.Order.UUID == result.UUID && x.Order.ExecutedVolume != result.ExecutedVolume);
                            if (item != null)
                            {
                                this.BithumbOrders.Remove(item);

                                if (result.State != "done")
                                    this.BithumbOrders.Add(new(result));

                                this.Action?.Invoke(this, new()
                                {
                                    Action = "OrderExecution",
                                    Value = new Models.Order()
                                    {
                                        UUID = result.UUID,
                                        Side = result.Side,
                                        Price = result.Price,
                                        State = result.State,
                                        Market = result.Market,
                                        ExecutedVolume = result.ExecutedVolume - item.Order.ExecutedVolume,
                                    }
                                });

                                System.Console.ForegroundColor = ConsoleColor.Red;
                                System.Console.WriteLine($"if (item != null) {result.State} {result.UUID} {result.ExecutedVolume}");
                                System.Console.ResetColor();
                            }
                        }

                    List<BithumbOrder> delete = new();
                    foreach (var item in this.BithumbOrders)
                    {
                        if ((item.Order.State == "done" || item.Order.State == "cancel") || item.InsertDateTime < DateTime.Now.AddDays(-15))
                            delete.Add(item);
                    }

                    foreach (var item in delete)
                    {
                        this.BithumbOrders.Remove(item);
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine($"this.BithumbOrders.Remove(item); {item.Order.State} {item.InsertDateTime} {item.Order.UUID} {item.Order.ExecutedVolume}");
                        System.Console.ResetColor();
                    }
                }
            }
        }


        Models.Order IApi.CancelOrder(string market, string sideName, string uuid)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.Order result = new();
            BithumbData? list;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/trade/cancel", new NameValueCollection { { "type", sideName }, { "order_id", uuid }, { "order_currency", market }, { "payment_currency", marketGroup } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbData>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.Order IApi.MakeOrder(string market, Models.OrderSide side, decimal volume, decimal price, Models.OrderType ord_type)
        {
            string? tmp = "";
            string[] tmps;
            string marketGroup;
            Models.Order result = new();
            BithumbData? list;

            try
            {
                if (side == Models.OrderSide.ask && ord_type == Models.OrderType.price)
                    throw new Exception("주문종류와 주문타입이 일치하지 않습니다(요청한 주문 => 주문종류:매도, 주문타입:시장가 매수).");
                if (side == Models.OrderSide.bid && ord_type == Models.OrderType.market)
                    throw new Exception("주문종류와 주문타입이 일치하지 않습니다(요청한 주문 => 주문종류:매수, 주문타입:시장가 매도).");

                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                volume = Math.Round(volume, 4);

                //지정가
                if (ord_type == Models.OrderType.limit)
                {
                    tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/trade/place"
                        , new NameValueCollection { { "order_currency", market }
                            , { "payment_currency", marketGroup }
                            , { "units", $"{volume}" }
                            , { "price", price >= 1000 ? price.ToString("#########0") : price.ToString("#########0.0###") }
                            , { "type", side.ToString() } });
                }
                //시장가 매수
                if (ord_type == Models.OrderType.price)
                {
                    Models.Ticker ticker = ((IApi)this).Ticker($"{marketGroup}-{market}");

                    if (ticker == null)
                    {
                        result.Error = GetError("5600", "Ticker error");
                        return result;
                    }

                    if (ticker.TickerList == null)
                    {
                        result.Error = GetError("5600", "TickerList error");
                        return result;
                    }

                    var ticker1 = ticker.TickerList.SingleOrDefault(x => x.Market == $"{marketGroup}-{market}");

                    if (ticker1 == null)
                    {
                        result.Error = GetError("5600", "Ticker1 error");
                        return result;
                    }

                    volume = price / ticker1.TradePrice;
                    volume = Math.Round(volume, 4);

                    tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/trade/market_buy"
                        , new NameValueCollection { { "order_currency", market }, { "payment_currency", marketGroup }, { "units", $"{volume}" } });
                }
                //시장가 매도
                if (ord_type == Models.OrderType.market)
                {
                    tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/trade/market_sell"
                        , new NameValueCollection { { "order_currency", market }, { "payment_currency", marketGroup }, { "units", $"{volume}" } });
                }

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbData>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }

                result.Market = $"{marketGroup}-{market}";
                result.Side = side.ToString();
                result.UUID = list.OrderID;
                result.Volume = volume;
                result.Price = price;
                result.RemainingVolume = volume;
                result.State = "wait";
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        private ClientWebSocket? WebSocketOrder;
        private DateTime RunOrderResultFromWebSocketDateTime;
        private async void RunOrderResultFromWebSocket()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000);

                    if (!((IApi)this).AccessKey.IsNullOrEmpty() && !((IApi)this).SecretKey.IsNullOrEmpty())
                        break;
                }

                this.RunOrderResultFromWebSocketDateTime = DateTime.Now;
                $"Start : RunOrderResultFromWebSocket".WriteMessage(((IApi)this).ExchangeID);
                while (true)
                {
                    await Task.Delay(2000);

                    if (this.IsDispose) break;

                    if (this.WebSocketOrder == null)
                    {
                        this.RunOrderResultFromWebSocketDateTime = DateTime.Now;
                        this.WebSocketOrder = new ClientWebSocket();
                        this.OrderResultFromWebSocket(((IApi)this).AccessKey, ((IApi)this).SecretKey);
                    }

                    if ((DateTime.Now - this.RunOrderResultFromWebSocketDateTime).TotalSeconds >= this.SocketCloseTimeOutSeconds * 2)
                    {
                        this.OrderResultFromWebSocketClose();
                        $"OrderResultFromWebSocketClose(RunOrderResultFromWebSocket)\"".WriteMessage(((IApi)this).ExchangeID);
                    }
                }
            });
        }
        private async void OrderResultFromWebSocket(string accessKey, string secretKey)
        {
            try
            {
                if (this.WebSocketOrder == null)
                    return;

                string endPoint = "";//url.Replace(this.BaseUrl, "");
                long nonce = MicroSecTime();

                this.WebSocketOrder.Options.SetRequestHeader("Api-Key", accessKey);
                this.WebSocketOrder.Options.SetRequestHeader("Api-Sign", Convert.ToBase64String(StringToByte(Hash_HMAC(secretKey, endPoint + (char)0 + "" + (char)0 + nonce.ToString()))));
                this.WebSocketOrder.Options.SetRequestHeader("Api-Nonce", nonce.ToString());

                await this.WebSocketOrder.ConnectAsync(new(this.BaseWebSocketUrl), CancellationToken.None);

                string msg = @"{
""type"" : ""transaction"", 
""symbols"" : [""ETH_KRW""]
}";
                //DEFAULT   SIMPLE
                await this.WebSocketOrder.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

                DateTime dateTime = DateTime.Now;

                while (this.WebSocketOrder.State == WebSocketState.Open)
                {
                    if ((DateTime.Now - dateTime.AddSeconds(10)).TotalSeconds >= this.SocketCloseTimeOutSeconds)
                    {
                        this.OrderResultFromWebSocketClose();
                        $"OrderResultFromWebSocketClose".WriteMessage(((IApi)this).ExchangeID);
                        break;
                    }

                    ArraySegment<byte> bytesReceived = new(new byte[1024]);

                    WebSocketReceiveResult result = await this.WebSocketOrder.ReceiveAsync(bytesReceived, CancellationToken.None);

                    if (bytesReceived.Array == null)
                        continue;

                    var data = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);

                    if (data.IsNullOrEmpty())
                        continue;

                    //Console.WriteLine(data);

                    //var orderWebSocket = JsonSerializer.Deserialize<OrderWebSocket>(data);
                    ////var orderWebSocket = JsonSerializer.Deserialize<OrderWebSocket>(Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count));

                    //if (orderWebSocket != null)
                    //{
                    //    Models.Order order = new()
                    //    {
                    //        UUID = orderWebSocket.OrderUUID,
                    //        Side = orderWebSocket.AskBid?.ToLower(),
                    //        OrdType = orderWebSocket.OrderType,
                    //        Price = orderWebSocket.Price,
                    //        State = "wait",
                    //        Market = orderWebSocket.Code,
                    //    };

                    //    this.Action?.Invoke(this, new() { Action = "OrderExecution", Value = order });
                    //}
                }
            }
            catch (Exception ex)
            {
                this.Action?.Invoke(this, new() { Action = "OrderExecution", Value = null });

                ex.WriteMessage(false, ((IApi)this).ExchangeID);
                this.OrderResultFromWebSocketClose();
            }
        }
        private void OrderResultFromWebSocketClose()
        {
            if (this.WebSocketOrder != null)
            {
                try
                {
                    this.WebSocketOrder.Abort();
                    this.WebSocketOrder.Dispose();
                    this.WebSocketOrder = null;
                }
                catch (Exception ex)
                {
                    ex.WriteMessage(false, ((IApi)this).ExchangeID);
                    this.WebSocketOrder = null;
                }
            }
        }
        #endregion


        #region "출금"
        #endregion


        #region "입금"
        Models.Deposits IApi.Deposits(string currency, int imit, int page, string order_by)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.Deposits result = new();
            BithumbDatas? list;

            try
            {
                tmps = currency.Split('-');
                marketGroup = tmps[0];
                currency = tmps[1];

                tmp = CallAPI_Private_WithParam($"{this.BaseUrl}/info/user_transactions"
                    , new NameValueCollection { { "offset", page.ToString() }
                        , { "count", imit.ToString() }
                        , { "searchGb", "5" }
                        , { "order_currency", currency }
                        , { "payment_currency", marketGroup } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDatas>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Datas == null) return result;

                result.DepositsList = new();

                foreach (var itemOrder in list.Datas)
                {
                    if (itemOrder == null)
                        continue;

                    Models.Deposits order = new() { Type = "deposit", State = "ACCEPTED"};
                    foreach (var item in itemOrder)
                    {
                        switch (item.Key)
                        {
                            case "transfer_date":
                                order.UUID = item.Value;
                                order.TxID = item.Value;
                                if (item.Value.ToTryLong(out long long_1))
                                {
                                    order.CreatedAt = ConvertFromUnixTimestamp(long_1);
                                    order.DoneAt = order.CreatedAt;
                                }
                                break;
                            case "order_currency":
                                order.Currency = item.Value; break;
                            case "units":
                                if (item.Value.ToTryDecimal(out decimal decimal_1))
                                    order.Amount = decimal_1; break;
                            case "fee":
                                if (item.Value.ToTryDecimal(out decimal decimal_2))
                                {
                                    order.Fee = decimal_2;
                                    order.TransactionType = order.Fee > 0 ? "default" : "internal";
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }
        #endregion


        #region "서비스 정보"
        Models.ApiKyes IApi.ApiKyes()
        {
            Models.ApiKyes result = new()
            {
                ApiKyesList = new()
                {
                    new()
                    {
                        AccessKey = ((IApi)this).AccessKey,
                        ExpireAt = DateTime.Now.AddYears(10),
                    }
                }
            };

            return result;
        }
        #endregion


        #region "시세 종목 조회/마켓 코드 조회"
        private static Models.Markets MarketsDB = new() { MarketList = new() };
        Models.Markets IApi.Markets()
        {
            Models.Ticker ticker1;
            Models.Ticker ticker2;
            Models.Markets result = new();

            try
            {
                if (!((IApi)this).AccessKey.IsNullOrEmpty())
                    lock (MarketsDB)
                        if (MarketsDB.MarketList == null || !MarketsDB.MarketList.Any() || MarketsDB.LastDateTime.Hour != DateTime.Now.Hour)
                        {
                            MarketsDB.LastDateTime = DateTime.Now;

                            ticker1 = this.TickerAll("KRW");
                            if (ticker1.Error != null)
                            {
                                GetError(ticker1.Error.Code, ticker1.Error.Message);
                                return MarketsDB;
                            }

                            ticker2 = this.TickerAll("BTC");
                            if (ticker2.Error != null)
                            {
                                GetError(ticker2.Error.Code, ticker2.Error.Message);
                                return MarketsDB;
                            }

                            result.MarketList = new();
                            if (ticker1.TickerList != null && ticker1.TickerList.Count > 0)
                            {
                                if (ticker2.TickerList != null && ticker2.TickerList.Count > 0)
                                    ticker1.TickerList.AddRange(ticker2.TickerList);

                                foreach (var item in ticker1.TickerList)
                                {
                                    result.MarketList.Add(new()
                                    {
                                        Market = item.Market,
                                        KoreanName = item.Market,
                                        EnglishName = item.Market,

                                    });
                                }

                                MarketsDB = result;
                            }
                        }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
                //result.Error = GetError(ex);
            }

            return MarketsDB;
        }
        #endregion


        #region "시세 캔들 조회"
        Models.CandlesMinute IApi.CandlesMinute(string market, Models.MinuteCandleType unit, DateTime to, int count)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.CandlesMinute result = new();
            BithumbDataJsonElementList? list;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                var unitString = unit switch
                {
                    Models.MinuteCandleType._1 => "1m",
                    Models.MinuteCandleType._3 => "3m",
                    Models.MinuteCandleType._5 => "5m",
                    Models.MinuteCandleType._10 => "10m",
                    Models.MinuteCandleType._30 => "30m",
                    Models.MinuteCandleType._60 => "1h",
                    _ => "",
                };

                if (unitString == "") { result.Error = GetError("5600", "unit error"); return result; }

                tmp = CallAPI_Public($"{this.BaseUrl}/public/candlestick/{market}_{marketGroup}/{unitString}", null);

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDataJsonElementList>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                result.CandlesMinuteList = new();
                foreach (var item in list.Data)
                {
                    if (item.ValueKind == JsonValueKind.Array && item is JsonElement element)
                    {
                        tmp = element.GetRawText();

                        if (!tmp.IsNullOrEmpty())
                        {
                            var contract = JsonSerializer.Deserialize<List<object>>(tmp);

                            if (contract != null)
                            {
                                int i = 0;
                                Models.CandlesMinute candlesMinute = new() { Market = $"{marketGroup}-{market}" };
                                foreach (var contractItem in contract)
                                {
                                    string? value = contractItem.ToString();

                                    if (value == null) break;

                                    switch (i)
                                    {
                                        case 0:
                                            candlesMinute.CandleDateTimeKst = ConvertFromUnixTimestamp(value.ToDouble() * 1000);
                                            candlesMinute.CandleDateTimeUtc = candlesMinute.CandleDateTimeKst.ToUniversalTime();
                                            candlesMinute.TimeStamp = value.ToLong();
                                            break;
                                        case 1:
                                            candlesMinute.OpeningPrice = value.ToDecimal();
                                            break;
                                        case 2:
                                            candlesMinute.TradePrice = value.ToDecimal();
                                            break;
                                        case 3:
                                            candlesMinute.HighPrice = value.ToDecimal();
                                            break;
                                        case 4:
                                            candlesMinute.LowPrice = value.ToDecimal();
                                            break;
                                        case 5:
                                            candlesMinute.CandleAccTradeVolume = value.ToDecimal();
                                            candlesMinute.CandleAccTradePrice = candlesMinute.TradePrice * candlesMinute.CandleAccTradeVolume;
                                            candlesMinute.Unit = 1;
                                            break;
                                    }

                                    i++;
                                }
                                if (i == 6)
                                    result.CandlesMinuteList.Add(candlesMinute);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.CandlesDay IApi.CandlesDay(string market, DateTime to, int count, string convertingPriceUnit)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.CandlesDay result = new();
            BithumbDataJsonElementList? list;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                tmp = CallAPI_Public($"{this.BaseUrl}/public/candlestick/{market}_{marketGroup}/24h", null);

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDataJsonElementList>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                result.CandlesDayList = new();
                foreach (var item in list.Data)
                {
                    if (item.ValueKind == JsonValueKind.Array && item is JsonElement element)
                    {
                        tmp = element.GetRawText();

                        if (!tmp.IsNullOrEmpty())
                        {
                            var contract = JsonSerializer.Deserialize<List<object>>(tmp);

                            if (contract != null)
                            {
                                int i = 0;
                                Models.CandlesDay candlesMinute = new() { Market = $"{marketGroup}-{market}" };
                                foreach (var contractItem in contract)
                                {
                                    string? value = contractItem.ToString();

                                    if (value == null) break;

                                    switch (i)
                                    {
                                        case 0:
                                            candlesMinute.CandleDateTimeKst = ConvertFromUnixTimestamp(value.ToDouble() * 1000);
                                            candlesMinute.CandleDateTimeUtc = candlesMinute.CandleDateTimeKst.ToUniversalTime();
                                            candlesMinute.TimeStamp = value.ToLong();
                                            break;
                                        case 1:
                                            candlesMinute.OpeningPrice = value.ToDecimal();
                                            break;
                                        case 2:
                                            candlesMinute.TradePrice = value.ToDecimal();
                                            break;
                                        case 3:
                                            candlesMinute.HighPrice = value.ToDecimal();
                                            break;
                                        case 4:
                                            candlesMinute.LowPrice = value.ToDecimal();
                                            break;
                                        case 5:
                                            candlesMinute.CandleAccTradeVolume = value.ToDecimal();
                                            candlesMinute.CandleAccTradePrice = candlesMinute.TradePrice * candlesMinute.CandleAccTradeVolume;
                                            break;
                                    }

                                    i++;
                                }
                                if (i == 6)
                                    result.CandlesDayList.Add(candlesMinute);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.CandlesWeek IApi.CandlesWeek(string market, DateTime to, int count)
        {
            return new();
        }

        Models.CandlesMonth IApi.CandlesMonth(string market, DateTime to, int count)
        {
            return new();
        }
        #endregion


        #region "시세 체결 조회/최근 체결 내역"
        Models.Ticks IApi.Ticks(string market, DateTime to, int count)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.Ticks result = new();
            BithumbDatas? list;

            try
            {
                tmps = market.Split('-');
                marketGroup = tmps[0];
                market = tmps[1];

                tmp = CallAPI_Public($"{this.BaseUrl}/public/transaction_history/{market}_{marketGroup}", new NameValueCollection { { "count", count.ToString() } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDatas>(tmp);

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Datas == null) return result;

                result.TicksList = new();

                foreach (var itemOrder in list.Datas)
                {
                    if (itemOrder == null)
                        continue;

                    Models.Ticks order = new() { Market = $"{marketGroup}-{market}" };

                    foreach (var item in itemOrder)
                    {
                        switch (item.Key)
                        {
                            case "transaction_date":
                                var timeSpan = (DateTime.Parse(item.Value) - new DateTime(1970, 1, 1, 0, 0, 0));
                                order.TimeStamp = (long)timeSpan.TotalSeconds;

                                var value = ConvertFromUnixTimestamp(order.TimeStamp);

                                order.TradeDateUtc = value.ToString("yyyy-MM-dd");
                                order.TradeTimeUtc = value.ToString("HH:mm:ss");
                                order.Sequential_ID = order.TimeStamp;
                                break;
                            case "price":
                                order.TradePrice = item.Value.ToDecimal(); break;
                            case "units_traded":
                                order.TradeVolume = item.Value.ToDecimal(); break;
                            case "type":
                                order.Side = item.Value.ToLower();
                                break;
                        }
                    }
                    result.TicksList.Add(order);
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }
        #endregion


        #region "시세 현재가(Ticker) 조회/현재가 정보"
        private static readonly Models.Ticker TickerDB = new();
        private static ClientWebSocket? WebSocketTickerDB;
        private static DateTime RunTickerFromWebSocketDateTime;
        Models.Ticker IApi.Ticker(string markets)
        {
            string[]? tmps = null;
            Models.Ticker ticker1;
            Models.Ticker ticker2;
            DateTime dateTime = DateTime.Now.AddSeconds(-60);

            try
            {
                tmps = markets.Split(',');

                //데이터를 가져온지 오래 되었고 요청한 리스트에 있으면
                var isIn = tmps.Where(x => TickerDB.TickerList.Where(x1 => x1.LastDateTime < dateTime).Select(x => x.Market).Contains(x));

                if (TickerDB.TickerList != null && TickerDB.TickerList.Any() && isIn != null && isIn.Any())
                {
                    ticker1 = this.TickerAll("KRW");
                    if (ticker1.Error != null)
                    {
                        GetError(ticker1.Error.Code, ticker1.Error.Message);
                    }

                    ticker2 = this.TickerAll("BTC");
                    if (ticker2.Error != null)
                    {
                        GetError(ticker2.Error.Code, ticker2.Error.Message);
                    }

                    if (ticker2.TickerList != null && ticker2.TickerList.Count > 0)
                        ticker1.TickerList.AddRange(ticker2.TickerList);

                    if (ticker1 != null && ticker1.TickerList != null)
                    {
                        dateTime = DateTime.Now;

                        lock (TickerDB)
                            foreach (var item in ticker1.TickerList)
                            {
                                var sel = TickerDB.TickerList.SingleOrDefault(x => x.Market == item.Market);

                                if (sel != null)
                                {
                                    sel.ExchangeID = 2;
                                    sel.LastDateTime = dateTime;
                                    sel.Market = item.Market;
                                    sel.TradeDate = item.TradeDate;
                                    sel.TradeTime = item.TradeTime;
                                    sel.TradeDateKst = item.TradeDateKst;
                                    sel.TradeTimeKst = item.TradeTimeKst;
                                    sel.TradeTimeStamp = item.TradeTimeStamp;
                                    sel.OpeningPrice = item.OpeningPrice;
                                    sel.HighPrice = item.HighPrice;
                                    sel.LowPrice = item.LowPrice;
                                    sel.TradePrice = item.TradePrice;
                                    sel.PrevClosingPrice = item.PrevClosingPrice;
                                    sel.Change = item.Change;
                                    sel.ChangePrice = item.ChangePrice;
                                    sel.ChangeRate = item.ChangeRate;
                                    sel.SignedChangePrice = item.SignedChangePrice;
                                    sel.SignedChangeRate = item.SignedChangeRate;
                                    sel.TradeVolume = item.TradeVolume;
                                    sel.AccTradePrice = item.AccTradePrice;
                                    sel.AccTradePrice24h = item.AccTradePrice24h;
                                    sel.AccTradeVolume = item.AccTradeVolume;
                                    sel.AccTradeVolume24h = item.AccTradeVolume24h;
                                    sel.Highest52WeekPrice = item.Highest52WeekPrice;
                                    sel.Highest52WeekDate = item.Highest52WeekDate;
                                    sel.Lowest52WeekPrice = item.Lowest52WeekPrice;
                                    sel.Lowest52WeekDate = item.Lowest52WeekDate;
                                    sel.TimeStamp = item.TimeStamp;
                                }
                            }
                    }
                }

                TickerDB.TickerList ??= new();

                var notIn = tmps.Where(x => !TickerDB.TickerList.Select(x => x.Market).Contains(x));

                //가져온 내역 중에 없는게 있다면
                if (notIn != null && notIn.Any())
                {
                    ticker1 = this.TickerAll("KRW");
                    if (ticker1.Error != null)
                    {
                        GetError(ticker1.Error.Code, ticker1.Error.Message);
                    }

                    ticker2 = this.TickerAll("BTC");
                    if (ticker2.Error != null)
                    {
                        GetError(ticker2.Error.Code, ticker2.Error.Message);
                    }

                    if (ticker2.TickerList != null && ticker2.TickerList.Count > 0)
                        ticker1.TickerList.AddRange(ticker2.TickerList);

                    if (ticker1 != null && ticker1.TickerList != null)
                    {
                        dateTime = DateTime.Now;

                        lock (TickerDB)
                            foreach (var item in ticker1.TickerList)
                            {
                                if (!TickerDB.TickerList.Any(x => x.Market == item.Market))
                                    TickerDB.TickerList.Add(new()
                                    {
                                        ExchangeID = 2,
                                        Market = item.Market,
                                        TradeDate = item.TradeDate,
                                        TradeTime = item.TradeTime,
                                        TradeDateKst = item.TradeDateKst,
                                        TradeTimeKst = item.TradeTimeKst,
                                        TradeTimeStamp = item.TradeTimeStamp,
                                        OpeningPrice = item.OpeningPrice,
                                        HighPrice = item.HighPrice,
                                        LowPrice = item.LowPrice,
                                        TradePrice = item.TradePrice,
                                        PrevClosingPrice = item.PrevClosingPrice,
                                        Change = item.Change,
                                        ChangePrice = item.ChangePrice,
                                        ChangeRate = item.ChangeRate,
                                        SignedChangePrice = item.SignedChangePrice,
                                        SignedChangeRate = item.SignedChangeRate,
                                        TradeVolume = item.TradeVolume,
                                        AccTradePrice = item.AccTradePrice,
                                        AccTradePrice24h = item.AccTradePrice24h,
                                        AccTradeVolume = item.AccTradeVolume,
                                        AccTradeVolume24h = item.AccTradeVolume24h,
                                        Highest52WeekPrice = item.Highest52WeekPrice,
                                        Highest52WeekDate = item.Highest52WeekDate,
                                        Lowest52WeekPrice = item.Lowest52WeekPrice,
                                        Lowest52WeekDate = item.Lowest52WeekDate,
                                        TimeStamp = item.TimeStamp,
                                    });
                            }
                    }
                }

            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);
            }

            Models.Ticker result = new();

            if (tmps != null)
            {
                var value = TickerDB.TickerList.Where(x => tmps.Contains(x.Market));

                if (value != null)
                    result.TickerList = value.ToList();
            }

            return result;
        }
        Models.Ticker TickerAll(string marketGroup)
        {
            string? tmp;
            Models.Ticker result = new();
            BithumbDataJsonElement? list;
            Models.Ticker ticker;
            DateTime dateTime;

            try
            {
                tmp = CallAPI_Public($"{this.BaseUrl}/public/ticker/ALL_{marketGroup}", null);

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDataJsonElement>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                dateTime = DateTime.Now;

                foreach (var item in list.Data)
                {
                    if (item.Value.ValueKind == JsonValueKind.String && item.Key == "date")
                    {
                        var timeStamp = item.Value.GetString()?.ToLong();

                        if (timeStamp == null)
                            continue;

                        var b = ConvertFromUnixTimestamp((long)timeStamp);

                        foreach (var item1 in result.TickerList)
                        {
                            item1.TimeStamp = timeStamp;
                            item1.TradeDate = b.ToString("yyyyMMdd");
                            item1.TradeTime = b.ToString("HHmmss");
                            item1.TradeDateKst = item1.TradeDate;
                            item1.TradeTimeKst = item1.TradeTime;
                        }
                    }

                    if (item.Value.ValueKind == JsonValueKind.Object && item.Value is JsonElement element)
                    {
                        tmp = element.GetRawText();

                        if (!tmp.IsNullOrEmpty())
                        {
                            var contract = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tmp);

                            if (contract != null)
                            {
                                ticker = new() { ExchangeID = 2, LastDateTime = dateTime, Market = $"{marketGroup}-{item.Key}" };
                                foreach (var contractDetail in contract)
                                {
                                    string? valueString = null;

                                    if (contractDetail.Value.ValueKind == JsonValueKind.String)
                                        valueString = contractDetail.Value.GetString();
                                    else if (contractDetail.Value.ValueKind == JsonValueKind.Number)
                                        valueString = contractDetail.Value.GetDecimal().ToString();

                                    if (valueString != null)
                                        switch (contractDetail.Key)
                                        {
                                            case "opening_price":
                                                ticker.OpeningPrice = valueString.ToDecimal();
                                                break;
                                            case "closing_price":
                                                ticker.TradePrice = valueString.ToDecimal();
                                                break;
                                            case "min_price":
                                                ticker.LowPrice = valueString.ToDecimal();
                                                break;
                                            case "max_price":
                                                ticker.HighPrice = valueString.ToDecimal();
                                                break;
                                            case "units_traded":
                                                ticker.AccTradeVolume = valueString.ToDecimal();
                                                break;
                                            case "acc_trade_value":
                                                ticker.AccTradePrice = valueString.ToDecimal();
                                                break;
                                            case "prev_closing_price":
                                                ticker.PrevClosingPrice = valueString.ToDecimal();
                                                break;
                                            case "units_traded_24H":
                                                ticker.AccTradeVolume24h = valueString.ToDecimal();
                                                break;
                                            case "acc_trade_value_24H":
                                                ticker.AccTradePrice24h = valueString.ToDecimal();
                                                break;
                                            case "fluctate_24H":
                                                ticker.SignedChangePrice = valueString.ToDecimal();
                                                ticker.ChangePrice = Math.Abs(ticker.SignedChangePrice);
                                                break;
                                            case "fluctate_rate_24H":
                                                ticker.SignedChangeRate = valueString.ToDecimal() / 100M;
                                                ticker.ChangeRate = Math.Abs(ticker.SignedChangeRate);
                                                break;
                                            case "date":
                                                ticker.TimeStamp = valueString.ToLong();
                                                var b = ConvertFromUnixTimestamp((long)ticker.TimeStamp);
                                                ticker.TradeDate = b.ToString("yyyyMMdd");
                                                ticker.TradeTime = b.ToString("HHmmss");
                                                ticker.TradeDateKst = ticker.TradeDate;
                                                ticker.TradeTimeKst = ticker.TradeTime;
                                                break;
                                        }
                                }

                                result.TickerList.Add(ticker);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        private async void RunTickerFromWebSocket()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    var markets = ((IApi)this).Markets();

                    if ((!((IApi)this).AccessKey.IsNullOrEmpty() && !((IApi)this).SecretKey.IsNullOrEmpty()) || (markets.MarketList != null && markets.MarketList.Any()))
                        break;

                    await Task.Delay(5000);
                }

                RunTickerFromWebSocketDateTime = DateTime.Now;
                $"Start : RunTickerFromWebSocket".WriteMessage(((IApi)this).ExchangeID);
                while (true)
                {
                    if (this.IsDispose) break;

                    if (WebSocketTickerDB == null)
                    {
                        RunTickerFromWebSocketDateTime = DateTime.Now;
                        WebSocketTickerDB = new ClientWebSocket();
                        this.TickerFromWebSocket();
                    }

                    if ((DateTime.Now - RunTickerFromWebSocketDateTime).TotalSeconds >= this.SocketCloseTimeOutSeconds * 2)
                    {
                        TickerFromWebSocketClose();
                        //$"TickerFromWebSocketClose(RunTickerFromWebSocket)".WriteMessage(((IApi)this).ExchangeID);
                    }

                    await Task.Delay(2000);
                }
            });
        }
        private async void TickerFromWebSocket()
        {
            string[] tmps;
            string codes;
            Models.Markets? markets;

            try
            {
                if (WebSocketTickerDB == null)
                    return;

                markets = ((IApi)this).Markets();

                if (markets == null || markets.MarketList == null || !markets.MarketList.Any())
                    return;

                codes = string.Join(',', markets.MarketList.Select(x => $"\"{(x.Market ?? "-").Split('-')[1]}_{(x.Market ?? "-").Split('-')[0]}\""));

                await WebSocketTickerDB.ConnectAsync(new(this.BaseWebSocketUrl), CancellationToken.None);

                string msg = @"{
""type"" : ""ticker"", 
""symbols"" : [" + codes + @"], 
""tickTypes"" : [""30M"", ""1H"", ""12H"", ""24H"", ""MID""]
}";
                //DEFAULT   SIMPLE
                await WebSocketTickerDB.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

                DateTime dateTime1 = DateTime.Now;

                while (WebSocketTickerDB.State == WebSocketState.Open)
                {
                    if ((DateTime.Now - dateTime1.AddSeconds(15)).TotalSeconds >= this.SocketCloseTimeOutSeconds)
                    {
                        TickerFromWebSocketClose();
                        //$"TickerFromWebSocketClose".WriteMessage(((IApi)this).ExchangeID);
                        break;
                    }

                    ArraySegment<byte> bytesReceived = new(new byte[1024]);

                    WebSocketReceiveResult result = await WebSocketTickerDB.ReceiveAsync(bytesReceived, CancellationToken.None);

                    if (bytesReceived.Array == null)
                        continue;

                    var data = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);

                    if (data.IsNullOrEmpty())
                        continue;

                    if (data.Contains("Connected Successfully") || data.Contains("Filter Registered Successfully"))
                    {
                        //Console.WriteLine(data);
                        continue;
                    }

                    if (data.Contains("Invalid Filter Syntax"))
                    {
                        TickerFromWebSocketClose();
                        break;
                    }

                    //Console.WriteLine(data);

                    var tickerWebSocket = JsonSerializer.Deserialize<BithumbDataJsonElementContent>(data);
                    //var tickerWebSocket = JsonSerializer.Deserialize<TickerWebSocket>(Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count));

                    if (tickerWebSocket != null)
                    {
                        if (tickerWebSocket.Content != null)
                            lock (TickerDB)
                            {
                                DateTime dateTime = DateTime.Now;
                                Models.Ticker ticker = new();

                                foreach (var item in tickerWebSocket.Content)
                                {
                                    switch (item.Key)
                                    {
                                        case "symbol":
                                            ticker.LastDateTime = dateTime;
                                            tmps = item.Value.Split('_');
                                            ticker.Market = $"{tmps[1]}-{tmps[0]}";
                                            break;
                                        case "date":
                                            ticker.TradeDate = item.Value;
                                            if (ticker.TradeTime != null)
                                                ticker.TradeTimeStamp = ((DateTimeOffset)DateTime.Parse($"{ticker.TradeDate[..4]}-{ticker.TradeDate.Substring(4, 2)}-{ticker.TradeDate.Substring(6, 2)} {ticker.TradeTime[..2]}:{ticker.TradeTime.Substring(2, 2)}:{ticker.TradeTime.Substring(4, 2)}")).ToUnixTimeSeconds();
                                            break;
                                        case "time":
                                            ticker.TradeTime = item.Value;
                                            if (ticker.TradeDate != null)
                                                ticker.TradeTimeStamp = ((DateTimeOffset)DateTime.Parse($"{ticker.TradeDate[..4]}-{ticker.TradeDate.Substring(4, 2)}-{ticker.TradeDate.Substring(6, 2)} {ticker.TradeTime[..2]}:{ticker.TradeTime.Substring(2, 2)}:{ticker.TradeTime.Substring(4, 2)}")).ToUnixTimeSeconds();
                                            break;
                                        case "openPrice":
                                            ticker.OpeningPrice = item.Value.ToDecimal();
                                            break;
                                        case "closePrice":
                                            ticker.TradePrice = item.Value.ToDecimal();
                                            break;
                                        case "lowPrice":
                                            ticker.LowPrice = item.Value.ToDecimal();
                                            break;
                                        case "highPrice":
                                            ticker.HighPrice = item.Value.ToDecimal();
                                            break;
                                        case "value":
                                            ticker.AccTradePrice = item.Value.ToDecimal();
                                            ticker.AccTradePrice24h = ticker.AccTradePrice;
                                            break;
                                        case "volume":
                                            ticker.AccTradeVolume = item.Value.ToDecimal();
                                            ticker.TradeVolume = ticker.AccTradeVolume;
                                            ticker.AccTradeVolume24h = ticker.AccTradeVolume;
                                            break;
                                        case "prevClosePrice":
                                            ticker.PrevClosingPrice = item.Value.ToDecimal();
                                            break;
                                        case "chgRate":
                                            ticker.SignedChangeRate = item.Value.ToDecimal() / 100M;
                                            ticker.ChangeRate = Math.Abs(ticker.SignedChangeRate);
                                            break;
                                        case "chgAmt":
                                            ticker.SignedChangePrice = item.Value.ToDecimal();
                                            ticker.ChangePrice = Math.Abs(ticker.SignedChangePrice);
                                            ticker.Change = (ticker.TradePrice + ticker.SignedChangePrice) == ticker.PrevClosingPrice ? "EVEN" : (ticker.TradePrice > ticker.PrevClosingPrice) ? "RISE" : "FALL";
                                            break;
                                    }
                                }

                                var sel = TickerDB.TickerList.SingleOrDefault(x => x.Market == ticker.Market);
                                if (sel != null)
                                {
                                    sel.ExchangeID = 2;
                                    sel.LastDateTime = dateTime;
                                    sel.Market = ticker.Market;
                                    sel.Icon = sel.Icon == null ? markets.MarketList.SingleOrDefault(x => x.Market == ticker.Market)?.Icon : sel.Icon;
                                    sel.TradeDate = ticker.TradeDate;
                                    sel.TradeTime = ticker.TradeTime;
                                    //sel.TradeDateKst = ticker.TradeDate;
                                    //sel.TradeTimeKst = ticker.TradeTime;
                                    sel.TradeTimeStamp = ticker.TradeTimeStamp;
                                    sel.OpeningPrice = ticker.OpeningPrice;
                                    sel.HighPrice = ticker.HighPrice;
                                    sel.LowPrice = ticker.LowPrice;
                                    sel.TradePrice = ticker.TradePrice;
                                    sel.PrevClosingPrice = ticker.PrevClosingPrice;
                                    sel.Change = ticker.Change;
                                    sel.ChangePrice = ticker.ChangePrice;
                                    sel.ChangeRate = ticker.ChangeRate;
                                    sel.SignedChangePrice = ticker.SignedChangePrice;
                                    sel.SignedChangeRate = ticker.SignedChangeRate;
                                    sel.TradeVolume = ticker.TradeVolume;
                                    sel.AccTradePrice = ticker.AccTradePrice;
                                    sel.AccTradePrice24h = ticker.AccTradePrice24h;
                                    sel.AccTradeVolume = ticker.AccTradeVolume;
                                    sel.AccTradeVolume24h = ticker.AccTradeVolume24h;
                                    //sel.Highest52WeekPrice = ticker.Highest52WeekPrice;
                                    //sel.Highest52WeekDate = ticker.Highest52WeekDate;
                                    //sel.Lowest52WeekPrice = ticker.Lowest52WeekPrice;
                                    //sel.Lowest52WeekDate = ticker.Lowest52WeekDate;
                                    sel.TimeStamp = ticker.TimeStamp;
                                }
                                else
                                    TickerDB.TickerList.Add(new()
                                    {
                                        ExchangeID = 2,
                                        LastDateTime = dateTime,
                                        Market = ticker.Market,
                                        Icon = markets.MarketList.SingleOrDefault(x => x.Market == ticker.Market)?.Icon,
                                        TradeDate = ticker.TradeDate,
                                        TradeTime = ticker.TradeTime,
                                        //TradeDateKst = ticker.TradeDate,
                                        //TradeTimeKst = ticker.TradeTime,
                                        TradeTimeStamp = ticker.TradeTimeStamp,
                                        OpeningPrice = ticker.OpeningPrice,
                                        HighPrice = ticker.HighPrice,
                                        LowPrice = ticker.LowPrice,
                                        TradePrice = ticker.TradePrice,
                                        PrevClosingPrice = ticker.PrevClosingPrice,
                                        Change = ticker.Change,
                                        ChangePrice = ticker.ChangePrice,
                                        ChangeRate = ticker.ChangeRate,
                                        SignedChangePrice = ticker.SignedChangePrice,
                                        SignedChangeRate = ticker.SignedChangeRate,
                                        TradeVolume = ticker.TradeVolume,
                                        AccTradePrice = ticker.AccTradePrice,
                                        AccTradePrice24h = ticker.AccTradePrice24h,
                                        AccTradeVolume = ticker.AccTradeVolume,
                                        AccTradeVolume24h = ticker.AccTradeVolume24h,
                                        //Highest52WeekPrice = ticker.Highest52WeekPrice,
                                        //Highest52WeekDate = ticker.Highest52WeekDate,
                                        //Lowest52WeekPrice = ticker.Lowest52WeekPrice,
                                        //Lowest52WeekDate = ticker.Lowest52WeekDate,
                                        TimeStamp = ticker.TimeStamp,
                                    });
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);
                TickerFromWebSocketClose();
            }
        }
        private static void TickerFromWebSocketClose()
        {
            if (WebSocketTickerDB != null)
            {
                try
                {
                    WebSocketTickerDB.Abort();
                    WebSocketTickerDB.Dispose();
                    WebSocketTickerDB = null;
                }
                catch (Exception ex)
                {
                    ex.WriteMessage(false);
                    WebSocketTickerDB = null;
                }
            }
        }
        #endregion


        #region "시세 호가 정보(Orderbook) 조회/호가 정보 조회"
        Models.Orderbook IApi.Orderbook(string markets)
        {
            string? tmp;
            string[] tmps;
            string marketGroup;
            Models.Orderbook result = new();
            BithumbDataJsonElement? list;
            string? tmpBids = "";
            string? tmpAsks = "";

            try
            {
                tmps = markets.Split('-');
                marketGroup = tmps[0];
                markets = tmps[1];

                tmp = CallAPI_Public($"{this.BaseUrl}/public/orderbook/{markets}_{marketGroup}", new NameValueCollection { { "count", "15" } });

                if (string.IsNullOrEmpty(tmp)) return result;

                list = JsonSerializer.Deserialize<BithumbDataJsonElement>(tmp, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });

                if (list == null) return result;
                if (list.Code != "0000") { result.Error = GetError(list.Code, list.Message); return result; }
                if (list.Data == null) return result;

                result.OrderbookList = new();
                foreach (var item in list.Data)
                {
                    if (item.Value.ValueKind == JsonValueKind.String)
                        switch (item.Key)
                        {
                            case "timestamp":
                                string? timeSpan = item.Value.GetString();

                                result.OrderbookList.Add(new()
                                {
                                    Market = $"{marketGroup}-{markets}",
                                    TimeStamp = timeSpan?.ToLong(),
                                });
                                break;
                        }

                    if (item.Key == "bids" && item.Value.ValueKind == JsonValueKind.Array && item.Value is JsonElement element1)
                    {
                        tmpBids = element1.GetRawText();
                    }
                    if (item.Key == "asks" && item.Value.ValueKind == JsonValueKind.Array && item.Value is JsonElement element2)
                    {
                        tmpAsks = element2.GetRawText();
                    }
                }

                if (!tmpBids.IsNullOrEmpty() && !tmpAsks.IsNullOrEmpty())
                {
                    var tmpBidsList = JsonSerializer.Deserialize<Dictionary<string, string>[]>(tmpBids);
                    var tmpAsksList = JsonSerializer.Deserialize<Dictionary<string, string>[]>(tmpAsks);

                    if (tmpBidsList != null && tmpAsksList != null && tmpBidsList.Length == 15 && tmpAsksList.Length == 15)
                    {
                        result.OrderbookUnits = new();
                        for (int i = 0; i < 15; i++)
                        {
                            result.OrderbookUnits.Add(new()
                            {
                                AskPrice = tmpAsksList[i]["price"].ToDecimal(),
                                BidPrice = tmpBidsList[i]["price"].ToDecimal(),
                                AskSize = tmpAsksList[i]["quantity"].ToDecimal(),
                                BidSize = tmpBidsList[i]["quantity"].ToDecimal(),
                            });
                        }

                        if (result.OrderbookList != null && result.OrderbookList.Count == 1)
                        {
                            result.OrderbookList[0].TotalAskSize = result.OrderbookUnits.Sum(x => x.AskSize);
                            result.OrderbookList[0].TotalBidSize = result.OrderbookUnits.Sum(x => x.BidSize);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }
        #endregion

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            try
            {
                //this.JwtHeader = null;

                this.HttpClient?.Dispose();
                this.HttpClient = null;

                this.OrderResultFromWebSocketClose();
                TickerFromWebSocketClose();

                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false);
            }
            finally
            {
                this.IsDispose = true;
            }
        }
    }

    internal class BithumbOrder
    {
        /// <summary>
        /// Order
        /// </summary>
        public Models.Order Order { get; set; }
        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        public DateTime InsertDateTime { get; set; } = DateTime.Now;

        public BithumbOrder(Models.Order order)
        {
            this.Order = order;
        }
    }
}