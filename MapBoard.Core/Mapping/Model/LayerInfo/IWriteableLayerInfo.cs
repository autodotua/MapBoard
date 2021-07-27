using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public interface ICanChangeGeometryType
    {
        void SetGeometryType(GeometryType type);
    }

    public interface ICanChangeField
    {
        void SetField(IEnumerable<FieldInfo> fields);
    }

    public interface IHasDefaultFields
    {
    }

    public interface IEditableLayerInfo : IMapLayerInfo
    {
        ObservableCollection<FeaturesChangedEventArgs> Histories { get; }

        event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

        Task AddFeatureAsync(Feature feature, FeaturesChangedSource source);

        Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source, bool rebuildFeatures = false);

        Feature CreateFeature();

        Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry);

        Task DeleteFeatureAsync(Feature feature, FeaturesChangedSource source);

        Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source);

        Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source);

        Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source);
    }
}