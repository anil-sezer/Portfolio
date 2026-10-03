using Portfolio.Ui.Endpoints;

namespace Portfolio.Ui.Extensions;

public static class ApiEndpointExtensions
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapEmailEndpoints();
        app.MapVisitEndpoints();
    }
}
