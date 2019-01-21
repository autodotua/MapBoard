using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MapBoard.IO.CoordinateTransformation;
using static MapBoard.Style.StyleHelper;

namespace MapBoard.Style
{
    public class StyleInfo : FzLib.Extension.ExtendedINotifyPropertyChanged, ICloneable
    {


        public string Name { get; set; }
        [JsonIgnore]
        public string FileName => Path.Combine(Config.DataPath, Name + ".shp");
        public double LineWidth { get; set; } = 5;

        public Color LineColor { get; set; } = Color.Red;
        public Color FillColor
        {
            get;
            set;
        } = Color.Green;
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
                SetValueAndNotify(ref table, value, nameof(LayerVisible), nameof(TypeDescription), nameof(FeatureCount));
            }
        }
        public async Task LayerComplete()
        {
            Layer.IsVisible = LayerVisible;
            Layer.LabelsEnabled = LabelVisible;

            await this.SetTimeExtent();
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
                //if (StyleCollection.Instance.Styles.Contains(this))
                //{
                //    Config.Instance.ShapefileStyles.First(p => p.Name == Name).LayerVisible = value;
                //}
                if (Layer != null)
                {
                    Layer.IsVisible = value;
                }
                Notify(nameof(LayerVisible));
            }
        }

        private bool labelVisible = true;
        public bool LabelVisible
        {
            get
            {
                return labelVisible;
            }
            set
            {
                labelVisible = value;
                if (Layer != null)
                {
                    Layer.LabelsEnabled = value;
                }
                Notify(nameof(LabelVisible));
            }
        }

        private TimeExtentInfo timeExtent;
        public TimeExtentInfo TimeExtent
        {
            get => timeExtent;
            set => SetValueAndNotify(ref timeExtent, value, nameof(TimeExtentEnable));
        }

        [JsonIgnore]
        public bool TimeExtentEnable
        {
            get => TimeExtent==null?false: TimeExtent.IsEnable;
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

                Notify(nameof(TimeExtentEnable));
            }
        }


        public string LabelJson { get; set; } = Resource.Resource.LabelJson;


        public void UpdateFeatureCount()
        {
            //QueryParameters p = new QueryParameters
            //{
            //    SpatialRelationship = SpatialRelationship.Contains
            //};
            //FeatureCount = (int)await Table.QueryFeatureCountAsync(p);
            Notify(nameof(FeatureCount));
        }

        public async Task<FeatureQueryResult> GetAllFeatures()
        {
            QueryParameters query = new QueryParameters
            {
                SpatialRelationship = SpatialRelationship.Contains
            };
            FeatureQueryResult result = await Table.QueryFeaturesAsync(query);
            return result;
        }

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
        public StyleInfo Clone()
        {
            return MemberwiseClone() as StyleInfo;
        }
        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }

        public void CopyStyleFrom(StyleInfo style)
        {
            LineWidth = style.LineWidth;
            LineColor = style.LineColor;
            FillColor = style.FillColor;
            LabelJson = style.LabelJson;
            Notify(nameof(LineColor),
                nameof(LineWidth),
                nameof(FillColor),
                nameof(Type),
                nameof(FeatureCount),
                nameof(LayerVisible),
                nameof(TypeDescription));
        }



        public override string ToString()
        {
            return Name;
        }

        //public bool StyleEquals(StyleInfo style)
        //{
        //    return LineWidth == style.LineWidth
        //        && LineColor == style.LineColor
        //        && FillColor == style.FillColor
        //        && Type == style.Type;
        //}
        //public bool StyleEquals(StyleInfo style, GeometryType type)
        //{
        //    return LineWidth == style.LineWidth
        //        && LineColor == style.LineColor
        //        && FillColor == style.FillColor
        //        && Type == type;
        //}
    }
}
