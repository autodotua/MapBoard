using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib;
using MapBoard.Model;
using MapBoard.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public abstract class MapLayerInfo : LayerInfo, IMapLayerInfo
    {
        public static readonly HashSet<string> SupportedLayerTypes = new HashSet<string>()
        {
           Types.Shapefile,null,Types.WFS,Types.Temp
        };

        public class Types
        {
            public const string Shapefile = "Shapefile";
            public const string WFS = "WFS";
            public const string Temp = "Temp";
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

        /// <summary>
        /// 获取要素类
        /// </summary>
        /// <returns></returns>
        protected abstract FeatureTable GetTable();

        /// <summary>
        /// 获取用于进行操作的图层
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual FeatureLayer GetNewLayer(FeatureTable table)
        {
            return new FeatureLayer(table);
        }

        /// <summary>
        /// 获取添加到MapView的图层集合中的图层
        /// </summary>
        /// <returns></returns>
        public virtual Layer GetAddedLayer()
        {
            return layer;
        }

        private bool isLoaded;

        [JsonIgnore]
        public bool IsLoaded
        {
            get => isLoaded;
            private set
            {
                this.SetValueAndNotify(ref isLoaded, value, nameof(IsLoaded));
                this.Notify(nameof(GeometryType));
            }
        }

        /// <summary>
        /// 重新加载
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        public async Task ReloadAsync(MapLayerCollection layers)
        {
            FeatureTable newTable = GetTable();
            try
            {
                await newTable.LoadAsync();
            }
            catch
            {
                IsLoaded = false;
                throw;
            }
            //如果上面加载失败，那么不会执行下面的语句
            table = newTable;
            layer = GetNewLayer(table);
            await Task.Run(this.ApplyStyle);
            await this.LayerCompleteAsync();

            //也许是Esri的BUG，如果不重新插入，那么可能啥都不会显示
            layers.RefreshEsriLayer(this);

            IsLoaded = true;
            this.Notify(nameof(NumberOfFeatures));
            this.Notify(nameof(GeometryType));
            LoadCompleted();
        }

        /// <summary>
        /// 加载图层
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            try
            {
                //确保即使Table没有成功加载，Layer也得先建起来
                try
                {
                    table = GetTable();
                }
                catch (Exception ex)
                {
                    LoadError = new Exception("创建要素表失败", ex);
                    IsLoaded = false;
                    return;
                }
                finally
                {
                    layer = GetNewLayer(table);
                }
                try
                {
                    await table.LoadAsync().TimeoutAfter(Parameters.LoadTimeout);
                }
                catch (TimeoutException)
                {
                    table.CancelLoad();
                    throw new TimeoutException("加载超时，通常是无法连接网络服务所致");
                }
                await Task.Run(this.ApplyStyle);
                await this.LayerCompleteAsync();
                IsLoaded = true;
                this.Notify(nameof(GeometryType));
                LoadCompleted();
            }
            catch (Exception ex)
            {
                LoadError = ex;
                IsLoaded = false;
                throw;
            }
        }

        [JsonIgnore]
        public Exception LoadError { get; private set; }

        protected virtual void LoadCompleted()
        {
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
                _ => null
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