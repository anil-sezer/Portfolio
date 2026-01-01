helm install prometheus prometheus-community/prometheus \
-f kubernetes/observability/prometheus/helm-values.yml \
-n observability

helm uninstall prometheus -n observability