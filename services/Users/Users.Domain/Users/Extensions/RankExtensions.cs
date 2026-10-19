using Venture.Domain.Users.Enums;

namespace Venture.Domain.Users.Extensions;

public static class RankExtensions
{
    public static Rank FromRating(int rating) => rating switch
    {
        >= (int)Rank.Legend => Rank.Legend,
        >= (int)Rank.Peak => Rank.Peak,
        >= (int)Rank.Flow => Rank.Flow,
        >= (int)Rank.Great => Rank.Great,
        >= (int)Rank.Successful => Rank.Successful,
        _ => Rank.Starter,
    };

    public static int MinRating(this Rank rank) => (int)rank;

    public static Rank? Next(this Rank rank)
    {
        var values = Enum.GetValues<Rank>();
        var index = Array.IndexOf(values, rank);

        return index >= 0 && index < values.Length - 1 ? values[index + 1] : null;
    }
}
