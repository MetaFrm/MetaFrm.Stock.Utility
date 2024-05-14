using MetaFrm.Control;
using MetaFrm.Stock.Console;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// UpbitApi
    /// </summary>
    public class UpbitApi : IApi, IAction, IDisposable
    {
        private readonly Task<AuthenticationState>? AuthState;

        private readonly object _lock = new();
        private JwtHeader? JwtHeader;
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
        int IApi.ExchangeID { get; set; } = 1;
        string IApi.AccessKey { get; set; } = "";
        string IApi.SecretKey { get; set; } = "";
        private string BaseUrl { get; set; } = "https://api.upbit.com/v1/";
        private string BaseWebSocketUrl { get; set; } = "wss://api.upbit.com/websocket/v1";

        private double BaseTimeoutMin { get; set; } = 1000;
        private int CallCount = 0;
        private int BaseTimeoutDecreaseMod { get; set; } = 200;

        private readonly int SocketCloseTimeOutSeconds = 60 * 5;

        /// <summary>
        /// Action event Handler입니다.
        /// </summary>
        public event MetaFrmEventHandler? Action;

        private static bool IsRunTickerFromWebSocket = false;

        private bool IsDispose = false;

        /// <summary>
        /// UpbitApi
        /// </summary>
        public UpbitApi(bool runTickerFromWebSocket, bool runOrderResultFromWebSocket, Task<AuthenticationState>? authState)
        {
            this.CreateHttpClient(null);

            this.AuthState = authState;

            if (runTickerFromWebSocket && !IsRunTickerFromWebSocket)
            {
                IsRunTickerFromWebSocket = true;
                this.RunTickerFromWebSocket();
            }

            if (runOrderResultFromWebSocket)
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

        private string? CallAPI(string url, NameValueCollection? nameValueCollection, HttpMethod httpMethod, int reTryCount = 2)
        {
            string? result = "";
            HttpRequestMessage requestMessage;

            try
            {
                this.CallCount += 1;

                lock (this._lock)
                    if (this.HttpClient != null)
                    {
                        if (url.Contains("order"))
                            Thread.Sleep(130);
                        else if (url.Contains("accounts") || url.Contains("withdraw") || url.Contains("deposit") || url.Contains("status") || url.Contains("api_keys"))
                            Thread.Sleep(40);
                        else
                            Thread.Sleep(110);

                        requestMessage = new()
                        {
                            Method = httpMethod,
                            RequestUri = new Uri(url + (nameValueCollection == null ? "" : ToQueryString(nameValueCollection)))
                        };
                        requestMessage.Headers.Authorization = new("Bearer", this.JWT(nameValueCollection, ((IApi)this).AccessKey, ((IApi)this).SecretKey));


                        result = this.HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).Result.Content.ReadAsStringAsync().Result;

                        //최소보다 크거나 연속으로 정상적인 호출 횟수가 BaseTimeoutDecreaseMod에 도달하면
                        //100 Milliseconds 감소
                        if (this.HttpClient.Timeout.TotalMilliseconds > this.BaseTimeoutMin && this.CallCount % this.BaseTimeoutDecreaseMod == 0)
                        {
                            this.CreateHttpClient(TimeSpan.FromMilliseconds(this.HttpClient.Timeout.TotalMilliseconds - 100));
                            this.CallCount = 0;
                        }
                    }

                return result;
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);

                if (this.HttpClient != null)
                    this.CreateHttpClient(TimeSpan.FromMilliseconds(this.HttpClient.Timeout.TotalMilliseconds + 100));//오류 발생하면 100 Milliseconds 증가

                this.CallCount = 0;

                if (reTryCount > 0)
                    return this.CallAPI(url, nameValueCollection, httpMethod, reTryCount - 1);
                else
                    return "";
            }
        }
        private static string ToQueryString(NameValueCollection nameValueCollection)
        {
            var array = (from key in nameValueCollection.AllKeys
                         from value in nameValueCollection.GetValues(key) ?? Array.Empty<string>()
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value ?? ""))).ToArray();

            //return "?" + string.Join("&", array);
            return string.Format("?{0}", string.Join("&", array));
        }
        private string JWT(NameValueCollection? nameValueCollection, string accessKey, string secretKey)
        {
            StringBuilder builder = new();
            string queryHash = "";

            if (nameValueCollection != null)
            {
                foreach (string key in nameValueCollection)
                    builder.Append(key).Append('=').Append(nameValueCollection[key]).Append('&');

                byte[] queryHashByteArray = SHA512.HashData(Encoding.UTF8.GetBytes(builder.ToString().TrimEnd('&')));
                queryHash = BitConverter.ToString(queryHashByteArray).Replace("-", "").ToLower();
            }

            var payload = new JwtPayload { { "access_key", accessKey }, { "nonce", Guid.NewGuid().ToString() }, { "query_hash", queryHash }, { "query_hash_alg", "SHA512" } };

            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(this.CreaetJwtHeader(secretKey), payload));
        }
        private JwtHeader? CreaetJwtHeader(string secretKey)
        {
            try
            {
                if (this.JwtHeader == null)
                {
                    var securityKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(secretKey));

                    this.JwtHeader = new(new SigningCredentials(securityKey, "HS256"));
                }

                return this.JwtHeader;
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);
                return null;
            }
        }
        private static string DateTime2String(DateTime to)
        {
            //return to.ToString("s") + "+09:00";
            return string.Format("{0}+09:00", to.ToString("s"));
        }
        private static Models.Error? GetError(string result, [CallerMemberName] string methodName = "")
        {
            Error? error = JsonSerializer.Deserialize<ErrorRoot>(result)?.Error;

            $"{methodName} : {error?.Message} {error?.Name}".WriteMessage();

            return error != null ? new() { Message = error.Message, Code = error.Name } : null;
        }
        private Models.Error GetError(Exception ex, bool isDetail)
        {
            ex.WriteMessage(isDetail, ((IApi)this).ExchangeID);

            return new() { Message = ex.Message, Code = ex.ToString() };
        }

        private static Models.Error? GetErrorName(string result, [CallerMemberName] string methodName = "")
        {
            ErrorName? error = JsonSerializer.Deserialize<ErrorName>(result);

            if (error != null && error.Name != null && !error.Name.Contains("too_many_requests"))
                $"{methodName} : ErrorName {error?.Name}".WriteMessage();

            return error != null ? new() { Message = error.Name, Code = error.Name } : null;
        }

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };

        #region "자산"
        Models.Account IApi.Account()
        {
            string? tmp;
            List<Account>? list;
            Models.Account result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}accounts", null, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<List<Account>>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                foreach (var item in list)
                    result.AccountList.Add(new()
                    {
                        Currency = item.Currency,
                        Balance = item.Balance,
                        Locked = item.Locked,
                        AvgKrwBuyPrice = item.AvgKrwBuyPrice,
                        Modified = item.Modified,
                        UnitCurrency = item.UnitCurrency,
                    });
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
            OrderChance? orderChance;
            Models.OrderChance result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}orders/chance", new NameValueCollection { { "market", market } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                orderChance = JsonSerializer.Deserialize<OrderChance>(tmp, jsonSerializerOptions);

                if (orderChance != null)
                {
                    result = new()
                    {
                        BidFee = orderChance.BidFee,
                        AskFee = orderChance.AskFee,
                    };

                    if (orderChance.Market != null)
                    {
                        result.Market = new()
                        {
                            ID = orderChance.Market.ID,
                            Name = orderChance.Market.Name,
                            AskTypes = orderChance.Market.AskTypes,
                            BidTypes = orderChance.Market.BidTypes,
                            OrderSides = orderChance.Market.OrderSides,
                            MaxTotal = orderChance.Market.MaxTotal,
                            State = orderChance.Market.State,
                        };
                        if (orderChance.Market.Bid != null)
                        {
                            result.Market.Bid = new()
                            {
                                Currency = orderChance.Market.Bid.Currency,
                                PriceUnit = orderChance.Market.Bid.PriceUnit,
                                MinTotal = orderChance.Market.Bid.MinTotal,
                            };
                        }
                        if (orderChance.Market.Ask != null)
                        {
                            result.Market.Bid = new()
                            {
                                Currency = orderChance.Market.Ask.Currency,
                                PriceUnit = orderChance.Market.Ask.PriceUnit,
                                MinTotal = orderChance.Market.Ask.MinTotal,
                            };
                        }
                    }
                    if (orderChance.BidAccount != null)
                    {
                        result.BidAccount = new()
                        {
                            Currency = orderChance.BidAccount.Currency,
                            Balance = orderChance.BidAccount.Balance,
                            Locked = orderChance.BidAccount.Locked,
                            AvgKrwBuyPrice = orderChance.BidAccount.AvgKrwBuyPrice,
                            Modified = orderChance.BidAccount.Modified,
                            UnitCurrency = orderChance.BidAccount.UnitCurrency,
                        };
                    }
                    if (orderChance.AskAccount != null)
                    {
                        result.AskAccount = new()
                        {
                            Currency = orderChance.AskAccount.Currency,
                            Balance = orderChance.AskAccount.Balance,
                            Locked = orderChance.AskAccount.Locked,
                            AvgBuyPrice = orderChance.AskAccount.AvgBuyPrice,
                            Modified = orderChance.AskAccount.Modified,
                            UnitCurrency = orderChance.AskAccount.UnitCurrency,
                        };
                    }
                }
                else
                    result = new() { Error = new() { Message = "OrderChance is null : OrderChance IApi.OrderChance(string market)", Code = "OrderChance is null" } };
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.Order IApi.Order(string market, string sideName, string uuid)
        {
            string? tmp;
            Order? order;
            Models.Order result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}order", new NameValueCollection { { "uuid", uuid } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                order = JsonSerializer.Deserialize<Order>(tmp, jsonSerializerOptions);

                if (order != null)
                {
                    result = new()
                    {
                        UUID = order.UUID,
                        Side = order.Side,
                        OrdType = order.OrdType,
                        Price = order.Price ?? 0,
                        State = order.State,
                        Market = order.Market,
                        CreatedAt = order.CreatedAt,
                        Volume = order.Volume ?? 0,
                        RemainingVolume = order.RemainingVolume ?? 0,
                        ReservedFee = order.ReservedFee,
                        RemainingFee = order.RemainingFee,
                        PaidFee = order.PaidFee,
                        Locked = order.Locked,
                        ExecutedVolume = order.ExecutedVolume,
                        TradesCount = order.TradesCount,
                    };

                    if (order.Trades != null)
                    {
                        result.Trades = new();
                        foreach (var tradeItem in order.Trades)
                        {
                            result.Trades.Add(new()
                            {
                                Market = tradeItem.Market,
                                UUID = tradeItem.UUID,
                                Price = tradeItem.Price,
                                Volume = tradeItem.Volume,
                                Funds = tradeItem.Funds,
                                Side = tradeItem.Side,
                                CreatedAt = tradeItem.CreatedAt,
                            });
                        }
                    }
                }
                else
                    result = new() { Error = new() { Message = "order is null : Order IApi.Order(string uuid)", Code = "order is null" } };
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.Order IApi.AllOrder(string market, string order_by)
        {
            int page;
            Models.Order order;
            Models.Order orderResult;

            page = 1;

            orderResult = ((IApi)this).AllOrder(market, page, order_by);
            order = orderResult;
            while (order != null && order.OrderList != null && orderResult.OrderList != null && order.OrderList.Count >= 100)
            {
                try
                {
                    page += 1;

                    order = ((IApi)this).AllOrder(market, page, order_by);

                    if (order.OrderList != null)
                        orderResult.OrderList = orderResult.OrderList.Union(order.OrderList).ToList();
                }
                catch (Exception ex)
                {
                    ex.WriteMessage(false, ((IApi)this).ExchangeID);
                    break;
                }
            }

            return orderResult;
        }
        Models.Order IApi.AllOrder(string market, int page, string order_by)
        {
            string? tmp;
            Order[]? list;
            Models.Order result = new();

            try
            {
                if (market == null || market == "" || market == "ALL")
                    tmp = this.CallAPI(this.BaseUrl + "orders", new NameValueCollection { { "page", page.ToString() }, { "order_by", order_by } }, HttpMethod.Get);
                else
                    tmp = this.CallAPI(this.BaseUrl + "orders", new NameValueCollection { { "market", market }, { "page", page.ToString() }, { "order_by", order_by } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<Order[]>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                result.OrderList = new();
                foreach (var item in list)
                {
                    Models.Order order = new()
                    {
                        UUID = item.UUID,
                        Side = item.Side,
                        OrdType = item.OrdType,
                        Price = item.Price ?? 0,
                        State = item.State,
                        Market = item.Market,
                        CreatedAt = item.CreatedAt,
                        Volume = item.Volume ?? 0,
                        RemainingVolume = item.RemainingVolume ?? 0,
                        ReservedFee = item.ReservedFee,
                        RemainingFee = item.RemainingFee,
                        PaidFee = item.PaidFee,
                        Locked = item.Locked,
                        ExecutedVolume = item.ExecutedVolume,
                        TradesCount = item.TradesCount,
                    };

                    if (item.Trades != null)
                    {
                        order.Trades = new();
                        foreach (var tradeItem in item.Trades)
                        {
                            order.Trades.Add(new()
                            {
                                Market = tradeItem.Market,
                                UUID = tradeItem.UUID,
                                Price = tradeItem.Price,
                                Volume = tradeItem.Volume,
                                Funds = tradeItem.Funds,
                                Side = tradeItem.Side,
                                CreatedAt = tradeItem.CreatedAt,
                            });
                        }
                    }

                    result.OrderList.Add(order);
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.Order IApi.CancelOrder(string market, string sideName, string uuid)
        {
            string? tmp;
            Order? order;
            Models.Order result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}order", new NameValueCollection { { "uuid", uuid } }, HttpMethod.Delete);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                order = JsonSerializer.Deserialize<Order>(tmp, jsonSerializerOptions);

                if (order != null)
                {
                    result = new()
                    {
                        UUID = order.UUID,
                        Side = order.Side,
                        OrdType = order.OrdType,
                        Price = order.Price ?? 0,
                        State = order.State,
                        Market = order.Market,
                        CreatedAt = order.CreatedAt,
                        Volume = order.Volume ?? 0,
                        RemainingVolume = order.RemainingVolume ?? 0,
                        ReservedFee = order.ReservedFee,
                        RemainingFee = order.RemainingFee,
                        PaidFee = order.PaidFee,
                        Locked = order.Locked,
                        ExecutedVolume = order.ExecutedVolume,
                        TradesCount = order.TradesCount,
                    };

                    if (order.Trades != null)
                    {
                        result.Trades = new();
                        foreach (var tradeItem in order.Trades)
                        {
                            order.Trades.Add(new()
                            {
                                Market = tradeItem.Market,
                                UUID = tradeItem.UUID,
                                Price = tradeItem.Price,
                                Volume = tradeItem.Volume,
                                Funds = tradeItem.Funds,
                                Side = tradeItem.Side,
                                CreatedAt = tradeItem.CreatedAt,
                            });
                        }
                    }
                }
                else
                    result = new() { Error = new() { Message = "order is null : Order IApi.CancelOrder(string uuid)", Code = "order is null" } };
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.Order IApi.MakeOrder(string market, Models.OrderSide side, decimal volume, decimal price, Models.OrderType ord_type)
        {
            string? tmp;
            Order? order;
            Models.Order result = new();

            try
            {
                if (side == Models.OrderSide.ask && ord_type == Models.OrderType.price)
                    throw new Exception("주문종류와 주문타입이 일치하지 않습니다(요청한 주문 => 주문종류:매도, 주문타입:시장가 매수).");
                if (side == Models.OrderSide.bid && ord_type == Models.OrderType.market)
                    throw new Exception("주문종류와 주문타입이 일치하지 않습니다(요청한 주문 => 주문종류:매수, 주문타입:시장가 매도).");

                tmp = this.CallAPI($"{this.BaseUrl}orders",
                    new NameValueCollection
                    {
                        { "market", market },
                        { "side", side.ToString() },
                        { "volume", side == Models.OrderSide.bid && ord_type == Models.OrderType.price ? "" : volume.ToString() },
                        { "price", side == Models.OrderSide.ask && ord_type == Models.OrderType.market ? "" : price.ToString() },
                        { "ord_type", ord_type.ToString() }
                    }
                    , HttpMethod.Post);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                order = JsonSerializer.Deserialize<Order>(tmp, jsonSerializerOptions);

                if (order != null)
                {
                    result = new()
                    {
                        UUID = order.UUID,
                        Side = order.Side,
                        OrdType = order.OrdType,
                        Price = order.Price ?? 0,
                        State = order.State,
                        Market = order.Market,
                        CreatedAt = order.CreatedAt,
                        Volume = order.Volume ?? 0,
                        RemainingVolume = order.RemainingVolume ?? 0,
                        ReservedFee = order.ReservedFee,
                        RemainingFee = order.RemainingFee,
                        PaidFee = order.PaidFee,
                        Locked = order.Locked,
                        ExecutedVolume = order.ExecutedVolume,
                        TradesCount = order.TradesCount,
                    };

                    if (order.Trades != null)
                    {
                        result.Trades = new();
                        foreach (var tradeItem in order.Trades)
                        {
                            order.Trades.Add(new()
                            {
                                Market = tradeItem.Market,
                                UUID = tradeItem.UUID,
                                Price = tradeItem.Price,
                                Volume = tradeItem.Volume,
                                Funds = tradeItem.Funds,
                                Side = tradeItem.Side,
                                CreatedAt = tradeItem.CreatedAt,
                            });
                        }
                    }
                }
                else
                    result = new() { Error = new() { Message = "order is null : Order IApi.MakeOrder(string market, OrderSide side, decimal volume, decimal price, OrderType ord_type)", Code = "order is null" } };
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
                        //$"OrderResultFromWebSocketClose(RunOrderResultFromWebSocket)".WriteMessage(((IApi)this).ExchangeID);
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

                this.WebSocketOrder.Options.SetRequestHeader("Authorization", $"Bearer {this.JWT(null, accessKey, secretKey)}");

                await this.WebSocketOrder.ConnectAsync(new(this.BaseWebSocketUrl), CancellationToken.None);

                string msg = @"[
  {
    ""ticket"": ""OrderResult""
  },
  {
    ""type"": ""myTrade"",
    ""codes"": []
    },
  {
    ""format"": ""SIMPLE""
  }
]";
                //DEFAULT   SIMPLE
                await this.WebSocketOrder.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

                DateTime dateTime = DateTime.Now;

                while (this.WebSocketOrder.State == WebSocketState.Open)
                {
                    if ((DateTime.Now - dateTime).TotalSeconds >= this.SocketCloseTimeOutSeconds)
                    {
                        this.OrderResultFromWebSocketClose();
                        //$"OrderResultFromWebSocketClose".WriteMessage(((IApi)this).ExchangeID);
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

                    var orderWebSocket = JsonSerializer.Deserialize<OrderWebSocket>(data);
                    //var orderWebSocket = JsonSerializer.Deserialize<OrderWebSocket>(Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count));

                    if (orderWebSocket != null)
                    {
                        Models.Order order = new()
                        {
                            UUID = orderWebSocket.OrderUUID,
                            Side = orderWebSocket.AskBid?.ToLower(),
                            OrdType = orderWebSocket.OrderType,
                            Price = orderWebSocket.Price,
                            State = "wait",
                            Market = orderWebSocket.Code,
                            ExecutedVolume = orderWebSocket.Volume,
                        };

                        this.Action?.Invoke(this, new() { Action = "OrderExecution", Value = order });
                    }
                }
            }
            catch (Exception ex)
            {
                this.Action?.Invoke(this, new() { Action = "OrderExecution", Value = null });

                if (ex.Message != "Aborted")
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
            Deposits[]? list;
            Models.Deposits result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}deposits", new NameValueCollection { { "currency", currency }, { "imit", imit.ToString() }, { "page", page.ToString() }, { "order_by", order_by } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<Deposits[]>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                result.DepositsList = new();
                foreach (var item in list)
                {
                    result.DepositsList.Add(new()
                    {
                        Type = item.Type,
                        UUID = item.UUID,
                        Currency = item.Currency,
                        NetType = item.NetType,
                        TxID = item.TxID,
                        State = item.State,
                        CreatedAt = item.CreatedAt,
                        DoneAt = item.DoneAt,
                        Amount = item.Amount,
                        Fee = item.Fee,
                        TransactionType = item.TransactionType,
                    });
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
            string? tmp;
            ApiKyes[]? list;
            Models.ApiKyes result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}api_keys", null, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<ApiKyes[]>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                result.ApiKyesList = new();
                foreach (var item in list)
                {
                    result.ApiKyesList.Add(new()
                    {
                        AccessKey = item.AccessKey,
                        ExpireAt = item.ExpireAt,
                    });
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }
        #endregion


        #region "시세 종목 조회/마켓 코드 조회"
        private static Models.Markets MarketsDB = new() { MarketList = new() };
        Models.Markets IApi.Markets()
        {
            string? tmp;
            Markets[]? list;
            Models.Markets result = new();

            try
            {
                if (!((IApi)this).AccessKey.IsNullOrEmpty())
                    lock (MarketsDB)
                        if (MarketsDB.MarketList == null || MarketsDB.MarketList.Count == 0 || MarketsDB.LastDateTime.Hour != DateTime.Now.Hour)
                        {
                            MarketsDB.LastDateTime = DateTime.Now;

                            tmp = this.CallAPI($"{this.BaseUrl}market/all", null, HttpMethod.Get);

                            if (string.IsNullOrEmpty(tmp)) return MarketsDB;
                            if (tmp.Contains("error")) { result.Error = GetError(tmp); return MarketsDB; }
                            if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return MarketsDB; }

                            list = JsonSerializer.Deserialize<Markets[]>(tmp, jsonSerializerOptions);

                            if (list == null) return MarketsDB;

                            result.MarketList = new();
                            foreach (var item in list)
                                result.MarketList.Add(new()
                                {
                                    Market = item.Market,
                                    KoreanName = item.KoreanName,
                                    EnglishName = item.EnglishName,
                                });

                            MarketsDB = result;
                        }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return MarketsDB;
        }
        #endregion


        #region "시세 캔들 조회"
        Models.CandlesMinute IApi.CandlesMinute(string market, Models.MinuteCandleType unit, DateTime to, int count)
        {
            string? tmp;
            CandlesMinute[]? list;
            Models.CandlesMinute result = new(market, 1, (int)unit);

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}candles/minutes/{(int)unit}", new NameValueCollection { { "market", market }, { "to", (to == default) ? DateTime2String(DateTime.Now) : DateTime2String(to) }, { "count", count.ToString() } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<CandlesMinute[]>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                result.CandlesMinuteList = new();
                foreach (var item in list)
                {
                    result.CandlesMinuteList.Add(new(market, 1, (int)unit)
                    {
                        Market = item.Market,
                        CandleDateTimeUtc = item.CandleDateTimeUtc,
                        CandleDateTimeKst = item.CandleDateTimeKst,
                        OpeningPrice = item.OpeningPrice,
                        HighPrice = item.HighPrice,
                        LowPrice = item.LowPrice,
                        TradePrice = item.TradePrice,
                        TimeStamp = item.TimeStamp,
                        CandleAccTradePrice = item.CandleAccTradePrice,
                        CandleAccTradeVolume = item.CandleAccTradeVolume,
                        Unit = item.Unit,
                    });
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        private readonly JsonSerializerOptions jsonSerializerOptions1 = new() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
        Models.CandlesDay IApi.CandlesDay(string market, DateTime to, int count, string convertingPriceUnit)
        {
            string? tmp;
            CandlesDay[]? list;
            Models.CandlesDay result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}candles/days", new NameValueCollection { { "market", market }, { "to", (to == default) ? DateTime2String(DateTime.Now) : DateTime2String(to) }, { "count", count.ToString() } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<CandlesDay[]>(tmp, jsonSerializerOptions1);

                if (list == null) return result;

                result.CandlesDayList = new();
                foreach (var item in list)
                {
                    result.CandlesDayList.Add(new()
                    {
                        Market = item.Market,
                        CandleDateTimeUtc = item.CandleDateTimeUtc,
                        CandleDateTimeKst = item.CandleDateTimeKst,
                        OpeningPrice = item.OpeningPrice,
                        HighPrice = item.HighPrice,
                        LowPrice = item.LowPrice,
                        TradePrice = item.TradePrice,
                        TimeStamp = item.TimeStamp,
                        CandleAccTradePrice = item.CandleAccTradePrice,
                        CandleAccTradeVolume = item.CandleAccTradeVolume,
                        PrevClosingPrice = item.PrevClosingPrice,
                        ChangePrice = item.ChangePrice,
                        ChangeRate = item.ChangeRate,
                        ConvertedTradePrice = item.ConvertedTradePrice,
                    });
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
            string? tmp;
            CandlesWeek[]? list;
            Models.CandlesWeek result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}candles/weeks", new NameValueCollection { { "market", market }, { "to", (to == default) ? DateTime2String(DateTime.Now) : DateTime2String(to) }, { "count", count.ToString() } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<CandlesWeek[]>(tmp, jsonSerializerOptions1);

                if (list == null) return result;

                result.CandlesWeekList = new();
                foreach (var item in list)
                {
                    result.CandlesWeekList.Add(new()
                    {
                        Market = item.Market,
                        CandleDateTimeUtc = item.CandleDateTimeUtc,
                        CandleDateTimeKst = item.CandleDateTimeKst,
                        OpeningPrice = item.OpeningPrice,
                        HighPrice = item.HighPrice,
                        LowPrice = item.LowPrice,
                        TradePrice = item.TradePrice,
                        TimeStamp = item.TimeStamp,
                        CandleAccTradePrice = item.CandleAccTradePrice,
                        CandleAccTradeVolume = item.CandleAccTradeVolume,
                        FirstDayOfPeriod = item.FirstDayOfPeriod,
                    });
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }

        Models.CandlesMonth IApi.CandlesMonth(string market, DateTime to, int count)
        {
            string? tmp;
            CandlesMonth[]? list;
            Models.CandlesMonth result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}candles/months", new NameValueCollection { { "market", market }, { "to", (to == default) ? DateTime2String(DateTime.Now) : DateTime2String(to) }, { "count", count.ToString() } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<CandlesMonth[]>(tmp, jsonSerializerOptions1);

                if (list == null) return result;

                result.CandlesMonthList = new();
                foreach (var item in list)
                {
                    result.CandlesMonthList.Add(new()
                    {
                        Market = item.Market,
                        CandleDateTimeUtc = item.CandleDateTimeUtc,
                        CandleDateTimeKst = item.CandleDateTimeKst,
                        OpeningPrice = item.OpeningPrice,
                        HighPrice = item.HighPrice,
                        LowPrice = item.LowPrice,
                        TradePrice = item.TradePrice,
                        TimeStamp = item.TimeStamp,
                        CandleAccTradePrice = item.CandleAccTradePrice,
                        CandleAccTradeVolume = item.CandleAccTradeVolume,
                        FirstDayOfPeriod = item.FirstDayOfPeriod,
                    });
                }
            }
            catch (Exception ex)
            {
                result.Error = this.GetError(ex, true);
            }

            return result;
        }
        #endregion


        #region "시세 체결 조회/최근 체결 내역"
        Models.Ticks IApi.Ticks(string market, DateTime to, int count)
        {
            string? tmp;
            Ticks[]? list;
            Models.Ticks result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}trades/ticks", new NameValueCollection { { "market", market }, { "to", (to == default) ? "" : to.ToString("HH:mm:ss") }, { "count", count.ToString() } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<Ticks[]>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                result.TicksList = new();
                foreach (var item in list)
                {
                    result.TicksList.Add(new()
                    {
                        Market = item.Market,
                        TradeDateUtc = item.TradeDateUtc,
                        TradeTimeUtc = item.TradeTimeUtc,
                        TimeStamp = item.TimeStamp,
                        TradePrice = item.TradePrice,
                        TradeVolume = item.TradeVolume,
                        PrevClosingPrice = item.PrevClosingPrice,
                        ChangePrice = item.ChangePrice,
                        Side = item.Side,
                        Sequential_ID = item.Sequential_ID,
                    });
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
            string? tmp;
            Ticker[]? list;
            string[]? tmps = null;
            DateTime dateTime = DateTime.Now.AddSeconds(-60);
            Models.Error? error;

            try
            {
                tmps = markets.Split(',');

                //데이터를 가져온지 오래 되었고 요청한 리스트에 있으면
                var isIn = tmps.Where(x => TickerDB.TickerList.Where(x1 => x1.LastDateTime < dateTime).Select(x => x.Market).Contains(x));

                if (TickerDB.TickerList != null && TickerDB.TickerList.Count != 0 && isIn != null && isIn.Any())
                {
                    tmp = string.Join(',', isIn);

                    tmp = this.CallAPI($"{this.BaseUrl}ticker", new NameValueCollection { { "markets", tmp } }, HttpMethod.Get);

                    if (tmp != null)
                    {
                        if (tmp.Contains("error"))
                        {
                            error = GetError(tmp);
                        }
                        else if (tmp.Contains("name") && tmp.Contains("too_"))
                        {
                            error = GetErrorName(tmp);
                        }
                        else
                        {
                            list = JsonSerializer.Deserialize<Ticker[]>(tmp.Replace(":null", ":0"), jsonSerializerOptions);

                            if (list != null)
                            {
                                dateTime = DateTime.Now;

                                lock (TickerDB)
                                    foreach (var item in list)
                                    {
                                        var sel = TickerDB.TickerList.SingleOrDefault(x => x.Market == item.Market);

                                        if (sel != null)
                                        {
                                            sel.ExchangeID = 1;
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
                    }
                }

                TickerDB.TickerList ??= new();

                var notIn = tmps.Where(x => !TickerDB.TickerList.Select(x => x.Market).Contains(x));

                //가져온 내역 중에 없는게 있다면
                if (notIn != null && notIn.Any())
                {
                    tmp = string.Join(',', notIn);

                    tmp = this.CallAPI($"{this.BaseUrl}ticker", new NameValueCollection { { "markets", tmp } }, HttpMethod.Get);

                    if (tmp != null)
                    {
                        if (tmp.Contains("error"))
                        {
                            error = GetError(tmp);
                        }
                        else
                        {
                            list = JsonSerializer.Deserialize<Ticker[]>(tmp.Replace(":null", ":0"), jsonSerializerOptions);

                            if (list != null)
                            {
                                lock (TickerDB)
                                    foreach (var item in list)
                                    {
                                        if (!TickerDB.TickerList.Any(x => x.Market == item.Market))
                                            TickerDB.TickerList.Add(new()
                                            {
                                                ExchangeID = 1,
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

        private async void RunTickerFromWebSocket()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    var markets = ((IApi)this).Markets();

                    if ((!((IApi)this).AccessKey.IsNullOrEmpty() && !((IApi)this).SecretKey.IsNullOrEmpty()) || (markets.MarketList != null && markets.MarketList.Count != 0))
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
            string codes;
            Models.Markets? markets;

            try
            {
                if (WebSocketTickerDB == null)
                    return;

                markets = ((IApi)this).Markets();

                if (markets == null || markets.MarketList == null || markets.MarketList.Count == 0)
                    return;

                codes = string.Join(',', markets.MarketList.Select(x => $"\"{x.Market}\""));

                await WebSocketTickerDB.ConnectAsync(new(this.BaseWebSocketUrl), CancellationToken.None);

                string msg = @"[
  {
    ""ticket"": ""Ticker""
  },
  {
    ""type"": ""ticker"",
    ""codes"": [
      " + codes + @"
    ]
  },
  {
    ""format"": ""SIMPLE""
  }
]";
                //DEFAULT   SIMPLE
                await WebSocketTickerDB.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

                DateTime dateTime1 = DateTime.Now;
                int i = 0;
                string authStateToken = this.AuthState != null ? this.AuthState.Token() : "";

                while (WebSocketTickerDB.State == WebSocketState.Open)
                {
                    if ((DateTime.Now - dateTime1.AddSeconds(5)).TotalSeconds >= this.SocketCloseTimeOutSeconds)
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

                    //Console.WriteLine(data);

                    var tickerWebSocket = JsonSerializer.Deserialize<TickerWebSocket>(data);
                    //var tickerWebSocket = JsonSerializer.Deserialize<TickerWebSocket>(Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count));

                    if (tickerWebSocket != null)
                    {
                        lock (TickerDB)
                        {
                            DateTime dateTime = DateTime.Now;

                            var sel = TickerDB.TickerList.SingleOrDefault(x => x.Market == tickerWebSocket.Code);
                            if (sel != null)
                            {
                                sel.ExchangeID = 1;
                                sel.LastDateTime = dateTime;
                                sel.Market = tickerWebSocket.Code;
                                sel.Icon ??= (markets.MarketList.SingleOrDefault(x => x.Market == tickerWebSocket.Code)?.Icon);
                                sel.TradeDate = tickerWebSocket.TradeDate;
                                sel.TradeTime = tickerWebSocket.TradeTime;
                                //sel.TradeDateKst = tickerWebSocket.TradeDate;
                                //sel.TradeTimeKst = tickerWebSocket.TradeTime;
                                sel.TradeTimeStamp = tickerWebSocket.TradeTimeStamp;
                                sel.OpeningPrice = tickerWebSocket.OpeningPrice;
                                sel.HighPrice = tickerWebSocket.HighPrice;
                                sel.LowPrice = tickerWebSocket.LowPrice;
                                sel.TradePrice = tickerWebSocket.TradePrice;
                                sel.PrevClosingPrice = tickerWebSocket.PrevClosingPrice;
                                sel.Change = tickerWebSocket.Change;
                                sel.ChangePrice = tickerWebSocket.ChangePrice;
                                sel.ChangeRate = tickerWebSocket.ChangeRate;
                                sel.SignedChangePrice = tickerWebSocket.SignedChangePrice;
                                sel.SignedChangeRate = tickerWebSocket.SignedChangeRate;
                                sel.TradeVolume = tickerWebSocket.TradeVolume;
                                sel.AccTradePrice = tickerWebSocket.AccTradePrice;
                                sel.AccTradePrice24h = tickerWebSocket.AccTradePrice24h;
                                sel.AccTradeVolume = tickerWebSocket.AccTradeVolume;
                                sel.AccTradeVolume24h = tickerWebSocket.AccTradeVolume24h;
                                sel.Highest52WeekPrice = tickerWebSocket.Highest52WeekPrice;
                                sel.Highest52WeekDate = tickerWebSocket.Highest52WeekDate;
                                sel.Lowest52WeekPrice = tickerWebSocket.Lowest52WeekPrice;
                                sel.Lowest52WeekDate = tickerWebSocket.Lowest52WeekDate;
                                sel.TimeStamp = tickerWebSocket.TimeStamp;
                            }
                            else
                                TickerDB.TickerList.Add(new()
                                {
                                    ExchangeID = 1,
                                    LastDateTime = dateTime,
                                    Market = tickerWebSocket.Code,
                                    Icon = markets.MarketList.SingleOrDefault(x => x.Market == tickerWebSocket.Code)?.Icon,
                                    TradeDate = tickerWebSocket.TradeDate,
                                    TradeTime = tickerWebSocket.TradeTime,
                                    //TradeDateKst = tickerWebSocket.TradeDate,
                                    //TradeTimeKst = tickerWebSocket.TradeTime,
                                    TradeTimeStamp = tickerWebSocket.TradeTimeStamp,
                                    OpeningPrice = tickerWebSocket.OpeningPrice,
                                    HighPrice = tickerWebSocket.HighPrice,
                                    LowPrice = tickerWebSocket.LowPrice,
                                    TradePrice = tickerWebSocket.TradePrice,
                                    PrevClosingPrice = tickerWebSocket.PrevClosingPrice,
                                    Change = tickerWebSocket.Change,
                                    ChangePrice = tickerWebSocket.ChangePrice,
                                    ChangeRate = tickerWebSocket.ChangeRate,
                                    SignedChangePrice = tickerWebSocket.SignedChangePrice,
                                    SignedChangeRate = tickerWebSocket.SignedChangeRate,
                                    TradeVolume = tickerWebSocket.TradeVolume,
                                    AccTradePrice = tickerWebSocket.AccTradePrice,
                                    AccTradePrice24h = tickerWebSocket.AccTradePrice24h,
                                    AccTradeVolume = tickerWebSocket.AccTradeVolume,
                                    AccTradeVolume24h = tickerWebSocket.AccTradeVolume24h,
                                    Highest52WeekPrice = tickerWebSocket.Highest52WeekPrice,
                                    Highest52WeekDate = tickerWebSocket.Highest52WeekDate,
                                    Lowest52WeekPrice = tickerWebSocket.Lowest52WeekPrice,
                                    Lowest52WeekDate = tickerWebSocket.Lowest52WeekDate,
                                    TimeStamp = tickerWebSocket.TimeStamp,
                                });

                            if (i % 10000 == 0 && !authStateToken.IsNullOrEmpty())
                            {
                                User.Upload(this, authStateToken, 1, 0, TickerDB);
                                i = 0;
                            }
                        }
                    }

                    i++;
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
            Orderbook[]? list;
            Models.Orderbook result = new();

            try
            {
                tmp = this.CallAPI($"{this.BaseUrl}orderbook", new NameValueCollection { { "markets", markets } }, HttpMethod.Get);

                if (string.IsNullOrEmpty(tmp)) return result;
                if (tmp.Contains("error")) { result.Error = GetError(tmp); return result; }
                if (tmp.Contains("name") && tmp.Contains("too_")) { result.Error = GetErrorName(tmp); return result; }

                list = JsonSerializer.Deserialize<Orderbook[]>(tmp, jsonSerializerOptions);

                if (list == null) return result;

                result.OrderbookList = new();
                foreach (var item in list)
                {
                    result.OrderbookList.Add(new()
                    {
                        Market = item.Market,
                        TimeStamp = item.TimeStamp,
                        TotalAskSize = item.TotalAskSize,
                        TotalBidSize = item.TotalBidSize,
                    });

                    if (item.OrderbookUnits != null)
                    {
                        result.OrderbookUnits = new();
                        foreach (var itemUnit in item.OrderbookUnits)
                        {
                            result.OrderbookUnits.Add(new()
                            {
                                AskPrice = itemUnit.AskPrice,
                                BidPrice = itemUnit.BidPrice,
                                AskSize = itemUnit.AskSize,
                                BidSize = itemUnit.BidSize,
                            });
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
                this.JwtHeader = null;

                this.HttpClient?.Dispose();
                this.HttpClient = null;

                this.OrderResultFromWebSocketClose();
                TickerFromWebSocketClose();

                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                ex.WriteMessage(false, ((IApi)this).ExchangeID);
            }
            finally
            {
                this.IsDispose = true;
            }
        }
    }
}