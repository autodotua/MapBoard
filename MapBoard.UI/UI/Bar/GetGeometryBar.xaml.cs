using Esri.ArcGISRuntime.Geometry;
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
    /// 获取图形条
    /// </summary>
    public partial class GetGeometryBar : BarBase
    {
        public GetGeometryBar()
        {
            InitializeComponent();
        }

        public override FeatureAttributeCollection Attributes => throw new NotSupportedException();
        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;
        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
        }

        /// <summary>
        /// 如果在绘制并且模式为<see cref="EditMode.GetGeometry"/>，则展开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTask.Draw)
            {
                if (MapView.Editor.Mode == EditMode.GetGeometry)
                {
                    Expand();
                }
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
        /// 单击完成按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.Editor.StopAndSave();
        }
    }
}