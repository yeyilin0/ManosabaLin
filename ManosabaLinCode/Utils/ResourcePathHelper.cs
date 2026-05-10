namespace ManosabaLin.Utils;

public static class ResourcePathHelper
{
    public static string BuildResPath(params string[] segments)
    {
        return string.Join(
            "/",
            new[] { MainFile.ResPath.TrimEnd('/') }
                .Concat(segments.Select(NormalizeSegment).Where(static s => !string.IsNullOrEmpty(s))));
    }

    public static string BuildRelativePath(params string[] segments)
    {
        return string.Join(
            "/",
            segments.Select(NormalizeSegment).Where(static s => !string.IsNullOrEmpty(s)));
    }

    public static string NormalizeSegment(string segment)
    {
        return segment.Replace('\\', '/').Trim('/');
    }
}
