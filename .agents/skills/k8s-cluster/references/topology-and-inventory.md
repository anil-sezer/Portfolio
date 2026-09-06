# Cluster Topology & Hardware Inventory

This document details the node specifications, operating systems, kernel versions, and roles across the 5-node Raspberry Pi homelab cluster and the supporting Debian storage host.

---

## 1. Master Inventory Table

| Hostname | Role | IP Address | SSH Alias | Hardware Model | vCPU | RAM | Disk Allocatable | OS Image | Kernel Version | K8s Version | Container Runtime |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`pi4-red`** | Control Plane | `192.168.1.120` | `sshrpired` | Raspberry Pi 4 Model B | 4 | 4 GB | ~53 GB | Ubuntu 26.04 LTS | `7.0.0-1016-raspi` | `v1.36.1` | `containerd://2.3.0` |
| **`pi4-blue`** | Worker | `192.168.1.121` | `sshrpiblue` | Raspberry Pi 4 Model B | 4 | 8 GB | ~53 GB | Ubuntu 26.04 LTS | `7.0.0-1017-raspi` | `v1.36.1` | `containerd://2.3.0` |
| **`pi5-green`** | Worker | `192.168.1.122` | `sshrpigreen` | Raspberry Pi 5 | 4 | 8 GB | ~53 GB | Ubuntu 25.10 | `6.17.0-1021-raspi` | `v1.36.1` | `containerd://2.3.0` |
| **`pi5-gold`** | Worker | `192.168.1.123` | `sshrpigold` | Raspberry Pi 5 | 4 | 8 GB | ~105 GB | Ubuntu 26.04 LTS | `7.0.0-1017-raspi` | `v1.36.2` | `containerd://2.3.0` |
| **`pi5-purple`** | Worker | `192.168.1.124` | `sshrpipurple` | Raspberry Pi 5 | 4 | 8 GB | ~53 GB | Ubuntu 26.04 LTS | `7.0.0-1017-raspi` | `v1.36.2` | `containerd://2.3.0` |
| **`cinema`** | NFS Storage | `192.168.1.119` | — | Regular PC | — | — | — | Debian Linux | — | N/A (External) | N/A |

---

## 2. Node Specifics & Scheduling Policies

### `pi4-red` (Control Plane)
* **IP**: `192.168.1.120`
* **SSH Alias**: `sshrpired`
* **User**: `anilsezer`
* **Labels**:
  * `node-role.kubernetes.io/control-plane=`
  * `node.kubernetes.io/exclude-from-external-load-balancers=`
* **Taints**:
  * `node-role.kubernetes.io/control-plane:NoSchedule`
* **Operational Notes**:
  * Only system pods tolerating this taint (etcd, kube-apiserver, kube-controller-manager, kube-scheduler, flannel, coredns, metallb speaker) run here.
  * Standard user application workloads must **never** be forced onto this node.
  * RAM is 4 GB (half of the worker nodes); monitor memory utilization before scheduling any daemonsets.

### `pi4-blue` (Worker Node)
* **IP**: `192.168.1.121`
* **SSH Alias**: `sshrpiblue`
* **User**: `anilsezer`
* **Specs**: Raspberry Pi 4 Model B, 8 GB RAM.
* **Taints**: None (Fully schedulable).

### `pi5-green` (Worker Node)
* **IP**: `192.168.1.122`
* **SSH Alias**: `sshrpigreen`
* **User**: `anilsezer`
* **Specs**: Raspberry Pi 5, 8 GB RAM.
* **OS**: **Ubuntu 25.10** (Kernel `6.17.0-1021-raspi`).
* **Operational Notes**: Note the OS version discrepancy compared to the other nodes running Ubuntu 26.04 LTS.

### `pi5-gold` (Worker Node)
* **IP**: `192.168.1.123`
* **SSH Alias**: `sshrpigold`
* **User**: `anilsezer`
* **Specs**: Raspberry Pi 5, 8 GB RAM, ~120 GB local drive capacity (~105 GB allocatable).
* **K8s Version**: `v1.36.2`.

### `pi5-purple` (Worker Node)
* **IP**: `192.168.1.124`
* **SSH Alias**: `sshrpipurple`
* **User**: `anilsezer`
* **Specs**: Raspberry Pi 5, 8 GB RAM.
* **K8s Version**: `v1.36.2`.

---

## 3. Storage Host (`cinema`)

* **IP**: `192.168.1.119`
* **User**: `anilsezer`
* **Machine Type**: Regular computer / dedicated server.
* **Operating System**: Debian Linux.
* **Role**: NFS storage provider for cluster PersistentVolumes.
* **Primary Export Root**: `/mnt/nas/disk/k8s/`
* **Operational Notes**:
  * This machine is outside the Kubernetes cluster control plane and does not run kubelet.
  * Cluster nodes mount volumes over the local network via `nfs-common`.

---

## 4. Known Discrepancies & Upgrade Cautions

> [!WARNING]
> 1. **Kubernetes Version Drift**:
>    * `pi5-gold` and `pi5-purple` are running Kubernetes **`v1.36.2`**.
>    * `pi4-red`, `pi4-blue`, and `pi5-green` are running Kubernetes **`v1.36.1`**.
>    * When planning cluster upgrades, align all nodes to `v1.36.2` or plan an incremental patch upgrade before transitioning to a new minor release.
>
> 2. **OS Distribution Drift**:
>    * `pi5-green` runs Ubuntu 25.10, while the rest run Ubuntu 26.04 LTS.
>    * Ensure APT repository keys and source lists are validated per-distribution before performing system package upgrades.
