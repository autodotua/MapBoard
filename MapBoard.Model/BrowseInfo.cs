using FzLib;
using System.ComponentModel;

namespace MapBoard.Model
{
    /// <summary>
    /// GPX游览信息
    /// </summary>
    public class BrowseInfo : INotifyPropertyChanged
    {
        private int extraRecordDelay = 100;
        private int recordInterval = 1000;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 视线与竖直方向夹角
        /// </summary>
        public int Angle { get; set; } = 60;

        /// <summary>
        /// 录制时额外延时（毫秒）
        /// </summary>
        public int ExtraRecordDelay
        {
            get => extraRecordDelay;
            set
            {
                if (value < 1000)
                {
                    value = value / 10 * 10;
                }
                else if (value < 10000)
                {
                    value = value / 100 * 100;
                }
                extraRecordDelay = value;
            }
        }

        /// <summary>
        /// 帧率
        /// </summary>
        public double FPS { get; set; } = 20;

        /// <summary>
        /// 录制间隔（毫秒）
        /// </summary>
        public int RecordInterval
        {
            get => recordInterval;
            set
            {
                if (value < 1000)
                {
                    value = value / 10 * 10;
                }
                else if (value < 10000)
                {
                    value = value / 500 * 500;
                }
                else
                {
                    value = value / 1000 * 1000;
                }
                recordInterval = value;
            }
        }

        /// <summary>
        /// 每隔多少个点重置视角
        /// </summary>
        public double Sensitivity { get; set; } = 5;

        /// <summary>
        /// 播放速度和实际速度的倍率
        /// </summary>
        public double Speed { get; set; } = 16;

        /// <summary>
        /// 视角高度（米）
        /// </summary>
        public double Zoom { get; set; } = 200;
    }
}