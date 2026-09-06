# Cluster Networking & Ingress Architecture

This document outlines the networking model, CNI plugin, IP address allocations, Layer 2 load balancer, and ingress traffic flow.

---

## 1. Network CIDRs & Allocations

| Network Layer | CIDR Block | Description |
| :--- | :--- | :--- |
| **Pod Overlay Network** | `10.244.0.0/16` | Flannel VXLAN overlay network for all pod communication |
| **Service Cluster IP Network** | `10.96.0.0/12` | Kubernetes ClusterIP range (`kube-dns` is at `10.96.0.10`) |
| **MetalLB LoadBalancer Pool** | `192.168.1.230 - 192.168.1.250` | Physical LAN IPs announced via ARP by MetalLB |
| **Physical LAN Network** | `192.168.1.0/24` | Local physical network connecting nodes and storage server |

### Node Pod Subnet Allocations (`/24` per node)

* **`pi4-red`**: `10.244.0.0/24`
* **`pi4-blue`**: `10.244.1.0/24`
* **`pi5-green`**: `10.244.2.0/24`
* **`pi5-gold`**: `10.244.3.0/24`
* **`pi5-purple`**: `10.244.4.0/24`

---

## 2. Flannel CNI (Container Network Interface)

* **Namespace**: `kube-flannel`
* **DaemonSet**: `kube-flannel-ds` (runs on all 5 nodes)
* **Backend Type**: `vxlan` (VNI 1)
* **Subnet Manager**: Kube Subnet Manager (`flannel.alpha.coreos.com/kube-subnet-manager: true`)

### Diagnostics for CNI
If pods cannot communicate across nodes:
```powershell
# Check flannel daemonset status
kubectl get pods -n kube-flannel -o wide

# Check flannel logs on a specific node
kubectl logs -n kube-flannel -l app=flannel --tail=50
```

---

## 3. MetalLB Load Balancer

* **Namespace**: `metallb-system`
* **Mode**: **Layer 2 (L2Advertisement)**
* **Components**:
  * `controller`: Manages IP allocations from pools.
  * `speaker` & `frr-k8s-daemon`: DaemonSets on every node responding to ARP requests for assigned IPs.

### MetalLB Configuration
* Configured in `kubernetes/metallb-config.yml`:
  * **IPAddressPool** `default`: `192.168.1.230 - 192.168.1.250`
  * **L2Advertisement** `default`: Advertises pool `default` over all node interfaces.

### Reserved / Allocated Load Balancer IPs
* **`192.168.1.230`**: Traefik Ingress Controller (`service/traefik` in namespace `traefik`)

---

## 4. Ingress Controller & Traffic Routing

### Traefik Ingress
* **Namespace**: `traefik`
* **Release**: Helm-managed (`traefik`)
* **Service Type**: `LoadBalancer` (External IP: `192.168.1.230`)
* **Ports**:
  * HTTP: `80` (NodePort `32563`)
  * HTTPS: `443` (NodePort `31116`)

### Gateway API
* The cluster has Kubernetes Gateway API CRDs installed (`v1.5.1`):
  * `gateways.gateway.networking.k8s.io`
  * `gatewayclasses.gateway.networking.k8s.io`
  * `httproutes.gateway.networking.k8s.io`
  * `grpcroutes.gateway.networking.k8s.io`
  * `tlsroutes.gateway.networking.k8s.io`

### TLS & Certificates
* **Cert-Manager**: Deployed in namespace `cert-manager`.
* **ClusterIssuer**: Configured via `kubernetes/letsencrypt-clusterissuer.yml` using ACME HTTP-01 challenge solvers.

---

## 5. Network Troubleshooting Commands

```powershell
# 1. Verify CoreDNS is healthy and resolving
kubectl get pods -n kube-system -l k8s-app=kube-dns -o wide

# 2. Check MetalLB speaker status across all nodes
kubectl get pods -n metallb-system -l app=metallb -o wide

# 3. Check Traefik Service and External IP
kubectl get svc -n traefik traefik

# 4. Test Ingress IP reachability from local machine
curl -k -I http://192.168.1.230
```
