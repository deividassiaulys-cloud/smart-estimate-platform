using System.Security.Cryptography;
using System.Text;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// A short, stable stand-in for an email in the logs (feature 021, F8): the same email always gives the same value,
/// so repeated attempts can be linked, but the email itself is never written (GDPR).
/// </summary>
public static class EmailHash
{
    /// <summary>
    /// The first 12 hex characters of the SHA-256 of the trimmed, lower-case email.
    /// </summary>
    public static string Of(string email) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())))[..12];
}
