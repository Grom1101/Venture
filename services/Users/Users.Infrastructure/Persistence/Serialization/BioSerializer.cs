using System.Text.Json;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Serialization;

/// <summary>
/// Bio — value object с вложенной коллекцией интересов, поэтому он хранится
/// одной JSON-колонкой, а не набором скалярных полей.
/// </summary>
internal static class BioSerializer
{
    private sealed record BioSnapshot(string About, string FocusedOn, string Comment, string[] Interests);

    public static string Serialize(Bio bio) =>
        JsonSerializer.Serialize(new BioSnapshot(
            bio.About,
            bio.FocusedOn,
            bio.Comment,
            bio.Interests.Select(interest => interest.Value).ToArray()));

    public static Bio Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Bio.Default();

        var snapshot = JsonSerializer.Deserialize<BioSnapshot>(json);

        return snapshot is null
            ? Bio.Default()
            : Bio.Create(
                snapshot.About,
                snapshot.FocusedOn,
                snapshot.Comment,
                snapshot.Interests.Select(value => Interest.Create(value).Value)).Value;
    }
}
