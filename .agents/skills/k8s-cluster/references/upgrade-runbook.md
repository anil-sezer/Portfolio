# Kubernetes Cluster Upgrade Runbook

This runbook defines the exact, production-safe procedures for upgrading the Kubernetes homelab cluster.

---

## 1. Upgrade Prerequisites & Golden Rules

> [!IMPORTANT]
> 1. **Upgrade Order is Strictly Sequential**:
>    * **Control Plane (`pi4-red`) MUST be upgraded first.**
>    * Worker nodes MUST be upgraded **one at a time** in sequence:
>      `pi4-blue` ➔ `pi5-green` ➔ `pi5-gold` ➔ `pi5-purple`
>    * Never drain or upgrade multiple worker nodes concurrently.
>
> 2. **Package Hold Enforcement (`dpkg_selections`)**:
>    * `kubeadm`, `kubelet`, and `kubectl` are held via `apt-mark hold` to prevent inadvertent upgrades.
>    * You must explicitly run `apt-mark unhold` before installing and re-run `apt-mark hold` immediately after.
>
> 3. **Version Skew Rules**:
>    * Kubelet version can never be newer than `kube-apiserver`.
>    * Never skip minor versions (e.g., upgrade `v1.36.x` ➔ `v1.37.x`, do not jump from `v1.35` directly to `v1.37`).
>
> 4. **Tool Execution Rules**:
>    * Run `kubectl` draining/cordoning/uncordoning commands in PowerShell.
>    * SSH commands can be run directly using the configured SSH aliases (`sshrpired`, `sshrpiblue`, etc.).

---

## 2. Phase 1: Pre-Upgrade Health Check

Before starting any upgrade, verify cluster health from PowerShell:

```powershell
# 1. Verify all nodes are Ready
kubectl get nodes -o wide

# 2. Check for any failing or crashlooping pods
kubectl get pods -A | grep -v -E "Running|Completed"

# 3. Verify control plane component status
kubectl get pods -n kube-system -o wide
```

---

## 3. Phase 2: Upgrading Control Plane (`pi4-red`)

Connect via SSH:
```bash
ssh sshrpired
```

### 3.1 Upgrade Kubeadm
```bash
# 1. Check available package versions
sudo apt-get update
apt-cache madison kubeadm

# 2. Unhold kubeadm
sudo apt-mark unhold kubeadm

# 3. Install target kubeadm version (e.g., 1.36.2-1.1 or latest patch)
sudo apt-get install -y kubeadm=1.36.2-1.1

# 4. Re-hold kubeadm
sudo apt-mark hold kubeadm

# 5. Verify kubeadm version
kubeadm version
```

### 3.2 Plan and Apply Upgrade
```bash
# Verify the upgrade plan
sudo kubeadm upgrade plan

# Apply the upgrade
sudo kubeadm upgrade apply v1.36.2 -y
```

### 3.3 Upgrade Kubelet and Kubectl
```bash
# 1. Unhold kubelet and kubectl
sudo apt-mark unhold kubelet kubectl

# 2. Install matching versions
sudo apt-get install -y kubelet=1.36.2-1.1 kubectl=1.36.2-1.1

# 3. Re-hold packages
sudo apt-mark hold kubelet kubectl

# 4. Reload systemd daemon and restart kubelet
sudo systemctl daemon-reload
sudo systemctl restart kubelet
```

### 3.4 Verify Control Plane
Exit the SSH session and verify from PowerShell:
```powershell
kubectl get nodes -o wide
```
Confirm `pi4-red` shows the updated version and is `Ready`.

---

## 4. Phase 3: Upgrading Worker Nodes (One by One)

Follow this exact loop for each worker node:
* `pi4-blue` (`sshrpiblue`)
* `pi5-green` (`sshrpigreen`)
* `pi5-gold` (`sshrpigold`)
* `pi5-purple` (`sshrpipurple`)

### Step 1: Cordon & Drain the Worker (PowerShell)
```powershell
# Replace <node-name> with the target node (e.g. pi4-blue)
kubectl cordon <node-name>

# Safely evict workloads (allowing daemonsets and deleting empty-dir data)
kubectl drain <node-name> --ignore-daemonsets --delete-emptydir-data --force
```

### Step 2: SSH into Worker and Upgrade Kubeadm
```bash
ssh <ssh-alias>

# 1. Update and unhold kubeadm
sudo apt-get update
sudo apt-mark unhold kubeadm

# 2. Install target kubeadm version
sudo apt-get install -y kubeadm=1.36.2-1.1

# 3. Re-hold kubeadm
sudo apt-mark hold kubeadm
```

### Step 3: Upgrade Worker Node Configuration
```bash
# Upgrades local kubelet configuration
sudo kubeadm upgrade node
```

### Step 4: Upgrade Kubelet and Kubectl
```bash
# 1. Unhold kubelet and kubectl
sudo apt-mark unhold kubelet kubectl

# 2. Install target versions
sudo apt-get install -y kubelet=1.36.2-1.1 kubectl=1.36.2-1.1

# 3. Re-hold packages
sudo apt-mark hold kubelet kubectl

# 4. Restart kubelet
sudo systemctl daemon-reload
sudo systemctl restart kubelet

exit
```

### Step 5: Uncordon Node & Verify (PowerShell)
```powershell
# Mark node schedulable again
kubectl uncordon <node-name>

# Verify node status
kubectl get node <node-name> -o wide

# Check that pods are rescheduling cleanly
kubectl get pods -A --field-selector spec.nodeName=<node-name>
```

> [!CAUTION]
> Wait until all pods on the uncordoned node reach `Running` status before proceeding to drain the next worker node!

---

## 5. Minor Version Upgrades (e.g., v1.36 ➔ v1.37)

When upgrading to a new minor release, the APT repository definition must be updated first:

```bash
# Update repository string in /etc/apt/sources.list.d/kubernetes.list
echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.37/deb/ /' | sudo tee /etc/apt/sources.list.d/kubernetes.list

# Update repository keyring if required by the new release
curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.37/deb/Release.key | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg

sudo apt-get update
```

---

## 6. Troubleshooting & Recovery

* **Kubelet Fails to Start**:
  ```bash
  sudo journalctl -u kubelet -e --no-pager
  ```
  Check if containerd socket is available:
  ```bash
  sudo systemctl status containerd
  ```
* **Containerd Service Override**:
  Kubelet systemd unit has an override located at `/etc/systemd/system/kubelet.service.d/override.conf` ensuring kubelet waits for `containerd.service`.
