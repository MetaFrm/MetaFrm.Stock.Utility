using MetaFrm.Stock.Models;

namespace MetaFrm.Stock.Service
{
    /// <summary>
    /// IService
    /// </summary>
    public interface IService : ICore
    {
        /// <summary>
        /// GetTicker
        /// </summary>
        /// <param name="exchangeID"></param>
        /// <returns></returns>
        List<Ticker> GetTicker(int exchangeID);
    }
}