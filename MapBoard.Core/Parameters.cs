using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard
{
    public static class Parameters
    {
        public const string ClassFieldName = "Key";
        public const string DateFieldName = "Date";
        public const string LabelFieldName = "Info";
        public const string CreateTimeFieldName = "CrtTime";
        public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string DateFormat = "yyyy-MM-dd";
        public const string CompactDateTimeFormat = "yyyyMMdd-HHmmss";

        public static TimeSpan AnimationDuration { get; set; } = TimeSpan.FromSeconds(0.5);
        public static TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}