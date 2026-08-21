using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using Indtec.Labz.Live.Application.Shows;

namespace Indtec.Labz.Live.ScheduleShow;

public static class SqsEventMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static ScheduleShowsCommand Map(SQSEvent request)
    {
        var messages = request.Records.Select(record =>
        {
            var payload = JsonSerializer.Deserialize<ScheduleShowPayload>(record.Body, SerializerOptions)
                ?? throw new InvalidOperationException($"Message {record.MessageId} has an invalid body.");

            return new ScheduleShowMessage(
                record.MessageId,
                payload.ShowId,
                payload.Artist,
                payload.Venue,
                payload.StartsAt);
        }).ToArray();

        return new ScheduleShowsCommand(messages);
    }

    private sealed record ScheduleShowPayload(Guid ShowId, string Artist, string Venue, DateTimeOffset StartsAt);
}
