using System.Text.Json;
using System.Text.Json.Serialization;

namespace Venture.Api.Services;

public sealed class ProfileMediaStorage(IWebHostEnvironment environment, ILogger<ProfileMediaStorage> logger)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public const long MaxAvatarBytes = 5 * 1024 * 1024;
    public const long MaxBannerBytes = 8 * 1024 * 1024;

    public async Task<ProfileMediaResult> SaveAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
        => await SaveAsync(userId, file, "avatar", MaxAvatarBytes, cancellationToken);

    public async Task<ProfileMediaResult> SaveBannerAsync(Guid userId, IFormFile file, CancellationToken cancellationToken)
        => await SaveAsync(userId, file, "banner", MaxBannerBytes, cancellationToken);

    public ProfileMediaInfo? ReadMedia(Guid userId)
    {
        var metaPath = GetMetaPath(userId);
        if (!File.Exists(metaPath))
            return null;

        try
        {
            var json = File.ReadAllText(metaPath);
            return JsonSerializer.Deserialize<ProfileMediaInfo>(json, JsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read profile media meta for {UserId}", userId);
            return null;
        }
    }

    private async Task<ProfileMediaResult> SaveAsync(
        Guid userId,
        IFormFile file,
        string kind,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
            return ProfileMediaResult.Fail("Файл пустой.");

        if (file.Length > maxBytes)
            return ProfileMediaResult.Fail($"Файл слишком большой. Максимум {maxBytes / (1024 * 1024)} МБ.");

        if (!AllowedContentTypes.Contains(file.ContentType) || !Extensions.TryGetValue(file.ContentType, out var extension))
            return ProfileMediaResult.Fail("Допустимы только JPEG, PNG, WebP и GIF.");

        var directory = GetUserDirectory(userId);
        Directory.CreateDirectory(directory);

        foreach (var old in Directory.EnumerateFiles(directory, $"{kind}.*"))
        {
            var name = Path.GetFileName(old);
            if (name.Equals("media.json", StringComparison.OrdinalIgnoreCase))
                continue;
            File.Delete(old);
        }

        var fileName = $"{kind}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream, cancellationToken);

        var meta = ReadMedia(userId) ?? new ProfileMediaInfo();
        if (kind == "avatar")
            meta.Avatar = fileName;
        else
            meta.Banner = fileName;

        meta.Version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await File.WriteAllTextAsync(GetMetaPath(userId), JsonSerializer.Serialize(meta, JsonOptions), cancellationToken);

        var url = $"/assets/profiles/{userId:D}/{fileName}?v={meta.Version}";
        return ProfileMediaResult.Ok(url, meta);
    }

    private string GetUserDirectory(Guid userId) =>
        Path.Combine(environment.WebRootPath, "assets", "profiles", userId.ToString("D"));

    private string GetMetaPath(Guid userId) =>
        Path.Combine(GetUserDirectory(userId), "media.json");
}

public sealed class ProfileMediaInfo
{
    public string? Avatar { get; set; }
    public string? Banner { get; set; }
    public long Version { get; set; }
}

public sealed class ProfileMediaResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }
    public string? Url { get; private init; }
    public ProfileMediaInfo? Media { get; private init; }

    public static ProfileMediaResult Ok(string url, ProfileMediaInfo media) => new()
    {
        Succeeded = true,
        Url = url,
        Media = media
    };

    public static ProfileMediaResult Fail(string error) => new()
    {
        Succeeded = false,
        Error = error
    };
}
