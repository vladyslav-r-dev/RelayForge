using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;
using Xunit;    

public class DeliveryTests
{
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