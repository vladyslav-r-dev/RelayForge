namespace RelayForge.Domain.Entities;

public class Endpoint
{
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string Url { get; private set; }
    public bool IsActive { get; private set; }

    public Endpoint(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be null or empty.", nameof(url));

        if (url.Length > 2048)
            throw new ArgumentException("Url cannot exceed 2048 characters.", nameof(url));

        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        Url = url;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
