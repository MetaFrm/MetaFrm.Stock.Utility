using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 입금 리스트 조회
    /// </summary>
    public class Deposits : ICore
    {
        /// <summary>
        /// DepositsList
        /// </summary>
        public Deposits[]? DepositsList { get; set; }

        /// <summary>
        /// 입출금 종류
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// 입금에 대한 고유 아이디
        /// </summary>
        [JsonPropertyName("uuid")]
        public string? UUID { get; set; }

        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// 입금 네트워크
        /// </summary>
        [JsonPropertyName("net_type")]
        public string? NetType { get; set; }

        /// <summary>
        /// 입금의 트랜잭션 아이디
        /// </summary>
        [JsonPropertyName("txid")]
        public string? TxID { get; set; }

        /// <summary>
        /// 입금 상태
        /// - PROCESSING : 입금 진행중
        /// - ACCEPTED : 완료
        /// - CANCELLED : 취소됨
        /// - REJECTED : 거절됨
        /// - TRAVEL_RULE_SUSPECTED : 트래블룰 추가 인증 대기중
        /// - REFUNDING : 반환절차 진행중
        /// - REFUNDED : 반환됨
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// 입금 생성 시간
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 입금 완료 시간
        /// </summary>
        [JsonPropertyName("done_at")]
        public DateTime DoneAt { get; set; }

        /// <summary>
        /// 입금 수량
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 입금 수수료
        /// </summary>
        [JsonPropertyName("fee")]
        public decimal Fee { get; set; }

        /// <summary>
        /// 입금 유형
        /// default : 일반입금
        /// internal : 바로입금
        /// </summary>
        [JsonPropertyName("transaction_type")]
        public string? TransactionType { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}