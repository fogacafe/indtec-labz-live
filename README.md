# INDTEC LABZ / 002 — Live

Event-driven .NET lab exploring AWS Lambda, SQS/SNS, idempotency, resilience and reusable serverless patterns.

`STATUS / FOUNDATION` · `.NET 10` · `AWS Lambda` · `SQS` · `SNS` · `DynamoDB`

## Mission

Schedule live shows through an asynchronous pipeline where duplicate delivery, partial failures and warm Lambda execution are treated as normal runtime conditions instead of edge cases.

```text
Producer
   ↓
  SQS
   ↓
ScheduleShow Lambda
   ├── DynamoDB / idempotency
   └── SNS / ShowScheduled
```

## The interesting part

The Lambda itself should remain thin. Runtime concerns are pushed into a reusable execution boundary:

```text
BaseLambda<TRequest, TCommand, TResponse>
   ├── maps AWS request → application command
   ├── creates one DI scope per invocation
   ├── centralizes unhandled exception logging
   └── executes application behavior
```

Each concrete Lambda owns a static root `IServiceProvider`. That container is built once per execution environment and can be reused by warm invocations, while scoped dependencies are still recreated and disposed for every invocation.

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

This is deliberate: reuse expensive composition work without leaking scoped state between messages.

## SQS partial batch response

A batch is not treated as all-or-nothing. `ScheduleShowsHandler` returns only the message IDs that failed, and the Lambda converts them into `SQSBatchResponse` failures. Successful records are not unnecessarily processed again.

## Idempotency

Before publishing `ShowScheduled`, the application reserves the SQS message ID in DynamoDB using a conditional write. Duplicate deliveries therefore become no-ops. A failed processing attempt releases its reservation so the SQS retry can try again.

## Retry philosophy

The lab deliberately avoids a generic `retry everything` policy inside `BaseLambda`.

- domain/validation failures should not magically become valid after retry;
- SQS/Lambda already provides delivery retry and DLQ/redrive semantics;
- retry for transient outbound dependencies belongs around that specific dependency, not around the entire business operation.

## Projects

```text
src/
├── Indtec.Labz.Live.Domain
├── Indtec.Labz.Live.Application
├── Indtec.Labz.Live.Infrastructure
├── Indtec.Labz.Live.Lambda.Core
└── Indtec.Labz.Live.ScheduleShow

tests/
└── Indtec.Labz.Live.UnitTests
```

## Current slice

`SQSEvent → ScheduleShowsCommand → idempotency → SNS → partial batch response`

## Next decisions

The next increments should add infrastructure-as-code, DLQ/redrive configuration and transient dependency resilience. Distributed tracing is intentionally deferred until this lab has enough cross-component behavior to make trace propagation meaningful.

---

**INDTEC LABZ** keeps each repository focused on a concrete engineering problem. Patterns earn their place by solving something observable.