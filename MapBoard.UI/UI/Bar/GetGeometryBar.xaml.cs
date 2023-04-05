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
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class GetGeometryBar : BarBase
    {
        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;
        public override FeatureAttributeCollection Attributes => throw new NotSupportedException();

        public GetGeometryBar()
        {
            InitializeComponent();
        }

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
        }

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.Editor.Cancel();
        }

        private void RemoveSelectedVertexButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.SketchEditor.RemoveSelectedVertex();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.Editor.StopAndSave();
        }
    }
}