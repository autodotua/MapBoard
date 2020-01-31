using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapBoard.TileDownloaderSplicer
{
    /// <summary>
    /// BoundaryInputTable.xaml 的交互逻辑
    /// </summary>
    public partial class BoundaryInputTable : UserControl
    {
        public BoundaryInputTable()
        {
            InitializeComponent();
        }

        private bool IsIntLegal(out int left, out int right, out int top, out int bottom)
        {

            left = right = top = bottom = 0;
            return (int.TryParse(txtTop.Text, out top)
                && int.TryParse(txtLeft.Text, out left)
                && int.TryParse(txtRight.Text, out right)
                && int.TryParse(txtBottom.Text, out bottom) &&
                !(left < 0 || right < 0 || top < 0 || bottom < 0
                || left > right || bottom < top));

        }
        private bool IsDoubleLegal(out double left, out double right, out double top, out double bottom)
        {

            left = right = top = bottom = 0;
            return (double.TryParse(txtTop.Text, out top)
                && double.TryParse(txtLeft.Text, out left)
                && double.TryParse(txtRight.Text, out right)
                && double.TryParse(txtBottom.Text, out bottom) &&
                !( left > right || bottom > top));

        }

        public void SetDoubleValue(double left, double top, double right, double bottom)
        {
            txtLeft.Text = left.ToString();
            txtTop.Text = top.ToString();
            txtRight.Text = right.ToString();
            txtBottom.Text = bottom.ToString();
        }
        public void SetIntValue(int left, int top, int right, int bottom)
        {
            txtLeft.Text = left.ToString();
            txtTop.Text = top.ToString();
            txtRight.Text = right.ToString();
            txtBottom.Text = bottom.ToString();
        }

        public Range<int> GetIntValue()
        {
            if (!IsIntLegal(out int left, out int right, out int top, out int bottom))
            {
                return null;
            }
            return new Range<int>(left, right, bottom, top);
        }
        public Range<double> GetDoubleValue()
        {
            if (!IsDoubleLegal(out double left, out double right, out double top, out double bottom))
            {
                return null;
            }
            return new Range<double>(left,right,bottom,top);
        }
    }
}
