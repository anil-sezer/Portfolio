helm repo add grafana-community https://grafana-community.github.io/helm-charts
helm repo update


helm upgrade --install grafana grafana-community/grafana -n observability -f helm-values.yml

helm install grafana grafana-community/grafana -n observability -f helm-values.yml
