using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MapBoard.GeoShare.Core.Dto;

public class GeoShareActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Path.HasValue)
        {
            throw new Exception();
        }
        if (context.HttpContext.Request.Path.Value.EndsWith("Login"))
        {
            return;
        }
        if (context.HttpContext.Session.GetInt32("user") == null)
        {
            context.Result = new ObjectResult(new HttpResponseContainer(false, "未登录", null));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception == null)
        {
            if (context.Result is ObjectResult or)
            {
                context.Result = new ObjectResult(new HttpResponseContainer(true, null, or.Value));
            }
            else
            {
                context.Result = new ObjectResult(new HttpResponseContainer(true, null, context.Result));
            }
        }
        else
        {
            context.Result = new ObjectResult(new HttpResponseContainer(false, context.Exception.Message, context.Exception.ToString()));
            context.Exception = null;
        }
    }
}