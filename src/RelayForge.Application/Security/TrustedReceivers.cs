namespace RelayForge.Application.Security;

public sealed class TrustedReceivers
{
    private readonly HashSet<string> _urls;

    public TrustedReceivers(IEnumerable<string> urls)
    {
        _urls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var url in urls)
        {
            if (!IsHttpsUrl(url))
                throw new ArgumentException("Trusted receivers must be absolute HTTPS URLs without credentials or fragments.");
            _urls.Add(url);
        }
    }

    public bool IsAllowed(string? url) => url is not null && IsHttpsUrl(url) && _urls.Contains(url);

    private static bool IsHttpsUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && url.Length <= 2048 &&
        url == url.Trim() && Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Fragment);
}
