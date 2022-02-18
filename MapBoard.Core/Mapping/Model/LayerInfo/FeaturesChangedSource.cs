using System.ComponentModel;

namespace MapBoard.Mapping.Model
{
    public enum FeaturesChangedSource
    {
        [Description("绘制")]
        Draw,

        [Description("编辑")]
        Edit,

        [Description("要素操作")]
        FeatureOperation,

        [Description("撤销")]
        Undo,

        [Description("导入")]
        Import
    }
}