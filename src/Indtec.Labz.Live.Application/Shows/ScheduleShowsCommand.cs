namespace Indtec.Labz.Live.Application.Shows;

public sealed record ScheduleShowsCommand(IReadOnlyCollection<ScheduleShowMessage> Messages);

public sealed record ScheduleShowMessage(
    string MessageId,
    Guid ShowId,
    string Artist,
    string Venue,
    DateTimeOffset StartsAt);