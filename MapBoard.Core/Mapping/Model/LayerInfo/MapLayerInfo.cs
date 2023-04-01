using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib;
using MapBoard.Model;
using MapBoard.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FieldInfo = MapBoard.Model.FieldInfo;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 包含ArcGIS类型的图层
    /// </summary>
    public abstract class MapLayerInfo : LayerInfo, IMapLayerInfo
    {
        protected FeatureTable table;

        private FeatureLayer layer;

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

        public event EventHandler Unattached;

        [JsonIgnore]
        public GeometryType GeometryType => table.GeometryType;

        [JsonIgnore]
        public bool HasTable => table != null;

        [JsonIgnore]
        [AlsoNotifyFor(nameof(GeometryType))]
        public bool IsLoaded { get; set; }

        [JsonIgnore]
        public FeatureLayer Layer => layer;

        [JsonIgnore]
        public Exception LoadError { get; private set; }

        [JsonIgnore]
        public bool CanEdit => this is IEditableLayerInfo && Interaction.CanEdit;

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

        /// <summary>
        /// 修改图层名，并同步修改物理文件的名称
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="layers">Esri图层集合</param>
        /// <returns></returns>
        public abstract Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers);

        public override object Clone()
        {
            var layer = new MapperConfiguration(cfg =>
              {
                  cfg.CreateMap<LayerInfo, MapLayerInfo>();
              }).CreateMapper().Map<MapLayerInfo>(this);

            return layer;
        }

        public virtual void Dispose()
        {
            Unattached?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// 获取添加到MapView的图层集合中的图层
        /// </summary>
        /// <returns></returns>
        public virtual Layer GetLayerForLayerList()
        {
            return layer;
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
                    layer = GetLayerForLoading(table);
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
                //应用属性
                ApplyProperties();
                //应用符号和标签
                this.ApplyStyle();
                IsLoaded = true;
                this.Notify(nameof(GeometryType));
            }
            catch (Exception ex)
            {
                LoadError = ex;
                IsLoaded = false;
                throw;
            }
        }

        public Task<Envelope> QueryExtentAsync(QueryParameters parameters)
        {
            return table.QueryExtentAsync(parameters);
        }

        public Task<FeatureQueryResult> QueryFeaturesAsync(QueryParameters parameters)
        {
            return table.QueryFeaturesAsync(parameters);
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
            layer = GetLayerForLoading(table);
            ApplyProperties();
            this.ApplyStyle();

            //也许是Esri的BUG，如果不重新插入，那么可能啥都不会显示
            layers.RefreshEsriLayer(this);

            IsLoaded = true;
            this.Notify(nameof(NumberOfFeatures), nameof(GeometryType));
        }

        /// <summary>
        /// 获取用于进行操作的图层
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected virtual FeatureLayer GetLayerForLoading(FeatureTable table)
        {
            return new FeatureLayer(table);
        }

        /// <summary>
        /// 获取要素类
        /// </summary>
        /// <returns></returns>
        protected abstract FeatureTable GetTable();

        protected virtual void ApplyProperties()
        {
            Fields = FieldExtension.SyncWithSource(base.Fields, table).ToArray();
            layer.IsVisible = LayerVisible;
            layer.MinScale = Display.MinScale;
            layer.MaxScale = Display.MaxScale;
            layer.Opacity = Display.Opacity;
            layer.DefinitionExpression = DefinitionExpression;
            layer.RenderingMode = (FeatureRenderingMode)Display.RenderingMode;
            PropertyChanged += PropertiesChanged;
            Display.PropertyChanged += PropertiesChanged;
        }

        private void PropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Display.Opacity):
                    layer.Opacity = Display.Opacity;
                    break;

                case nameof(Display.MinScale):
                    layer.MinScale = Display.MinScale;
                    break;

                case nameof(Display.MaxScale):
                    layer.MaxScale = Display.MaxScale;
                    break;

                case nameof(Display.RenderingMode):
                    layer.RenderingMode = (FeatureRenderingMode)Display.RenderingMode;
                    break;

                case nameof(LayerVisible):
                    layer.IsVisible = LayerVisible;
                    break;

                case nameof(DefinitionExpression):
                    layer.DefinitionExpression = DefinitionExpression;
                    break;

                default:
                    break;
            }
        }

        public class Types
        {
            [Description("Shapefile文件")]
            public const string Shapefile = "Shapefile";

            [Description("临时矢量图层")]
            public const string Temp = "Temp";

            [Description("网络矢量服务")]
            public const string WFS = "WFS";

            /// <summary>
            /// 获取给定类型代码的图层类型描述
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public static string GetDescription(string type)
            {
                var types = GetSupportedTypes();
                var target = types.Where(p => p.GetRawConstantValue() as string == type).FirstOrDefault();
                if (target == null)
                {
                    throw new ArgumentException("找不到类型：" + type);
                }
                return target.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault()?.Description ?? type;
            }

            /// <summary>
            /// 获取所有支持的图层类型名
            /// </summary>
            /// <returns></returns>
            public static IEnumerable<string> GetSupportedTypeNames()
            {
                return GetSupportedTypes().Select(p => p.GetRawConstantValue() as string).Concat(new string[] { null });
            }

            private static IEnumerable<System.Reflection.FieldInfo> GetSupportedTypes()
            {
                return typeof(Types).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly);
            }
        }
    }
}