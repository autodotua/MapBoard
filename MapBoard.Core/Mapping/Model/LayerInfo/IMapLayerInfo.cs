using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Model;
using System;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public interface IMapLayerInfo : ILayerInfo, IDisposable
    {
        GeometryType GeometryType { get; }
        bool HasTable { get; }
        FeatureLayer Layer { get; }
        long NumberOfFeatures { get; }
        Func<QueryParameters, Task<Envelope>> QueryExtentAsync { get; }
        Func<QueryParameters, Task<FeatureQueryResult>> QueryFeaturesAsync { get; }
        bool TimeExtentEnable { get; set; }

        event EventHandler Unattached;

        Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers);

        SymbolInfo GetDefaultSymbol();

        Task LoadAsync();

        Task ReloadAsync(MapLayerCollection layers);

        bool IsLoaded { get; }
        Exception LoadError { get; }
    }
}