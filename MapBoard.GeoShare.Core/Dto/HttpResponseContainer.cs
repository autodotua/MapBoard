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
            Message = mesasge;
            Data = data;
        }

        public bool Success { get; set; }

        public string Message {  get; set; }

        public object Data { get; set; }
    }    

    public class HttpResponseContainer<T>
    {
        public HttpResponseContainer()
        {
        }

        public HttpResponseContainer(bool success, string mesasge, T data)
        {
            Success = success;
            Message = mesasge;
            Data = data;
        }

        public bool Success { get; set; }

        public string Message {  get; set; }

        public T Data { get; set; }
    }
}
