# Kube-Bench CIS Security Benchmark Audits

This directory contains declarative Kubernetes manifests for running [Aqua Security kube-bench](https://github.com/aquasecurity/kube-bench) to audit cluster security compliance against the **CIS Kubernetes Benchmark**.

Both CronJobs are configured to automatically send a formatted summary report and top failures directly to **Telegram** upon completion.

---

## 📁 Manifests

| File | Type | Target | Schedule | Description |
| :--- | :--- | :--- | :--- | :--- |
| `cronjob-kube-bench-master.yml` | `CronJob` | Control Plane (`pi4-red`) | Every Sunday at 03:00 UTC | Audits API Server, Controller Manager, Scheduler, etcd, and Policies. Sends Telegram summary. |
| `cronjob-kube-bench-node.yml` | `CronJob` | Worker Nodes | Every Sunday at 03:30 UTC | Audits Kubelet, kube-proxy, and node configuration files. Sends Telegram summary. |

---

## 🚀 Deployment

### Deploy Scheduled CronJobs (Continuous Weekly Audits)

```powershell
kubectl apply -f kubernetes/crons/kube-bench/cronjob-kube-bench-master.yml
kubectl apply -f kubernetes/crons/kube-bench/cronjob-kube-bench-node.yml
```

Verify the CronJobs:
```powershell
kubectl get cronjobs -n kube-system -l app.kubernetes.io/name=kube-bench
```

---

## ⚡ Manual / On-Demand Execution

You can trigger an immediate audit run at any time directly from the CronJob:

```powershell
# Run control plane audit immediately
kubectl create job --from=cronjob/kube-bench-master kube-bench-master-manual -n kube-system

# Run worker node audit immediately
kubectl create job --from=cronjob/kube-bench-node kube-bench-node-manual -n kube-system
```

---

## 📱 Telegram Notification Format

When each job completes, it dispatches an instant HTML-formatted Telegram alert to your notifications channel:

> 🛡️ **CIS Kubernetes Benchmark Audit**  
> **Target:** Control Plane (`pi4-red`)  
>  
> ⚠️ **10 Checks Failed**  
> • ✅ **Pass:** 47  
> • ❌ **Fail:** 10  
> • ⚠️ **Warn:** 44  
>  
> 🔍 **Top Failures:**  
> • **1.1.12**: Ensure that the etcd data directory ownership is set to etcd:etcd  
> • **1.2.5**: Ensure that the --kubelet-certificate-authority argument is set as appropriate  
> • **1.2.15**: Ensure that the --profiling argument is set to false  
> ...  
>  
> *View full report: `kubectl logs -n kube-system <pod>`*

---

## 🔍 Viewing Full Audit Reports

List recent audit jobs:
```powershell
kubectl get jobs -n kube-system -l app.kubernetes.io/name=kube-bench
```

Inspect the full CIS benchmark report and remediation recommendations in the pod logs:

```powershell
# For manual runs
kubectl logs -n kube-system job/kube-bench-master-manual
kubectl logs -n kube-system job/kube-bench-node-manual

# For scheduled runs (replace with the job name from `kubectl get jobs`)
kubectl logs -n kube-system job/kube-bench-master-<timestamp>
kubectl logs -n kube-system job/kube-bench-node-<timestamp>
```