using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.GeoShare.Core.Dto
{
    public class HttpResponseContainer
    {
        public HttpResponseContainer()
        {
        }

        public HttpResponseContainer(bool success, string mesasge, object data)
        {
            Success = success;
            Mesasge = mesasge;
            Data = data;
        }

        public bool Success { get; set; }

        public string Mesasge {  get; set; }

        public object Data { get; set; }
    }
}
