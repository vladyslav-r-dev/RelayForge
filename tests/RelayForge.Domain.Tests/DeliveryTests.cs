using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;
using Xunit;

namespace RelayForge.Domain.Tests;

public class DeliveryTests
{
    [Fact]
    public void Attempts_have_bounded_exponential_backoff_and_terminal_states_cannot_change()
    {
        var delivery = new Delivery(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(delivery.MarkAsDelivered);
        foreach (var seconds in new[] { 5, 10, 20 })
        {
            delivery.RegisterAttempt();
            Assert.Equal(TimeSpan.FromSeconds(seconds), delivery.GetRetryDelay());
            Assert.NotNull(delivery.NextAttemptAt);
        }
        Assert.Throws<InvalidOperationException>(delivery.RegisterAttempt);
        delivery.RegisterFailure(Delivery.MaxAttempts);
        Assert.Null(delivery.NextAttemptAt);
        Assert.Throws<InvalidOperationException>(delivery.MarkAsDelivered);
        Assert.Throws<InvalidOperationException>(() => delivery.ScheduleNextAttempt(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_EventId_Is_Empty()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Delivery(Guid.Empty, Guid.NewGuid()));
        Assert.Equal("Event ID cannot be empty. (Parameter 'eventId')", exception.Message);
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_EndpointId_Is_Empty()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Delivery(Guid.NewGuid(), Guid.Empty));
        Assert.Equal("Endpoint ID cannot be empty. (Parameter 'endpointId')", exception.Message);
    }

    [Fact]
    public void Delivery_Should_Be_Marked_As_Delivered()
    {
        var delivery = new Delivery(Guid.NewGuid(), Guid.NewGuid());
        delivery.RegisterAttempt();
        delivery.MarkAsDelivered();

        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(1, delivery.AttemptCount);
    }

    [Fact]
    public void Delivery_Should_Fail_After_Max_Attempts()
    {
        var delivery = new Delivery(Guid.NewGuid(), Guid.NewGuid());
        int maxAttempts = 3;

        for (int i = 0; i < maxAttempts; i++)
        {
            delivery.RegisterAttempt();
            delivery.RegisterFailure(maxAttempts);
        }

        Assert.Equal(DeliveryStatus.Failed, delivery.Status);
        Assert.Equal(maxAttempts, delivery.AttemptCount);
    }
}
