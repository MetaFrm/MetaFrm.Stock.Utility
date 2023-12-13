using MetaFrm.Service;
using MetaFrm.Stock.Console;
using MetaFrm.Stock.Exchange;
using MetaFrm.Stock.Models;

namespace MetaFrm.Stock.Service
{
    /// <summary>
    /// Service
    /// </summary>
    public class Service : IService
    {
        private static readonly Dictionary<int, Markets> Markets = new();
        private static readonly Dictionary<int, IApi?> Apis = new();

        /// <summary>
        /// Service
        /// </summary>
        public Service()
        {
            this.LoadMarkets(1);
            this.LoadMarkets(2);

            InitApi(1);
            InitApi(2);
        }

        private void LoadMarkets(int EXCHANGE_ID)
        {
            Response response;

            if (Markets.ContainsKey(EXCHANGE_ID))
                return;

            ServiceData data = new()
            {
                TransactionScope = false,
                Token = Factory.AccessKey,
            };
            data["1"].CommandText = "MetaFrm.Stock.Utility".GetAttribute("Service.LoadMarkets");
            data["1"].AddParameter(nameof(EXCHANGE_ID), Database.DbType.Int, 3, EXCHANGE_ID);
            data["1"].AddParameter("USER_ID", Database.DbType.Int, 3, null);

            response = this.ServiceRequest(data);

            if (response.Status != Status.OK)
                response.Message?.WriteMessage();
            else
            {
                if (response.DataSet != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                {
                    Markets markets = new() { MarketList = new() };
                    foreach (var item in response.DataSet.DataTables[0].DataRows)
                    {
                        markets.MarketList.Add(new()
                        {
                            Market = item.String("MARKET"),
                            KoreanName = item.String("KOR_NAME"),
                            EnglishName = item.String("ENG_NAME"),
                            Icon = item.String("ICON"),
                        });
                    }

                    Markets.Add(EXCHANGE_ID, markets);
                }
            }
        }
        private static void InitApi(int EXCHANGE_ID)
        {
            if (Apis.ContainsKey(EXCHANGE_ID))
                return;

            IApi? api = Exchanger.CreateApi(EXCHANGE_ID, 0, false);

            if (api != null && Markets.ContainsKey(EXCHANGE_ID) && Markets.TryGetValue(EXCHANGE_ID, out Markets? markets) && markets != null && markets.MarketList != null && markets.MarketList.Count > 0)
            {
                api.Markets().MarketList?.AddRange(markets.MarketList);

                Apis.Add(EXCHANGE_ID, api);
            }
        }

        /// <summary>
        /// GetTicker
        /// </summary>
        /// <param name="exchangeID"></param>
        /// <returns></returns>
        public List<Ticker> GetTicker(int exchangeID)
        {
            if (Apis.TryGetValue(exchangeID, out IApi? api) && api != null)
            {
                var markets1 = api.Markets();

                if (markets1 != null && markets1.MarketList != null && markets1.MarketList.Any())
                {
                    string codes = string.Join(',', markets1.MarketList.Where(y => y.Market != null && y.Market.StartsWith("KRW")).Select(x => $"{(x.Market ?? "-").Split('-')[0]}-{(x.Market ?? "-").Split('-')[1]}"));

                    return api.Ticker(codes).TickerList;
                }

            }

            return new();
        }
    }
}