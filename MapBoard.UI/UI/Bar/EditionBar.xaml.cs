using Esri.ArcGISRuntime.Geometry;
using FzLib;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static MapBoard.Mapping.EditorHelper;
using SCM = Esri.ArcGISRuntime.UI.SketchCreationMode;

namespace MapBoard.UI.Bar
{
    /// <summary>
    /// 编辑条
    /// </summary>
    public partial class EditionBar : BarBase
    {
        public EditionBar()
        {
            InitializeComponent();
        }

        public override FeatureAttributeCollection Attributes => MapView.Editor.Attributes;

        /// <summary>
        /// 是否能增加一部分
        /// </summary>
        public bool CanAddPart => IsOpen
            && MapView.SketchEditor.Geometry != null
            && MapView.SketchEditor.Geometry is Multipart
            && MapView.SketchEditor.CreationMode is SCM.Polyline or SCM.Polygon;

        /// <summary>
        /// 能否删除选中的结点
        /// </summary>
        public bool CanDeleteSelectedVertex => MapView != null && MapView.SketchEditor != null && MapView.SketchEditor.SelectedVertex != null;

        /// <summary>
        /// 能否移除一部分
        /// </summary>
        public bool CanRemovePart => IsOpen
            && MapView.SketchEditor.Geometry != null
            && MapView.SketchEditor.Geometry is Multipart m
            && MapView.SketchEditor.CreationMode is SCM.Polyline or SCM.Polygon
            && MapView.SketchEditor.SelectedVertex != null
            && m.Parts.Count > 1;

        public override double ExpandDistance => 28;

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTask_Changed;
            MapView.SketchEditor.SelectedVertexChanged += SketchEditorSelectedVertex_Changed;
            MapView.SketchEditor.PropertyChanged += SketchEditor_PropertyChanged;
        }

        /// <summary>
        /// 单击增加部分按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotSupportedException"></exception>
        private void AddPartButton_Click(object sender, RoutedEventArgs e)
        {
            var geometry = MapView.SketchEditor.Geometry;
            if (geometry is Multipart m)
            {
                if (m.Parts[^1].PointCount == 0)
                {
                    //如果最后一个部分已经建立但没有图形，那么就取消选中就可以了
                    MapView.SketchEditor.ClearVertexSelection();
                    return;
                }
                var parts = new List<IEnumerable<Segment>>(m.Parts)
                {
                    Array.Empty<Segment>()
                };
                MapView.SketchEditor.ReplaceGeometry(m is Polyline ? new Polyline(parts) : new Polygon(parts));
                MapView.SketchEditor.ClearVertexSelection();//清除选中的节点，才能开始下一个部分
            }
            else
            {
                throw new NotSupportedException("只支持对多部分图形增加部分");
            }
        }

        /// <summary>
        /// 任务类型改变，更新标题和信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardTask_Changed(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTask.Draw && MapView.Editor.Mode is EditMode.Create or EditMode.Edit)
            {
                Title = MapView.Editor.Mode switch
                {
                    EditMode.Create => "正在绘制",
                    EditMode.Edit => "正在编辑",
                    _ => ""
                };
                Message = "";
                this.Notify(nameof(MapView));
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
        /// 获取信息字符串
        /// </summary>
        /// <returns></returns>
        private string GetMessage()
        {
            var geometry = MapView.SketchEditor.Geometry;
            if (geometry is MapPoint pp)
            {
                geometry = GeometryEngine.Project(pp, SpatialReferences.Wgs84);
            }
            return geometry switch
            {
                MapPoint p => $"，经度 ={ p.X:0.0000000}，纬度 ={ p.Y:0.0000000}",
                Multipoint mp => $"共{mp.Points.Count}个点",
                Polyline l
                when l.Parts.Count > 0
                && l.Parts[0].PointCount > 0
                && l.Parts.Sum(p => p.PointCount) <= 100
                => $"共{l.Parts.Count}个部分，总长度{NumberConverter.MeterToFitString(GeometryUtility.GetLength(l))}",
                Polygon p
                 when p.Parts.Count > 0
                 && p.Parts[0].PointCount > 0
                 && p.Parts.Sum(p => p.PointCount) <= 100
                 => $"共{p.Parts.Count}个部分，总面积{NumberConverter.SquareMeterToFitString(GeometryUtility.GetArea(p))}",
                Multipart m
                when m.Parts.Count > 0
                && m.Parts[0].PointCount > 0
                && m.Parts.Sum(p => p.PointCount) > 100
                => $"共{m.Parts.Count}个部分，{m.Parts.Sum(p => p.PointCount)}个结点",
                _ => ""
            };
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

        /// <summary>
        /// 单击移除部分按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotSupportedException"></exception>
        private void RemovePartButton_Click(object sender, RoutedEventArgs e)
        {
            var geometry = MapView.SketchEditor.Geometry;
            var vertex = MapView.SketchEditor.SelectedVertex;
            if (vertex == null)
            {
                throw new Exception("没有选中的结点");
            }
            if (geometry is Multipart m)
            {
                if (m.Parts.Count <= 1)
                {
                    throw new Exception("需要包含两个或以上的部分才可删除");
                }
                var parts = new List<IEnumerable<Segment>>(m.Parts);
                parts.RemoveAt(vertex.PartIndex);
                MapView.SketchEditor.ReplaceGeometry(m is Polyline ? new Polyline(parts) : new Polygon(parts));
                MapView.SketchEditor.ClearVertexSelection();
            }
            else
            {
                throw new NotSupportedException("只支持对多部分图形增加部分");
            }
        }

        /// <summary>
        /// 单击移除选中的结点按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveSelectedVertexButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.SketchEditor.RemoveSelectedVertex();
        }

        /// <summary>
        /// 编辑器属性改变，通知按钮IsEnable和信息改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SketchEditor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MapView.SketchEditor.Geometry) or nameof(MapView.SketchEditor.CreationMode))
            {
                this.Notify(nameof(CanAddPart), nameof(CanRemovePart));

                Message = (!IsOpen || MapView.SketchEditor.Geometry == null) ? "" : GetMessage();
            }
        }

        /// <summary>
        /// 选择的节点改变，通知能否删除和移除部分按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SketchEditorSelectedVertex_Changed(object sender, Esri.ArcGISRuntime.UI.VertexChangedEventArgs e)
        {
            this.Notify(nameof(CanDeleteSelectedVertex), nameof(CanRemovePart));
        }
    }
}