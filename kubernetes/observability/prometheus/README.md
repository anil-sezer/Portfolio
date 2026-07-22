helm install prometheus prometheus-community/prometheus \
-f kubernetes/observability/prometheus/helm-values.yml \
-n observability

helm uninstall prometheus -n observability

NOTES:
The Prometheus server can be accessed via port 80 on the following DNS name from within your cluster:
prometheus.observability.svc.cluster.local
