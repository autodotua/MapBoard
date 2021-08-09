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
    public class TimeBasedChartHelper<TPoint, TLine, TPolygon>
        where TPoint : class
        where TLine : class
        where TPolygon : class
    {
        private bool isDrawing = false;

        /// <summary>
        /// 在SizeChanged时设置为True，在下一轮的Timer中重绘
        /// </summary>
        private bool needToDraw = false;

        private Timer timer;
        private object lockObj = new object();

        public TimeBasedChartHelper(Canvas canvas)
        {
            Sketchpad = canvas;
            canvas.Loaded += (p1, p2) => canvas.SizeChanged += SketchpadSizeChanged;
            canvas.PreviewMouseMove += SketchpadPreviewMouseMove;
            canvas.MouseLeave += SketchpadMouseLeave;
            canvas.Background = Brushes.White;
            canvas.ClipToBounds = true;
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

        private List<BorderInfo> borders = new List<BorderInfo>();

        public Canvas Sketchpad { get; set; }

        private void AddSketchpadChildren(UIElement element, int zIndex)
        {
            Panel.SetZIndex(element, zIndex);
            element.IsHitTestVisible = false;
            Sketchpad.Children.Add(element);
        }

        #region 指示性元素

        private ToolTip tip;
        private Line mouseLine;
        private Dictionary<EllipseGeometry, TPoint> UiPoint2Point = new Dictionary<EllipseGeometry, TPoint>();
        private Dictionary<TPoint, EllipseGeometry> Point2UiPoint = new Dictionary<TPoint, EllipseGeometry>();

        #endregion 指示性元素

        #region 刷新重绘

        private int sizeChangeCount = 0;

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

        // string lastAction = "";
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

        private EllipseGeometry lastSelectedPoint = null;

        private bool canMouseMoveUpdate = true;

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

        public void SetLine(DateTime time)
        {
            double percent = 1.0 * (time - BorderInfo.minBorderTime).Ticks / (BorderInfo.maxBorderTime - BorderInfo.minBorderTime).Ticks;

            double width = percent * Sketchpad.ActualWidth;
            RefreshMouseLine(width);
            //RefreshToolTip(GetPoint(width));
        }

        public void ClearLine()
        {
            if (mouseLine != null)
            {
                Sketchpad.Children.Remove(mouseLine);
                mouseLine = null;
            }
        }

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

        private void RefreshMouseLine(EllipseGeometry point)
        {
            double x = displayedPointsX[point];

            RefreshMouseLine(x);
        }

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

        #endregion 刷新重绘

        #region 暴露的方法

        public Func<Task> DrawActionAsync { private get; set; }

        public async Task DrawPointsAsync(IEnumerable<TPoint> items, int borderIndex,bool draw)
        {
            BorderInfo border = borders[borderIndex];
            await Task.Run(() =>
            {
                foreach (var item in items)
                {
                    DateTime time = XAxisPointValueConverter(item);
                    double value = YAxisPointValueConverter(item);

                    TimeSpan currentSpan = time - border.minTime;

                    double x = Sketchpad.ActualWidth * (time - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks;
                    double y = Sketchpad.ActualHeight * (value - border.minBorderValue) / border.borderValueSpan;
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

        public async Task DrawLinesAsync(IEnumerable<TLine> items, int borderIndex)
        {
            BorderInfo border = borders[borderIndex];
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

                        TimeSpan currentSpan1 = time1 - border.minTime;

                        double x1 = Sketchpad.ActualWidth * (time1 - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks;
                        double y1 = Sketchpad.ActualHeight * (value1 - border.minBorderValue) / border.borderValueSpan;
                        DateTime time2 = XAxisLineValueConverter(item);
                        double value2 = YAxisLineValueConverter(item);

                        TimeSpan currentSpan2 = time2 - border.minTime;

                        double x2 = Sketchpad.ActualWidth * (time2 - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks;
                        double y2 = Sketchpad.ActualHeight * (value2 - border.minBorderValue) / border.borderValueSpan;
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

        public void BeginDraw()
        {
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
                }
                finally
                {
                    isDrawing = false;
                }
            });
        }

        public async Task DrawPolygonAsync(IEnumerable<TPolygon> items, int borderIndex)
        {
            BorderInfo border = borders[borderIndex];
            double yZero = Sketchpad.ActualHeight
                - (-border.minBorderValue) / (border.maxBorderValue - border.minBorderValue)
                * Sketchpad.ActualHeight;
            List<Point> points = new List<Point>();

            await Task.Run(() =>
            {
                points.Add(new Point(Sketchpad.ActualWidth * (items.Max(q => XAxisPolygonValueConverter(q)) - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks, yZero));
                points.Add(new Point(Sketchpad.ActualWidth * (items.Min(q => XAxisPolygonValueConverter(q)) - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks, yZero));
                foreach (var item in items)
                {
                    DateTime time = XAxisPolygonValueConverter(item);
                    double value = YAxisPolygonValueConverter(item);
                    if (double.IsNaN(value))
                    {
                        continue;
                    }
                    TimeSpan currentSpan = time - border.minTime;
                    double x = Sketchpad.ActualWidth * (time - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks;
                    double y = Sketchpad.ActualHeight - Sketchpad.ActualHeight * (value - border.minBorderValue) / border.borderValueSpan;
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

        public void Initialize()
        {
            borders.Clear();
            displayedPointsX.Clear();
            Sketchpad.Children.Clear();
            UiPoint2Point.Clear();
            Point2UiPoint.Clear();
            BorderInfo.Initialize();
            Sketchpad.RenderTransform = null;
        }

        public async Task<int> DrawBorderAsync<TBorder>(IEnumerable<TBorder> items, bool draw, BorderSetting<TBorder> setting)
        {
            BorderInfo border = new BorderInfo();
            borders.Add(border);
            TimeSpan lineTimeSpan = TimeSpan.Zero;
            await Task.Run(() =>
            {
                border.minTime = items.Min(p => setting.XAxisBorderValueConverter(p));
                border.maxTime = items.Max(p => setting.XAxisBorderValueConverter(p));
                border.minValue = items.Min(p => setting.YAxisBorderValueConverter(p));
                border.maxValue = items.Max(p => setting.YAxisBorderValueConverter(p));
                border.valueSpan = border.maxValue - border.minValue;
                border.timeSpan = border.maxTime - border.minTime;

                foreach (var item in setting.TimeSpanMapping)
                {
                    if (border.timeSpan < item.Key)
                    {
                        lineTimeSpan = item.Value;
                        break;
                    }
                }
                if (lineTimeSpan == TimeSpan.Zero)
                {
                    lineTimeSpan = TimeSpan.FromTicks(border.timeSpan.Ticks / 10);
                }

                var minBorderTime = new DateTime(border.minTime.Ticks / lineTimeSpan.Ticks * lineTimeSpan.Ticks);
                var maxBorderTime = new DateTime((border.maxTime.Ticks / lineTimeSpan.Ticks + 1) * lineTimeSpan.Ticks);

                if (minBorderTime < BorderInfo.minBorderTime)
                {
                    BorderInfo.minBorderTime = minBorderTime;
                }
                if (maxBorderTime > BorderInfo.maxBorderTime)
                {
                    BorderInfo.maxBorderTime = maxBorderTime;
                }
                BorderInfo.borderTimeSpan = BorderInfo.maxBorderTime - BorderInfo.minBorderTime;
            });
            //border.borderTimeSpan = border.maxBorderTime - border.minBorderTime;
            if (draw)
            {
                int verticleBorderCount = (int)(BorderInfo.borderTimeSpan.Ticks / lineTimeSpan.Ticks);
                for (int i = 0; i < verticleBorderCount; i++)
                {
                    double x = Sketchpad.ActualWidth * i / verticleBorderCount;
                    DrawVerticalLine(x, 1);
                    DrawText(x, FontSize * 1.2, XLabelFormat(new DateTime(BorderInfo.minBorderTime.Ticks + lineTimeSpan.Ticks * i)), VerticleTextBrush, true, "xLabel");
                }
            }

            double lineValueSpan = 0;
            foreach (var item in setting.ValueSpanMapping)
            {
                if (border.valueSpan < item.Key)
                {
                    lineValueSpan = item.Value;
                    break;
                }
            }
            if (lineValueSpan == 0)
            {
                lineValueSpan = border.valueSpan / 10;
            }

            border.minBorderValue = ((int)(border.minValue / lineValueSpan - 1)) * lineValueSpan;
            border.maxBorderValue = ((int)(border.maxValue / lineValueSpan + 1)) * lineValueSpan;
            border.borderValueSpan = border.maxBorderValue - border.minBorderValue;
            if (draw)
            {
                double horizentalBorderCount = (border.maxBorderValue - border.minBorderValue) / lineValueSpan;
                for (int i = 0; i < horizentalBorderCount; i++)
                {
                    double y = Sketchpad.ActualHeight * i / horizentalBorderCount;
                    DrawHorizentalLine(y);
                    DrawText(0, y + FontSize * 1.2, YLabelFormat(border.minBorderValue + lineValueSpan * i), HorizentalTextBrush, false, "yLabel");
                }
            }

            //lastBorderPoints = items;
            return borders.Count - 1;
        }

        public async Task StretchToFitAsync()
        {
            DateTime minTime = default;
            DateTime maxTime = default;
            double minTimeToLeft = 0;
            double scaleValue = 0;
            await Task.Run(() =>
            {
                minTime = borders.Min(p => p.minTime);
                maxTime = borders.Max(p => p.maxTime);
                minTimeToLeft = 1.0
                   * (minTime - BorderInfo.minBorderTime).Ticks
                   / BorderInfo.borderTimeSpan.Ticks
                   * Sketchpad.ActualWidth;
                scaleValue = 1.0 * BorderInfo.borderTimeSpan.Ticks
                        / (maxTime.Ticks - minTime.Ticks);
            });
            TranslateTransform translate = new TranslateTransform(-minTimeToLeft, 0);

            ScaleTransform scale = new ScaleTransform(scaleValue, 1);

            //重置左侧标签的X坐标
            foreach (var tbk in Sketchpad.Children.OfType<TextBlock>().Where(p => "yLabel".Equals(p.Tag)))
            {
                Canvas.SetLeft(tbk, minTimeToLeft);
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

        public Func<TLine, TLine, bool> LinePointEnbale { get; set; } = (p1, p2) => true;
        public Func<TPoint, DateTime> XAxisPointValueConverter { get; set; } = p => DateTime.MinValue;
        public Func<TLine, DateTime> XAxisLineValueConverter { get; set; } = p => DateTime.MinValue;
        public Func<TPolygon, DateTime> XAxisPolygonValueConverter { get; set; } = p => DateTime.MinValue;
        public Func<TPoint, double> YAxisPointValueConverter { get; set; } = p => 0;
        public Func<TLine, double> YAxisLineValueConverter { get; set; } = p => 0;
        public Func<TPolygon, double> YAxisPolygonValueConverter { get; set; } = p => 0;
        public Func<DateTime, string> XLabelFormat { get; set; } = p => p.ToString();
        public Func<double, string> YLabelFormat { get; set; } = p => p.ToString();
        public Brush VerticleGridlinesBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush HorizentalGridlinesBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush HorizentalTextBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush VerticleTextBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC));
        public Brush PointBrush { get; set; } = Brushes.Red;
        public Brush SelectedPointBrush { get; set; } = Brushes.Green;
        public Brush LineBrush { get; set; } = Brushes.Blue;
        public Brush PolygonBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0x33, 0x55, 0x55, 0x55));
        public double LineThickness { get; set; } = 1;
        public double PointSize { get; set; } = 1;
        public bool MouseLineEnable { get; set; } = true;
        public bool ToolTipEnable { get; set; } = true;
        public Func<TPoint, string> ToolTipConverter { get; set; } = p => p.ToString();
        public TimeSpan SizeChangeDelay { get; set; } = TimeSpan.FromSeconds(1);

        public double FontSize { get; set; } = 9;

        #endregion 可从外部更改的属性

        #region 基础绘制

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
            Point2UiPoint.Add(point, e);
            return e;
        }

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

        private LineGeometry GetLine(double x1, double y1, double x2, double y2)
        {
            //Line line = new Line()
            //{
            //    X1 = x1,
            //    X2 = x2,
            //    Y1 = Sketchpad.ActualHeight - y1,
            //    Y2 = Sketchpad.ActualHeight - y2,
            //    StrokeThickness = 1,
            //    Stroke = LineBrush,
            //};
            LineGeometry lg = new LineGeometry()
            {
                StartPoint = new Point(x1, Sketchpad.ActualHeight - y1),
                EndPoint = new Point(x2, Sketchpad.ActualHeight - y2),
            };
            return lg;
        }

        public Dictionary<EllipseGeometry, double> displayedPointsX { get; private set; } = new Dictionary<EllipseGeometry, double>();

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

        #endregion 基础绘制

        #region 事件相关

        public class MouseOverPointChangedEventArgs : MouseEventArgs
        {
            public MouseOverPointChangedEventArgs(MouseEventArgs baseEvent, TPoint item) : base(baseEvent.MouseDevice, baseEvent.Timestamp, baseEvent.StylusDevice)
            {
                Item = item;
            }

            public TPoint Item { get; private set; }
        }

        public delegate void MouseOverPointEventHandler(object sender, MouseOverPointChangedEventArgs e);

        public event MouseOverPointEventHandler MouseOverPoint;

        #endregion 事件相关

        private class BorderInfo
        {
            public static void Initialize()
            {
                minBorderTime = DateTime.MaxValue;
                maxBorderTime = DateTime.MinValue;
                borderTimeSpan = TimeSpan.Zero;
            }

            public double maxValue { get; set; }
            public double minValue { get; set; }
            public double valueSpan { get; set; }
            public double maxBorderValue { get; set; }
            public double minBorderValue { get; set; }
            public double borderValueSpan { get; set; }
            public DateTime minTime { get; set; }
            public DateTime maxTime { get; set; }
            public static DateTime minBorderTime { get; set; }
            public static DateTime maxBorderTime { get; set; }
            public static TimeSpan borderTimeSpan { get; set; }
            public TimeSpan timeSpan { get; set; }
        }

        public class BorderSetting<TBorder>
        {
            public Func<TBorder, DateTime> XAxisBorderValueConverter { get; set; } = p => DateTime.MinValue;
            public Func<TBorder, double> YAxisBorderValueConverter { get; set; } = p => 0;

            public Dictionary<TimeSpan, TimeSpan> TimeSpanMapping { get; set; } = new Dictionary<TimeSpan, TimeSpan>()
            {
                { TimeSpan.FromMinutes(1),TimeSpan.FromSeconds(5) },
                { TimeSpan.FromMinutes(5),TimeSpan.FromSeconds(10) },
                { TimeSpan.FromMinutes(20),TimeSpan.FromMinutes(2) },
                { TimeSpan.FromMinutes(60),TimeSpan.FromMinutes(5) },
                { TimeSpan.FromHours(5),TimeSpan.FromMinutes(30) },
                { TimeSpan.FromHours(60),TimeSpan.FromHours(5) },
            };

            public Dictionary<double, double> ValueSpanMapping { get; set; } = new Dictionary<double, double>()
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
        }
    }
}