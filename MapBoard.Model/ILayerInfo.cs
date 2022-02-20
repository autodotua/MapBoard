using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    public interface ILayerInfo : INotifyPropertyChanged
    {
        /// <summary>
        /// 字段
        /// </summary>
        FieldInfo[] Fields { get; set; }

        /// <summary>
        /// 组别名
        /// </summary>
        string Group { get; set; }

        /// <summary>
        /// 标签定义
        /// </summary>
        LabelInfo[] Labels { get; set; }

        /// <summary>
        /// 图层可见性
        /// </summary>
        bool LayerVisible { get; set; }

        /// <summary>
        /// 显示相关属性
        /// </summary>
        LayerDisplay Display { get; set; }

        /// <summary>
        /// 图层名
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 符号系统
        /// </summary>
        UniqueValueRendererInfo Renderer { get; set; }

        /// <summary>
        /// 时间范围
        /// </summary>
        string DefinitionExpression { get; set; }

        /// <summary>
        /// 图层的类型
        /// </summary>
        string Type { get; }

        /// <summary>
        /// 表示一些额外参数
        /// </summary>
        Dictionary<string, string> ServiceParameters { get; }

        /// <summary>
        /// 交互相关属性
        /// </summary>
        LayerInteraction Interaction { get; set; }

        /// <summary>
        /// 创建新的深度克隆副本
        /// </summary>
        /// <returns></returns>
        object Clone();
    }
}