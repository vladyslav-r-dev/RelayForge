using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RelayForge.Api.Middleware;
using RelayForge.Application.DTOs;
using RelayForge.Application.Security;
using RelayForge.Application.UseCases;

namespace RelayForge.Tests;

public class SecurityTests
{
    [Fact]
    public async Task Allowlist_matches_the_entire_url_and_blocks_creation_before_persistence()
    {
        var policy = new TrustedReceivers(["https://receiver.example/webhook"]);
        Assert.True(policy.IsAllowed("https://receiver.example/webhook"));
        foreach (var url in new[] { "http://receiver.example/webhook", "https://receiver.example/other",
                     "https://receiver.example.evil/webhook", "https://receiver.example/webhook?token=1",
                     "https://receiver.example/webhook#fragment", "https://user@receiver.example/webhook" })
            Assert.False(policy.IsAllowed(url));
        Assert.False(new TrustedReceivers([]).IsAllowed("https://receiver.example/webhook"));
        Assert.Throws<ArgumentException>(() => new TrustedReceivers(["http://localhost"]));

        var fixture = new DeliveryFixture();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new CreateEndpointUseCase(fixture, fixture, policy).ExecuteAsync(
                new CreateEndpointRequest { Url = "https://untrusted.example" }, default));
        Assert.Equal(0, fixture.SaveCount);
    }

    [Fact]
    public async Task Api_key_protects_all_routes_except_health_and_rejects_multiple_values()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Security:ApiKey"] = "test-only-key" }).Build();
        var reached = 0;
        var middleware = new ApiKeyMiddleware(_ => { reached++; return Task.CompletedTask; }, configuration);
        foreach (var path in new[] { "/events", "/endpoints", "/future-route", "/swagger/index.html" })
        {
            var denied = new DefaultHttpContext();
            denied.Request.Path = path;
            await middleware.InvokeAsync(denied);
            Assert.Equal(401, denied.Response.StatusCode);
        }
        var multiple = new DefaultHttpContext();
        multiple.Request.Headers["X-Api-Key"] = new Microsoft.Extensions.Primitives.StringValues(["test-only-key", "test-only-key"]);
        await middleware.InvokeAsync(multiple);
        Assert.Equal(401, multiple.Response.StatusCode);
        Assert.Equal(0, reached);

        var allowed = new DefaultHttpContext();
        allowed.Request.Headers["X-Api-Key"] = "test-only-key";
        await middleware.InvokeAsync(allowed);
        var health = new DefaultHttpContext();
        health.Request.Path = "/health";
        await middleware.InvokeAsync(health);
        Assert.Equal(2, reached);
    }
}
