# INDTEC LABZ / 002 — Live

Event-driven .NET lab exploring AWS Lambda, SQS/SNS, idempotency, resilience and reusable serverless patterns.

`STATUS / FOUNDATION` · `.NET 10` · `AWS Lambda` · `SQS` · `SNS` · `DynamoDB` · `CloudFormation`

## Mission

Schedule live shows through an asynchronous pipeline where duplicate delivery, partial failures and warm Lambda execution are treated as normal runtime conditions instead of edge cases.

```text
Producer
   ↓
  SQS ───────────────→ DLQ
   ↓
ScheduleShow Lambda
   ├── DynamoDB / idempotency
   └── SNS / ShowScheduled
```

## Reusable Lambda runtime

The Lambda itself stays thin. Runtime concerns live behind a reusable execution boundary:

```text
BaseLambda<TRequest, TCommand, TResponse>
   ├── maps AWS request → application command
   ├── creates one DI scope per invocation
   ├── centralizes unhandled exception logging
   └── executes application behavior
```

Each concrete Lambda owns a static root `IServiceProvider`. The container is built once per execution environment and reused by warm invocations, while scoped dependencies are recreated and disposed for each invocation.

```text
cold start
  ↓
static DI container created
  ↓
invocation scope

warm invocation
  ↓
reuse static container
  ↓
new invocation scope
```

This reuses composition work without leaking scoped state between invocations.

## SQS partial batch response

A batch is not treated as all-or-nothing. `ScheduleShowsHandler` returns only failed message IDs and the Lambda maps them to `SQSBatchResponse`. CloudFormation enables `ReportBatchItemFailures`, so successful records are not unnecessarily retried.

## Idempotency

Before publishing `ShowScheduled`, the application reserves the SQS message ID in DynamoDB using a conditional write. Duplicate deliveries become no-ops. Failed processing releases the reservation so a later SQS retry can attempt the message again.

## Retry philosophy

The lab deliberately avoids a generic `retry everything` policy inside `BaseLambda`.

- domain/validation failures do not become valid after retry;
- SQS/Lambda owns delivery retry and DLQ/redrive semantics;
- transient retries belong around the dependency that can actually recover;
- partial batch responses prevent successful messages from being replayed with failed ones.

## Infrastructure as code

`infra/cloudformation/live.yml` provisions the runtime boundary used by the code:

- ScheduleShow SQS queue;
- dead-letter queue with configurable `maxReceiveCount`;
- Lambda event source mapping with partial batch failure reporting;
- DynamoDB idempotency table with TTL;
- `ShowScheduled` SNS topic;
- Lambda execution role with least-purpose permissions;
- Lambda environment variables for table/topic discovery.

The template receives the packaged Lambda artifact through `LambdaCodeS3Bucket` and `LambdaCodeS3Key`, keeping build/package concerns separate from the stack definition.

Example deployment after uploading the Lambda zip to S3:

```bash
aws cloudformation deploy \
  --template-file infra/cloudformation/live.yml \
  --stack-name indtec-labz-live-dev \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides \
    EnvironmentName=dev \
    LambdaCodeS3Bucket=<artifact-bucket> \
    LambdaCodeS3Key=<schedule-show.zip>
```

CI runs `cfn-lint` against the template in addition to the .NET build and tests.

## Runtime contract test

`SqsEventMapperTests` exercises an AWS-shaped `SQSEvent` body and verifies the exact mapping into the application command. This keeps AWS serialization/request shape at the edge instead of leaking it into the application layer.

## Projects

```text
src/
├── Indtec.Labz.Live.Domain
├── Indtec.Labz.Live.Application
├── Indtec.Labz.Live.Infrastructure
├── Indtec.Labz.Live.Lambda.Core
└── Indtec.Labz.Live.ScheduleShow

infra/
└── cloudformation/live.yml

tests/
└── Indtec.Labz.Live.UnitTests
```

## Current slice

`SQSEvent → ScheduleShowsCommand → idempotency → SNS → partial batch response`

## Intentionally deferred

Distributed tracing / OpenTelemetry is intentionally left for a dedicated LABZ where correlation across multiple components provides real value. The same applies to adding more queues, consumers or retry libraries without a concrete failure mode that justifies them.

---

**INDTEC LABZ** keeps each repository focused on a concrete engineering problem. Patterns earn their place by solving something observable.
