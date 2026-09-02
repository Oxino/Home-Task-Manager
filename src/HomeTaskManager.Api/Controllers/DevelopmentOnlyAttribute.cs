using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeTaskManager.Api.Controllers;

// Returns 404 (instead of a manual per-action check) outside Development, so seed/backfill endpoints
// are not reachable at all once deployed, and their existence isn't disclosed via a 403.
public sealed class DevelopmentOnlyAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            context.Result = new Microsoft.AspNetCore.Mvc.NotFoundResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
