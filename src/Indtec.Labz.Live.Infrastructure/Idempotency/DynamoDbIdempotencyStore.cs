using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Indtec.Labz.Live.Application.Abstractions;

namespace Indtec.Labz.Live.Infrastructure.Idempotency;

public sealed class DynamoDbIdempotencyStore(
    IAmazonDynamoDB dynamoDb,
    string tableName) : IIdempotencyStore
{
    public async Task<bool> TryStartAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["pk"] = new() { S = key },
                    ["status"] = new() { S = "PROCESSING" }
                },
                ConditionExpression = "attribute_not_exists(pk)"
            }, cancellationToken);

            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public Task CompleteAsync(string key, CancellationToken cancellationToken)
        => dynamoDb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue> { ["pk"] = new() { S = key } },
            UpdateExpression = "SET #status = :completed",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "status" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":completed"] = new() { S = "COMPLETED" } }
        }, cancellationToken);

    public Task AbandonAsync(string key, CancellationToken cancellationToken)
        => dynamoDb.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue> { ["pk"] = new() { S = key } }
        }, cancellationToken);
}