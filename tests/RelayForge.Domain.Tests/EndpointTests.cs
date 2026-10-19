using RelayForge.Domain.Entities;
using Xunit;

namespace RelayForge.Domain.Tests;

public class EndpointTests
{
    [Fact]
    public void Endpoint_Should_Activate_And_Deactivate()
    {
        var endpoint = new Endpoint("https://example.com");

        Assert.True(endpoint.IsActive);

        endpoint.Deactivate();

        Assert.False(endpoint.IsActive);

        endpoint.Activate();

        Assert.True(endpoint.IsActive);
    }
}
