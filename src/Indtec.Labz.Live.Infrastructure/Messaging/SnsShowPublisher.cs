using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Indtec.Labz.Live.Application.Abstractions;
using Indtec.Labz.Live.Application.Shows;

namespace Indtec.Labz.Live.Infrastructure.Messaging;

public sealed class SnsShowPublisher(
    IAmazonSimpleNotificationService sns,
    string topicArn) : IShowPublisher
{
    public Task PublishScheduledAsync(ShowScheduled integrationEvent, CancellationToken cancellationToken)
        => sns.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Subject = nameof(ShowScheduled),
            Message = JsonSerializer.Serialize(integrationEvent),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new() { DataType = "String", StringValue = nameof(ShowScheduled) }
            }
        }, cancellationToken);
}