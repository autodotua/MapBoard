using FzLib.Control.Dialog;
using FzLib.Control.Extension;
using MapBoard.Common;
using MapBoard.IO;
using MapBoard.Style;
using MapBoard.UI.Map;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace MapBoard.UI.Panel
{
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class StyleSettingPanel : ExtendedUserControl
    {
        public StyleSettingPanel()
        {
            InitializeComponent();
        }

        private string styleName;
        public string StyleName
        {
            get => styleName;
            set => SetValueAndNotify(ref styleName, value, nameof(StyleName));
        }

        private double lineWidth = 5;
        public double LineWidth
        {
            get => lineWidth;
            set => SetValueAndNotify(ref lineWidth, value, nameof(LineWidth));
        }

        public System.Drawing.Color LineColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(lineColorPicker.ColorBrush.Color);
            set => lineColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }


        public System.Drawing.Color FillColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(fillColorPicker.ColorBrush.Color);
            set => fillColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }

        public System.Drawing.Color LabelLineColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(labelLineColor.ColorBrush.Color);
            set => labelLineColor.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }


        public System.Drawing.Color LabelFillColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(labelFillColor.ColorBrush.Color);
            set => labelFillColor.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }

        private double labelFontSize;
        public double LabelFontSize
        {
            get => labelFontSize;
            set => SetValueAndNotify(ref labelFontSize, value, nameof(LabelFontSize));
        }
        private double labelStrokeThickness;
        public double LabelStrokeThickness
        {
            get => labelStrokeThickness;
            set => SetValueAndNotify(ref labelStrokeThickness, value, nameof(LabelStrokeThickness));
        }

        private double labelMinScale;
        public double LabelMinScale
        {
            get => labelMinScale;
            set
            {
                if (value >= 0)
                {
                    labelMinScale = value;
                }
                else
                {
                    labelMinScale = 0;
                }
                Notify(nameof(LabelMinScale));
            }
        }

        public void SetStyleFromUI()
        {
            var style = StyleCollection.Instance.Selected;
            style.LineColor = LineColor;
            style.FillColor = FillColor;
            style.LineWidth = LineWidth;

            SetLabelFromUI();
            //styleSetting.SetStyleFromUI(StyleCollection.Instance.Selected);

            string newName = StyleName;
            if (newName != style.Name)
            {
                int index = StyleCollection.Instance.Styles.IndexOf(StyleCollection.Instance.Selected);

                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.Length > 240)
                {
                    SnakeBar.ShowError("新文件名不合法");
                    goto end;
                }
                if (File.Exists(Path.Combine(Config.DataPath, newName + ".shp")))
                {
                    SnakeBar.ShowError("文件已存在");
                }
                try
                {
                    StyleHelper.RemoveStyle(StyleCollection.Instance.Selected, false);
                    foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, style.Name))
                    {
                        File.Move(file, Path.Combine(Config.DataPath, newName + Path.GetExtension(file)));
                    }
                    style.Name = newName;
                }
                catch (Exception ex)
                {
                    SnakeBar.ShowException(ex, "重命名失败");
                }
                end:
                style.Table = null;
                StyleCollection.Instance.Styles.Insert(index,style);
            }
            else
            {
                StyleHelper.ApplyStyles(style);
            }

        }

        public void ResetStyleSettingUI()
        {
            if (!IsLoaded || StyleCollection.Instance.Selected == null)
            {
                return;
            }
            else
            {
                StyleName = StyleCollection.Instance.Selected?.Name;

                LineWidth = StyleCollection.Instance.Selected.LineWidth;
                LineColor = StyleCollection.Instance.Selected.LineColor;
                FillColor = StyleCollection.Instance.Selected.FillColor;
                ResetLabelSettingUI();
            }
        }

        private void SetScaleButtonClick(object sender, RoutedEventArgs e)
        {
            LabelMinScale = ArcMapView.Instance.MapScale;
        }
        private JObject labelJson;
        private void SetLabelFromUI()
        {
            labelJson = JObject.Parse(Resource.Resource.LabelJson);
            SetLabelJsonValue<byte>("symbol.haloColor", GetRgbaFromColor(LabelLineColor));
            SetLabelJsonValue<byte>("symbol.color", GetRgbaFromColor(LabelFillColor));
            SetLabelJsonValue("symbol.font.size", LabelFontSize);
            SetLabelJsonValue("symbol.haloSize", LabelStrokeThickness);
            SetLabelJsonValue("minScale", LabelMinScale);
            StyleCollection.Instance.Selected.LabelJson = labelJson.ToString();
        }
        private void ResetLabelSettingUI()
        {
            labelJson = JObject.Parse(StyleCollection.Instance.Selected.LabelJson);
            LabelLineColor = GetColorFromArgb(GetLabelJsonValue<byte>("symbol.haloColor", new byte[] { 0, 0, 0, 0 }));
            LabelFillColor = GetColorFromArgb(GetLabelJsonValue<byte>("symbol.color", new byte[] { 0, 0, 0, 0 }));
            LabelFontSize = GetLabelJsonValue("symbol.font.size", 9);
            LabelStrokeThickness = GetLabelJsonValue("symbol.haloSize", 1);
            LabelMinScale = GetLabelJsonValue("minScale", 0);
        }
        private static byte[] GetRgbaFromColor(System.Drawing.Color color)
        {
            return new byte[] { color.R, color.G, color.B, color.A };
        }
        private static System.Drawing.Color GetColorFromArgb(IList<byte> argb)
        {
            return System.Drawing.Color.FromArgb(argb[3], argb[0], argb[1], argb[2]);
        }

        public void SetLabelJsonValue<T>(string path, IList<T> value)
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
        public void SetLabelJsonValue<T>(string path, T value)
        {
            string[] paths = path.Split('.');

            JToken token = labelJson[paths[0]];
            for (int i = 1; i < paths.Length; i++)
            {
                token = token[paths[i]];
            }

           (token as JValue).Value = value;

        }

        public T[] GetLabelJsonValue<T>(string path, IEnumerable<T> defaultValue)
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
        public T GetLabelJsonValue<T>(string path, T defaultValue)
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


        //private bool useStatic = false;
        //public bool UseStatic
        //{
        //    get => useStatic;
        //    set => SetValueAndNotify(ref useStatic, value, nameof(UseStatic));
        //}


    }
}
