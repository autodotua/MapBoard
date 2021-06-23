using FzLib.Basic.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MapBoard.UI.GpxToolbox
{
    public class TimeBasedChartHelper<TPoint, TLine, TPolygon>
        where TPoint : class
        where TLine : class
        where TPolygon : class
    {
        private bool isBusy = false;

        public TimeBasedChartHelper(Canvas canvas)
        {
            Sketchpad = canvas;
            canvas.SizeChanged += SketchpadSizeChanged;
            canvas.PreviewMouseMove += SketchpadPreviewMouseMove;
            canvas.MouseLeave += SketchpadMouseLeave;
            canvas.Background = Brushes.White;
            canvas.ClipToBounds = true;
        }

        private List<BorderInfo> borders = new List<BorderInfo>();

        public Canvas Sketchpad { get; set; }

        private void AddSketchpadChildren(UIElement element)
        {
            element.IsHitTestVisible = false;
            Sketchpad.Children.Add(element);
        }

        #region 指示性元素

        private ToolTip tip;
        private Line mouseLine;
        private TwoWayDictionary<Ellipse, TPoint> PointItems = new TwoWayDictionary<Ellipse, TPoint>();

        #endregion 指示性元素

        #region 刷新重绘

        private int sizeChangedEventCount = 0;

        private async void SketchpadSizeChanged(object sender, SizeChangedEventArgs e)
        {
            isBusy = true;
            if (sizeChangedEventCount == 0)
            {
                mouseLine = null;
                Canvas overlay = new Canvas()
                {
                    Width = Sketchpad.ActualWidth,
                    Height = Sketchpad.ActualHeight,
                    Background = Brushes.White,
                    Opacity = 0,
                };
                AddSketchpadChildren(overlay);
                DoubleAnimation ani = new DoubleAnimation(1, SizeChangeDelay);
                Storyboard.SetTarget(ani, overlay);
                Storyboard.SetTargetProperty(ani, new PropertyPath(Canvas.OpacityProperty));
                new Storyboard() { Children = { ani } }.Begin();
            }
            sizeChangedEventCount++;
            await Task.Delay(SizeChangeDelay);
            sizeChangedEventCount--;
            if (sizeChangedEventCount == 0)
            {
                Sketchpad.Children.Clear();
                if (DrawActionAsync != null)
                {
                    await DrawActionAsync.Invoke();
                }

                isBusy = false;
            }
        }

        // string lastAction = "";
        private void SketchpadMouseLeave(object sender, MouseEventArgs e)
        {
            if (lastSelectedPoint != null)
            {
                lastSelectedPoint.Fill = PointBrush;
                lastSelectedPoint.Width = lastSelectedPoint.Height = PointSize;
                lastSelectedPoint = null;
            }
            if (tip != null)
            {
                tip.IsOpen = false;
                tip = null;
            }
            ClearLine();
        }

        private Ellipse lastSelectedPoint = null;

        private void SketchpadPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isBusy || displayedPointsX.Count == 0)
            {
                return;
            }
            double x = e.GetPosition(Sketchpad).X;
            Ellipse point = GetPoint(x);
            SelectPoint(point);
            RefreshMouseLine(point);
            RefreshToolTip(point);
            MouseOverPoint?.Invoke(this, new MouseOverPointChangedEventArgs(e, PointItems[point]));
        }

        private Ellipse GetPoint(double x)
        {
            Ellipse point = displayedPointsX.First().Key;
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

        public void SelectPoint(TPoint point)
        {
            SelectPoint(PointItems.GetKey(point));
        }

        private void SelectPoint(Ellipse point)
        {
            if (lastSelectedPoint == point)
            {
                return;
            }
            if (lastSelectedPoint != null)
            {
                lastSelectedPoint.Fill = PointBrush;
                lastSelectedPoint.Width = lastSelectedPoint.Height = PointSize;
            }
            point.Fill = SelectedPointBrush;
            point.Width = point.Height = PointSize * 2;
            lastSelectedPoint = point;
        }

        public void SetLine(DateTime time)
        {
            double percent = 1.0 * (time - BorderInfo.minBorderTime).Ticks / (BorderInfo.maxBorderTime - BorderInfo.minBorderTime).Ticks;

            double width = percent * Sketchpad.ActualWidth;
            RefreshMouseLine(width);
            RefreshToolTip(GetPoint(width));
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
                    Sketchpad.Children.Insert(0, mouseLine);
                }
                mouseLine.X1 = mouseLine.X2 = x;
            }
        }

        private void RefreshMouseLine(Ellipse point)
        {
            double x = displayedPointsX[point];

            RefreshMouseLine(x);
        }

        private void RefreshToolTip(Ellipse point)
        {
            if (ToolTipEnable)
            {
                if (tip == null)
                {
                    tip = new ToolTip
                    {
                        Placement = PlacementMode.Left,
                        HorizontalOffset = 20,
                    };
                }
                tip.PlacementTarget = point;
                tip.Content = ToolTipConverter(PointItems[point]);
                tip.IsOpen = true;
            }
        }

        #endregion 刷新重绘

        #region 暴露的方法

        public Func<Task> DrawActionAsync { get; set; }

        public void DrawPoints(IEnumerable<TPoint> items, int borderIndex)
        {
            BorderInfo border = borders[borderIndex];
            foreach (var item in items)
            {
                DateTime time = XAxisPointValueConverter(item);
                double value = YAxisPointValueConverter(item);

                TimeSpan currentSpan = time - border.minTime;

                double x = Sketchpad.ActualWidth * (time - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks;
                double y = Sketchpad.ActualHeight * (value - border.minBorderValue) / border.borderValueSpan;
                PointItems.Add(DrawPoint(x, y), item);
            }
            // lastAction = nameof(DrawPoints);
            // lastPointPoints = items;
        }

        public void DrawLines(IEnumerable<TLine> items, int borderIndex)
        {
            BorderInfo border = borders[borderIndex];
            TLine last = default;
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
                    DrawLine(x1, y1, x2, y2);
                }
                last = item;
            }
            //lastAction = nameof(DrawLines);
            //lastLinePoints = items;
        }

        public void DrawPolygon(IEnumerable<TPolygon> items, int borderIndex)
        {
            BorderInfo border = borders[borderIndex];
            Polygon p = new Polygon()
            {
                Fill = PolygonBrush,
            };
            double yZero = Sketchpad.ActualHeight
                - (-border.minBorderValue) / (border.maxBorderValue - border.minBorderValue)
                * Sketchpad.ActualHeight;

            p.Points.Add(new Point(Sketchpad.ActualWidth * (items.Max(q => XAxisPolygonValueConverter(q)) - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks, yZero));
            p.Points.Add(new Point(Sketchpad.ActualWidth * (items.Min(q => XAxisPolygonValueConverter(q)) - BorderInfo.minBorderTime).Ticks / BorderInfo.borderTimeSpan.Ticks, yZero));
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
                p.Points.Add(new Point(x, y));
            }
            Sketchpad.Children.Add(p);
        }

        public void Initialize()
        {
            borders.Clear();
            displayedPointsX.Clear();
            Sketchpad.Children.Clear();
            PointItems.Clear();
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

        public void StretchToFit()
        {
            var minTime = borders.Min(p => p.minTime);
            var maxTime = borders.Max(p => p.maxTime);
            var minTimeToLeft = 1.0
                * (minTime - BorderInfo.minBorderTime).Ticks
                / BorderInfo.borderTimeSpan.Ticks
                * Sketchpad.ActualWidth;

            TranslateTransform translate = new TranslateTransform(-minTimeToLeft, 0);

            double scaleValue = 1.0 * BorderInfo.borderTimeSpan.Ticks
                / (maxTime.Ticks - minTime.Ticks);
            ScaleTransform scale = new ScaleTransform(scaleValue, 1);

            //重置左侧标签的X坐标
            foreach (var tbk in Sketchpad.Children.OfType<TextBlock>().Where(p => "yLabel".Equals(p.Tag)))
            {
                Canvas.SetLeft(tbk, minTimeToLeft);
            }
            ScaleTransform scaleTextBlock = new ScaleTransform(1 / scaleValue, 1);

            //防止标签吧被拉伸变形
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
        public double PointSize { get; set; } = 3;
        public bool MouseLineEnable { get; set; } = true;
        public bool ToolTipEnable { get; set; } = true;
        public Func<TPoint, string> ToolTipConverter { get; set; } = p => p.ToString();
        public TimeSpan SizeChangeDelay { get; set; } = TimeSpan.FromSeconds(1);

        public double FontSize { get; set; } = 9;

        #endregion 可从外部更改的属性

        #region 基础绘制

        private Ellipse DrawPoint(double x, double y)
        {
            double r = PointSize / 2;
            Ellipse e = new Ellipse()
            {
                Width = 2 * r,
                Height = 2 * r,
                Fill = PointBrush,
            };
            displayedPointsX.Add(e, x);
            Canvas.SetLeft(e, x - r);
            Canvas.SetTop(e, Sketchpad.ActualHeight - (y - r));
            AddSketchpadChildren(e);
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
            AddSketchpadChildren(line);
        }

        private void DrawLine(double x1, double y1, double x2, double y2)
        {
            Line line = new Line()
            {
                X1 = x1,
                X2 = x2,
                Y1 = Sketchpad.ActualHeight - y1,
                Y2 = Sketchpad.ActualHeight - y2,
                StrokeThickness = 1,
                Stroke = LineBrush,
            };

            AddSketchpadChildren(line);
        }

        public Dictionary<Ellipse, double> displayedPointsX { get; private set; } = new Dictionary<Ellipse, double>();

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
            AddSketchpadChildren(line);
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
            AddSketchpadChildren(tbk);
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