namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 에러
    /// </summary>
    public class Error : ICore
    {
        /// <summary>
        /// 메시지 코드
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 메시지
        /// </summary>
        public string? Message { get; set; }
    }
}