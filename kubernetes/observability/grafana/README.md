# Grafana Helm & Dashboard Management

## Add & Update Helm Repository (in WSL)
```bash
wsl helm repo add grafana-community https://grafana-community.github.io/helm-charts
wsl helm repo update
```

## Deploy / Upgrade Grafana (in WSL)
```bash
wsl helm upgrade --install grafana grafana-community/grafana -n observability -f /mnt/c/Repositories/Portfolio/kubernetes/observability/grafana/helm-values.yml --version 12.9.0
```

## Apply Custom Dashboards (Sidecar Pattern)
Custom dashboards in `dashboards/` use the `grafana_dashboard: "1"` label and `grafana_folder` annotation, and are hot-reloaded automatically by the `grafana-sc-dashboard` sidecar container without restarting Grafana:
```bash
kubectl apply -f kubernetes/observability/grafana/dashboards/
```
