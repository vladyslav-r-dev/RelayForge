using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using RelayForge.Api.BackgroundServices;
using RelayForge.Application.DTOs;
using RelayForge.Application.UseCases;
using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;
using RelayForge.Infrastructure;
using RelayForge.Infrastructure.Persistence;
using RelayForge.Infrastructure.Repositories;

namespace RelayForge.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RELAYFORGE_TEST_POSTGRES")))
            Skip = "Set RELAYFORGE_TEST_POSTGRES to a PostgreSQL connection with CREATEDB permission.";
    }
}

public class PostgresTests
{
    [PostgresFact]
    [Trait("Category", "Integration")]
    public async Task Migrations_due_query_atomic_event_creation_and_restart_recovery()
    {
        var databaseName = "relayforge_test_" + Guid.NewGuid().ToString("N");
        var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RELAYFORGE_TEST_POSTGRES"))
        {
            Pooling = false
        };
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            settings.Database = databaseName;
            var options = new DbContextOptionsBuilder<RelayForgeDbContext>().UseNpgsql(settings.ConnectionString).Options;
            Guid exhaustedId;
            Guid dueId;
            Guid immediateId;
            await using (var db = new RelayForgeDbContext(options))
            {
                await db.Database.MigrateAsync();
                Assert.False(db.Database.HasPendingModelChanges());
                var useCase = new CreateWebhookEventUseCase(new WebhookEventRepository(db),
                    new DeliveryRepository(db), new EndpointRepository(db), new UnitOfWork(db),
                    NullLogger<CreateWebhookEventUseCase>.Instance);
                var emptyEventId = await useCase.ExecuteAsync(
                    new CreateWebhookEventRequest { EventType = "empty", Data = "{}" }, default);
                Assert.NotNull(await db.WebhookEvents.FindAsync(emptyEventId));
                Assert.Empty(await db.Deliveries.ToListAsync());

                var endpoint = new Endpoint("https://receiver.example/webhook");
                db.Endpoints.Add(endpoint);
                await db.SaveChangesAsync();
                var eventId = await useCase.ExecuteAsync(
                    new CreateWebhookEventRequest { EventType = "order.paid", Data = "{}" }, default);
                var immediate = await db.Deliveries.SingleAsync(d => d.EventId == eventId);
                immediateId = immediate.Id;
                var due = new Delivery(eventId, endpoint.Id);
                due.RegisterAttempt();
                due.ScheduleNextAttempt(TimeSpan.Zero);
                dueId = due.Id;
                var future = new Delivery(eventId, endpoint.Id);
                future.ScheduleNextAttempt(TimeSpan.FromHours(1));
                var terminal = new Delivery(eventId, endpoint.Id);
                terminal.MarkAsFailed();
                var exhausted = new Delivery(eventId, endpoint.Id);
                for (int i = 0; i < Delivery.MaxAttempts; i++) exhausted.RegisterAttempt();
                exhausted.ScheduleNextAttempt(TimeSpan.Zero);
                exhaustedId = exhausted.Id;
                db.Deliveries.AddRange(due, future, terminal, exhausted);
                await db.SaveChangesAsync();
            }

            await using (var db = new RelayForgeDbContext(options))
            {
                var pending = (await new DeliveryRepository(db).GetPendingAsync(default)).ToList();
                Assert.Equal(new[] { immediateId, dueId, exhaustedId }.Order(), pending.Select(d => d.Id).Order());
                var fixture = new DeliveryFixture();
                var processor = new DeliveryProcessor(new EndpointRepository(db), new WebhookEventRepository(db),
                    new UnitOfWork(db), fixture, fixture.Policy, NullLogger<DeliveryProcessor>.Instance);
                await processor.ProcessAsync(pending.Single(d => d.Id == exhaustedId), default);
                Assert.Equal(0, fixture.SendCount);

                using var receiver = new TestReceiver(async () =>
                {
                    await using var observer = new RelayForgeDbContext(options);
                    var reservation = await observer.Deliveries.SingleAsync(d => d.Id == dueId);
                    Assert.Equal(2, reservation.AttemptCount);
                    Assert.Equal(DeliveryStatus.Pending, reservation.Status);
                    Assert.NotNull(reservation.NextAttemptAt);
                });
                var sender = new DeliveryProcessor(new EndpointRepository(db), new WebhookEventRepository(db),
                    new UnitOfWork(db), receiver, fixture.Policy, NullLogger<DeliveryProcessor>.Instance);
                await sender.ProcessAsync(pending.Single(d => d.Id == dueId), default);
            }
            await using (var db = new RelayForgeDbContext(options))
            {
                var recovered = await db.Deliveries.SingleAsync(d => d.Id == exhaustedId);
                Assert.Equal(DeliveryStatus.Failed, recovered.Status);
                Assert.Equal(3, recovered.AttemptCount);
                Assert.Null(recovered.NextAttemptAt);
                var delivered = await db.Deliveries.SingleAsync(d => d.Id == dueId);
                Assert.Equal(DeliveryStatus.Delivered, delivered.Status);
                Assert.Equal(2, delivered.AttemptCount);
                Assert.Null(delivered.NextAttemptAt);
            }

            await using (var db = new RelayForgeDbContext(options))
            {
                using var missingReceiver = new DeliveryFixture();
                var useCase = new CreateWebhookEventUseCase(new WebhookEventRepository(db),
                    new DeliveryRepository(db), missingReceiver, new UnitOfWork(db),
                    NullLogger<CreateWebhookEventUseCase>.Instance);
                await Assert.ThrowsAsync<DbUpdateException>(() => useCase.ExecuteAsync(
                    new CreateWebhookEventRequest { EventType = "must.rollback", Data = "{}" }, default));
            }
            await using (var db = new RelayForgeDbContext(options))
            {
                Assert.False(await db.WebhookEvents.AnyAsync(e => e.EventType == "must.rollback"));
            }
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class TestReceiver(Func<Task> checkReservation) : HttpMessageHandler, IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await checkReservation();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
