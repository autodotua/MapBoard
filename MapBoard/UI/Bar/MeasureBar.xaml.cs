using Esri.ArcGISRuntime.Geometry;
using FzLib.Extension;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System.Windows;
using static MapBoard.Main.UI.Map.EditorHelper;

namespace MapBoard.Main.UI.Bar
{
    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class MeasureBar : BarBase
    {
        protected override bool CanEdit => throw new System.NotImplementedException();
        public override FeatureAttributes Attributes => throw new System.NotImplementedException();

        public MeasureBar()
        {
            InitializeComponent();
            BoardTaskManager.BoardTaskChanged += BoardTaskChanged;
            MapView.Editor.GeometryChanged += Editor_GeometryChanged;
        }

        private void Editor_GeometryChanged(object sender, Esri.ArcGISRuntime.UI.GeometryChangedEventArgs e)
        {
            if (IsOpen)
            {
                if (e.NewGeometry is Polyline line)
                {
                    var length = GeometryEngine.LengthGeodetic(line, null, GeodeticCurveType.NormalSection);
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
                    var length = GeometryEngine.LengthGeodetic(polygon, null, GeodeticCurveType.NormalSection);
                    var area = GeometryEngine.AreaGeodetic(polygon, null, GeodeticCurveType.NormalSection);

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

        private double barHeight = 28;
        public override double BarHeight => barHeight;

        public ArcMapView MapView => ArcMapView.Instance;
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

        private void BoardTaskChanged(object sender, BoardTaskManager.BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTaskManager.BoardTask.Draw)
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
                Show();
            }
            else
            {
                Hide();
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