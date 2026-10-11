using RelayForge.Domain.Entities;
using Xunit;

namespace RelayForge.Domain.Tests;

public class WebhookEventTests
{
    [Fact]
    public void WebhookEvent_Should_Be_Created()
    {
    var webhookEvent = new WebhookEvent(
        "order.paid",
        "{\"orderId\":123}"
    );

    Assert.Equal("order.paid", webhookEvent.EventType);
    Assert.Equal("{\"orderId\":123}", webhookEvent.Data);
    }

    [Fact]
    public void WebhookEvent_Should_Throw_When_Data_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new WebhookEvent("order.paid", " ");
        });
    }

    [Fact]
    public void WebhookEvent_Should_Throw_When_EventType_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new WebhookEvent(" ", "{\"orderId\":123}");
        });
    }
}