# Alertmanager Setup

Alertmanager is deployed as a subchart of the Prometheus Helm release (`prometheus-community/prometheus`) in the `observability` namespace.

## Storage & State
Alertmanager runs statelessly with `persistence.enabled: false` (`emptyDir`). All routing rules, inhibit rules, alerting rules, and notification receivers are declared as code in `kubernetes/observability/prometheus/helm-values.yml`.

## Secret
Telegram credentials must be provided as a Kubernetes secret:
```bash
kubectl apply -f kubernetes/observability/alertmanager/secret.example.yml
```
Or create it manually:
```bash
kubectl create secret generic alertmanager-telegram-secret \
  -n observability \
  --from-literal=bot_token="<YOUR_TELEGRAM_BOT_TOKEN>" \
  --from-literal=chat_id="<YOUR_TELEGRAM_CHAT_ID>"
```

## Manifests in this Directory
- `secret.example.yml`: Example Secret manifest for Telegram bot token & chat ID.
- `ingress.yml`: Exposes Alertmanager Web UI at `https://alertmanager.anilsezer.net` with Let's Encrypt TLS and Authentik Forward-Auth SSO.
