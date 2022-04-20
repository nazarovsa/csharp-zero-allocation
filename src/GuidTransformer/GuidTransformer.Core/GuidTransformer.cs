namespace GuidTransformer.Core;

public static partial class GuidTransformer
{
    public static string ToStringFromGuid(Guid id)
    {
        return Convert.ToBase64String(id.ToByteArray())
            .Replace("/", "-")
            .Replace("+", "_")
            .Replace("=", string.Empty);
    }

    public static Guid ToGuidFromString(string id)
    {
        var efficientBase64 = Convert.FromBase64String(id
            .Replace("-", "/")
            .Replace("_", "+") + "==");

        return new Guid(efficientBase64);
    }
}