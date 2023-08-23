using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Editing;
using FzLib;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using static MapBoard.Mapping.EditorHelper;

namespace MapBoard.UI.Bar
{
    /// <summary>
    /// 测量条
    /// </summary>
    public partial class MeasureBar : BarBase
    {
        public MeasureBar()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 面积值
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// 面积标题
        /// </summary>
        public string AreaTitle { get; set; }

        public override FeatureAttributeCollection Attributes => throw new System.NotImplementedException();

        public override double ExpandDistance => 28;

        /// <summary>
        /// 长度值
        /// </summary>
        public string Length { get; set; }

        /// <summary>
        /// 长度标题
        /// </summary>
        public string LengthTitle { get; set; }

        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
            MapView.Editor.GeometryChanged += Editor_GeometryChanged;
        }

        /// <summary>
        /// 如果在绘制并且模式为<see cref="EditMode.MeasureArea"/>或<see cref="EditMode.MeasureLength"/>，则展开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 单击取消按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.Editor.Cancel();
        }

        /// <summary>
        /// 图形改变，更新长度和面积
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Editor_GeometryChanged(object sender, GeometryUpdatedEventArgs e)
        {
            if (IsOpen)
            {
                if (e.Geometry is Polyline line)
                {
                    var length = line.GetLength();
                    Length = length < 10000 ? $"{length:0.000}米" : $"{length / 1000:0.000}千米";
                }
                else if (e.Geometry is Polygon polygon)
                {
                    var length = polygon.GetLength();
                    var area = polygon.GetArea();

                    Length = length < 10000 ? $"{length:0.000}米" : $"{length / 1000:0.000}千米";
                    Area = area < 1_000_000 ? $"{area:0.000}平方米" : $"{area / 1_000_000:0.000}平方千米";
                }
                else
                {
                    Length = "";
                    Area = "";
                }
            }
        }
    }
}