using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using RelayForge.Api.BackgroundServices;
using RelayForge.Application.Interfaces;
using RelayForge.Application.Security;
using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;

namespace RelayForge.Tests;

public class DeliveryProcessorTests
{
    [Theory]
    [InlineData(200, DeliveryStatus.Delivered)]
    [InlineData(302, DeliveryStatus.Failed)]
    [InlineData(400, DeliveryStatus.Failed)]
    [InlineData(429, DeliveryStatus.Pending)]
    [InlineData(503, DeliveryStatus.Pending)]
    public async Task Http_outcome_is_persisted(int status, DeliveryStatus expected)
    {
        var fixture = new DeliveryFixture { StatusCode = (HttpStatusCode)status };
        await fixture.ProcessAsync();
        Assert.Equal(expected, fixture.Delivery.Status);
        Assert.Equal(1, fixture.Delivery.AttemptCount);
        Assert.Equal(2, fixture.SaveCount);
        Assert.Equal(expected == DeliveryStatus.Pending, fixture.Delivery.NextAttemptAt.HasValue);
        Assert.Contains(fixture.Event!.Id.ToString(), fixture.Body);
    }

    [Fact]
    public async Task Retry_after_is_respected()
    {
        var fixture = new DeliveryFixture { StatusCode = HttpStatusCode.TooManyRequests, RetryAfter = TimeSpan.FromMinutes(2) };
        var before = DateTimeOffset.UtcNow;
        await fixture.ProcessAsync();
        Assert.True(fixture.Delivery.NextAttemptAt >= before.AddMinutes(2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Network_error_and_timeout_are_retried(bool timeout)
    {
        var fixture = new DeliveryFixture
        {
            SendError = timeout ? new TaskCanceledException() : new HttpRequestException()
        };
        await fixture.ProcessAsync();
        Assert.Equal(DeliveryStatus.Pending, fixture.Delivery.Status);
        Assert.Equal(1, fixture.Delivery.AttemptCount);
        Assert.NotNull(fixture.Delivery.NextAttemptAt);
    }

    [Fact]
    public async Task Shutdown_preserves_the_durable_reservation()
    {
        using var cancellation = new CancellationTokenSource();
        var fixture = new DeliveryFixture { BeforeSend = cancellation.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.ProcessAsync(cancellation.Token));
        Assert.Equal(1, fixture.SaveCount);
        Assert.Equal(1, fixture.Delivery.AttemptCount);
        Assert.Equal(DeliveryStatus.Pending, fixture.Delivery.Status);
        Assert.NotNull(fixture.Delivery.NextAttemptAt);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public async Task Persistence_failure_escapes_without_extra_send_or_failure_save(int failedSave, int sends)
    {
        var fixture = new DeliveryFixture { FailSaveNumber = failedSave };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ProcessAsync());
        Assert.Equal(sends, fixture.SendCount);
        Assert.Equal(failedSave, fixture.SaveCount);
    }

    [Fact]
    public async Task Exhausted_reservation_is_failed_without_a_fourth_request()
    {
        var fixture = new DeliveryFixture();
        for (int i = 0; i < Delivery.MaxAttempts; i++)
            fixture.Delivery.RegisterAttempt();
        await fixture.ProcessAsync();
        Assert.Equal(DeliveryStatus.Failed, fixture.Delivery.Status);
        Assert.Equal(3, fixture.Delivery.AttemptCount);
        Assert.Equal(0, fixture.SendCount);
    }

    [Theory]
    [InlineData("event")]
    [InlineData("endpoint")]
    [InlineData("inactive")]
    [InlineData("allowlist")]
    public async Task Missing_or_disallowed_dependency_is_terminal(string reason)
    {
        var fixture = new DeliveryFixture();
        if (reason == "event") fixture.Event = null;
        if (reason == "endpoint") fixture.Endpoint = null;
        if (reason == "inactive") fixture.Endpoint!.Deactivate();
        if (reason == "allowlist") fixture.Policy = new TrustedReceivers([]);
        await fixture.ProcessAsync();
        Assert.Equal(DeliveryStatus.Failed, fixture.Delivery.Status);
        Assert.Equal(0, fixture.SendCount);
        Assert.Equal(0, fixture.Delivery.AttemptCount);
    }
}

internal sealed class DeliveryFixture : HttpMessageHandler, IHttpClientFactory,
    IEndpointRepository, IWebhookEventRepository, IUnitOfWork
{
    public Endpoint? Endpoint { get; set; } = new("https://receiver.example/webhook");
    public WebhookEvent? Event { get; set; } = new("order.paid", "{\"orderId\":123}");
    public Delivery Delivery { get; }
    public TrustedReceivers Policy { get; set; } = new(["https://receiver.example/webhook"]);
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public TimeSpan? RetryAfter { get; set; }
    public Exception? SendError { get; set; }
    public Action? BeforeSend { get; set; }
    public int FailSaveNumber { get; set; }
    public int SaveCount { get; private set; }
    public int SendCount { get; private set; }
    public string? Body { get; private set; }

    public DeliveryFixture() => Delivery = new Delivery(Event!.Id, Endpoint!.Id);

    public Task ProcessAsync(CancellationToken token = default) =>
        new DeliveryProcessor(this, this, this, this, Policy, NullLogger<DeliveryProcessor>.Instance)
            .ProcessAsync(Delivery, token);

    public HttpClient CreateClient(string name)
    {
        Assert.Equal(DeliveryProcessor.ClientName, name);
        return new HttpClient(this, disposeHandler: false);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Assert.Equal(1, SaveCount);
        Assert.NotNull(Delivery.NextAttemptAt);
        SendCount++;
        Body = await request.Content!.ReadAsStringAsync(token);
        BeforeSend?.Invoke();
        token.ThrowIfCancellationRequested();
        if (SendError is not null) throw SendError;
        var response = new HttpResponseMessage(StatusCode);
        if (RetryAfter is not null) response.Headers.RetryAfter = new RetryConditionHeaderValue(RetryAfter.Value);
        return response;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveCount++;
        if (SaveCount == FailSaveNumber) throw new InvalidOperationException("Simulated database failure");
        return Task.CompletedTask;
    }

    public void Add(Endpoint endpoint, CancellationToken cancellationToken) => Endpoint = endpoint;
    public void Add(WebhookEvent webhookEvent) => Event = webhookEvent;
    Task<Endpoint?> IEndpointRepository.GetByIdAsync(Guid id, CancellationToken token) => Task.FromResult(Endpoint);
    Task<WebhookEvent?> IWebhookEventRepository.GetByIdAsync(Guid id, CancellationToken token) => Task.FromResult(Event);
    public Task<IEnumerable<Endpoint>> GetActiveAsync(CancellationToken token) =>
        Task.FromResult<IEnumerable<Endpoint>>(Endpoint is { IsActive: true } ? [Endpoint] : []);
}
