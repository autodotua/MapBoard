using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard
{
    /// <summary>
    /// 一些在Core项目中就需要用到的参数
    /// </summary>
    public static class Parameters
    {
        /// <summary>
        /// 创建时间字段名
        /// </summary>
        public const string CreateTimeFieldName = "CrtTime";

        /// <summary>
        /// 修改时间字段名
        /// </summary>
        public const string ModifiedTimeFieldName = "MdfTime";

        /// <summary>
        /// 日期时间格式
        /// </summary>
        public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 日期格式
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// 短日期时间格式
        /// </summary>
        public const string CompactDateTimeFormat = "yyyyMMdd-HHmmss";

        /// <summary>
        /// 快速动画时长
        /// </summary>
        public static TimeSpan FastAnimationDuration { get; set; } = TimeSpan.FromSeconds(0.2);

        /// <summary>
        /// 普通动画时长
        /// </summary>
        public static TimeSpan AnimationDuration { get; set; } = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// 加载超时
        /// </summary>
        public static TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}