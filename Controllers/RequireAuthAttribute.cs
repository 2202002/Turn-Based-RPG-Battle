using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RpgGame.Models;
using RpgGame.Services;

namespace RpgGame.Controllers;

public class RequireAuthAttribute : Attribute, IAsyncActionFilter
{
    public const string UserItemKey = "AuthenticatedUser";

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var http = ctx.HttpContext;
        var auth = http.Request.Headers["Authorization"].ToString();

        string? token = null;
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = auth.Substring("Bearer ".Length).Trim();

        // Attributes can't use constructor injection so we resolve via the request services.
        var authService = http.RequestServices.GetRequiredService<AuthService>();
        var user = await authService.GetUserByTokenAsync(token);
        if (user == null)
        {
            ctx.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing token." });
            return;
        }

        http.Items[UserItemKey] = user;
        await next();
    }
}

public static class HttpContextExtensions
{
    public static User GetUser(this HttpContext ctx)
    {
        if (ctx.Items.TryGetValue(RequireAuthAttribute.UserItemKey, out var u) && u is User user)
            return user;
        throw new InvalidOperationException("No authenticated user on context.");
    }
}
