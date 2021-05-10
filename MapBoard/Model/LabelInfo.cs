using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Labeling;
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
        private Color backgroundColor = Color.Transparent;
        private Color fontColor = Color.Black;
        private double fontSize = 12;
        private Color haloColor = Color.FromArgb(255, 248, 220);
        private double haloWidth = 3;
        private double minScale = 0;
        private bool newLine;
        private Color outlineColor = Color.Transparent;
        private double outlineWidth = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 是否允许重叠
        /// </summary>
        public bool AllowOverlap { get; set; }

        /// <summary>
        /// 是否允许重复
        /// </summary>
        public bool AllowRepeat { get; set; }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set => this.SetValueAndNotify(ref backgroundColor, value, nameof(BackgroundColor));
        }

        public bool Class { get; set; }
        public bool Date { get; set; }
        public bool Enable => Info || Date || Class;

        public Color FontColor
        {
            get => fontColor;
            set => this.SetValueAndNotify(ref fontColor, value, nameof(FontColor));
        }

        public double FontSize
        {
            get => fontSize;
            set => this.SetValueAndNotify(ref fontSize, value, nameof(FontSize));
        }

        public Color HaloColor
        {
            get => haloColor;
            set => this.SetValueAndNotify(ref haloColor, value, nameof(HaloColor));
        }

        public double HaloWidth
        {
            get => haloWidth;
            set => this.SetValueAndNotify(ref haloWidth, value, nameof(HaloWidth));
        }

        public bool Info { get; set; } = true;

        /// <summary>
        /// 标签布局
        /// </summary>
        public int Layout { get; set; } = 0;

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

        public bool NewLine
        {
            get => newLine;
            set => this.SetValueAndNotify(ref newLine, value, nameof(NewLine));
        }

        public Color OutlineColor
        {
            get => outlineColor;
            set => this.SetValueAndNotify(ref outlineColor, value, nameof(OutlineColor));
        }

        public double OutlineWidth
        {
            get => outlineWidth;
            set => this.SetValueAndNotify(ref outlineWidth, value, nameof(OutlineWidth));
        }

        public string Get11Expression()
        {
            List<string> exps = new List<string>();
            if (Info)
            {
                exps.Add($"[{ Resource.LabelFieldName}]");
            }
            if (Date)
            {
                exps.Add($"[{ Resource.DateFieldName}]");
            }
            if (Class)
            {
                exps.Add($"[{ Resource.ClassFieldName}]");
            }
            string exp = string.Join("+\"\\n\"+", exps);
            return exp;
        }

        public string GetExpression()
        {
            string l = Resource.LabelFieldName;
            string d = Resource.DateFieldName;
            string c = Resource.ClassFieldName;

            string newLine = $@"
if({NewLine})
{{
    exp=exp+'\n';
}}
else
{{
    exp=exp+'    ';
}}
";
            string exp = @$"
var exp='';
if({Info}&&$feature.{ l}!='')
{{
    exp=exp+$feature.{ l};
    {newLine}
}}
if({Date})
{{
if($feature.{ d}!=null)
{{
    exp=exp+Year($feature.{d})+'-'+Month($feature.{d})+'-'+Day($feature.{d});
    {newLine}
}}
}}
if({Class}&&$feature.{ c}!='')
{{
    exp=exp+$feature.{ c};
    {newLine}
}}
if({NewLine})
{{
    exp=Left(exp,Count(exp)-1);
}}
else
{{
    exp=Left(exp,Count(exp)-4);
}}
exp";
            return exp;
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

        public string ToJson()
        {
            var labelJson = JObject.Parse(Resource.LabelJson);
            SetLabelJsonValue<byte>(labelJson, "symbol.haloColor", GetRgbaFromColor(HaloColor));
            SetLabelJsonValue<byte>(labelJson, "symbol.color", GetRgbaFromColor(FontColor));
            SetLabelJsonValue(labelJson, "symbol.font.size", FontSize);
            SetLabelJsonValue(labelJson, "symbol.haloSize", HaloWidth);
            SetLabelJsonValue(labelJson, "minScale", MinScale);
            SetLabelJsonValue(labelJson, "labelExpressionInfo.expression", GetExpression());
            return labelJson.ToString();
        }

        private static Color GetColorFromArgb(IList<byte> argb)
        {
            return Color.FromArgb(argb[3], argb[0], argb[1], argb[2]);
        }

        private static byte[] GetRgbaFromColor(Color color)
        {
            return new byte[] { color.R, color.G, color.B, color.A };
        }
    }
}