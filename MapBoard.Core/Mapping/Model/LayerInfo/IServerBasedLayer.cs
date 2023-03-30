using Esri.ArcGISRuntime.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 基于网络服务的图层
    /// </summary>
    public interface IServerBasedLayer:IMapLayerInfo
    {
        /// <summary>
        /// 是否自动下载所有要素
        /// </summary>
        bool AutoPopulateAll { get; }

        /// <summary>
        /// 是否已经下载所有要素
        /// </summary>
        bool HasPopulateAll { get; }

        /// <summary>
        /// 是否正在下载要素
        /// </summary>
        bool IsDownloading { get; }

        /// <summary>
        /// 下载所有要素
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PopulateAllFromServiceAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        /// 根据查询条件下载要素
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="clearCache"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<FeatureQueryResult> PopulateFromServiceAsync(QueryParameters parameters, bool clearCache = false, CancellationToken? cancellationToken = null);
    }
}