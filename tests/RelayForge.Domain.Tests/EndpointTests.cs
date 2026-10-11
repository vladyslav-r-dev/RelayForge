using RelayForge.Domain.Entities;
using Xunit;

namespace RelayForge.Domain.Tests;

public class EndpointTests
{
    [Fact]
    public void Endpoint_Should_Activate_And_Deactivate()
    {
        // Arrange
        var endpoint = new Endpoint("https://example.com");

        Assert.True(endpoint.IsActive);

        // Act
        endpoint.Deactivate();

        // Assert
        Assert.False(endpoint.IsActive);

        // Act
        endpoint.Activate();

        // Assert
        Assert.True(endpoint.IsActive);
    }
}