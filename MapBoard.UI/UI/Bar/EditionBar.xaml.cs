using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Editing;
using FzLib;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Bar
{
    /// <summary>
    /// 编辑条
    /// </summary>
    public partial class EditionBar : BarBase
    {
        private bool requestNewPart = false;

        public EditionBar()
        {
            InitializeComponent();
        }

        public override FeatureAttributeCollection Attributes => MapView.Editor.Attributes;

        /// <summary>
        /// 是否能增加一部分
        /// </summary>
        public bool CanAddPart => IsOpen
            && MapView.GeometryEditor.IsStarted
            && MapView.GeometryEditor.Geometry is Multipart;

        /// <summary>
        /// 能否删除选中的结点
        /// </summary>
        public bool CanDeleteSelectedVertex => MapView != null
            && MapView.GeometryEditor.IsStarted
            && MapView.GeometryEditor.SelectedElement is GeometryEditorVertex;

        /// <summary>
        /// 能否移除一部分
        /// </summary>
        public bool CanRemovePart => IsOpen
            && MapView.GeometryEditor.IsStarted
            && MapView.GeometryEditor.Geometry is Multipart m
            && MapView.GeometryEditor.SelectedElement is GeometryEditorPart or GeometryEditorMidVertex or GeometryEditorVertex
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
            MapView.Editor.GeometryChanged += (s, e) =>
            {
                UpdateButtonAndMessage();
                if (requestNewPart
                && e.Geometry is Multipart m
                && m.Parts.Count > 0
                && m.Parts[^1].PointCount > 1)
                {
                    requestNewPart = false;

                    var geometry = e.Geometry;
                    if (geometry is Polyline line)
                    {
                        var builder = new PolylineBuilder(line);
                        var lastPart = builder.Parts[^1];
                        builder.Parts.Remove(lastPart);
                        var oldPart = lastPart.Points.Take(lastPart.PointCount - 1);
                        dynamic newPart = new[] { lastPart.Points[^1] };
                        builder.Parts.Add(new Part(oldPart));
                        builder.Parts.Add(new Part(newPart));
                        MapView.GeometryEditor.ReplaceGeometry(builder.ToGeometry());
                    }
                    else if (geometry is Polygon polygon)
                    {
                        var builder = new PolygonBuilder(polygon);
                        var lastPart = builder.Parts[^1];
                        builder.Parts.Remove(lastPart);
                        var oldPart = lastPart.Points.Take(lastPart.PointCount - 1);
                        dynamic newPart = new[] { lastPart.Points[^1] };
                        builder.Parts.Add(new Part(oldPart));
                        builder.Parts.Add(new Part(newPart));
                        MapView.GeometryEditor.ReplaceGeometry(builder.ToGeometry());
                    }
                }
            };
            MapView.Editor.SelectedElementChanged += (s, e) =>
            {
                //如果点击新增部分后又点了其他元素，那么认为需要取消新增部分操作
                if (MapView.GeometryEditor.SelectedElement != null)
                {
                    requestNewPart = false;
                }
                UpdateButtonAndMessage();
            };
        }

        /// <summary>
        /// 单击增加部分按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotSupportedException"></exception>
        private void AddPartButton_Click(object sender, RoutedEventArgs e)
        {
            requestNewPart = true;
            MapView.GeometryEditor.ClearSelection();//清除选中的节点，才能开始下一个部分
            //var geometry = MapView.GeometryEditor.Geometry;
            //if (geometry is Multipart m)
            //{
            //    if (m.Parts[^1].PointCount == 0)
            //    {
            //        //如果最后一个部分已经建立但没有图形，那么就取消选中就可以了
            //        MapView.GeometryEditor.ClearSelection();
            //        return;
            //    }
            //    //新的GeometryEditor有点问题，新的Part不能是空的，否则就会报错ArcGISRuntimeException: Out of range: point index out of range.

            //    if (geometry is Polyline line)
            //    {
            //        var builder =new PolylineBuilder(line);
            //        builder.AddPart(new[] { new MapPoint(0,0)});
            //        MapView.GeometryEditor.ReplaceGeometry(builder.ToGeometry());
            //    }
            //    else if(geometry is Polygon polygon)
            //    { 
            //        var builder = new PolygonBuilder(polygon);
            //        builder.AddPart(new[] { new MapPoint(0, 0) });
            //        MapView.GeometryEditor.ReplaceGeometry(builder.ToGeometry());
            //    }
            //    MapView.GeometryEditor.ClearSelection();//清除选中的节点，才能开始下一个部分
            //}
            //else
            //{
            //    throw new NotSupportedException("只支持对多部分图形增加部分");
            //}
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
            var geometry = MapView.GeometryEditor.Geometry;
            if (geometry is MapPoint pp)
            {
                geometry = GeometryEngine.Project(pp, SpatialReferences.Wgs84);
            }
            double la = 0; //Length or Area
            if (geometry is Polyline line)
            {
                la = GeometryUtility.GetLength(line);
            }
            else if (geometry is Polygon gon)
            {
                la = GeometryUtility.GetArea(gon);
            }
            return geometry switch
            {
                MapPoint p => $"，经度 ={p.X:0.0000000}，纬度 ={p.Y:0.0000000}",
                Multipoint mp => $"共{mp.Points.Count}个点",
                Polyline l
                when l.Parts.Count > 0
                && l.Parts[0].PointCount > 0
                && l.Parts.Sum(p => p.PointCount) <= 100
                => $"共{l.Parts.Count}个部分，总长度{(la < 10000 ? $"{la:0.000}米" : $"{la / 1000:0.000}千米")}",
                Polygon p
                 when p.Parts.Count > 0
                 && p.Parts[0].PointCount > 0
                 && p.Parts.Sum(p => p.PointCount) <= 100
                 => $"共{p.Parts.Count}个部分，总面积{(la < 1_000_000 ? $"{la:0.000}平方米" : $"{la / 1_000_000:0.000}平方千米")}",
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

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (MapView.GeometryEditor.CanRedo)
            {
                MapView.GeometryEditor.Redo();
            }
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
            var geometry = MapView.GeometryEditor.Geometry;
            var selectedElement = MapView.GeometryEditor.SelectedElement;
            if (geometry is not Multipart m)
            {
                throw new NotSupportedException("只支持对多部分图形增加部分");
            }
            if (m.Parts.Count <= 1)
            {
                throw new Exception("需要包含两个或以上的部分才可删除");
            }
            long partIndex;
            if (selectedElement is GeometryEditorVertex v)
            {
                partIndex = v.PartIndex;
            }
            else if (selectedElement is GeometryEditorMidVertex mv)
            {
                partIndex = mv.PartIndex;
            }
            else if (selectedElement is GeometryEditorPart p)
            {
                partIndex = p.PartIndex;
            }
            else
            {
                throw new NotSupportedException("未知的选中PartIndex");
            }
            var parts = new List<IEnumerable<Segment>>(m.Parts);
            parts.RemoveAt((int)partIndex);
            MapView.GeometryEditor.ReplaceGeometry(m is Polyline ? new Polyline(parts) : new Polygon(parts));

        }

        /// <summary>
        /// 单击移除选中的结点按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveSelectedVertexButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedElement = MapView.GeometryEditor.SelectedElement;
            if (selectedElement is GeometryEditorVertex)
            {
                MapView.GeometryEditor.DeleteSelectedElement();
            }
            else
            {
                throw new NotSupportedException("不支持的GeometryEditorElement");
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (MapView.GeometryEditor.CanUndo)
            {
                MapView.GeometryEditor.Undo();
            }
        }

        private void UpdateButtonAndMessage()
        {
            this.Notify(nameof(CanDeleteSelectedVertex), nameof(CanAddPart), nameof(CanAddPart), nameof(CanRemovePart));

            Message = (!IsOpen || MapView.GeometryEditor.Geometry == null) ? "" : GetMessage();
        }
    }
}