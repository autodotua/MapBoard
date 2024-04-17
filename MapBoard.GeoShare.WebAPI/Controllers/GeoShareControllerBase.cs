using Microsoft.AspNetCore.Mvc;

namespace MapBoard.GeoShare.WebAPI.Controllers
{
    public class GeoShareControllerBase : ControllerBase
    {
        protected int GetUser()
        {
            return HttpContext.Session.GetInt32("user") ?? throw new Exception("未登录");
        }
    }
}
