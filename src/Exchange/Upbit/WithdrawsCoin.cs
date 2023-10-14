using System.Text.Json.Serialization;

namespace MetaFrm.Stock.Exchange.Upbit
{
    /// <summary>
    /// 출금 결과
    /// </summary>
    public class WithdrawsCoin : ICore
    {
        /// <summary>
        /// 출금 종류
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// 출금에 대한 고유 아이디
        /// </summary>
        [JsonPropertyName("uuid")]
        public string? UUID { get; set; }

        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// 출금의 트랜잭션 아이디
        /// </summary>
        [JsonPropertyName("txid")]
        public string? TxID { get; set; }

        /// <summary>
        /// 출금 상태
        /// </summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        /// <summary>
        /// 출금 생성 시간
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 출금 완료 시간
        /// </summary>
        [JsonPropertyName("done_at")]
        public DateTime? DoneAt { get; set; }

        /// <summary>
        /// 출금 수량
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 출금 수수료
        /// </summary>
        [JsonPropertyName("fee")]
        public decimal Fee { get; set; }

        /// <summary>
        /// 원화
        /// </summary>
        [JsonPropertyName("krw_amount")]
        public decimal KrwAmount { get; set; }

        /// <summary>
        /// 출금 유형
        /// default : 일반출금
        /// internal : 바로출금
        /// </summary>
        [JsonPropertyName("transaction_type")]
        public string? TransactionType { get; set; }

        /// <summary>
        /// 에러
        /// </summary>
        public Error? Error { get; set; }
    }
}