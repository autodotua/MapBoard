using Esri.ArcGISRuntime.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public interface IServerMapLayerInfo : IMapLayerInfo
    {
        bool AutoPopulateAll { get; }
        bool HasPopulateAll { get; }
        bool IsDownloading { get; }

        Task PopulateAllFromServiceAsync(CancellationToken? cancellationToken = null);

        Task<FeatureQueryResult> PopulateFromServiceAsync(QueryParameters parameters, bool clearCache = false, CancellationToken? cancellationToken = null);
    }
}