using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Extension;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map.Model
{
    public class MapLayerInfo : LayerInfo, IDisposable
    {
        [JsonIgnore]
        public bool TimeExtentEnable
        {
            get => TimeExtent == null ? false : TimeExtent.IsEnable;
            set
            {
                if (TimeExtent != null)
                {
                    if (value != TimeExtent.IsEnable)
                    {
                        TimeExtent.IsEnable = value;
                        this.SetTimeExtentAsync();
                    }
                }

                this.Notify(nameof(TimeExtentEnable));
            }
        }

        public MapLayerInfo()
        {
        }

        public MapLayerInfo(LayerInfo layer)
        {
            Name = layer.Name;
            TimeExtent = layer.TimeExtent;
            Symbols = layer.Symbols;
            Fields = layer.Fields;
            Label = layer.Label;
            LayerVisible = layer.LayerVisible;
        }

        public override object Clone()
        {
            MapLayerInfo layer = MemberwiseClone() as MapLayerInfo;
            layer = null;
            foreach (var key in Symbols.Keys.ToList())
            {
                layer.Symbols[key] = Symbols[key].Clone() as SymbolInfo;
            }
            layer.Fields = Fields == null ? null : Fields.Select(p => p.Clone() as FieldInfo).ToArray();
            layer.Label = Label.Clone() as LabelInfo;
            return layer;
        }

        private FeatureLayer layer;

        [JsonIgnore]
        public FeatureLayer Layer => layer;

        private ShapefileFeatureTable table;

        public async Task SetTableAsync(ShapefileFeatureTable table)
        {
            this.table = table;
            await table.LoadAsync();
            layer = new FeatureLayer(table);
        }

        public bool HasTable => table != null;
        public GeometryType GeometryType => table.GeometryType;

        public override bool LayerVisible
        {
            get => base.LayerVisible;
            set
            {
                base.LayerVisible = value;
                if (Layer != null)
                {
                    Layer.IsVisible = value;
                }
                this.Notify(nameof(LayerVisible));
            }
        }

        public async Task AddFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            await table.AddFeatureAsync(feature);
            NotifyFeaturesChanged(new[] { feature }, null, null, source);
        }

        public SymbolInfo GetDefaultSymbol()
        {
            return GeometryType switch
            {
                GeometryType.Point => SymbolInfo.DefaultPointSymbol,
                GeometryType.Multipoint => SymbolInfo.DefaultPointSymbol,
                GeometryType.Polyline => SymbolInfo.DefaultLineSymbol,
                GeometryType.Polygon => SymbolInfo.DefaultPolygonSymbol,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public async Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            await table.AddFeaturesAsync(features);
            NotifyFeaturesChanged(features, null, null, source);
        }

        public Task<FeatureQueryResult> QueryFeaturesAsync(QueryParameters queryParameters)
        {
            return table.QueryFeaturesAsync(queryParameters);
        }

        public async Task DeleteFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            await table.DeleteFeatureAsync(feature);
            NotifyFeaturesChanged(null, new[] { feature }, null, source);
        }

        public async Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            await table.DeleteFeaturesAsync(features);
            NotifyFeaturesChanged(null, features, null, source);
        }

        public async Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source)
        {
            await table.UpdateFeatureAsync(feature.Feature);
            NotifyFeaturesChanged(null, null, new[] { feature }, source);
        }

        public async Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source)
        {
            await table.UpdateFeaturesAsync(features.Select(p => p.Feature));
            NotifyFeaturesChanged(null, null, features, source);
        }

        public long NumberOfFeatures => table == null ? 0 : table.NumberOfFeatures;

        public Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry)
        {
            return table.CreateFeature(attributes, geometry);
        }

        public Feature CreateFeature()
        {
            return table.CreateFeature();
        }

        public void Dispose()
        {
            table?.Close();
        }

        public Task<Envelope> QueryExtentAsync(QueryParameters queryParameters)
        {
            return table.QueryExtentAsync(queryParameters);
        }

        public event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

        private void NotifyFeaturesChanged(IEnumerable<Feature> added,
            IEnumerable<Feature> deleted,
            IEnumerable<UpdatedFeature> updated,
            FeaturesChangedSource source)
        {
            this.Notify(nameof(NumberOfFeatures));
            var h = new FeaturesChangedEventArgs(this, added, deleted, updated, source);
            FeaturesChanged?.Invoke(this, h);
            Histories.Add(h);
        }

        [JsonIgnore]
        public ObservableCollection<FeaturesChangedEventArgs> Histories { get; } = new ObservableCollection<FeaturesChangedEventArgs>();
    }

    public class UpdatedFeature
    {
        public UpdatedFeature(Feature newFeature)
        {
            Feature = newFeature;
            OldGeometry = Feature.Geometry;
            OldAttributes = new Dictionary<string, object>(Feature.Attributes);
        }

        public UpdatedFeature(Feature newFeature, Geometry oldGeometry, IDictionary<string, object> oldAttributes)
        {
            Feature = newFeature;
            OldGeometry = oldGeometry;
            OldAttributes = oldAttributes;
        }

        public Feature Feature { get; }
        public Geometry OldGeometry { get; }
        public IDictionary<string, object> OldAttributes { get; }
    }

    public enum FeaturesChangedSource
    {
        [Description("绘制")]
        Draw,

        [Description("编辑")]
        Edit,

        [Description("要素操作")]
        FeatureOperation,

        [Description("撤销")]
        Undo,

        [Description("导入")]
        Import
    }

    public class FeaturesChangedEventArgs : EventArgs, INotifyPropertyChanged
    {
        public IReadOnlyList<Feature> DeletedFeatures { get; }
        public IReadOnlyList<Feature> AddedFeatures { get; }
        public IReadOnlyList<UpdatedFeature> UpdatedFeatures { get; }
        public MapLayerInfo Layer { get; }
        public DateTime Time { get; }
        public FeaturesChangedSource Source { get; }
        private bool canUndo = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanUndo
        {
            get => canUndo;
            set => this.SetValueAndNotify(ref canUndo, value, nameof(CanUndo));
        }

        public FeaturesChangedEventArgs(MapLayerInfo layer,
            IEnumerable<Feature> addedFeatures,
            IEnumerable<Feature> deletedFeatures,
            IEnumerable<UpdatedFeature> changedFeatures,
            FeaturesChangedSource source)
        {
            Source = source;
            Time = DateTime.Now;
            int count = 0;
            if (deletedFeatures != null)
            {
                count++;
                DeletedFeatures = new List<Feature>(deletedFeatures).AsReadOnly();
            }
            if (addedFeatures != null)
            {
                count++;
                AddedFeatures = new List<Feature>(addedFeatures).AsReadOnly();
            }
            if (changedFeatures != null)
            {
                count++;
                UpdatedFeatures = new List<UpdatedFeature>(changedFeatures).AsReadOnly();
            }
            Debug.Assert(count == 1);
            Layer = layer;
        }
    }
}