namespace RelayForge.Application.Responses;

public class EndpointResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
