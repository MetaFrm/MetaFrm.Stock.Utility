namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// ISettingAction
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
        /// <param name="BID_CURRENT_PRICE"></param>
        /// <param name="SAVE_WORKDATA"></param>
        /// <param name="REMOVE_SETTING"></param>
        public void Organized(int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool BID_CURRENT_PRICE, bool SAVE_WORKDATA, bool REMOVE_SETTING);

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="allOrder"></param>
        void Run(Models.Order? allOrder);
    }
}