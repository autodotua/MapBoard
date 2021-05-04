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
    public class LabelInfo : INotifyPropertyChanged
    {
        public bool Info { get; set; } = true;
        public bool Date { get; set; }
        public bool Class { get; set; }
        public bool Enable => Info || Date || Class;

        private Color lineColor = Color.FromArgb(255, 248, 220);

        public Color LineColor
        {
            get => lineColor;
            set => this.SetValueAndNotify(ref lineColor, value, nameof(LineColor));
        }

        private Color fillColor = Color.Black;

        public Color FillColor
        {
            get => fillColor;
            set => this.SetValueAndNotify(ref fillColor, value, nameof(FillColor));
        }

        private double fontSize = 12;

        public double FontSize
        {
            get => fontSize;
            set => this.SetValueAndNotify(ref fontSize, value, nameof(FontSize));
        }

        private double strokeThickness = 3;

        public double StrokeThickness
        {
            get => strokeThickness;
            set => this.SetValueAndNotify(ref strokeThickness, value, nameof(StrokeThickness));
        }

        private double minScale = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public double MinScale
        {
            get => minScale;
            set
            {
                if (value >= 0)
                {
                    minScale = value;
                }
                else
                {
                    minScale = 0;
                }
                this.Notify(nameof(MinScale));
            }
        }

        public string GetExpression()
        {
            List<string> exps = new List<string>();
            if (Info)
            {
                exps.Add("$feature." + Resource.DisplayFieldName);
            }
            if (Date)
            {
                exps.Add("$feature." + Resource.TimeExtentFieldName);
            }
            if (Class)
            {
                exps.Add("$feature." + Resource.KeyFieldName);
            }
            string exp = string.Join("+\"\\n\"+", exps);
            return exp;
        }

        public string ToJson()
        {
            var labelJson = JObject.Parse(Resource.LabelJson);
            SetLabelJsonValue<byte>(labelJson, "symbol.haloColor", GetRgbaFromColor(LineColor));
            SetLabelJsonValue<byte>(labelJson, "symbol.color", GetRgbaFromColor(FillColor));
            SetLabelJsonValue(labelJson, "symbol.font.size", FontSize);
            SetLabelJsonValue(labelJson, "symbol.haloSize", StrokeThickness);
            SetLabelJsonValue(labelJson, "minScale", MinScale);
            SetLabelJsonValue(labelJson, "labelExpressionInfo.expression", GetExpression());
            return labelJson.ToString();
        }

        //private static LabelInfo FromJson(string json)
        //{
        //    JObject labelJson = JObject.Parse(json);

        //    LineColor = GetColorFromArgb(GetLabelJsonValue<byte>(labelJson,"symbol.haloColor", new byte[] { 0, 0, 0, 0 }));
        //    FillColor = GetColorFromArgb(GetLabelJsonValue<byte>(labelJson, "symbol.color", new byte[] { 0, 0, 0, 0 }));
        //    FontSize = GetLabelJsonValue(labelJson, "symbol.font.size", 9);
        //    StrokeThickness = GetLabelJsonValue(labelJson, "symbol.haloSize", 1);
        //    MinScale = GetLabelJsonValue(labelJson, "minScale", 0);
        //    string exp = GetLabelJsonValue(labelJson, "labelExpressionInfo.expression", "");
        //    Info = exp.Contains(Resource.DisplayFieldName);
        //    Date = exp.Contains(Resource.TimeExtentFieldName);
        //    Class = exp.Contains(Resource.KeyFieldName);
        //}

        private static byte[] GetRgbaFromColor(Color color)
        {
            return new byte[] { color.R, color.G, color.B, color.A };
        }

        private static Color GetColorFromArgb(IList<byte> argb)
        {
            return Color.FromArgb(argb[3], argb[0], argb[1], argb[2]);
        }

        public void SetLabelJsonValue<T>(JObject labelJson, string path, IList<T> value)
        {
            string[] paths = path.Split('.');

            JToken token = labelJson[paths[0]];
            for (int i = 1; i < paths.Length; i++)
            {
                token = token[paths[i]];
            }

            for (int i = 0; i < value.Count; i++)
            {
                (token as JArray)[i] = new JValue(value[i]);
            }
        }

        public void SetLabelJsonValue<T>(JObject labelJson, string path, T value)
        {
            string[] paths = path.Split('.');

            JToken token = labelJson[paths[0]];
            for (int i = 1; i < paths.Length; i++)
            {
                token = token[paths[i]];
            }

           (token as JValue).Value = value;
        }

        public T[] GetLabelJsonValue<T>(JObject labelJson, string path, IEnumerable<T> defaultValue)
        {
            try
            {
                string[] paths = path.Split('.');

                JToken token = labelJson[paths[0]];
                for (int i = 1; i < paths.Length; i++)
                {
                    token = token[paths[i]];
                }

                if (token is JArray value)
                {
                    if (value[0] is JValue)
                    {
                        return value.Select(p => (p as JValue).Value<T>()).ToArray();
                    }
                }
                return defaultValue.ToArray();
            }
            catch (Exception ex)
            {
                return defaultValue.ToArray();
            }
        }

        public T GetLabelJsonValue<T>(JObject labelJson, string path, T defaultValue)
        {
            try
            {
                string[] paths = path.Split('.');

                JToken token = labelJson[paths[0]];
                for (int i = 1; i < paths.Length; i++)
                {
                    token = token[paths[i]];
                }

                if (token is JValue value)
                {
                    return value.Value<T>();
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }
    }
}