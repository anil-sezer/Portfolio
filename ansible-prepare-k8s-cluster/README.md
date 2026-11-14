# Ansible Kubernetes Cluster Automation

Ansible playbooks and tasks to provision, configure, and maintain the 5-node Raspberry Pi Kubernetes cluster and Debian NAS server.

---

## Folder Structure

```text
ansible-prepare-k8s-cluster/
├── .ansible-lint              # Ansible lint rules and skip configuration
├── inventory.yml              # Cluster node definitions (IPs, users, groups)
├── variables.yml              # Central variables (versions, IPs, download paths)
├── README.md                  # This documentation
├── playbooks/                 # Top-level orchestration playbooks
│   ├── prepare-core-components.yml # OS update, zsh, CLI tools, watchdog & tuning
│   ├── reboot.yml             # Drain, reboot workers & control plane, uncordon
│   ├── setup-kubernetes.yml   # containerd, kubeadm, kubelet, MetalLB
│   ├── setup-nas-n-cinema.yml # NFS mounts & kernel server for NAS
│   ├── shutdown.yml           # Drain and safely shut down nodes
│   ├── start-kubernetes.yml   # Start containerd & kubelet, uncordon nodes
│   ├── stop-kubernetes.yml    # Drain, stop containers, stop services
│   └── verify-node-uniqueness.yml # Verify DMI/serial and MAC address uniqueness
└── tasks/                     # Modular task files grouped by category
    ├── generic/               # Generic utilities (fetch-file, sandbox image parsing)
    ├── kubernetes-setup/      # containerd, kubeadm, kubelet, crictl, MetalLB
    └── os-and-cli-tools/      # OS updates, watchdogs, sysctls, eth0 tuning, CLI tools
        ├── cli-tools/         # bat, dust, eza, fd-find installers
        └── oh-my-zsh/         # Powerlevel10k theme configuration
```

---

## Prerequisites & Connectivity

### Test Connectivity (Ping all nodes)
```bash
ansible all -m ansible.builtin.ping -i ansible-prepare-k8s-cluster/inventory.yml
```

### Verify Node Hardware Uniqueness (MAC & DMI / Serial)
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/verify-node-uniqueness.yml
```

---

## Cluster Provisioning Runbook

### 1. OS Preparation & Core Tooling
Updates system packages, installs zsh/oh-my-zsh, modern CLI tools, hardware watchdog, kernel panic auto-reboot sysctls, eth0 network tuning, and official case fan control for Pi 4 nodes:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/prepare-core-components.yml
```

### 2. NAS Storage Setup (Cinema Host)
Configures `/mnt/nas/disk` and `/mnt/nas/ssd` fstab entries and NFS exports:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/setup-nas-n-cinema.yml
```

### 3. Bootstrap Kubernetes Cluster
Provisions containerd, kubeadm, kubelet, Flannel CNI, and MetalLB on all nodes:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/setup-kubernetes.yml
```

#### Dry-run Check:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/setup-kubernetes.yml --check
```

#### Syntax Check:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/setup-kubernetes.yml --syntax-check
```

#### Linting:
```bash
ansible-lint -c ansible-prepare-k8s-cluster/.ansible-lint ansible-prepare-k8s-cluster/playbooks/setup-kubernetes.yml
```

---

## Cluster Lifecycle & Maintenance

### Graceful Cluster Reboot
Drains worker nodes, reboots workers, reboots control plane, waits for API server and nodes to be `Ready`, then uncordons all nodes:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/reboot.yml
```

### Stop Kubernetes (Maintenance Mode)
Drains nodes, stops kubelet, stops running containers via `crictl`, and stops containerd:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/stop-kubernetes.yml
```

### Start Kubernetes (Exit Maintenance Mode)
Starts containerd, starts kubelet, waits for API server readiness, and uncordons all nodes:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/start-kubernetes.yml
```

### Safe Cluster Shutdown
Drains nodes, halts worker nodes, pauses, and halts the control plane host:
```bash
ansible-playbook -i ansible-prepare-k8s-cluster/inventory.yml ansible-prepare-k8s-cluster/playbooks/shutdown.yml
```

---

## Post-Install Steps

### Gateway API CRDs
```bash
kubectl apply --server-side=true -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.5.1/standard-install.yaml
```

### Traefik Ingress Controller
```bash
helm install traefik traefik/traefik -f kubernetes/traefik-ingress/helm-values.yml --wait
```

### Print Default kubeadm Config
```bash
kubeadm config print init-defaults
```

---

## References & Upstream Inspiration
- [My Ansible Role for Oh-My-Zsh & CLI Tools](https://www.lorenzobettini.it/2023/07/my-ansible-role-for-oh-my-zsh-and-other-cli-programs/)
- [Setup Oh-My-ZSH & PowerLevel10K](https://shaunandersonaz.github.io/Ansible-To-Setup-OhMyZSH-and-PowerLevel10K/)
- [Raspberry Pi 5 Kubernetes Cluster](https://devops.datenkollektiv.de/raspberry-pi-5-a-kubernetes-cluster.html)
- [Building a K8s Cluster with Raspberry Pis & Containerd](https://www.danielcolomb.com/2023/07/16/building-a-kubernetes-cluster-with-raspberry-pis-and-containerd/)
- [Geerlingguy Raspberry Pi Dramble](https://github.com/geerlingguy/raspberry-pi-dramble/tree/master)
