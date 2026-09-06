---
name: k8s-cluster
description: >-
  Comprehensive knowledge base, operational procedures, and runbooks for managing,
  health-checking, debugging, and upgrading the 5-node Raspberry Pi Kubernetes cluster
  and its Debian-based NFS storage server. Use this skill whenever an agent needs to
  inspect cluster state, run maintenance tasks, perform Kubernetes upgrades, or manage
  core infrastructure components like Flannel, MetalLB, and Traefik.
---

# Kubernetes Homelab Cluster Skill

This skill provides essential knowledge, topology details, and operational runbooks for managing the 5-node Raspberry Pi Kubernetes homelab cluster and its Debian storage server.

---

## ⚠️ Critical Agent Rules (MANDATORY)

1. **Helm Execution Environment**:
   > [!IMPORTANT]
   > **Helm commands MUST ALWAYS be run inside WSL** (e.g., via `wsl helm <command>`). Never execute `helm` directly from PowerShell CLI.

2. **Kubectl Execution Environment**:
   > `kubectl` commands can be run directly on the standard Windows PowerShell CLI.

3. **SSH Aliases & Access**:
   * All cluster nodes run on user account: `anilsezer`
   * SSH configuration aliases are preconfigured on the host machine:
     * Control Plane: `sshrpired` (`192.168.1.120`)
     * Worker 1: `sshrpiblue` (`192.168.1.121`)
     * Worker 2: `sshrpigreen` (`192.168.1.122`)
     * Worker 3: `sshrpigold` (`192.168.1.123`)
     * Worker 4: `sshrpipurple` (`192.168.1.124`)
     * Storage / Cinema Host: `192.168.1.119` (`anilsezer`)

---

## Quick Reference: Cluster Node Matrix

| Hostname | Role | IP Address | SSH Alias | Hardware | RAM | OS Image | K8s Ver | CNI Subnet |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`pi4-red`** | Control Plane | `192.168.1.120` | `sshrpired` | Raspberry Pi 4 | 4 GB | Ubuntu 26.04 LTS | `v1.36.1` | `10.244.0.0/24` |
| **`pi4-blue`** | Worker | `192.168.1.121` | `sshrpiblue` | Raspberry Pi 4 | 8 GB | Ubuntu 26.04 LTS | `v1.36.1` | `10.244.1.0/24` |
| **`pi5-green`** | Worker | `192.168.1.122` | `sshrpigreen` | Raspberry Pi 5 | 8 GB | Ubuntu 25.10 | `v1.36.1` | `10.244.2.0/24` |
| **`pi5-gold`** | Worker | `192.168.1.123` | `sshrpigold` | Raspberry Pi 5 | 8 GB | Ubuntu 26.04 LTS | `v1.36.2` | `10.244.3.0/24` |
| **`pi5-purple`** | Worker | `192.168.1.124` | `sshrpipurple` | Raspberry Pi 5 | 8 GB | Ubuntu 26.04 LTS | `v1.36.2` | `10.244.4.0/24` |
| **`cinema`** | NFS Storage | `192.168.1.119` | — | PC / Server | — | Debian Linux | — | — |

> [!NOTE]
> * **Control Plane Taint**: `pi4-red` has the taint `node-role.kubernetes.io/control-plane:NoSchedule`. General workloads are not scheduled on it.
> * **Version Divergence**: `pi5-gold` and `pi5-purple` are on `v1.36.2`, while `pi4-red`, `pi4-blue`, and `pi5-green` are on `v1.36.1`.
> * **OS Divergence**: `pi5-green` is on Ubuntu 25.10 (Linux kernel 6.17), while other Pi nodes are on Ubuntu 26.04 LTS (Linux kernel 7.0.0).

---

## Core Infrastructure Components

* **Architecture**: `linux/arm64` across all cluster nodes.
* **Container Runtime**: `containerd://2.3.0` with `runc` v1.3.2 and CNI plugins v1.6.0.
* **Network Plugin (CNI)**: Flannel VXLAN overlay (`10.244.0.0/16`).
* **Service CIDR**: `10.96.0.0/12` (CoreDNS at `10.96.0.10`).
* **Load Balancer**: MetalLB in Layer 2 mode (`metallb-system`), managing IP pool `192.168.1.230 - 192.168.1.250`.
* **Ingress Controller**: Traefik v3 (namespace `traefik`), bound to LoadBalancer VIP `192.168.1.230`.
* **Gateway API**: Gateway API v1.5.1 CRDs installed (`gateway.networking.k8s.io`).
* **Certificates**: `cert-manager` with Let's Encrypt ClusterIssuer.
* **Persistent Storage**: NFS shares exported from Debian server `cinema` (`192.168.1.119:/mnt/nas/disk/k8s/`).

---

## Reference Manuals & Runbooks

For in-depth procedures and technical specifications, refer to:

* [Topology & Hardware Inventory](./references/topology-and-inventory.md): Full hardware specs, OS/kernel versions, and taints.
* [Networking & Ingress Guide](./references/networking-and-ingress.md): Flannel, MetalLB L2 pool, Traefik, Gateway API, and DNS.
* [Storage Architecture Guide](./references/storage-architecture.md): NFS server details (`cinema`), PV/PVC patterns, and mount configuration.
* [Cluster Upgrade Runbook](./references/upgrade-runbook.md): Step-by-step procedures for safely upgrading K8s nodes (package holds, drain, kubeadm, kubelet, uncordon).
* [Health Check & Diagnostics Runbook](./references/health-check-procedures.md): Pre-upgrade checklists, condition validation, and troubleshooting.
