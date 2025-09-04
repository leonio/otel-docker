# (very... very...) Basic OTEL setup with Docker
The most basic, vanilla setup with docker-compose for an OTEL stack consisting of prom, grafana, loki and tempo.

There are so many great examples online, however some of them are using older versions and some aren't taking full advantage of the otel/otelhttp exporters (or even exporting directly to prom/jaeger/tempo instead of taking advantage of the otel-collector).

This is a quick and dirty attempt to see what's involved in setting up a local config that is sorta, kinda close to what one could expect with a vanilla setup in mid-2025.

## 0. Docker Compose Setup
bring the stack up:
```pwsh
cd src
docker-compose up -d
```

tear the stack down:
```pwsh
docker-compose down -v
```

services available at:
- Grafana: http://localhost:3000
- Prometheus: http://localhost:9090
- Loki: http://localhost:3100
- Tempo: http://localhost:3200
- OpenTelemetry Collector: http://localhost:4318 (OTLP/HTTP)

## 1. OpenTelemetry Collector
OTEL collector configuration can be found at `config-otel-collector.yaml` and acts as the central gathering point for all telemetry data.

- Receives data via OTLP/HTTP on port 4318
- Exports metrics to Prometheus
- Exports logs to Loki
- Exports traces to Tempo

Three pipelines have been configured
- Metrics: OTLP → Prometheus
- Logs: OTLP → Loki
- Traces: OTLP → Tempo

## 2. Prometheus
Prometheus is configured to scrape metrics from the OTEL collector.

- UI is accessible at http://localhost:9090
- Grafana can display metrics at http://localhost:3000
  1. Navigate to Explore > Prometheus 
  2. Query metrics to your hearts content (using [PromQL](https://prometheus.io/docs/prometheus/latest/querying/basics/))

## 3. Loki
- Logs can be viewed using Grafana
  1. Go to http://localhost:3000
  2. Navigate to Explore > Loki
  3. Query logs to your hearts content (using [LogQL](https://grafana.com/docs/loki/latest/query/))

## 4. Tempo
- Traces can be viewed using Grafana
  1. Go to http://localhost:3000
  2. Navigate to Explore > Tempo
  3. Query traces by `TraceID` or use the search interface with [TraceQL](https://grafana.com/docs/tempo/latest/traceql/construct-traceql-queries/)

## 5. OtelConsoleApp (sample)
- A small sample .NET console application that generates synthetic traces, metrics and logs to demonstrate the stack
- By default the sample pushes telemetry to the local OpenTelemetry Collector over gRPC (OTLP/gRPC default for OTEL libraries in dotnet)
- There are debug toggles (commented out) in the sample that you can enable to print verbose telemetry or enable an internal debug exporter

You an use this sample to generate telemetry to test linking between logs, traces and metrics in Grafana.
