# Mini Commerce — Azure Service Bus

Azure Service Bus is encapsulated in **`MiniCommerce.Messaging`**. Application code depends on **`IMessagePublisher`** / **`IMessageHandler`** only — the Azure SDK stays inside the messaging library.

---

## Project layout

| File | Purpose |
|------|---------|
| `Options/ServiceBusOptions.cs` | Strongly typed `ServiceBus` configuration (auth, topic, subscription, retries) |
| `Abstractions/IMessagePublisher.cs` | Publish contract (event type + payload + correlation id) |
| `Abstractions/IMessageConsumer.cs` | Consume contract + `IMessageHandler` |
| `ServiceBusPublisher.cs` | Production publisher (`Azure.Messaging.ServiceBus`) |
| `NoOpMessagePublisher.cs` | No-op when `ServiceBus:Enabled=false` (local HTTP fallback) |
| `ServiceBusConsumer.cs` | Subscription processor: complete / dead-letter / correlation logging |
| `ServiceBusConsumerHostedService.cs` | `BackgroundService` host for the consumer |
| `ServiceBusConnectivity.cs` | Public helper for health checks |
| `Internal/ServiceBusClientFactory.cs` | Connection string **or** Managed Identity + exponential retry |
| `DependencyInjection/ServiceBusServiceRegistrar.cs` | Instance-based DI registration |

---

## Authentication

| Environment | Configuration |
|-------------|---------------|
| **Local / Development** | `ServiceBus:ConnectionString` |
| **Azure / Production** | `ServiceBus:FullyQualifiedNamespace` + **Managed Identity** (`DefaultAzureCredential` via [`AZURE-AUTH.md`](AZURE-AUTH.md)) |

Never set a connection string in production manifests.

---

## Events published

| Event | Publisher | When |
|-------|-----------|------|
| `OrderCreated` | Order → `IIntegrationEventPublisher` → `IMessagePublisher` | After order create |
| `OrderStatusChanged` | Order | Status change (non-cancel) |
| `OrderCancelled` | Order | Cancel / delete |
| `PaymentCompleted` | Payment API | Successful mock charge |
| `InventoryReserved` | Inventory API `/reserve` | Stock reserved |
| `InventoryFailed` | Inventory API `/reserve` | Insufficient stock |

Contracts live in `MiniCommerce.Contracts` (`OrderEvents.cs` + related types). Wire names: `ServiceBusNames.*`.

---

## Retry & dead-letter

- **Exponential backoff** via `ServiceBusClientOptions.RetryOptions` (`MaxRetryCount`, `RetryDelaySeconds`, `MaxRetryDelaySeconds`).
- **Dead-letter** on unknown event types or handler exceptions (`DeadLetterMessageAsync` with reason + description).
- Processor uses `AutoCompleteMessages = false` so only successful handlers complete the message.

---

## Correlation & logging

Every published message sets:

- `CorrelationId` (message property + application property)
- `MessageId`
- `Subject` / `EventType`

Structured `ILogger` fields include `EventType`, `MessageId`, `CorrelationId`, and `Exception` on failures.

---

## Registration

```csharp
// Publisher only (Order, Inventory, Payment)
new ServiceBusServiceRegistrar().Register(services, configuration, registerConsumer: false);

// Publisher + BackgroundService consumer (Notification)
new ServiceBusServiceRegistrar().Register(services, configuration, registerConsumer: true);
services.AddScoped<IMessageHandler, OrderEventsMessageHandler>();
```

When `ServiceBus:Enabled=false`:

- `IMessagePublisher` → `NoOpMessagePublisher`
- Order still uses **HTTP** `/api/events/*` via `NotificationHttpPublisher`
- No consumer host starts

---

## Configuration (`ServiceBus` section)

| Property | Default | Env variable |
|----------|---------|--------------|
| `Enabled` | `false` | `ServiceBus__Enabled` |
| `ConnectionString` | `""` | `ServiceBus__ConnectionString` |
| `FullyQualifiedNamespace` | — | `ServiceBus__FullyQualifiedNamespace` |
| `TopicName` | `orders` | `ServiceBus__TopicName` |
| `SubscriptionName` | `notification-service` | `ServiceBus__SubscriptionName` |
| `MaxRetryCount` | `5` | `ServiceBus__MaxRetryCount` |
| `RetryDelaySeconds` | `1` | `ServiceBus__RetryDelaySeconds` |
| `MaxRetryDelaySeconds` | `30` | `ServiceBus__MaxRetryDelaySeconds` |
| `MaxConcurrentCalls` | `4` | `ServiceBus__MaxConcurrentCalls` |
| `MaxAutoLockRenewalMinutes` | `5` | `ServiceBus__MaxAutoLockRenewalMinutes` |

---

## Architecture (unchanged)

```
Controller → Service → Repository
                ↓
         IIntegrationEventPublisher / IMessagePublisher  (side effects only)
```

HTTP APIs and business outcomes are unchanged. Messaging failures are logged and do not fail primary flows.

---

## Related

- [`CONFIGURATION.md`](CONFIGURATION.md)
- [`HEALTHCHECKS.md`](HEALTHCHECKS.md) — `servicebus` readiness check when enabled
- [`LOGGING.md`](LOGGING.md)
