using FzLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MapBoard.Model
{
    /// <summary>
    /// 图层显示设置
    /// </summary>
    public class LayerDisplay : INotifyPropertyChanged
    {
        private double maxScale = 0;
        private double minScale = 0;
        private double opacity = 1;
        private int renderingMode = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 最大缩放比例的倒数
        /// </summary>
        public double MaxScale
        {
            get => maxScale;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                maxScale = value;
            }
        }

        /// <summary>
        /// 最小缩放比例的倒数
        /// </summary>
        public double MinScale
        {
            get => minScale;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                minScale = value;
            }
        }

        /// <summary>
        /// 透明度（0-1）
        /// </summary>
        public double Opacity
        {
            get => opacity;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > 1)
                {
                    value = 1;
                }
                opacity = value;
            }
        }

        /// <summary>
        /// 渲染模式，0：自动；1：静态；2：动态
        /// </summary>
        public int RenderingMode
        {
            get => renderingMode;
            set
            {
                if (value < 0 || value > 2)
                {
                    value = 0;
                }
                renderingMode = value;
            }
        }
    }
}