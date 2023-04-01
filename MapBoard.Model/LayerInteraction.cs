using System.ComponentModel;

namespace MapBoard.Model
{
    /// <summary>
    /// 图层交互设置
    /// </summary>
    public class LayerInteraction : INotifyPropertyChanged
    {
        /// <summary>
        /// 允许选择
        /// </summary>
        /// <remarks>
        /// 涉及到选择的功能：点选、框选、操作之后代码中的选择
        /// </remarks>
        public bool CanSelect { get; set; } = true;

        /// <summary>
        /// 允许捕捉
        /// </summary>
        public bool CanCatch { get; set; } = true;

        /// <summary>
        /// 允许编辑
        /// </summary>
        /// <remarks>
        /// 涉及到编辑的功能：绘制按钮、空格和回车键绘制、选择条的编辑、选择条中的操作、缓冲区目标、字段赋值、属性表编辑、坐标转换、历史记录
        /// </remarks>
        public bool CanEdit { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}