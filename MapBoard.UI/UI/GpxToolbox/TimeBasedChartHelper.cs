using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace MapBoard.UI.GpxToolbox
{
    /// <summary>
    /// 横坐标为时间的图表，用于显示速度（点和线）和海拔（面）
    /// </summary>
    /// <typeparam name="TPoint">点的类型</typeparam>
    /// <typeparam name="TLine">线的类型</typeparam>
    /// <typeparam name="TPolygon">面的类型</typeparam>
    public class TimeBasedChartHelper<TPoint, TLine, TPolygon>
        where TPoint : class
        where TLine : class
        where TPolygon : class
    {
        /// <summary>
        /// 线程锁
        /// </summary>
        private readonly object lockObj = new object();

        /// <summary>
        /// 绘制定时器
        /// </summary>
        private readonly Timer timer;

        /// <summary>
        /// 全部坐标系
        /// </summary>
        private List<CoordinateSystemInfo> coordinateSystems = new List<CoordinateSystemInfo>();

        /// <summary>
        /// 全局的坐标系横坐标最大值
        /// </summary>
        private DateTime globalAxisMaxX = DateTime.MinValue;

        /// <summary>
        /// 全局的坐标系横坐标最小值
        /// </summary>
        private DateTime globalAxisMinX = DateTime.MaxValue;

        /// <summary>
        /// 全局的坐标系横坐标跨度
        /// </summary>
        private TimeSpan globalAxisXSpan = TimeSpan.Zero;

        /// <summary>
        /// 是否正在绘制
        /// </summary>
        private bool isDrawing = false;

        /// <summary>
        /// 在SizeChanged时设置为True，在下一轮的Timer中重绘
        /// </summary>
        private bool needToDraw = false;

        public TimeBasedChartHelper(Canvas canvas)
        {
            Sketchpad = canvas;
            canvas.Loaded += (p1, p2) => canvas.SizeChanged += SketchpadSizeChanged;
            canvas.PreviewMouseMove += SketchpadPreviewMouseMove;
            canvas.MouseLeave += SketchpadMouseLeave;
            canvas.Background = Brushes.White;
            canvas.ClipToBounds = true;
            //初始化绘制定时器
            timer = new Timer(new TimerCallback(p =>
            {
                bool draw = false;
                lock (lockObj)
                {
                    if (needToDraw && !isDrawing)
                    {
                        needToDraw = false;
                        draw = true;
                    }
                }
                if (draw)
                {
                    BeginDraw();
                }
            }), null, 1000, 1000);
        }
        /// <summary>
        /// 画板
        /// </summary>
        public Canvas Sketchpad { get; set; }

        /// <summary>
        /// 向画板增加元素
        /// </summary>
        /// <param name="element"></param>
        /// <param name="zIndex"></param>
        private void AddSketchpadChildren(UIElement element, int zIndex)
        {
            Panel.SetZIndex(element, zIndex);
            element.IsHitTestVisible = false;
            Sketchpad.Children.Add(element);
        }

        #region 指示性元素

        /// <summary>
        /// 鼠标位置的纵线
        /// </summary>
        private Line mouseLine;

        /// <summary>
        /// 从绘制点的原始数据到UI点的映射
        /// </summary>
        private Dictionary<TPoint, EllipseGeometry> point2UiPoint = new Dictionary<TPoint, EllipseGeometry>();

        /// <summary>
        /// 鼠标位置提示信息
        /// </summary>
        private ToolTip tip;

        /// <summary>
        /// 从UI点到绘制点的原始数据的映射
        /// </summary>
        private Dictionary<EllipseGeometry, TPoint> UiPoint2Point = new Dictionary<EllipseGeometry, TPoint>();

        #endregion 指示性元素

        #region 刷新重绘

        /// <summary>
        /// 鼠标移动时是否更新UI
        /// </summary>
        private bool canMouseMoveUpdate = true;

        /// <summary>
        /// 上一个选择的点
        /// </summary>
        private EllipseGeometry lastSelectedPoint = null;

        /// <summary>
        /// 面板尺寸改变计数。当发生改变时，计数+1，然后一段时间后-1。用于持续改变大小时，保证不会频繁刷新
        /// </summary>
        private int sizeChangeCount = 0;

        /// <summary>
        /// 清除鼠标线
        /// </summary>
        public void ClearLine()
        {
            if (mouseLine != null)
            {
                Sketchpad.Children.Remove(mouseLine);
                mouseLine = null;
            }
        }

        /// <summary>
        /// 绘制鼠标线
        /// </summary>
        /// <param name="time"></param>
        public void SetLine(DateTime time)
        {
            double percent = 1.0 * (time - globalAxisMinX).Ticks / (globalAxisMaxX - globalAxisMinX).Ticks;

            double width = percent * Sketchpad.ActualWidth;
            RefreshMouseLine(width);
            //RefreshToolTip(GetPoint(width));
        }

        /// <summary>
        /// 获取指定横坐标下对应的点
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private EllipseGeometry GetPoint(double x)
        {
            EllipseGeometry point = displayedPointsX.First().Key;
            double min = double.MaxValue;
            foreach (var p in displayedPointsX)
            {
                if (min != (min = Math.Min(Math.Abs(p.Value - x), min)))
                {
                    point = p.Key;
                }
            }

            return point;
        }

        /// <summary>
        /// 绘制鼠标线
        /// </summary>
        /// <param name="x"></param>
        private void RefreshMouseLine(double x)
        {
            if (MouseLineEnable)
            {
                if (mouseLine == null)
                {
                    mouseLine = new Line()
                    {
                        Y1 = 0,
                        Y2 = Sketchpad.ActualHeight,
                        StrokeThickness = 2,
                        Stroke = Brushes.Gray,
                    };
                    AddSketchpadChildren(mouseLine, 100);
                }
                mouseLine.X1 = mouseLine.X2 = x;
            }
        }

        /// <summary>
        /// 根据点刷新鼠标线
        /// </summary>
        /// <param name="point"></param>
        private void RefreshMouseLine(EllipseGeometry point)
        {
            double x = displayedPointsX[point];

            RefreshMouseLine(x);
        }

        /// <summary>
        /// 刷新鼠标位置信息
        /// </summary>
        /// <param name="point"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void RefreshToolTip(EllipseGeometry point, double x, double y)
        {
            if (ToolTipEnable)
            {
                if (tip == null)
                {
                    tip = new ToolTip
                    {
                        PlacementTarget = Sketchpad,
                        Placement = PlacementMode.Left,
                        HorizontalOffset = 20,
                    };
                }
                if (!UiPoint2Point.TryGetValue(point, out TPoint position))
                {
                    return;
                }
                tip.VerticalOffset = y;
                tip.HorizontalOffset = x;
                tip.Content = ToolTipConverter(position);
                if (!tip.IsOpen)
                {
                    tip.IsOpen = true;
                }
            }
        }

        /// <summary>
        /// 鼠标移开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SketchpadMouseLeave(object sender, MouseEventArgs e)
        {
            if (lastSelectedPoint != null)
            {
                //lastSelectedPoint.Fill = PointBrush;
                //lastSelectedPoint.Width = lastSelectedPoint.Height = PointSize;
                lastSelectedPoint = null;
            }
            if (tip != null)
            {
                tip.IsOpen = false;
                tip = null;
            }
            ClearLine();
        }

        /// <summary>
        /// 鼠标移动，更新鼠标线和提示框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SketchpadPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!canMouseMoveUpdate
                || isDrawing
                || displayedPointsX.Count == 0)
            {
                return;
            }
            canMouseMoveUpdate = false;
            var pos = e.GetPosition(Sketchpad);
            EllipseGeometry point = GetPoint(pos.X);
            RefreshMouseLine(point);
            RefreshToolTip(point, pos.X, pos.Y);
            MouseOverPoint?.Invoke(this, new MouseOverPointChangedEventArgs(e, UiPoint2Point[point]));

            await Task.Delay(100);
            canMouseMoveUpdate = true;
        }

        /// <summary>
        /// 尺寸改变，延时重绘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SketchpadSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Window.GetWindow(Sketchpad).WindowState == WindowState.Minimized)
            {
                return;
            }
            try
            {
                sizeChangeCount++;
                await Task.Delay(SizeChangeDelay);
                sizeChangeCount--;
                if (sizeChangeCount != 0)
                {
                    return;
                }
                lock (lockObj)
                {
                    needToDraw = true;
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("绘制图表失败", ex);
                throw;
            }
        }
        #endregion 刷新重绘

        #region 暴露的方法

        public Func<Task> DrawActionAsync { private get; set; }

        /// <summary>
        /// 开始绘制
        /// </summary>
        public void BeginDraw()
        {
            if (DrawActionAsync == null)
            {
                return;
            }
            if (isDrawing)
            {
                return;
            }
            isDrawing = true;

            needToDraw = false;

            //有可能从Timer线程中调用，因此要保证UI线程
            Sketchpad.Dispatcher.Invoke(async () =>
            {
                try
                {
                    Sketchpad.Children.Clear();
                    await DrawActionAsync();
                }
                catch (Exception ex)
                {
                    App.Log.Error("定时绘制图表失败", ex);
                }
                finally
                {
                    isDrawing = false;
                }
            });
        }

        /// <summary>
        /// 绘制坐标轴和坐标系
        /// </summary>
        /// <typeparam name="TBorder"></typeparam>
        /// <param name="items"></param>
        /// <param name="draw"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public async Task<int> DrawAxisAsync<TBorder>(IEnumerable<TBorder> items, bool draw, CoordinateSystemSetting<TBorder> setting)
        {
            CoordinateSystemInfo axis = new CoordinateSystemInfo();
            coordinateSystems.Add(axis);
            TimeSpan gridXSpan = TimeSpan.Zero;
            await Task.Run(() =>
            {
                //更新坐标轴
                axis.MinX = items.Min(p => setting.XAxisValueConverter(p));
                axis.MaxX = items.Max(p => setting.XAxisValueConverter(p));
                axis.MinY = items.Min(p => setting.YAxisValueConverter(p));
                axis.MaxY = items.Max(p => setting.YAxisValueConverter(p));

                //计算X轴网格的间距
                foreach (var item in setting.SpanXMapping)
                {
                    if (axis.SpanX < item.Key)
                    {
                        gridXSpan = item.Value;
                        break;
                    }
                }
                if (gridXSpan == TimeSpan.Zero)
                {
                    gridXSpan = TimeSpan.FromTicks(axis.SpanX.Ticks / 10);
                }

                //更新X坐标轴
                var minAxisTime = new DateTime(axis.MinX.Ticks / gridXSpan.Ticks * gridXSpan.Ticks);
                var maxAxisTime = new DateTime((axis.MaxX.Ticks / gridXSpan.Ticks + 1) * gridXSpan.Ticks);

                //更新全局X坐标轴
                if (minAxisTime < globalAxisMinX)
                {
                    globalAxisMinX = minAxisTime;
                }
                if (maxAxisTime > globalAxisMaxX)
                {
                    globalAxisMaxX = maxAxisTime;
                }
                globalAxisXSpan = globalAxisMaxX - globalAxisMinX;
            });

            //计算Y轴网格的间距
            double gridYSpan = 0;
            foreach (var item in setting.SpanYMapping)
            {
                if (axis.SpanY < item.Key)
                {
                    gridYSpan = item.Value;
                    break;
                }
            }
            if (gridYSpan == 0)
            {
                gridYSpan = axis.SpanY / 10;
            }

            //更新X坐标轴
            axis.AxisMinY = ((int)(axis.MinY / gridYSpan - 1)) * gridYSpan;
            axis.AxisMaxY = ((int)(axis.MaxY / gridYSpan + 1)) * gridYSpan;
            if (draw)
            {
                int verticleGridCount = (int)(globalAxisXSpan.Ticks / gridXSpan.Ticks); //竖线的数量
                for (int i = 0; i < verticleGridCount; i++)
                {
                    double x = Sketchpad.ActualWidth * i / verticleGridCount;
                    DrawVerticalLine(x, 1); //绘制竖线
                    DrawText(x, FontSize * 1.2, XLabelFormat(new DateTime(globalAxisMinX.Ticks + gridXSpan.Ticks * i)),
                        VerticleTextBrush, true, "xLabel"); //画标签
                }

                double horizentalBorderCount = (axis.AxisMaxY - axis.AxisMinY) / gridYSpan;
                for (int i = 0; i < horizentalBorderCount; i++)
                {
                    double y = Sketchpad.ActualHeight * i / horizentalBorderCount;
                    DrawHorizentalLine(y);
                    DrawText(0, y + FontSize * 1.2, YLabelFormat(axis.AxisMinY + gridYSpan * i), HorizentalTextBrush, false, "yLabel");
                }
            }

            //lastBorderPoints = items;
            return coordinateSystems.Count - 1;
        }

        /// <summary>
        /// 绘制线
        /// </summary>
        /// <param name="items"></param>
        /// <param name="axisIndex"></param>
        /// <returns></returns>
        public async Task DrawLinesAsync(IEnumerable<TLine> items, int axisIndex)
        {
            CoordinateSystemInfo axis = coordinateSystems[axisIndex];
            TLine last = default;
            List<LineGeometry> lines = new List<LineGeometry>();
            await Task.Run(() =>
            {
                foreach (var item in items)
                {
                    if (last != default && LinePointEnbale(last, item))
                    {
                        DateTime time1 = XAxisLineValueConverter(last);
                        double value1 = YAxisLineValueConverter(last);

                        TimeSpan currentSpan1 = time1 - axis.MinX;

                        double x1 = Sketchpad.ActualWidth * (time1 - globalAxisMinX).Ticks / globalAxisXSpan.Ticks;
                        double y1 = Sketchpad.ActualHeight * (value1 - axis.AxisMinY) / axis.AxisSpanY;
                        DateTime time2 = XAxisLineValueConverter(item);
                        double value2 = YAxisLineValueConverter(item);

                        TimeSpan currentSpan2 = time2 - axis.MinX;

                        double x2 = Sketchpad.ActualWidth * (time2 - globalAxisMinX).Ticks / globalAxisXSpan.Ticks;
                        double y2 = Sketchpad.ActualHeight * (value2 - axis.AxisMinY) / axis.AxisSpanY;
                        Sketchpad.Dispatcher.Invoke(() =>
                        lines.Add(GetLine(x1, y1, x2, y2)));
                    }

                    last = item;
                }
            });
            Path path = new Path()
            {
                Data = new GeometryGroup() { Children = new GeometryCollection(lines) },
                StrokeThickness = 1,
                Stroke = LineBrush,
            };

            AddSketchpadChildren(path, 4);
        }

        /// <summary>
        /// 绘制点
        /// </summary>
        /// <param name="items"></param>
        /// <param name="axisIndex"></param>
        /// <param name="draw"></param>
        /// <returns></returns>
        public async Task DrawPointsAsync(IEnumerable<TPoint> items, int axisIndex, bool draw)
        {
            CoordinateSystemInfo coord = coordinateSystems[axisIndex];
            await Task.Run(() =>
            {
                foreach (var item in items)
                {
                    DateTime time = XAxisPointValueConverter(item);
                    double value = YAxisPointValueConverter(item);

                    TimeSpan currentSpan = time - coord.MinX;

                    double x = Sketchpad.ActualWidth * (time - globalAxisMinX).Ticks / globalAxisXSpan.Ticks;
                    double y = Sketchpad.ActualHeight * (value - coord.AxisMinY) / coord.AxisSpanY;
                    AddPoint(x, y, item);
                }
            });
            if (draw)
            {
                Path path = new Path()
                {
                    Data = new GeometryGroup() { Children = new GeometryCollection(UiPoint2Point.Keys) },
                    StrokeThickness = PointSize,
                    Stroke = PointBrush,
                };

                AddSketchpadChildren(path, 3);
            }
            // lastAction = nameof(DrawPoints);
            // lastPointPoints = items;
        }

        /// <summary>
        /// 绘制多边形
        /// </summary>
        /// <param name="items"></param>
        /// <param name="axisIndex"></param>
        /// <returns></returns>
        public async Task DrawPolygonAsync(IEnumerable<TPolygon> items, int axisIndex)
        {
            CoordinateSystemInfo axis = coordinateSystems[axisIndex];
            double yZero = Sketchpad.ActualHeight
                - (-axis.AxisMinY) / (axis.AxisMaxY - axis.AxisMinY)
                * Sketchpad.ActualHeight;
            List<Point> points = new List<Point>();

            await Task.Run(() =>
            {
                points.Add(new Point(Sketchpad.ActualWidth * (items.Max(q => XAxisPolygonValueConverter(q)) - globalAxisMinX).Ticks / globalAxisXSpan.Ticks, yZero));
                points.Add(new Point(Sketchpad.ActualWidth * (items.Min(q => XAxisPolygonValueConverter(q)) - globalAxisMinX).Ticks / globalAxisXSpan.Ticks, yZero));
                foreach (var item in items)
                {
                    DateTime time = XAxisPolygonValueConverter(item);
                    double value = YAxisPolygonValueConverter(item);
                    if (double.IsNaN(value))
                    {
                        continue;
                    }
                    TimeSpan currentSpan = time - axis.MinX;
                    double x = Sketchpad.ActualWidth * (time - globalAxisMinX).Ticks / globalAxisXSpan.Ticks;
                    double y = Sketchpad.ActualHeight - Sketchpad.ActualHeight * (value - axis.AxisMinY) / axis.AxisSpanY;
                    points.Add(new Point(x, y));
                }
            });
            var g = new StreamGeometry();
            using (StreamGeometryContext context = g.Open())
            {
                context.BeginFigure(points[0], true, true);
                for (int i = 1; i < points.Count; i++)
                {
                    context.LineTo(points[i], true, false);
                }
            }
            Path path = new Path()
            {
                Data = g,
                Fill = PolygonBrush
            };
            AddSketchpadChildren(path, 1);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            coordinateSystems.Clear();
            displayedPointsX.Clear();
            Sketchpad.Children.Clear();
            UiPoint2Point.Clear();
            point2UiPoint.Clear();
            Sketchpad.RenderTransform = null;
        }

        /// <summary>
        /// 拉伸到合适的尺寸
        /// </summary>
        /// <returns></returns>
        public async Task StretchToFitAsync()
        {
            DateTime minX = default;
            DateTime maxX = default;
            double minXMarginLeft = 0;
            double scaleValue = 0;
            await Task.Run(() =>
            {
                minX = coordinateSystems.Min(p => p.MinX);
                maxX = coordinateSystems.Max(p => p.MaxX);
                minXMarginLeft = 1.0 //数据源X最左侧到坐标系最左侧的距离
                   * (minX - globalAxisMinX).Ticks
                   / globalAxisXSpan.Ticks
                   * Sketchpad.ActualWidth;
                scaleValue = 1.0 * globalAxisXSpan.Ticks //拉伸比例
                        / (maxX.Ticks - minX.Ticks);
            });
            TranslateTransform translate = new TranslateTransform(-minXMarginLeft, 0);

            ScaleTransform scale = new ScaleTransform(scaleValue, 1);

            //重置左侧标签的X坐标
            foreach (var tbk in Sketchpad.Children.OfType<TextBlock>().Where(p => "yLabel".Equals(p.Tag)))
            {
                Canvas.SetLeft(tbk, minXMarginLeft);
            }
            ScaleTransform scaleTextBlock = new ScaleTransform(1 / scaleValue, 1);

            //防止标签被拉伸变形
            foreach (var tbk in Sketchpad.Children.OfType<TextBlock>())
            {
                tbk.RenderTransform = scaleTextBlock;
            }

            var group = new TransformGroup();
            group.Children.Add(translate);
            group.Children.Add(scale);
            Sketchpad.RenderTransform = group;
        }

        #endregion 暴露的方法

        #region 可从外部更改的属性

        public double FontSize { get; set; } = 9;
        public Brush HorizentalGridlinesBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush HorizentalTextBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush LineBrush { get; set; } = Brushes.Blue;
        public Func<TLine, TLine, bool> LinePointEnbale { get; set; } = (p1, p2) => true;
        public double LineThickness { get; set; } = 1;
        public bool MouseLineEnable { get; set; } = true;
        public Brush PointBrush { get; set; } = Brushes.Red;
        public double PointSize { get; set; } = 1;
        public Brush PolygonBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0x33, 0x55, 0x55, 0x55));
        public Brush SelectedPointBrush { get; set; } = Brushes.Green;
        public TimeSpan SizeChangeDelay { get; set; } = TimeSpan.FromSeconds(1);
        public Func<TPoint, string> ToolTipConverter { get; set; } = p => p.ToString();
        public bool ToolTipEnable { get; set; } = true;
        public Brush VerticleGridlinesBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush VerticleTextBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Func<TLine, DateTime> XAxisLineValueConverter { get; set; } = p => DateTime.MinValue;
        public Func<TPoint, DateTime> XAxisPointValueConverter { get; set; } = p => DateTime.MinValue;
        public Func<TPolygon, DateTime> XAxisPolygonValueConverter { get; set; } = p => DateTime.MinValue;
        public Func<DateTime, string> XLabelFormat { get; set; } = p => p.ToString();
        public Func<TLine, double> YAxisLineValueConverter { get; set; } = p => 0;
        public Func<TPoint, double> YAxisPointValueConverter { get; set; } = p => 0;
        public Func<TPolygon, double> YAxisPolygonValueConverter { get; set; } = p => 0;
        public Func<double, string> YLabelFormat { get; set; } = p => p.ToString();
        #endregion 可从外部更改的属性

        #region 基础绘制

        /// <summary>
        /// 显示出来的点对应的横坐标
        /// </summary>
        public Dictionary<EllipseGeometry, double> displayedPointsX { get; private set; } = new Dictionary<EllipseGeometry, double>();

        /// <summary>
        /// 绘制一个点
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private EllipseGeometry AddPoint(double x, double y, TPoint point)
        {
            double r = PointSize / 2;
            EllipseGeometry e = null;
            Sketchpad.Dispatcher.Invoke(() =>
            {
                e = new EllipseGeometry()
                {
                    Center = new Point(x - r, Sketchpad.ActualHeight - (y - r)),
                    RadiusX = r,
                    RadiusY = r
                };
            });
            displayedPointsX.Add(e, x);
            UiPoint2Point.Add(e, point);
            point2UiPoint.Add(point, e);
            return e;
        }

        /// <summary>
        /// 绘制水平线
        /// </summary>
        /// <param name="y"></param>
        private void DrawHorizentalLine(double y)
        {
            Line line = new Line()
            {
                X1 = 0,
                X2 = Sketchpad.ActualWidth,
                Y1 = Sketchpad.ActualHeight - y,
                Y2 = Sketchpad.ActualHeight - y,
                Stroke = HorizentalGridlinesBrush,
                StrokeThickness = 1,
            };
            AddSketchpadChildren(line, 1);
        }

        /// <summary>
        /// 绘制标签
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="text"></param>
        /// <param name="brush"></param>
        /// <param name="centerX"></param>
        /// <param name="tag"></param>
        private void DrawText(double x, double y, string text, Brush brush, bool centerX, object tag)
        {
            TextBlock tbk = new TextBlock()
            {
                Text = text,
                FontSize = FontSize,
                Foreground = brush,
                Tag = tag,
            };
            if ("xLabel".Equals(tag))
            {
                tbk.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            if (centerX)
            {
                tbk.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                tbk.Arrange(new Rect(tbk.DesiredSize));
                Canvas.SetLeft(tbk, x - tbk.ActualWidth / 2);
                Canvas.SetTop(tbk, Sketchpad.ActualHeight - y);
            }
            else
            {
                Canvas.SetLeft(tbk, x);
                Canvas.SetTop(tbk, Sketchpad.ActualHeight - y);
            }
            AddSketchpadChildren(tbk, 5);
        }

        /// <summary>
        /// 绘制垂直线
        /// </summary>
        /// <param name="x"></param>
        /// <param name="thickness"></param>
        private void DrawVerticalLine(double x, double thickness)
        {
            Line line = new Line()
            {
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = Sketchpad.ActualHeight,
                Stroke = VerticleGridlinesBrush,
                StrokeThickness = thickness,
            };
            AddSketchpadChildren(line, 1);
        }

        /// <summary>
        /// 根据坐标生成一条线
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        private LineGeometry GetLine(double x1, double y1, double x2, double y2)
        {
            LineGeometry lg = new LineGeometry()
            {
                StartPoint = new Point(x1, Sketchpad.ActualHeight - y1),
                EndPoint = new Point(x2, Sketchpad.ActualHeight - y2),
            };
            return lg;
        }
        #endregion 基础绘制

        #region 事件相关

        public event EventHandler<MouseOverPointChangedEventArgs> MouseOverPoint;

        public class MouseOverPointChangedEventArgs : MouseEventArgs
        {
            public MouseOverPointChangedEventArgs(MouseEventArgs baseEvent, TPoint item) : base(baseEvent.MouseDevice, baseEvent.Timestamp, baseEvent.StylusDevice)
            {
                Item = item;
            }

            public TPoint Item { get; private set; }
        }
        #endregion 事件相关

        /// <summary>
        /// 坐标系相关设置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class CoordinateSystemSetting<T>
        {
            public Dictionary<TimeSpan, TimeSpan> SpanXMapping { get; set; } = new Dictionary<TimeSpan, TimeSpan>()
            {
                { TimeSpan.FromMinutes(1),TimeSpan.FromSeconds(5) },
                { TimeSpan.FromMinutes(5),TimeSpan.FromSeconds(10) },
                { TimeSpan.FromMinutes(20),TimeSpan.FromMinutes(2) },
                { TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(5) },
                { TimeSpan.FromHours(5),TimeSpan.FromMinutes(30) },
                { TimeSpan.FromHours(60),TimeSpan.FromHours(5) },
            };

            public Dictionary<double, double> SpanYMapping { get; set; } = new Dictionary<double, double>()
            {
                {1,0.1 },
                {3,0.5 },
                {10,1 },
                {20,2 },
                {50,5 },
                {100,10 },
                {200,20 },
                {500,50 },
                {1000,100 },
            };

            /// <summary>
            /// 从自定义类型转到X坐标所需的<see cref="DateTime"/>
            /// </summary>
            public Func<T, DateTime> XAxisValueConverter { get; set; } = p => DateTime.MinValue;

            /// <summary>
            /// 从自定义类型转到Y坐标所需的<see cref="double"/>
            /// </summary>
            public Func<T, double> YAxisValueConverter { get; set; } = p => 0;
        }

        /// <summary>
        /// 单一元素（点/线/面）的坐标系信息
        /// </summary>
        private class CoordinateSystemInfo
        {
            /// <summary>
            /// 坐标轴的Y最大值
            /// </summary>
            public double AxisMaxY { get; set; }

            /// <summary>
            /// 坐标轴的Y最小值
            /// </summary>
            public double AxisMinY { get; set; }

            /// <summary>
            /// 坐标轴的Y跨度
            /// </summary>
            public double AxisSpanY => AxisMaxY - AxisMinY;

            /// <summary>
            /// 数据源的X最大值
            /// </summary>
            public DateTime MaxX { get; set; }

            /// <summary>
            /// 数据源中的Y最大值
            /// </summary>
            public double MaxY { get; set; }

            /// <summary>
            /// 数据源的X最小值
            /// </summary>
            public DateTime MinX { get; set; }

            /// <summary>
            /// 数据源中的Y最小值
            /// </summary>
            public double MinY { get; set; }

            /// <summary>
            /// 数据源的X跨度
            /// </summary>
            public TimeSpan SpanX => MaxX - MinX;

            /// <summary>
            /// 数据源中的Y跨度
            /// </summary>
            public double SpanY =>MaxY - MinY;
        }
    }
}