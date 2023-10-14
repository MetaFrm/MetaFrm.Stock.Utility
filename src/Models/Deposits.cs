namespace MetaFrm.Stock.Models
{
    /// <summary>
    /// 입금 리스트 조회
    /// </summary>
    public class Deposits : ICore
    {
        /// <summary>
        /// DepositsList
        /// </summary>
        public List<Deposits>? DepositsList { get; set; }

        /// <summary>
        /// 입출금 종류
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 입금에 대한 고유 아이디
        /// </summary>
        public string? UUID { get; set; }

        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// 입금 네트워크
        /// </summary>
        public string? NetType { get; set; }

        /// <summary>
        /// 입금의 트랜잭션 아이디
        /// </summary>
        public string? TxID { get; set; }

        /// <summary>
        /// 입금 상태
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// 입금 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 입금 완료 시간
        /// </summary>
        public DateTime DoneAt { get; set; }

        /// <summary>
        /// 입금 수량
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 입금 수수료
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// 입금 유형
        /// default : 일반입금
        /// internal : 바로입금
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}