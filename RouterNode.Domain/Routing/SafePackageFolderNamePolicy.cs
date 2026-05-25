using System.Security.Cryptography;
using System.Text;

namespace RouterNode.Domain.Routing;

public sealed class SafePackageFolderNamePolicy : IPackageFolderNamePolicy
{
    private const int MaxFolderNameLength = 80;

    public string CreateFolderName(string orderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        var normalized = new StringBuilder(orderId.Length);
        foreach (var character in orderId.Trim())
        {
            normalized.Append(IsSafe(character) ? character : '_');
        }

        var candidate = normalized.ToString().Trim('.', ' ');
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = "order";
        }

        if (candidate.Length > MaxFolderNameLength)
        {
            candidate = candidate[..MaxFolderNameLength];
        }

        return $"{candidate}-{CreateStableSuffix(orderId)}";
    }

    private static bool IsSafe(char character) =>
        char.IsLetterOrDigit(character) || character is '-' or '_';

    private static string CreateStableSuffix(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}
