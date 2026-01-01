helm install otel-collector open-telemetry/opentelemetry-collector \
-f helm-values.yml \
-n observability

helm upgrade otel-collector open-telemetry/opentelemetry-collector \
-f helm-values.yml \
-n observability

helm uninstall otel-collector -n observability