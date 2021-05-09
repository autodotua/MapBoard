using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Common.Resource;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FzLib.Extension.ExtendedINotifyPropertyChanged;
using static MapBoard.Common.CoordinateTransformation;
using static MapBoard.Main.Util.LayerUtility;

namespace MapBoard.Main.Model
{
    public class LayerInfo : INotifyPropertyChanged, ICloneable
    {
        public string Name { get; set; }

        [JsonIgnore]
        public string FileName => Path.Combine(Config.DataPath, Name + ".shp");

        public Dictionary<string, SymbolInfo> Symbols { get; set; } = new Dictionary<string, SymbolInfo>();

        [JsonIgnore]
        public GeometryType Type => table == null ? GeometryType.Unknown : table.GeometryType;

        private ShapefileFeatureTable table;

        [JsonIgnore]
        public ShapefileFeatureTable Table
        {
            get => table;
            set
            {
                //if(value.FeatureLayer==null)
                //{
                //    throw new Exception("必须先加入Layer才能设置Table");
                //}
                //value.FeatureLayer.IsVisible = LayerVisible;
                this.SetValueAndNotify(ref table, value, nameof(LayerVisible), nameof(TypeDescription), nameof(FeatureCount));
            }
        }

        [JsonIgnore]
        public FeatureLayer Layer => Table?.FeatureLayer;

        private bool layerVisible = true;

        public bool LayerVisible
        {
            get
            {
                //if (Layer == null)
                //{
                //    return true;
                //}
                return layerVisible;
            }
            set
            {
                layerVisible = value;
                //if (LayerCollection.Instance.Layers.Contains(this))
                //{
                //    Config.Instance.ShapefileLayers.First(p => p.Name == Name).LayerVisible = value;
                //}
                if (Layer != null)
                {
                    Layer.IsVisible = value;
                }
                this.Notify(nameof(LayerVisible));
            }
        }

        //private LabelDisplay label = new LabelDisplay();
        //public LabelDisplay Label
        //{
        //    get
        //    {
        //        return label;
        //    }
        //    set
        //    {
        //        label = value;
        //        if (Layer != null)
        //        {
        //            Layer.LabelsEnabled = Label.Enable;
        //        }
        //        this.Notify(nameof(Label));
        //    }
        //}

        private TimeExtentInfo timeExtent;

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeExtentInfo TimeExtent
        {
            get => timeExtent;
            set => this.SetValueAndNotify(ref timeExtent, value, nameof(TimeExtentEnable));
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
                        this.SetTimeExtent();
                    }
                }

                this.Notify(nameof(TimeExtentEnable));
            }
        }

        public LabelInfo Label { get; set; } = new LabelInfo();

        public void UpdateFeatureCount()
        {
            this.Notify(nameof(FeatureCount));
        }

        public async Task<FeatureQueryResult> GetAllFeatures()
        {
            FeatureQueryResult result = await Table.QueryFeaturesAsync(new QueryParameters());
            return result;
        }

        [JsonIgnore]
        public long FeatureCount
        {
            get
            {
                try
                {
                    return Table == null || Table.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded ? 0 : Table.NumberOfFeatures;
                }
                catch
                {
                    return 0;
                }
            }
        }

        [JsonIgnore]
        public string TypeDescription
        {
            get
            {
                switch (Type)
                {
                    case GeometryType.Point:
                        return "点";

                    case GeometryType.Polygon:
                        return "面";

                    case GeometryType.Polyline:
                        return "线";

                    case GeometryType.Multipoint:
                        return "多点";

                    default:
                        return "未知";
                }
            }
        }

        public LayerInfo Clone()
        {
            return MemberwiseClone() as LayerInfo;
        }

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }

        public void CopyLayerFrom(LayerInfo layer)
        {
            foreach (var s in layer.Symbols)
            {
                Symbols.Add(s.Key, new SymbolInfo()
                {
                    FillColor = s.Value.FillColor,
                    LineColor = s.Value.LineColor,
                    LineWidth = s.Value.LineWidth,
                });
            }
            //Renderer.LineWidth = style.Renderer.LineWidth;
            //Renderer.LineColor = style.Renderer.LineColor;
            //Renderer.FillColor = style.Renderer.FillColor;
            Label = layer.Label;
            this.Notify(
                //nameof(Renderer.LineColor),
                //nameof(Renderer),
                //nameof(Renderer.LineWidth),
                //nameof(Renderer.FillColor),
                nameof(Type),
                nameof(FeatureCount),
                nameof(LayerVisible),
                nameof(TypeDescription));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}