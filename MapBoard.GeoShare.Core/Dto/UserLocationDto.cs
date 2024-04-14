using MapBoard.GeoShare.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.GeoShare.Core.Dto
{
    public class UserLocationDto
    {
        public string UserName { get; set; }
        public SharedLocationEntity Location { get; set; }
    }
}
