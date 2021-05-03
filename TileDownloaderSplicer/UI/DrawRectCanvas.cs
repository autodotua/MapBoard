using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MapBoard.TileDownloaderSplicer
{
    public class DrawRectCanvas : Canvas
    {
        public DrawRectCanvas()
        {
        }

        public bool IsDrawing { get; private set; }

        public void StartDraw()
        {
            IsDrawing = true;

            Children.Clear();
            Cursor = Cursors.Cross;
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
        }

        private int pointIndex = 0;

        public void StopDrawing(bool raiseEvent)
        {
            Cursor = Cursors.Arrow;
            Background = null;
            pointIndex = 0;

            IsDrawing = false;
            if (raiseEvent)
            {
                ChooseComplete?.Invoke(this, new EventArgs());
            }
            else
            {
                Children.Clear();
            }
        }

        public Point FirstPoint { get; private set; }
        public Point SecondPoint { get; private set; }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            if (IsDrawing)
            {
                if (pointIndex == 0)
                {
                    FirstPoint = e.GetPosition(this);
                    pointIndex = 1;

                    rectangle = new Rectangle()
                    {
                        Fill = new SolidColorBrush(Color.FromArgb(0x33, 0x66, 0x66, 0x66)),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                    };
                    SetLeft(rectangle, FirstPoint.X);
                    SetTop(rectangle, FirstPoint.Y);
                    Children.Add(rectangle);
                }
                else
                {
                    rectangle.Stroke = Brushes.Green;
                    SecondPoint = e.GetPosition(this);
                    StopDrawing(true);
                }
            }
        }

        public event EventHandler ChooseComplete;

        private Rectangle rectangle;

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (IsDrawing && pointIndex == 1)
            {
                var currentPoint = e.GetPosition(this);
                if (currentPoint.X - FirstPoint.X > 0 && currentPoint.Y - FirstPoint.Y > 0)
                {
                    rectangle.Width = currentPoint.X - FirstPoint.X;
                    rectangle.Height = currentPoint.Y - FirstPoint.Y;
                }
            }
        }
    }
}