using VendorReview.Api.Auth;
using VendorReview.Api.Dtos;

namespace VendorReview.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", (CurrentUser me) =>
            Results.Ok(new MeDto(me.ObjectId, me.DisplayName, me.Email, me.Role.ToString(), me.IsAdmin)))
            .RequireAuthorization();
    }
}
