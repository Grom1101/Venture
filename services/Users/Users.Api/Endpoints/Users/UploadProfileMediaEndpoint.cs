using Microsoft.AspNetCore.Mvc;
using Venture.Api.Services;
using Venture.Application.Abstractions.Authentication;

namespace Venture.Api.Endpoints.Users;

public sealed class UploadProfileMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/me/avatar", UploadAvatarAsync)
            .DisableAntiforgery()
            .WithTags("Users")
            .RequireAuthorization()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost("users/me/banner", UploadBannerAsync)
            .DisableAntiforgery()
            .WithTags("Users")
            .RequireAuthorization()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet("users/{userId:guid}/media", GetMedia)
            .WithTags("Users")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UploadAvatarAsync(
        HttpRequest request,
        ICurrentUser currentUser,
        ProfileMediaStorage storage,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var file = request.Form.Files.GetFile("file") ?? request.Form.Files.FirstOrDefault();
        if (file is null)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Файл не передан",
                Detail = "Ожидается multipart поле file.",
                Status = StatusCodes.Status400BadRequest
            });

        var result = await storage.SaveAvatarAsync(userId.Value, file, cancellationToken);
        if (!result.Succeeded)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Не удалось сохранить аватар",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });

        return Results.Ok(new { url = result.Url, media = ToResponse(userId.Value, result.Media!) });
    }

    private static async Task<IResult> UploadBannerAsync(
        HttpRequest request,
        ICurrentUser currentUser,
        ProfileMediaStorage storage,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var file = request.Form.Files.GetFile("file") ?? request.Form.Files.FirstOrDefault();
        if (file is null)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Файл не передан",
                Detail = "Ожидается multipart поле file.",
                Status = StatusCodes.Status400BadRequest
            });

        var result = await storage.SaveBannerAsync(userId.Value, file, cancellationToken);
        if (!result.Succeeded)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Не удалось сохранить шапку",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });

        return Results.Ok(new { url = result.Url, media = ToResponse(userId.Value, result.Media!) });
    }

    private static IResult GetMedia(Guid userId, ProfileMediaStorage storage)
    {
        var media = storage.ReadMedia(userId);
        if (media is null || (media.Avatar is null && media.Banner is null))
            return Results.NotFound();

        return Results.Ok(ToResponse(userId, media));
    }

    private static object ToResponse(Guid userId, ProfileMediaInfo media)
    {
        string? Url(string? fileName) =>
            fileName is null ? null : $"/assets/profiles/{userId:D}/{fileName}?v={media.Version}";

        return new
        {
            avatarUrl = Url(media.Avatar),
            bannerUrl = Url(media.Banner),
            version = media.Version
        };
    }
}
