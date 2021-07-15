using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Extension;
using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public interface IWriteableLayerInfo : IMapLayerInfo
    {
        ObservableCollection<FeaturesChangedEventArgs> Histories { get; }

        event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

        Task AddFeatureAsync(Feature feature, FeaturesChangedSource source);

        Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source);

        Feature CreateFeature();

        Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry);

        Task DeleteFeatureAsync(Feature feature, FeaturesChangedSource source);

        Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source);

        Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source);

        Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source);
    }

    public interface IMapLayerInfo : ILayerInfo
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

        Task LoadTableAsync();
    }

    //http://192.168.1.18:8080/geoserver/topp/ows?service=WFS&request=GetCapabilities

    public class ShapefileMapLayerInfo : MapLayerInfo, IWriteableLayerInfo
    {
        public ShapefileMapLayerInfo() : base()
        {
        }

        public ShapefileMapLayerInfo(ILayerInfo layer) : base(layer)
        {
        }

        public ShapefileMapLayerInfo(string name) : base(name)
        {
        }

        public ShapefileMapLayerInfo(MapLayerInfo template, string newName, bool includeFields)
        {
            new MapperConfiguration(cfg =>
               {
                   cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
               }).CreateMapper().Map<LayerInfo, ShapefileMapLayerInfo>(template, this);
            Name = newName;

            if (!includeFields)
            {
                Fields = Array.Empty<FieldInfo>();
            }
        }

        public string FilePath => Path.Combine(Parameters.DataPath, Name + ".shp");

        public override object Clone()
        {
            var layer = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
            }).CreateMapper().Map<ShapefileMapLayerInfo>(this);
            return layer;
        }

        public override string Type => Types.Shapefile;
        public override bool IsWriteable => true;

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
        [IgnoreMap]
        public ObservableCollection<FeaturesChangedEventArgs> Histories { get; private set; } = new ObservableCollection<FeaturesChangedEventArgs>();

        public async Task AddFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                || feature.Attributes[Parameters.CreateTimeFieldName] == null)
            {
                feature.SetAttributeValue(Parameters.CreateTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
            }
            await table.AddFeatureAsync(feature);

            NotifyFeaturesChanged(new[] { feature }, null, null, source);
        }

        public async Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            foreach (var feature in features)
            {
                if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                    || feature.Attributes[Parameters.CreateTimeFieldName] == null)
                {
                    feature.SetAttributeValue(Parameters.CreateTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
                }
            }
            await table.AddFeaturesAsync(features);
            NotifyFeaturesChanged(features, null, null, source);
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

        public Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry)
        {
            return table.CreateFeature(attributes, geometry);
        }

        public Feature CreateFeature()
        {
            return table.CreateFeature();
        }

        public async override Task LoadTableAsync()
        {
            table = new ShapefileFeatureTable(FilePath);
            await table.LoadAsync();
        }

        public override void Dispose()
        {
            (table as ShapefileFeatureTable)?.Close();
            table = null;
            base.Dispose();
        }

        public async override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            Debug.Assert(!string.IsNullOrEmpty(newName));
            if (newName == Name)
            {
                return;
            }
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                   || newName.Length > 240 || newName.Length < 1)
            {
                throw new IOException("新文件名不合法");
            }
            //检查文件存在
            foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, Layer.Name))
            {
                if (File.Exists(Path.Combine(Parameters.DataPath, newName + Path.GetExtension(file))))
                {
                    throw new IOException("该名称的文件已存在");
                }
            }
            //检查图层是否在集合中
            if (!layers.Contains(Layer))
            {
                throw new ArgumentException("本图层不在给定的图层集合中");
            }
            int index = layers.IndexOf(Layer);

            (table as ShapefileFeatureTable).Close();
            //重命名
            foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, Layer.Name))
            {
                File.Move(file, Path.Combine(Parameters.DataPath, newName + Path.GetExtension(file)));
            }
            Name = newName;
            await LoadAsync();
            layers[index] = Layer;
        }
    }

    public class EmptyMapLayerInfo : MapLayerInfo
    {
        public override string Type => "Empty";
        public override bool IsWriteable => false;

        public async override Task LoadTableAsync()
        {
        }

        private EmptyMapLayerInfo()
        {
        }

        public static MapLayerInfo CreateTemplate()
        {
            return new EmptyMapLayerInfo();
        }

        public async override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
        }
    }

    public abstract class MapLayerInfo : LayerInfo, IDisposable, IMapLayerInfo
    {
        public static readonly HashSet<string> SupportedLayerTypes = new HashSet<string>()
        {
           Types.Shapefile,null
        };

        public class Types
        {
            public const string Shapefile = "Shapefile";
        }

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

        public MapLayerInfo(string name)
        {
            Name = name;
        }

        public MapLayerInfo(ILayerInfo layer)
        {
            new MapperConfiguration(cfg =>
           {
               cfg.CreateMap<LayerInfo, MapLayerInfo>();
           }).CreateMapper().Map(layer, this);
        }

        public override object Clone()
        {
            var layer = new MapperConfiguration(cfg =>
              {
                  cfg.CreateMap<LayerInfo, MapLayerInfo>();
              }).CreateMapper().Map<MapLayerInfo>(this);

            return layer;
        }

        /// <summary>
        /// 修改图层名，并同步修改物理文件的名称
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="layers">Esri图层集合</param>
        /// <returns></returns>
        public abstract Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers);

        private FeatureLayer layer;

        [JsonIgnore]
        public FeatureLayer Layer => layer;

        protected FeatureTable table;

        public abstract Task LoadTableAsync();

        public async Task LoadAsync()
        {
            await LoadTableAsync();
            layer = new FeatureLayer(table);

            await Task.Run(this.ApplyStyle);
            await this.LayerCompleteAsync();
        }

        [JsonIgnore]
        public bool HasTable => table != null;

        [JsonIgnore]
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

        public SymbolInfo GetDefaultSymbol()
        {
            return GeometryType switch
            {
                GeometryType.Point => SymbolInfo.DefaultPointSymbol,
                GeometryType.Multipoint => SymbolInfo.DefaultPointSymbol,
                GeometryType.Polyline => SymbolInfo.DefaultLineSymbol,
                GeometryType.Polygon => SymbolInfo.DefaultPolygonSymbol,
                _ => throw new InvalidEnumArgumentException(),
            };
        }

        [JsonIgnore]
        public long NumberOfFeatures
        {
            get
            {
                try
                {
                    return table == null ? 0 : table.NumberOfFeatures;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public virtual void Dispose()
        {
            Unattached?.Invoke(this, new EventArgs());
        }

        [JsonIgnore]
        public Func<QueryParameters, Task<Envelope>> QueryExtentAsync => table.QueryExtentAsync;

        [JsonIgnore]
        public Func<QueryParameters, Task<FeatureQueryResult>> QueryFeaturesAsync => table.QueryFeaturesAsync;

        public event EventHandler Unattached;
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