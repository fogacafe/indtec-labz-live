namespace Indtec.Labz.Live.Application.Shows;

public sealed record ShowScheduled(
    Guid ShowId,
    string Artist,
    string Venue,
    DateTimeOffset StartsAt,
    DateTimeOffset OccurredAt);