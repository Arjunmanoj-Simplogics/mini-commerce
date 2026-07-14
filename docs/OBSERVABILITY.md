# Mini Commerce — OpenTelemetry Observability

Production-ready OpenTelemetry for **distributed tracing**, **metrics**, and **structured logs**, with **CorrelationId**, **TraceId**, and **SpanId** on every request.

Registration (already in every API):

```csharp
builder.Services.AddMiniCommerceTelemetry(builder.Configuration);
app.UseMiniCommerceStructuredLogging();
```

Business logic, controllers, repositories, and APIs are unchanged.

---

## Signals

| Signal | What is collected |
|--------|-------------------|
| **Traces** | ASP.NET Core requests, HttpClient, EF Core, SqlClient (Azure SQL), Azure Service Bus, Azure Blob Storage |
| **Metrics** | ASP.NET Core, HttpClient, .NET runtime |
| **Logs** | Serilog structured logs + optional OTLP / Console log export (includes TraceId / SpanId via LogContext) |

---

## Correlation fields

| Field | Source |
|-------|--------|
| **CorrelationId** | `X-Correlation-ID` (request header) or generated; tagged on the Activity |
| **RequestId** | `HttpContext.TraceIdentifier` |
| **TraceId** | `Activity.Current.TraceId` (W3C) |
| **SpanId** | `Activity.Current.SpanId` |

Response headers: `X-Correlation-ID`, `X-Trace-Id`, `X-Span-Id`.

---

## Instrumentation

| Stack | How |
|-------|-----|
| ASP.NET Core | `AddAspNetCoreInstrumentation` (health paths filtered by default) |
| HttpClient | `AddHttpClientInstrumentation` |
| EF Core | `AddEntityFrameworkCoreInstrumentation` |
| Azure SQL / SqlClient | `AddSqlClientInstrumentation` |
| Azure Service Bus | Azure SDK `ActivitySource` + `MiniCommerce.Messaging` spans on publish/process |
| Azure Blob Storage | Azure SDK `ActivitySource` + `MiniCommerce.Storage` spans on upload/download/delete |

Azure SDK spans require `Azure.Experimental.EnableActivitySource` (set automatically at startup).

---

## Exporters

Configured under `OpenTelemetry:Exporters`:

| Exporter | Dev default | Prod default | Notes |
|----------|-------------|--------------|-------|
| **Console** | `true` | `false` | Local debugging |
| **OTLP** | `false` | `false` until endpoint set | Jaeger / Collector / Grafana Alloy |
| **Azure Monitor** | `true` when `ApplicationInsights:ConnectionString` is set | same | Optional Application Insights |

### Example (`appsettings.json`)

```json
"OpenTelemetry": {
  "Enabled": true,
  "ServiceName": "OrderService",
  "OtlpEndpoint": "http://localhost:4317",
  "OtlpProtocol": "Grpc",
  "ExcludeHealthChecks": true,
  "CaptureSqlText": false,
  "Exporters": {
    "Console": true,
    "Otlp": false,
    "AzureMonitor": true
  }
},
"ApplicationInsights": {
  "ConnectionString": ""
}
```

Enable OTLP:

```bash
OpenTelemetry__Exporters__Otlp=true
OpenTelemetry__OtlpEndpoint=http://otel-collector:4317
```

Enable Azure Monitor:

```bash
ApplicationInsights__ConnectionString="InstrumentationKey=...;IngestionEndpoint=..."
```

---

## Configuration reference

| Property | Env | Description |
|----------|-----|-------------|
| `Enabled` | `OpenTelemetry__Enabled` | Master switch for Console/OTLP + instrumentations |
| `ServiceName` | `OpenTelemetry__ServiceName` | Resource `service.name` |
| `ServiceVersion` | `OpenTelemetry__ServiceVersion` | Resource `service.version` |
| `OtlpEndpoint` | `OpenTelemetry__OtlpEndpoint` | Collector URL |
| `OtlpProtocol` | `OpenTelemetry__OtlpProtocol` | `Grpc` or `HttpProtobuf` |
| `ExcludeHealthChecks` | `OpenTelemetry__ExcludeHealthChecks` | Skip `/health*` spans |
| `CaptureSqlText` | `OpenTelemetry__CaptureSqlText` | Include SQL text in spans (Dev only recommended) |
| `Exporters:Console` | `OpenTelemetry__Exporters__Console` | Console exporter |
| `Exporters:Otlp` | `OpenTelemetry__Exporters__Otlp` | OTLP exporter |
| `Exporters:AzureMonitor` | `OpenTelemetry__Exporters__AzureMonitor` | Azure Monitor when CS present |

---

## Implementation files

| File | Role |
|------|------|
| `Observability/TelemetryExtensions.cs` | `AddMiniCommerceTelemetry` |
| `Observability/MiniCommerceTelemetry.cs` | ActivitySource / Meter names |
| `Configuration/OpenTelemetryOptions.cs` | Strongly typed config |
| `Logging/CorrelationLoggingMiddleware.cs` | CorrelationId + TraceId + SpanId |
| `Messaging/Internal/MessagingTelemetry.cs` | Service Bus ActivitySource |
| `Storage/Internal/StorageTelemetry.cs` | Blob ActivitySource |

Related: [`LOGGING.md`](LOGGING.md), [`CONFIGURATION.md`](CONFIGURATION.md), [`HEALTHCHECKS.md`](HEALTHCHECKS.md).
