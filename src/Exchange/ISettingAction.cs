namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// ISetting
    /// </summary>
    public interface ISettingAction : ICore
    {
        /// <summary>
        /// Organized
        /// </summary>
        /// <param name="SETTING_ID"></param>
        /// <param name="BID_CANCEL"></param>
        /// <param name="ASK_CANCEL"></param>
        /// <param name="ASK_CURRENT_PRICE"></param>
        /// <param name="IS_PROFIT_STOP"></param>
        public void Organized(int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool IS_PROFIT_STOP);

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="allOrder"></param>
        void Run(Models.Order? allOrder);
    }
}