namespace ShopDesk.Api.Models;

public static class JobStatus
{
    public const string New = "new";
    public const string InProgress = "in_progress";
    public const string Review = "review";
    public const string Done = "done";

    public static readonly string[] All = [New, InProgress, Review, Done];

    public static bool IsValid(string status) => All.Contains(status);

    public static string? Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var value = status.Trim().ToLowerInvariant().Replace(' ', '_');
        return IsValid(value) ? value : null;
    }
}
