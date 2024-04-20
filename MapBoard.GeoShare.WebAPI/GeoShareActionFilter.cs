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
        if (context.HttpContext.Request.Path.Value.EndsWith("Login") || context.HttpContext.Request.Path.Value.EndsWith("Register"))
        {
            return;
        }
        if (context.HttpContext.Session.GetInt32("user") == null)
        {
            context.Result = new UnauthorizedObjectResult("未登录");// new ObjectResult(new HttpResponseContainer(HttpResponseStatus.Unauthorized, "未登录", null));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        //if (context.Exception == null)
        //{
        //    if (context.Result is ObjectResult r)
        //    {
        //        context.Result = new ObjectResult(new HttpResponseContainer(HttpResponseStatus.OK, null, r.Value));
        //    }
        //    else
        //    {
        //        context.Result = new ObjectResult(new HttpResponseContainer(HttpResponseStatus.OK, null, context.Result));
        //    }
        //}
        //else
        //{
        //    context.Result = new ObjectResult(new HttpResponseContainer(HttpResponseStatus.ServiceUnavailable, context.Exception.Message, context.Exception.ToString()));
        //    context.Exception = null;
        //}
    }
}