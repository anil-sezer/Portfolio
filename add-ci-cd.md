# CI/CD Implementation Plan & Task List

## Architecture Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         GitHub Cloud (ubuntu-latest)                       │
│  1. CI Workflows:                                                          │
│     - ci-dotnet.yml      (.NET 10 build & unit/integration tests)          │
│     - ci-go.yml          (Go cron vet, test & build)                       │
│     - ci-yaml-lint.yml   (yamllint across repository)                      │
│     - ci-infra.yml       (kubeconform, hadolint, ansible-lint)             │
│     - ci-security.yml    (NuGet/Go vulnerability audit & Trivy scan)       │
│                                                                            │
│  2. CD Build & Push Stage:                                                 │
│     - Multi-arch Docker Buildx (linux/amd64, linux/arm64) + GHA caching    │
│     - Pushes over HTTPS to imgregistry.anil-sezer.com                      │
└─────────────────────────────────────┬──────────────────────────────────────┘
                                      │
                                      │ Job Handoff (Outbound HTTPS poll only,
                                      │ zero inbound router ports opened)
                                      ▼
┌────────────────────────────────────────────────────────────────────────────┐
│        Raspberry Pi K8s Cluster (self-hosted runner in `portfolio`)        │
│  3. CD Deploy Stage (`runs-on: [self-hosted, linux, ARM64]`):              │
│     - Uses in-cluster ServiceAccount with namespace-scoped RBAC            │
│     - Runs `kubectl rollout restart deployment/frontend -n portfolio`      │
│     - Runs `kubectl rollout restart deployment/grpc -n portfolio`          │
│     - Waits for `kubectl rollout status` health verification               │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## Task Breakdown (Execute 1 by 1)

### Task 1: In-Cluster Self-Hosted Runner & Least-Privilege RBAC (`kubernetes/github-runner/`)
- [x] **1.1** Create namespace-scoped RBAC manifests in `portfolio` namespace:
  - `serviceaccount-github-runner.yml`: Dedicated `ServiceAccount` (`github-runner-deployer`).
  - `role-github-runner.yml`: Least-privilege `Role` restricted strictly to the `portfolio` namespace (`get`, `list`, `watch`, `patch` on `apps/deployments` and `batch/cronjobs`).
  - `rolebinding-github-runner.yml`: Binds the `Role` to `github-runner-deployer`.
- [x] **1.2** Create the ARM64-compatible GitHub Actions Runner image/manifest (`deployment.yml` & `kustomization.yml` under `kubernetes/github-runner/`) with `kubectl` pre-installed and resource limits tailored for the Raspberry Pi worker nodes.
- [x] **1.3** Create the Kubernetes Secret on the cluster for the GitHub runner registration token/PAT, deploy the runner pod, and verify it appears as **Idle** in GitHub (`Settings -> Actions -> Runners`).

---

### Task 2: Configure GitHub Secrets & Repository Variables
- [x] **2.1** Add Docker registry secrets in GitHub (`Settings -> Secrets and variables -> Actions`):
  - `REGISTRY_USERNAME`: Username for `imgregistry.anil-sezer.com`
  - `REGISTRY_PASSWORD`: Password for `imgregistry.anil-sezer.com`
- [x] **2.2** Add repository variable `CD_ENABLED` (`true` / `false`) so live registry push and cluster deployment can be toggled safely without failing public CI runs.

---

### Task 3: CD Workflow for `.NET` Services (`.github/workflows/cd-portfolio.yml`)
- [x] **3.1** Create `.github/workflows/cd-portfolio.yml` triggered on pushes to `prod` (for paths `Portfolio.**`, `protos/**`, `Portfolio.sln`, `global.json`) and manual `workflow_dispatch`.
- [x] **3.2** Add **Build & Push** job (`runs-on: ubuntu-latest`):
  - Uses QEMU + Docker Buildx for `linux/arm64` builds of `Portfolio.Ui/Dockerfile` and `Portfolio.Grpc/Dockerfile`.
  - Uses GitHub Actions cache (`type=gha`) for fast layer caching.
  - Tags images with `:latest` and `:<short-sha>` and pushes to `imgregistry.anil-sezer.com` when `vars.CD_ENABLED == 'true'` (or runs dry-run build verification when disabled).
- [x] **3.3** Add **Deploy to K8s** job (`runs-on: [self-hosted, linux, ARM64]`):
  - Runs `kubectl rollout restart deployment/frontend deployment/grpc -n portfolio`.
  - Verifies zero-downtime rollout completion via `kubectl rollout status`.

---

### Task 4: CD Workflow for Go Cron Microservices (`.github/workflows/cd-crons.yml`)
- [x] **4.1** Ensure explicit `imagePullPolicy: Always` is set in `kubernetes/crons/ip-lookup-cron/cronjob-ip-lookup-go.yml` and `kubernetes/crons/get-image-of-the-day-from-bing/cronjob-get-daily-images-go.yml`.
- [x] **4.2** Create `.github/workflows/cd-crons.yml` triggered on changes under `kubernetes/crons/**` or `workflow_dispatch` to build & push multi-arch (`linux/amd64,linux/arm64`) images to `imgregistry.anil-sezer.com`:
  - `imgregistry.anil-sezer.com/iplookup-cron-go:latest`
  - `imgregistry.anil-sezer.com/get-daily-images-go-cron:latest`

---

### Task 5: CI Workflow for Infrastructure & Container Validation (`.github/workflows/ci-infra.yml`)
- [x] **5.1** Add **Dockerfile Lint** job using `hadolint` across all 5 `Dockerfile`s in the repository.
- [x] **5.2** Add **Kubernetes Manifest Validation** job using `kubeconform` to validate manifests and Kustomize overlays in `kubernetes/`.
- [x] **5.3** Add **Ansible Validation** job using `ansible-lint` / `ansible-playbook --syntax-check` for `ansible-prepare-k8s-cluster/setup-kubernetes.yml`.

---

### Task 6: CI Workflow for Security & Dependency Auditing (`.github/workflows/ci-security.yml`)
- [ ] **6.1** Add **.NET Vulnerability Check** step (`dotnet list Portfolio.sln package --vulnerable --include-transitive`).
- [ ] **6.2** Add **Go Vulnerability Check** step (`govulncheck ./...` across Go cron modules).
- [ ] **6.3** Add **Trivy Config & Filesystem Scan** (`aquasecurity/trivy-action`) on push, PR, and weekly schedule.

---

### Task 7: Polish Existing CI Workflows, Documentation & YAML Lint Check
- [ ] **7.1** Enhance `.github/workflows/ci-dotnet.yml` and `.github/workflows/ci-go.yml` with GitHub Actions job test summaries.
- [ ] **7.2** Add CI/CD workflow status badges to `README.md` and document the runner setup in `.agents/AGENT_REFERENCE.md`.
- [ ] **7.3** Run `yamllint` across all new workflows and Kubernetes manifests to ensure 100% compliance with `.yamllint`.
