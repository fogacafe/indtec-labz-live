using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Indtec.Labz.Live.Application.Abstractions;
using Indtec.Labz.Live.Application.Shows;
using Indtec.Labz.Live.Infrastructure.Idempotency;
using Indtec.Labz.Live.Infrastructure.Messaging;
using Indtec.Labz.Live.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Indtec.Labz.Live.ScheduleShow;

public sealed class Function : BaseLambda<SQSEvent, ScheduleShowsCommand, SQSBatchResponse>
{
    private static readonly IServiceProvider RootProvider = LambdaBootstrapper.Build(services =>
    {
        var tableName = Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE")
            ?? throw new InvalidOperationException("IDEMPOTENCY_TABLE is required.");
        var topicArn = Environment.GetEnvironmentVariable("SHOW_SCHEDULED_TOPIC_ARN")
            ?? throw new InvalidOperationException("SHOW_SCHEDULED_TOPIC_ARN is required.");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddSingleton<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>();
        services.AddSingleton<IIdempotencyStore>(sp => new DynamoDbIdempotencyStore(sp.GetRequiredService<IAmazonDynamoDB>(), tableName));
        services.AddSingleton<IShowPublisher>(sp => new SnsShowPublisher(sp.GetRequiredService<IAmazonSimpleNotificationService>(), topicArn));
        services.AddScoped<ScheduleShowsHandler>();
    });

    protected override IServiceProvider Services => RootProvider;

    protected override ScheduleShowsCommand Map(SQSEvent request, ILambdaContext context)
        => SqsEventMapper.Map(request);

    protected override async Task<SQSBatchResponse> ExecuteAsync(
        IServiceProvider services,
        ScheduleShowsCommand command,
        ILambdaContext context)
    {
        var handler = services.GetRequiredService<ScheduleShowsHandler>();
        var failures = await handler.HandleAsync(command, CancellationToken.None);

        return new SQSBatchResponse
        {
            BatchItemFailures = failures.Select(messageId => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = messageId }).ToList()
        };
    }
}
