using Amazon.Lambda.SQSEvents;
using Indtec.Labz.Live.ScheduleShow;

namespace Indtec.Labz.Live.UnitTests;

public sealed class SqsEventMapperTests
{
    [Fact]
    public void Maps_sqs_record_into_schedule_show_command()
    {
        var showId = Guid.NewGuid();
        var startsAt = new DateTimeOffset(2026, 8, 21, 23, 0, 0, TimeSpan.FromHours(-3));
        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage
                {
                    MessageId = "message-001",
                    Body = $$"""
                    {
                      "showId": "{{showId}}",
                      "artist": "Foo Fighters",
                      "venue": "Allianz Parque",
                      "startsAt": "{{startsAt:O}}"
                    }
                    """
                }
            ]
        };

        var command = SqsEventMapper.Map(sqsEvent);

        var message = Assert.Single(command.Messages);
        Assert.Equal("message-001", message.MessageId);
        Assert.Equal(showId, message.ShowId);
        Assert.Equal("Foo Fighters", message.Artist);
        Assert.Equal("Allianz Parque", message.Venue);
        Assert.Equal(startsAt, message.StartsAt);
    }
}
