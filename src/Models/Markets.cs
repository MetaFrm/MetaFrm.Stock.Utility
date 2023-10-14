namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// Markets
    /// </summary>
    public class Markets : ICore
    {
        /// <summary>
        /// MarketList
        /// </summary>
        public List<Markets>? MarketList { get; set; }

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
        public string? Market { get; set; }

        /// <summary>
        /// 거래 대상 암호화폐 한글명
        /// </summary>
        public string? KoreanName { get; set; }

        /// <summary>
        /// 거래 대상 암호화폐 영문명
        /// </summary>
        public string? EnglishName { get; set; }

        /// <summary>
        /// LastDateTime
        /// </summary>
        public DateTime LastDateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}