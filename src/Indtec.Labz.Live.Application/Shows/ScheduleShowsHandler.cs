using Indtec.Labz.Live.Application.Abstractions;

namespace Indtec.Labz.Live.Application.Shows;

public sealed class ScheduleShowsHandler(
    IIdempotencyStore idempotencyStore,
    IShowPublisher publisher,
    TimeProvider clock)
{
    public async Task<IReadOnlyCollection<string>> HandleAsync(
        ScheduleShowsCommand command,
        CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        foreach (var message in command.Messages)
        {
            try
            {
                var started = await idempotencyStore.TryStartAsync(message.MessageId, cancellationToken);
                if (!started) continue;

                var integrationEvent = new ShowScheduled(
                    message.ShowId,
                    message.Artist,
                    message.Venue,
                    message.StartsAt,
                    clock.GetUtcNow());

                await publisher.PublishScheduledAsync(integrationEvent, cancellationToken);
                await idempotencyStore.CompleteAsync(message.MessageId, cancellationToken);
            }
            catch
            {
                failures.Add(message.MessageId);
            }
        }

        return failures;
    }
}