using Microsoft.AspNetCore.Antiforgery;

namespace Portfolio.Ui.Extensions;

public static class AntiforgeryExtensions
{
    public static RouteHandlerBuilder RequireAntiforgeryToken(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            var isValid = await antiforgery.IsRequestValidAsync(context.HttpContext);
            if (!isValid)
            {
                Log.Warning("Antiforgery token validation failed for {Path} from {ClientIp}",
                    context.HttpContext.Request.Path,
                    context.HttpContext.GetClientIpAddress());
                return Results.BadRequest(new { message = "Antiforgery token validation failed." });
            }

            return await next(context);
        });
    }
}
