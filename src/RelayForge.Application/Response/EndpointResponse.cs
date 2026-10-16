public class EndpointResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}