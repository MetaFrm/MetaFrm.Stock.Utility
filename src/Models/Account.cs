namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 계좌
    /// </summary>
    public class Account : ICore, IEquatable<Account?>
    {
        /// <summary>
        /// AccountList
        /// </summary>
        public List<Account> AccountList { get; set; } = new();

        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 주문가능 금액/수량
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// 주문 중 묶여있는 금액/수량
        /// </summary>
        public decimal Locked { get; set; }

        /// <summary>
        /// 매수평균가
        /// </summary>
        public decimal AvgKrwBuyPrice { get; set; }

        /// <summary>
        /// 매수금액KRW
        /// </summary>
        public decimal KrwBuyPrice
        {
            get
            {
                return (this.Balance + this.Locked) * this.AvgKrwBuyPrice;
                //return (this.Balance == 0.0M ? 1 : this.Balance) * (this.Locked == 0.0M ? 1 : this.Locked) * this.AvgKrwBuyPrice;
            }
        }

        /// <summary>
        /// 매수평균가 수정 여부
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// 평단가 기준 화폐
        /// </summary>
        public string? UnitCurrency { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }

        /// <summary>
        /// Ticker
        /// </summary>
        public Ticker? Ticker { get; set; }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(Account? obj)
        {
            if (obj is null)
                return false;

            return this.Balance == obj.Balance && this.Locked == obj.Locked;
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj) => Equals(obj as Order);

        /// <summary>
        /// GetHashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => (this.Balance, this.Locked).GetHashCode();
    }
}