using Esri.ArcGISRuntime.Geometry;
using FzLib.Extension;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
using System.Windows;
using System.Windows.Controls;
using static MapBoard.Main.UI.Map.EditorHelper;

namespace MapBoard.Main.UI.Bar
{
    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class MeasureBar : BarBase
    {
        public override FeatureAttributes Attributes => throw new System.NotImplementedException();

        public MeasureBar()
        {
            InitializeComponent();
        }

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
            MapView.Editor.GeometryChanged += Editor_GeometryChanged;
        }

        private void Editor_GeometryChanged(object sender, Esri.ArcGISRuntime.UI.GeometryChangedEventArgs e)
        {
            if (IsOpen)
            {
                if (e.NewGeometry is Polyline line)
                {
                    var length = line.GetLength();
                    if (length < 10000)
                    {
                        Length = string.Format("{0:0.000}米", length);
                    }
                    else
                    {
                        Length = string.Format("{0:0.000}千米", length / 1000);
                    }
                }
                else if (e.NewGeometry is Polygon polygon)
                {
                    var length = polygon.GetLength();
                    var area = polygon.GetArea();

                    if (length < 10000)
                    {
                        Length = string.Format("{0:0.000}米", length);
                    }
                    else
                    {
                        Length = string.Format("{0:0.000}千米", length / 1000);
                    }
                    if (area < 1_000_000)
                    {
                        Area = string.Format("{0:0.000}平方米", area);
                    }
                    else
                    {
                        Area = string.Format("{0:0.000}平方千米", area / 1_000_000);
                    }
                }
                else
                {
                    Length = "";
                    Area = "";
                }
            }
        }

        public override double ExpandDistance => 28;
        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;

        private string lengthTitle;

        public string LengthTitle
        {
            get => lengthTitle;
            set => this.SetValueAndNotify(ref lengthTitle, value, nameof(LengthTitle));
        }

        private string length;

        public string Length
        {
            get => length;
            set => this.SetValueAndNotify(ref length, value, nameof(Length));
        }

        private string areaTitle;

        public string AreaTitle
        {
            get => areaTitle;
            set => this.SetValueAndNotify(ref areaTitle, value, nameof(AreaTitle));
        }

        private string area;

        public string Area
        {
            get => area;
            set => this.SetValueAndNotify(ref area, value, nameof(Area));
        }

        private void BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTask.Draw)
            {
                switch (MapView.Editor.Mode)
                {
                    case EditMode.MeasureLength:
                        LengthTitle = "长度：";
                        AreaTitle = "";
                        Length = "0米";
                        Area = "";
                        break;

                    case EditMode.MeasureArea:
                        LengthTitle = "周长：";
                        AreaTitle = "面积：";
                        Length = "0米";
                        Area = "0平方米";
                        break;

                    default:
                        return;
                }
                Expand();
            }
            else
            {
                Collapse();
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Editor.Cancel();
        }

        private void RemoveSelectedVertexButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.SketchEditor.RemoveSelectedVertex();
        }
    }
}