using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MapBoard.GeoShare.Core.Dto;
using MapBoard.GeoShare.Core;
using Microsoft.AspNetCore.Http.HttpResults;

public class GeoShareActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Path.HasValue)
        {
            throw new Exception();
        }
        if (context.HttpContext.Request.Path.Value.EndsWith("Login") 
            || context.HttpContext.Request.Path.Value.EndsWith("Register"))
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
        if (context.Exception != null)
        {
            if(context.Exception is StatusBasedException sbe)
            {
                if(string.IsNullOrEmpty(sbe.Message))
                {
                    context.Result = new StatusCodeResult((int)sbe.StatusCode);
                }
                else
                {
                    context.Result = new ObjectResult(sbe.Message) { StatusCode = (int)sbe.StatusCode };
                }
                context.ExceptionHandled = true;
            }
        }
    }
}