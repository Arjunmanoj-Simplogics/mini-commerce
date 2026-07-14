# Mini Commerce — Structured Logging

All APIs use `ILogger<T>` for application logging. There is **no** `Console.WriteLine` usage.

Serilog hosts the logging pipeline (`UseSerilog` + `Enrich.FromLogContext()` + console sink). HTTP request enrichment is shared in `MiniCommerce.BuildingBlocks.Logging`.

---

## Pipeline registration

Every API calls:

```csharp
app.UseMiniCommerceStructuredLogging();
```

This registers, in order:

1. **`CorrelationLoggingMiddleware`** — ambient LogContext properties
2. **`StructuredRequestLoggingMiddleware`** — `ILogger` request/response timing logs

`UseMiniCommerceCorrelationLogging()` remains as a backward-compatible alias.

---

## Structured fields

| Field | Source | Where it appears |
|-------|--------|------------------|
| **CorrelationId** | `X-Correlation-ID` header, or Activity / generated GUID | LogContext + every request log + problem responses |
| **RequestId** | `HttpContext.TraceIdentifier` | LogContext + every request log |
| **TraceId** | `Activity.Current.TraceId` (W3C) | LogContext + every request log + response `X-Trace-Id` |
| **SpanId** | `Activity.Current.SpanId` | LogContext + every request log + response `X-Span-Id` |
| **ExecutionTimeMs** | Stopwatch around the request | Completion and unhandled-exception logs |
| **Exception** | Exception type + Serilog/MEL exception detail | Error logs (`LogError(ex, ...)`) |

Also pushed to LogContext: `UserId`, `ServiceName`, `Timestamp`.

### Header

Clients may send:

```http
X-Correlation-ID: <guid-or-opaque-id>
```

The same value is echoed on the response.

---

## Example log lines

**Incoming**

```text
Incoming HTTP GET /api/orders CorrelationId=... RequestId=... TraceId=... SpanId=...
```

**Completed**

```text
Completed HTTP GET /api/orders with 200 in 42ms CorrelationId=... RequestId=... TraceId=... SpanId=...
```

**Unhandled exception** (when no downstream exception middleware handles it)

```text
Unhandled exception for HTTP POST /api/orders after 15ms CorrelationId=... RequestId=... TraceId=... SpanId=... Exception=InvalidOperationException
```

Message templates use named placeholders (`{CorrelationId}`, `{ExecutionTimeMs}`, …) so sinks (App Insights, Seq, JSON console) receive structured properties—not string-interpolated text.

---

## ILogger usage guidelines

| Do | Don't |
|----|--------|
| Inject `ILogger<T>` | `Console.WriteLine` / `Debug.WriteLine` |
| Use message templates: `"Order {OrderId} created"` | `$"Order {orderId} created"` |
| `LogError(ex, "…")` for failures | Swallow exceptions without logging |
| Rely on LogContext for ambient ids | Rebuild correlation ids manually in every call |

Bootstrap `Log.Fatal` / `Log.CloseAndFlush` in `Program.cs` is intentional for process lifetime only.

---

## Per-service notes

| Service | Exception handling | Notes |
|---------|-------------------|--------|
| Order | `GlobalExceptionMiddleware` | Also logs Exception + CorrelationId / RequestId / TraceId |
| Inventory | Inline exception middleware | Uses `ILogger` with the same structured fields |
| Others | Structured middleware catch | Logs Exception + ExecutionTimeMs then rethrows |

---

## Implementation files

| File | Role |
|------|------|
| `CorrelationLoggingMiddleware.cs` | LogContext: CorrelationId, RequestId, TraceId, SpanId |
| `StructuredRequestLoggingMiddleware.cs` | ILogger request timing + exception path |
| `StructuredLoggingExtensions.cs` | `UseMiniCommerceStructuredLogging()` |

Related: [`OBSERVABILITY.md`](OBSERVABILITY.md) (OpenTelemetry), [`CONFIGURATION.md`](CONFIGURATION.md), [`HEALTHCHECKS.md`](HEALTHCHECKS.md).
