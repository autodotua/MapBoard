using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    /// <summary>
    /// 图层接口
    /// </summary>
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
        /// 数据源的名称，如数据库表名
        /// </summary>
        string SourceName { get; set; }

        /// <summary>
        /// 符号系统
        /// </summary>
        UniqueValueRendererInfo Renderer { get; set; }

        /// <summary>
        /// 筛选
        /// </summary>
        string DefinitionExpression { get; set; }

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