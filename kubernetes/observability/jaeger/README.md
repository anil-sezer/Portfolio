Got the helm-values file from here:
https://github.com/jaegertracing/helm-charts/tree/v2

helm install jaeger jaegertracing/jaeger -f helm-values.yml -n observability