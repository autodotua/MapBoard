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
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map
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

        private void NotifyFeaturesChanged(IEnumerable<Feature> added, IEnumerable<Feature> deleted, IEnumerable<Feature> updated)
        {
            this.Notify(nameof(NumberOfFeatures));
        }

        public async Task AddFeatureAsync(Feature feature)
        {
            await table.AddFeatureAsync(feature);
            NotifyFeaturesChanged(new[] { feature }, null, null);
        }

        public SymbolInfo GetDefaultSymbol()
        {
            return GeometryType switch
            {
                Esri.ArcGISRuntime.Geometry.GeometryType.Point => SymbolInfo.DefaultPointSymbol,
                Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint => SymbolInfo.DefaultPointSymbol,
                Esri.ArcGISRuntime.Geometry.GeometryType.Polyline => SymbolInfo.DefaultLineSymbol,
                Esri.ArcGISRuntime.Geometry.GeometryType.Polygon => SymbolInfo.DefaultPolygonSymbol,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public async Task AddFeaturesAsync(IEnumerable<Feature> features)
        {
            await table.AddFeaturesAsync(features);
            NotifyFeaturesChanged(features, null, null);
        }

        public Task<FeatureQueryResult> QueryFeaturesAsync(QueryParameters queryParameters)
        {
            return table.QueryFeaturesAsync(queryParameters);
        }

        public async Task DeleteFeatureAsync(Feature feature)
        {
            await table.DeleteFeatureAsync(feature);
            NotifyFeaturesChanged(null, new[] { feature }, null);
        }

        public async Task DeleteFeaturesAsync(IEnumerable<Feature> features)
        {
            await table.DeleteFeaturesAsync(features);
            NotifyFeaturesChanged(null, features, null);
        }

        public async Task UpdateFeatureAsync(Feature feature)
        {
            await table.UpdateFeatureAsync(feature);
            NotifyFeaturesChanged(null, null, new[] { feature });
        }

        public async Task UpdateFeaturesAsync(IEnumerable<Feature> features)
        {
            await table.UpdateFeaturesAsync(features);
            NotifyFeaturesChanged(null, null, features);
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
    }
}