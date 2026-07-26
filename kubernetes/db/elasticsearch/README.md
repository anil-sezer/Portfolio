# Elasticsearch & Kibana Deployment

## Step 1: Install ECK Operator via Helm (in WSL)

```bash
helm repo add elastic https://helm.elastic.co
helm repo update
helm install eck-operator elastic/eck-operator -n database -f helm-values.yml
```

## Step 2: Apply Custom Resources & Ingress (via kubectl)

```bash
kubectl apply -f pv-nfs.yml
kubectl apply -f cr-elastic.yml
kubectl apply -f cr-kibana.yml
kubectl apply -f ingress.yml
```

## Step 3: Get elastic Superuser Password
(username is elastic)
```powershell
kubectl get secret elasticsearch-es-elastic-user -n database -o jsonpath="{.data.elastic}" | [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_))
```