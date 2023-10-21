namespace MetaFrm.Stock
{
    /// <summary>
    /// Setting
    /// </summary>
    public class Setting : ICore
    {
        /// <summary>
        /// User
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// SettingID
        /// </summary>
        public int SettingID { get; set; }

        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        public string? Market { get; set; }

        /// <summary>
        /// StopType
        /// </summary>
        public string? StopType { get; set; }
    }
}