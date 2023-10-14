using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 마켓 코드 조회
    /// </summary>
    public class Markets : ICore
    {
        private string? market;

        /// <summary>
        /// MarketList
        /// </summary>
        public Markets[]? MarketList { get; set; }

        /// <summary>
        /// 암호화폐 그룹 코드
        /// </summary>
        public string? MarketGroup { get; set; }

        /// <summary>
        /// 암호화폐 코드
        /// </summary>
        public string? MarketCode { get; set; }

        /// <summary>
        /// 업비트에서 제공중인 시장 정보(그룹-코드)
        /// </summary>
        [JsonPropertyName("market")]
        public string? Market
        {
            get
            {
                return this.market;
            }
            set
            {
                string[] tmps;

                this.market = value;

                if (this.market != null && this.market.Contains('-'))
                {
                    tmps = this.market.Split('-');
                    this.MarketGroup = tmps[0];
                    this.MarketCode = tmps[1];
                }
            }
        }

        /// <summary>
        /// 거래 대상 암호화폐 한글명
        /// </summary>
        [JsonPropertyName("korean_name")]
        public string? KoreanName { get; set; }

        /// <summary>
        /// 거래 대상 암호화폐 영문명
        /// </summary>
        [JsonPropertyName("english_name")]
        public string? EnglishName { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}