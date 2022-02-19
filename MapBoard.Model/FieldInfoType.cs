using System.ComponentModel;

namespace MapBoard.Model
{
    public enum FieldInfoType
    {
        [Description("整数")]
        Integer,

        [Description("小数")]
        Float,

        [Description("日期")]
        Date,

        [Description("文本")]
        Text,

        [Description("日期时间")]
        Time
    }
}