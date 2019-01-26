using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Style
{
    public class TimeExtentInfo
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }
        public bool IsEnable { get; set; }
    }
}
