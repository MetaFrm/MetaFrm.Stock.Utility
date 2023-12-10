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
        /// <param name="SAVE_WORKDATA">서버 프로그램 중지 할떄</param>
        /// <param name="REMOVE_SETTING">서버에서 세팅을 제거 할때</param>
        /// <param name="IS_PROFIT_STOP">수익/StopLoss/TopStop 발생 해서 중지 할때</param>
        public void Organized(int SETTING_ID, bool BID_CANCEL, bool ASK_CANCEL, bool ASK_CURRENT_PRICE, bool BID_CURRENT_PRICE, bool SAVE_WORKDATA, bool REMOVE_SETTING, bool IS_PROFIT_STOP);

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="allOrder"></param>
        void Run(Models.Order? allOrder);
    }
}