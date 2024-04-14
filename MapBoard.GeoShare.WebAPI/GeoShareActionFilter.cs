using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MapBoard.GeoShare.Core.Dto;

public class GeoShareActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 在这里可以处理请求执行前的操作
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // 请求执行后的处理

        if (context.Exception == null)
        {
            // 获取操作结果
            var result = context.Result as ObjectResult;

            if (result != null)
            {
                context.Result = new ObjectResult(new HttpResponseContainer(true, null, result.Value));
            }
        }
        else
        {
            context.Result = new ObjectResult(new HttpResponseContainer(false, context.Exception.Message, null));
            context.Exception = null;
        }
    }
}