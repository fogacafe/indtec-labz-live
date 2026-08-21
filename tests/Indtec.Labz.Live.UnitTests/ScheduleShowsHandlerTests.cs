using Indtec.Labz.Live.Application.Abstractions;
using Indtec.Labz.Live.Application.Shows;

namespace Indtec.Labz.Live.UnitTests;

public sealed class ScheduleShowsHandlerTests
{
    [Fact]
    public async Task Duplicate_message_is_not_published_again()
    {
        var idempotency = new FakeIdempotencyStore(alreadyProcessed: true);
        var publisher = new FakePublisher();
        var handler = new ScheduleShowsHandler(idempotency, publisher, TimeProvider.System);
        var command = new ScheduleShowsCommand([
            new ScheduleShowMessage("m-1", Guid.NewGuid(), "Paramore", "Arena", DateTimeOffset.UtcNow.AddDays(1))
        ]);

        var failures = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Empty(failures);
        Assert.Empty(publisher.Events);
    }

    [Fact]
    public async Task Failed_publication_is_returned_for_sqs_partial_batch_retry()
    {
        var idempotency = new FakeIdempotencyStore();
        var publisher = new FakePublisher(shouldFail: true);
        var handler = new ScheduleShowsHandler(idempotency, publisher, TimeProvider.System);
        var command = new ScheduleShowsCommand([
            new ScheduleShowMessage("m-2", Guid.NewGuid(), "Foo Fighters", "Stadium", DateTimeOffset.UtcNow.AddDays(1))
        ]);

        var failures = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(["m-2"], failures);
        Assert.Contains("m-2", idempotency.Abandoned);
    }

    private sealed class FakeIdempotencyStore(bool alreadyProcessed = false) : IIdempotencyStore
    {
        public List<string> Abandoned { get; } = [];
        public Task<bool> TryStartAsync(string key, CancellationToken cancellationToken) => Task.FromResult(!alreadyProcessed);
        public Task CompleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AbandonAsync(string key, CancellationToken cancellationToken) { Abandoned.Add(key); return Task.CompletedTask; }
    }

    private sealed class FakePublisher(bool shouldFail = false) : IShowPublisher
    {
        public List<ShowScheduled> Events { get; } = [];
        public Task PublishScheduledAsync(ShowScheduled integrationEvent, CancellationToken cancellationToken)
        {
            if (shouldFail) throw new HttpRequestException("Transient downstream failure.");
            Events.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}