# Cluster Storage Architecture

This document details the persistent storage infrastructure backing the Kubernetes cluster, including the Debian NFS storage host, volume patterns, and troubleshooting steps.

---

## 1. Storage Host Specifications

* **Host Identifier**: `cinema`
* **IP Address**: `192.168.1.119`
* **Operating System**: Debian Linux
* **SSH User**: `anilsezer`
* **Role**: External Network File System (NFS) server
* **Base Export Path**: `/mnt/nas/disk/k8s/`

> [!NOTE]
> The storage server is an independent, regular Debian computer. It does not run Kubernetes components and communicates with the cluster strictly via NFS protocols over the local network (`192.168.1.0/24`).

---

## 2. Client Node Configuration

Each Raspberry Pi worker and control plane node is provisioned with `nfs-common`:
* `mount.nfs` client utilities are installed.
* Kernel NFS client modules are loaded.
* Pods requiring persistent volumes mount subpaths directly from `192.168.1.119`.

---

## 3. PersistentVolume (PV) Architecture

Persistent storage in the cluster relies on static NFS PersistentVolumes defined with access modes suited for single or multi-replica pods.

### Standard PV Manifest Template

```yaml
apiVersion: v1
kind: PersistentVolume
metadata:
  name: example-nfs-pv
spec:
  capacity:
    storage: 10Gi
  accessModes:
    - ReadWriteMany    # Or ReadWriteOnce depending on application needs
  persistentVolumeReclaimPolicy: Retain
  volumeMode: Filesystem
  nfs:
    server: 192.168.1.119
    path: /mnt/nas/disk/k8s/example-dir
```

### Access Modes Used
* **`ReadWriteMany` (RWX)**: Shared filesystems across multiple pods running on different nodes (e.g., shared assets, image registries).
* **`ReadWriteOnce` (RWO)**: Dedicated storage instances locked to a single node at a time (e.g., individual database instances).

---

## 4. Diagnostics & Troubleshooting NFS Volumes

### Verify Volume Status via `kubectl`
```powershell
# List all PVs and their bound PVCs
kubectl get pv

# Check all PVCs across the cluster
kubectl get pvc -A
```

### Common Symptoms & Fixes

1. **Pod Stuck in `ContainerCreating` with Mount Timeout**:
   * Inspect pod events:
     ```powershell
     kubectl describe pod <pod-name> -n <namespace>
     ```
   * Look for `MountVolume.SetUp failed for volume ... mount failed: Connection timed out`.
   * Check if `192.168.1.119` is reachable from the host where the pod is scheduled:
     ```powershell
     ping 192.168.1.119
     ```

2. **Permission Denied (`mount.nfs: access denied by server`)**:
   * Verify exports on the Debian host:
     ```bash
     ssh anilsezer@192.168.1.119 "sudo cat /etc/exports"
     ```
   * Verify export permissions allow access from the Pi node IP range (`192.168.1.0/24`) with `rw,sync,no_subtree_check,no_root_squash`.

3. **Stale File Handles (`ESTALE`)**:
   * Occurs if files or directories were deleted or unmounted on `cinema` while still mounted in a container.
   * Solution: Restart the affected pod to re-initialize the NFS mount handle.
