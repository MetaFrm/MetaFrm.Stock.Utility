namespace MetaFrm.Stock
{
    /// <summary>
    /// Convert
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// 거래 호가 단위 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="exchangeID"></param>
        /// <param name="market"></param>
        /// <returns></returns>
        public static decimal PriceRound(this decimal value, int exchangeID, string market)
        {
            var tmp0 = Math.Ceiling(value / PriceRoundPoint(value, exchangeID, market));
            var tmp1 = PriceRoundPoint(value, exchangeID, market);
            value = tmp0 * tmp1;

            tmp0 = Math.Ceiling(value / PriceRoundPoint(value, exchangeID, market));
            tmp1 = PriceRoundPoint(value, exchangeID, market);
            return tmp0 * tmp1;
        }

        /// <summary>
        /// 호가 단위
        /// </summary>
        /// <param name="price"></param>
        /// <param name="exchangeID"></param>
        /// <param name="market"></param>
        /// <returns></returns>
        public static decimal PriceRoundPoint(decimal price, int exchangeID, string market)
        {
            if (exchangeID == 1)
                return price switch
                {
                    decimal value when value >= 2000000.00M => 1000M,
                    decimal value when value >= 1000000.00M && value < 2000000.00M => 500M,
                    decimal value when value >= 500000.00M && value < 1000000.00M => 100M,
                    decimal value when value >= 100000.00M && value < 500000.00M => 50M,
                    decimal value when value >= 10000.00M && value < 100000.00M => 10M,
                    decimal value when value >= 1000.00M && value < 10000.00M => 1M,
                    decimal value when value >= 100.00M && value < 1000.00M => 0.1M,
                    decimal value when value >= 10.00M && value < 100.00M => 0.01M,
                    decimal value when value >= 1.00M && value < 10.00M => 0.001M,
                    decimal value when value >= 0.10M && value < 1.00M => 0.0001M,
                    decimal value when value >= 0.01M && value < 0.10M => 0.00001M,
                    decimal value when value >= 0.001M && value < 0.01M => 0.000001M,
                    decimal value when value >= 0.0001M && value < 0.001M => 0.0000001M,
                    decimal value when value >= 0.00001M && value < 0.0001M => 0.00000001M,
                    _ => 0.00000001M
                };
            else if (exchangeID == 2)
                return price switch
                {
                    decimal value when value >= 1000000.00M => 1000M,
                    decimal value when value >= 500000.00M && value < 1000000.00M => 500M,
                    decimal value when value >= 100000.00M && value < 500000.00M => 100M,
                    decimal value when value >= 50000.00M && value < 100000.00M => 50M,
                    decimal value when value >= 10000.00M && value < 50000.00M => 10M,
                    decimal value when value >= 5000.00M && value < 10000.00M => 5M,
                    decimal value when value >= 1000.00M && value < 5000.00M => 1M,
                    decimal value when value >= 100.00M && value < 1000.00M => 1M,
                    decimal value when value >= 10.00M && value < 100.00M => 0.01M,
                    decimal value when value >= 1.00M && value < 10.00M => 0.001M,
                    _ => 0.0001M
                };
            else
                return 0.00000001M;
        }

        /// <summary>
        /// 거래 호가 문자 변환
        /// </summary>
        /// <param name="price"></param>
        /// <param name="exchangeID"></param>
        /// <param name="market"></param>
        /// <returns></returns>
        public static string PriceToString(this decimal price, int exchangeID, string market)
        {
            if (exchangeID == 1)
                return price switch
                {
                    decimal value when value >= 2000000.00M => price.ToString("#,###,###,000"),
                    decimal value when value >= 1000000.00M && value < 2000000.00M => price.ToString("#,###,###,#00"),
                    decimal value when value >= 500000.00M && value < 1000000.00M => price.ToString("#,###,###,#00"),
                    decimal value when value >= 100000.00M && value < 500000.00M => price.ToString("#,###,###,##0"),
                    decimal value when value >= 10000.00M && value < 100000.00M => price.ToString("#,###,###,##0"),
                    decimal value when value >= 1000.00M && value < 10000.00M => price.ToString("#,###,###,###"),
                    decimal value when value >= 100.00M && value < 1000.00M => price.ToString("#,###,###,##0.0"),
                    decimal value when value >= 10.00M && value < 100.00M => price.ToString("#,###,###,##0.00"),
                    decimal value when value >= 1.00M && value < 10.00M => price.ToString("#,###,###,##0.000"),
                    decimal value when value >= 0.10M && value < 1.00M => price.ToString("#,###,###,##0.0000"),
                    decimal value when value >= 0.01M && value < 0.10M => price.ToString("#,###,###,##0.00000"),
                    decimal value when value >= 0.001M && value < 0.01M => price.ToString("#,###,###,##0.000000"),
                    decimal value when value >= 0.0001M && value < 0.001M => price.ToString("#,###,###,##0.0000000"),
                    decimal value when value >= 0.00001M && value < 0.0001M => price.ToString("#,###,###,##0.00000000"),
                    _ => price.ToString("#,###,###,###.00000000")
                };
            else if (exchangeID == 2)
                return price switch
                {
                    decimal value when value >= 1000000.00M => price.ToString("#,###,###,000"),
                    decimal value when value >= 500000.00M && value < 1000000.00M => price.ToString("#,###,###,#00"),
                    decimal value when value >= 100000.00M && value < 500000.00M => price.ToString("#,###,###,#00"),
                    decimal value when value >= 50000.00M && value < 100000.00M => price.ToString("#,###,###,##0"),
                    decimal value when value >= 10000.00M && value < 50000.00M => price.ToString("#,###,###,##0"),
                    decimal value when value >= 5000.00M && value < 10000.00M => price.ToString("#,###,###,###"),
                    decimal value when value >= 1000.00M && value < 5000.00M => price.ToString("#,###,###,###"),
                    decimal value when value >= 100.00M && value < 1000.00M => price.ToString("#,###,###,###"),
                    decimal value when value >= 10.00M && value < 100.00M => price.ToString("#,###,###,##0.00"),
                    decimal value when value >= 1.00M && value < 10.00M => price.ToString("#,###,###,##0.000"),
                    _ => price.ToString("#,###,###,##0.0000"),
                };
            else
                return price.ToString("#,###,###,###.00000000");
        }

        /// <summary>
        /// price 기준으로 변화 호가 문자 변환
        /// </summary>
        /// <param name="changePrice"></param>
        /// <param name="price"></param>
        /// <param name="exchangeID"></param>
        /// <param name="market"></param>
        /// <returns></returns>
        public static string ChangePriceToString(this decimal changePrice, decimal price, int exchangeID, string market)
        {
            if (exchangeID == 1)
                return price switch
                {
                    decimal value when value >= 2000000.00M => changePrice.ToString("#,###,###,000"),
                    decimal value when value >= 1000000.00M && value < 2000000.00M => changePrice.ToString("#,###,###,#00"),
                    decimal value when value >= 500000.00M && value < 1000000.00M => changePrice.ToString("#,###,###,#00"),
                    decimal value when value >= 100000.00M && value < 500000.00M => changePrice.ToString("#,###,###,##0"),
                    decimal value when value >= 10000.00M && value < 100000.00M => changePrice.ToString("#,###,###,##0"),
                    decimal value when value >= 1000.00M && value < 10000.00M => changePrice.ToString("#,###,###,###"),
                    decimal value when value >= 100.00M && value < 1000.00M => changePrice.ToString("#,###,###,##0.0"),
                    decimal value when value >= 10.00M && value < 100.00M => changePrice.ToString("#,###,###,##0.00"),
                    decimal value when value >= 1.00M && value < 10.00M => changePrice.ToString("#,###,###,##0.000"),
                    decimal value when value >= 0.10M && value < 1.00M => changePrice.ToString("#,###,###,##0.0000"),
                    decimal value when value >= 0.01M && value < 0.10M => changePrice.ToString("#,###,###,##0.00000"),
                    decimal value when value >= 0.001M && value < 0.01M => changePrice.ToString("#,###,###,##0.000000"),
                    decimal value when value >= 0.0001M && value < 0.001M => changePrice.ToString("#,###,###,##0.0000000"),
                    decimal value when value >= 0.00001M && value < 0.0001M => changePrice.ToString("#,###,###,##0.00000000"),
                    _ => changePrice.ToString("#,###,###,###.00000000")
                };
            else if (exchangeID == 2)
                return price switch
                {
                    decimal value when value >= 1000000.00M => changePrice.ToString("#,###,###,000"),
                    decimal value when value >= 500000.00M && value < 1000000.00M => changePrice.ToString("#,###,###,#00"),
                    decimal value when value >= 100000.00M && value < 500000.00M => changePrice.ToString("#,###,###,#00"),
                    decimal value when value >= 50000.00M && value < 100000.00M => changePrice.ToString("#,###,###,##0"),
                    decimal value when value >= 10000.00M && value < 50000.00M => changePrice.ToString("#,###,###,##0"),
                    decimal value when value >= 5000.00M && value < 10000.00M => changePrice.ToString("#,###,###,###"),
                    decimal value when value >= 1000.00M && value < 5000.00M => changePrice.ToString("#,###,###,###"),
                    decimal value when value >= 100.00M && value < 1000.00M => changePrice.ToString("#,###,###,###"),
                    decimal value when value >= 10.00M && value < 100.00M => changePrice.ToString("#,###,###,##0.00"),
                    decimal value when value >= 1.00M && value < 10.00M => changePrice.ToString("#,###,###,##0.000"),
                    _ => changePrice.ToString("#,###,###,##0.0000"),
                };
            else
                return changePrice.ToString("#,###,###,###.00000000");
        }
    }
}