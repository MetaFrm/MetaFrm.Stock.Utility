using MetaFrm.Service;
using MetaFrm.Stock.Console;
using System.Text;
using System.Text.Json;

namespace MetaFrm.Stock.Exchange
{
    /// <summary>
    /// SettingType
    /// </summary>
    public enum SettingType
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// GridMartingaleShort
        /// </summary>
        GridMartingaleShort,

        /// <summary>
        /// Grid
        /// </summary>
        Grid,

        /// <summary>
        /// MartingaleLong
        /// </summary>
        MartingaleLong,

        /// <summary>
        /// MartingaleShort
        /// </summary>
        MartingaleShort,

        /// <summary>
        /// TraillingStop
        /// </summary>
        TraillingStop,
    }
}