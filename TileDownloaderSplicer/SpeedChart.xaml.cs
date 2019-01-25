using FzLib.Program.Runtime;
using LiveCharts;
using LiveCharts.Configurations;
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
using static FzLib.Geography.Analysis.SpeedAnalysis;

namespace GeographicTrajectoryToolbox
{
    /// <summary>
    /// SpeedChart.xaml 的交互逻辑
    /// </summary>
    public partial class SpeedChart : UserControl
    {
        public SpeedChart()
        {
            InitializeComponent();

        }

        public void Load(IEnumerable< SpeedInfo> speeds)
        {
            IChartValues Values = new ChartValues<SpeedInfo>();
            s.Values = Values;
            c.Series.Configuration = Mappers.Xy<SpeedInfo>()
                   .X(p => p.CenterTime.Ticks)
                   .Y(p => p.Speed)
                   .Fill(p =>  Brushes.Red);

            axisX.LabelFormatter = p => new DateTime((long)p).ToString("HH:mm:ss");
            axisY.LabelFormatter = p => p + "m/s";
            foreach (var speed in speeds)
            {
                Values.Add(speed);
                Thread.DoEvents();
            }
        }
    }
}
