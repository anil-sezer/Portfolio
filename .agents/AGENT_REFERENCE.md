# Portfolio Repository: AI Agent Reference Guide

> **Target Audience**: Future AI Agents (Antigravity, Cursor, Copilot, Claude Code, etc.) and human maintainers working on this codebase.  
> **Last Repository Audit**: September 2026

---

## 1. Executive Summary & Repository Identity

This repository contains the complete source code, infrastructure as code (IaC), deployment manifests, and automation scripts for **Anil Sezer's personal portfolio website and homelab ecosystem** ([anil-sezer.com](https://www.anil-sezer.com/) / [anilsezer.net](https://www.anilsezer.net/)).

The system is a production-grade homelab demonstrating **Domain-Driven Design (DDD)**, **Clean Architecture**, **gRPC-based microservice communication**, **OpenTelemetry observability**, and a **bare-metal 5-node Raspberry Pi Kubernetes cluster** backed by a dedicated Debian NFS storage server.

### Primary Technology Stack

| Layer | Technologies |
| :--- | :--- |
| **Frontend UI** | [.NET 10](https://dotnet.microsoft.com/), Blazor Server (`InteractiveServerRenderMode`), Bootstrap-based responsive layout, Vanilla JS helpers |
| **Backend API** | [.NET 10](https://dotnet.microsoft.com/), [gRPC](https://grpc.io/) (HTTP/2), Channel-based asynchronous worker queues |
| **Domain & Data** | Pure DDD domain entities, [EF Core 10](https://docs.microsoft.com/ef/core/), [PostgreSQL 18](https://www.postgresql.org/), Snake-case naming conventions |
| **Microservice Crons** | [Go 1.23+](https://go.dev/) (IP lookup/enrichment, Bing Daily Image scraper), Shell scripts (Postgres backups) |
| **Cluster & IaC** | Bare-metal Kubernetes (v1.36), [Ansible](https://www.ansible.com/), [Helm 3](https://helm.sh/), [MetalLB](https://metallb.universe.tf/) (L2), [Traefik v3](https://traefik.io/) Ingress, [cert-manager](https://cert-manager.io/) (Let's Encrypt) |
| **Observability** | [OpenTelemetry](https://opentelemetry.io/), [Prometheus](https://prometheus.io/), [Grafana](https://grafana.com/), [Jaeger](https://www.jaegertracing.io/), [Seq](https://datalust.co/seq), [Alertmanager](https://prometheus.io/docs/alerting/latest/alertmanager/) |
| **Identity & Security** | [Authentik](https://goauthentik.io/) (Forward Auth Proxy SSO), Private Docker Image Registry (`imgregistry.anil-sezer.com`) |

---

## 2. Mandatory Rules of Engagement (Golden Rules)

All AI agents operating in this repository **must** adhere to these non-negotiable operational rules:

1. **Helm Execution Environment**:
   > [!IMPORTANT]
   > **Helm commands MUST ALWAYS be executed inside WSL** (e.g., `wsl helm <command>`). Never execute `helm` directly from standard Windows PowerShell.
2. **Kubectl Execution Environment**:
   > `kubectl` commands can be run directly from standard Windows PowerShell CLI or WSL.
3. **Multi-Architecture Docker Builds**:
   > [!WARNING]
   > The physical Kubernetes cluster runs on **ARM64** (`linux/arm64`) Raspberry Pi hardware. Any Docker images built for deployment must be compiled for multiple architectures and pushed to the internal registry:
   > ```bash
   > docker buildx build -t imgregistry.anil-sezer.com/<image-name>:<tag> --platform linux/amd64,linux/arm64 --push .
   > ```
4. **Target Framework & SDK**:
   > All .NET projects target `net10.0` with SDK pinned in [global.json](file:///c:/Repositories/Portfolio/global.json) (`10.0.100` rollForward `latestMinor`). Always verify builds with `dotnet build Portfolio.sln`.
5. **Database Migrations Prohibition**:
   > [!CAUTION]
   > **Agents must NEVER create or add database migrations** (never run `dotnet ef migrations add`). Database schema migrations are strictly managed manually by the repository maintainer.
6. **Synchronized Enums Across Languages**:
   > The `ImageOfTheDaySource` enum is shared across Protobuf, C#, and Go. Whenever modifying this enum, search the codebase for the token `GXJQJZ` and update all definitions in tandem.

---

## 3. High-Level Architecture & Component Map

```
┌────────────────────────────────────────────────────────────────────────┐
│                              Clients / Internet                        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ HTTPS (443)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│  MetalLB VIP (192.168.1.230) -> Traefik v3 Ingress Controller         │
│  - SSL Termination (cert-manager / Let's Encrypt prod)                │
│  - Canonical Host Redirects (portfolio-redirect-to-canonical)          │
│  - Authentik Forward-Auth Middleware (for adminer, grafana, etc.)      │
└──────────┬───────────────────────────────────────────────┬─────────────┘
           │                                               │
           │ HTTP (8080)                                   │ Internal Routing
           ▼                                               ▼
┌─────────────────────────┐                     ┌────────────────────────┐
│   Portfolio.Ui          │                     │ Protected Tools        │
│   (Blazor Server UI)    │                     │ - Authentik SSO        │
│   Namespace: portfolio  │                     │ - Adminer DB UI        │
└──────────┬──────────────┘                     │ - Uptime Kuma          │
           │                                    │ - Observability Stack  │
           │ gRPC over HTTP/2 (Port 8080)       └────────────────────────┘
           ▼
┌────────────────────────────────────────────────────────────────────────┐
│   Portfolio.Grpc (Backend gRPC Service)                                │
│   Namespace: portfolio                                                 │
│   - K8sStatsService (queries cluster state via k8s client)            │
│   - BackgroundImageServices (Bing / NASA image provider)               │
│   - SendNotificationToAdminService (Telegram bot notifications)        │
│   - VisitorInsightsService (traffic telemetry & IP logging)            │
│   - DatabaseOperationQueueWorker (Channel-based async worker)          │
└──────────┬─────────────────────────────────────────────────────────────┘
           │
           │ EF Core (Port 5432, snake_case)
           ▼
┌────────────────────────────────────────────────────────────────────────┐
│   PostgreSQL 18 Database (Namespace: database)                         │
│   - Persistent Volume on Debian NFS Storage Server (`cinema`)          │
└────────────────────────────────────────────────────────────────────────┘
     ▲                              ▲
     │ gRPC (Port 8080)             │ Compressed pg_dump (NFS)
     │                              │
┌────┴───────────────────────────┐ ┌┴───────────────────────────────────┐
│ Cron: ip-lookup-go             │ │ Cron: backup-db-v18                │
│ - Enriches visitor IP with     │ │ - Hourly/daily pg_dump             │
│   city, country, flag & deletes│ │   compressed to NFS storage        │
│   internal/unwanted traffic    │ │                                    │
└────────────────────────────────┘ └────────────────────────────────────┘
```

---

## 4. Directory Structure & Project Breakdown

```
c:\Repositories\Portfolio\
├── .agents/                                # AI Agent rules, skills, runbooks, and references
│   ├── AGENTS.md                           # Core workspace rules injected into agent contexts
│   ├── AGENT_REFERENCE.md                  # This master reference document
│   └── skills/k8s-cluster/                 # In-depth skill for cluster maintenance & runbooks
├── protos/                                 # Shared Protocol Buffer definitions
│   ├── background_images.proto             # Daily background image contract
│   ├── k8s_stats.proto                     # Live cluster metrics contract
│   ├── send_email_to_admin.proto           # Contact form & admin notification contract
│   └── visitor_insights.proto              # Visitor telemetry & IP check contract
├── src/                                    # Source projects
│   ├── Portfolio.Domain/                   # Clean Architecture: Pure domain models & contracts
│   │   ├── Entities/                       # EntityBase, DailyImage, NotificationToAdmin, RequestLog
│   │   ├── Enums/                          # ImageOfTheDaySource, NotificationType
│   │   └── Interfaces/                     # Repository and service contracts (IRepository, etc.)
│   ├── Portfolio.Infrastructure/           # Persistence, EF Core, external providers, telemetry
│   │   ├── PortfolioDbContext.cs           # EF Core DbContext with snake_case naming
│   │   ├── Migrations/                     # EF Core database migration history
│   │   ├── Repositories/                   # EF Core repository implementations
│   │   ├── Extensions/                     # EnvVars, DbContextExtensions, HealthChecks, Logging, OTel
│   │   └── ThirdPartyServices/             # NotificationProviderTelegram (Telegram bot integration)
│   ├── Portfolio.Grpc/                     # Backend gRPC host service
│   │   ├── Program.cs                      # Startup configuration, DI, gRPC mappings, auto-migrations
│   │   ├── Dockerfile                      # Multi-stage build for linux/amd64 & linux/arm64
│   │   ├── BackgroundServices/             # DatabaseOperationQueueWorker, LogVisitorInfoQueuedOperation
│   │   └── Services/                       # BackgroundImageServices, K8sStatsService, VisitorInsights
│   └── Portfolio.Ui/                       # Blazor Server (.NET 10) user-facing application
│       ├── Program.cs                      # Blazor Web App setup, endpoints, middlewares, caching
│       ├── Dockerfile                      # Multi-stage build for linux/amd64 & linux/arm64
│       ├── Components/Pages/               # Home.razor, Error.razor, and IndexPageSections (About, ClusterInfo, etc.)
│       ├── Endpoints/                      # Minimal API endpoints (EmailEndpoints, VisitEndpoints)
│       ├── Extensions/                     # ApiEndpointExtensions, Localization, RateLimiter
│       ├── Services/                       # ClusterStatsService (5-min cache), BackgroundImageService (23-hr cache)
│       ├── Localization/                   # Localization provider, marker type, and resx files (English and Turkish tr-TR)
│       └── wwwroot/                        # Static assets, CSS, JS scripts, oneko cat mascot
├── tests/                                  # Test projects
│   ├── Portfolio.Test.Domain/              # Domain unit tests
│   ├── Portfolio.Test.Grpc/                # Backend gRPC service tests
│   ├── Portfolio.Test.Infrastructure/      # Infrastructure tests (unit/integration)
│   ├── Portfolio.Test.Shared/              # Shared test helpers & base classes
│   └── Portfolio.Test.Ui/                  # Blazor UI service and endpoint tests
├── kubernetes/                             # All Kubernetes manifests and Helm value files
│   ├── website/                            # Deployments, Services, Ingress, RBAC for Ui & Grpc
│   ├── database/                           # Postgres 18, Redis, Adminer, Elasticsearch/Kibana
│   ├── observability/                      # Prometheus, Grafana, Jaeger, OpenTelemetry Collector, Seq
│   ├── traefik-ingress/                    # Traefik v3 Ingress controller, certificates, middlewares
│   ├── authentik/                          # Authentik Identity Provider & Forward Auth proxy
│   ├── image-registry/                     # Docker private image registry & web UI
│   ├── uptime/                             # Uptime Kuma monitoring & status page
│   ├── crons/                              # Microservice CronJobs
│   │   ├── ip-lookup-cron/                 # Go cron: visitor IP geolocation enrichment
│   │   ├── get-image-of-the-day-from-bing/ # Go cron: Bing daily image scraper & gRPC persist
│   │   └── backup-db-v18/                  # Bash cron: automated pg_dump database backups
│   └── debug/                              # Diagnostic utilities (netshoot, stress-test, kuard)
├── ansible-prepare-k8s-cluster/            # Ansible automation for cluster provisioning & node setup
│   ├── inventory.yml                       # Cluster hosts, IPs, and node groups
│   ├── variables.yml                       # Versions, CNI configuration, architecture settings
│   └── setup-kubernetes.yml                # Main cluster setup playbook
├── compose.yml                             # Local development multi-container Docker Compose file
├── global.json                             # Pinned .NET SDK configuration (10.0.2)
└── Portfolio.sln                           # Master Visual Studio / .NET Solution
```

---

## 5. Domain Model & Persistence (EF Core / PostgreSQL)

### Entity Relationships & Schemas

The database uses PostgreSQL with snake_case naming conventions enforced by `EFCore.NamingConventions`.

1. **`DailyImage`** (Table: `daily_images`):
   - Stores URLs and metadata for daily background images (Bing, NASA, etc.).
   - Key fields: `image_url` (string, URL validation), `alt_text` (string), `source` (`ImageOfTheDaySource` enum: Bing=0, NASA=1, None=2), `url_works` (bool), `do_i_prefer_to_display_this` (bool).
2. **`NotificationToAdmin`** (Table: `notifications_to_admin`):
   - Stores contact messages submitted by visitors.
   - Key fields: `name`, `email_address`, `subject`, `message`, `is_it_sent_successfully` (bool).
   - Rate limiting rule: Verified against `IsThisEmailAlreadySentAtLastHourAsync` to prevent spamming the admin within a 1-hour window.
3. **`RequestLog`** (Table: `request_logs`):
   - Stores visitor telemetry collected by client-side JS (`logVisits.js`) and HTTP headers.
   - Browser telemetry: `user_agent`, `accept_language`, `platform`, `webdriver`, `device_memory`, `hardware_concurrency`, `max_touch_points`, `do_not_track`, `connection`, `cookie_enabled`, `on_line`, `referrer`, `resolution`, `requested_url`, `extras`.
   - Network & Geo info: `client_ip`, `country`, `city`. Initially stored empty, populated asynchronously by `ip-lookup-cron`.

### Asynchronous Processing via Channels

To maintain high throughput on visitor telemetry without blocking web requests:
- When a visit is logged, `VisitorInsightsService.StoreVisitorInfo` enqueues a `LogVisitorInfoQueuedOperation` into `DatabaseOperationQueue`.
- `DatabaseOperationQueue` uses a bounded `System.Threading.Channels.Channel<IDatabaseOperationQueueWorker>` (`Capacity: 1000`, `BoundedChannelFullMode.Wait`) to provide backpressure under load.
- `DatabaseOperationQueueWorker` consumes operations with an exponential backoff retry policy (3 attempts, jitter, fresh `IServiceScope` per attempt) to handle transient database connection issues.
- On shutdown, `DatabaseOperationQueueWorker.StopAsync` marks the queue complete and drains all remaining queued operations before process exit.

---

## 6. gRPC Protocol & Service Contracts

All Protobuf definitions live in [protos/](file:///c:/Repositories/Portfolio/protos/). The C# namespace is `Portfolio.Grpc`.

| Service | Proto File | Key Methods | Description |
| :--- | :--- | :--- | :--- |
| `BackgroundImages` | `background_images.proto` | `Get()`, `Persist()` | Fetches or writes daily background images. Validates URL availability via HTTP HEAD. |
| `K8sStats` | `k8s_stats.proto` | `Get()` | Queries Kubernetes cluster API for pod count, services, nodes, deployments, and cronjobs. |
| `SendEmailToAdmin` | `send_email_to_admin.proto` | `Send()` | Validates, deduplicates, logs, and forwards visitor contact messages via Telegram bot. |
| `VisitorInsights` | `visitor_insights.proto` | `StoreVisitorInfo()`, `GetIpsToCheck()`, `PersistCheckedIps()` | Ingestion and asynchronous enrichment API for visitor telemetry and IP geocoding. |

---

## 7. Cluster Infrastructure & Hardware Topology

The homelab runs on bare metal with 5 Raspberry Pi nodes and a dedicated Debian storage host:

### Node Matrix

| Hostname | Role | IP Address | SSH Alias | Hardware | RAM | OS Image | CNI Subnet |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`pi4-red`** | Control Plane | `192.168.1.120` | `sshrpired` | Raspberry Pi 4 | 4 GB | Ubuntu 26.04 LTS | `10.244.0.0/24` |
| **`pi4-blue`** | Worker | `192.168.1.121` | `sshrpiblue` | Raspberry Pi 4 | 8 GB | Ubuntu 26.04 LTS | `10.244.1.0/24` |
| **`pi5-green`** | Worker | `192.168.1.122` | `sshrpigreen` | Raspberry Pi 5 | 8 GB | Ubuntu 25.10 | `10.244.2.0/24` |
| **`pi5-gold`** | Worker | `192.168.1.123` | `sshrpigold` | Raspberry Pi 5 | 8 GB | Ubuntu 26.04 LTS | `10.244.3.0/24` |
| **`pi5-purple`** | Worker | `192.168.1.124` | `sshrpipurple` | Raspberry Pi 5 | 8 GB | Ubuntu 26.04 LTS | `10.244.4.0/24` |
| **`cinema`** | NFS Storage | `192.168.1.119` | — | PC / Server | — | Debian Linux | — |

> [!NOTE]
> * Control plane `pi4-red` has the taint `node-role.kubernetes.io/control-plane:NoSchedule`. User workloads run exclusively on worker nodes.
> * Node user account: `anilsezer`.

### Networking & Routing

- **CNI**: Flannel VXLAN overlay (`10.244.0.0/16`).
- **Load Balancer**: MetalLB in Layer 2 mode (`metallb-system`), IP pool `192.168.1.230 - 192.168.1.250`.
- **Ingress Controller**: Traefik v3 bound to VIP `192.168.1.230`.

### Ingress Domain & Port Directory

| Domain | Target Service | Namespace | Authentication / Notes |
| :--- | :--- | :--- | :--- |
| `anil-sezer.com` / `anilsezer.net` | `frontend:80` | `portfolio` | Public portfolio website. Enforces HTTPS & canonical redirect. |
| `uptime.anil-sezer.com` | `uptime:3001` | `uptime` | Uptime Kuma monitoring dashboard. |
| `imgregistry.anil-sezer.com` | `docker-registry:5000` | `images` | Private Docker V2 image registry (NodePort 30003). |
| `registry-ui.anil-sezer.com` | `docker-registry-ui:80` | `images` | Web UI for Docker image registry. |
| `adminer.anil-sezer.com` | `adminer:8080` | `database` | PostgreSQL Web GUI. Protected by Authentik Forward-Auth. |
| `authentik.anil-sezer.com` | `authentik-server:80` | `authentik` | SSO and forward authentication proxy provider. |
| `grafana.anilsezer.net` | `grafana:80` | `observability` | Prometheus & Jaeger visualization dashboards. |
| `prometheus.anilsezer.net` | `prometheus:80` | `observability` | Metrics engine (NodePort 31388). |
| `seq.anilsezer.net` | `seq:80` | `observability` | Structured log search server. |
| `jaeger.anilsezer.net` | `jaeger:16686` | `observability` | Distributed tracing query UI. |
| `otel.anilsezer.net` | `otel-collector:4318` | `observability` | OpenTelemetry OTLP/HTTP receiver. |
| `otel-grpc.anilsezer.net` | `otel-collector:4317` | `observability` | OpenTelemetry OTLP/gRPC receiver. |
| `kibana.anilsezer.net` | `kibana:5601` | `database` | Elasticsearch log exploration UI. |

### Dedicated NodePorts

- **`30001`**: PostgreSQL (`postgres-nodeport`, namespace `database`)
- **`30003`**: Docker Registry (`svc-nodeport`, namespace `images`)
- **`31388`**: Prometheus (`svc-nodeport`, namespace `observability`)

### Persistent Storage (NFS Tiers on `cinema`)

All stateful workloads mount NFS exports from `192.168.1.119`:
- **SSD Fast Tier** (`/mnt/nas/ssd/k8s/...`):
  - Database: `db` (Postgres 18), `redis`, `database/elasticsearch`
  - Ingress: `traefik`
  - Observability: `observability/grafana`, `observability/prometheus`, `observability/seq`
  - Applications: `uptime`
- **HDD Storage Tier** (`/mnt/nas/disk/k8s/...`):
  - Backups: `db-backups/db-backups/postgres/portfolio`
  - Container images: `image-registry`

---

## 8. CI/CD Architecture & GitHub Actions Self-Hosted Runner

The repository uses a hybrid CI/CD pipeline combining GitHub-hosted runners (`ubuntu-latest`) for public pull request checks and multi-arch image compilation with an **in-cluster self-hosted runner** running on physical ARM64 Raspberry Pi hardware for deployment automation.

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         GitHub Cloud (ubuntu-latest)                       │
│  CI Workflows:                                                             │
│  - ci-dotnet.yml      (.NET 10 build, tests, GHA test summary)             │
│  - ci-go.yml          (Go cron vet, test, build, GHA test summary)         │
│  - ci-infra.yml       (kubeconform, hadolint, ansible-lint)                │
│  - ci-security.yml    (NuGet/Go audit, Trivy config/fs scan)               │
│  - ci-yaml-lint.yml   (yamllint across workflows & k8s manifests)          │
│                                                                            │
│  CD Build Stage:                                                           │
│  - cd-portfolio.yml / cd-crons.yml (QEMU + Docker Buildx multi-arch)       │
│  - Pushes images to imgregistry.anil-sezer.com (toggled by CD_ENABLED)     │
└─────────────────────────────────────┬──────────────────────────────────────┘
                                      │
                                      │ Outbound HTTPS polling only
                                      │ (zero inbound router ports opened)
                                      ▼
┌────────────────────────────────────────────────────────────────────────────┐
│        Raspberry Pi K8s Cluster (self-hosted runner in `portfolio`)        │
│  CD Deploy Stage (`runs-on: [self-hosted, linux, ARM64]`):                 │
│  - Runs as non-root user (1001:1001) in Deployment `github-runner`         │
│  - In-cluster ServiceAccount: `github-runner-deployer`                     │
│  - Namespace-scoped Role: `github-runner-role` (portfolio namespace only) │
│  - Executes zero-downtime rollouts via `kubectl rollout restart` & status │
└────────────────────────────────────────────────────────────────────────────┘
```

### 8.1 In-Cluster Runner Architecture (`kubernetes/github-runner/`)

- **Manifests**: Located in [kubernetes/github-runner/](file:///c:/Repositories/Portfolio/kubernetes/github-runner/).
- **Base Image**: `ghcr.io/actions/actions-runner:latest` (native `linux/arm64` container).
- **Tooling**: Uses an init container `bitnami/kubectl:latest` to extract the `kubectl` binary into a shared `/opt/bin` volume mount accessible in the runner's `$PATH`.
- **Labels**: `self-hosted,linux,ARM64`. Runner name: `k8s-pi-portfolio-runner`.
- **Security Context**:
  - `runAsNonRoot: true`, `runAsUser: 1001`, `fsGroup: 1001`.
  - Resource requests: 100m CPU / 256Mi RAM; limits: 500m CPU / 512Mi RAM.
- **Dynamic Registration & Graceful Deregistration**:
  - Fetches ephemeral runner registration tokens dynamically via the GitHub API using `GITHUB_PAT` from secret `github-runner-secret`.
  - Sets up `SIGTERM` / `SIGINT` trap in `entrypoint` to request a remove token and deregister cleanly upon pod termination or cluster upgrade.

### 8.2 Least-Privilege RBAC Setup

The runner pod is strictly sandboxed to the `portfolio` namespace through Kubernetes RBAC:
- **ServiceAccount**: `github-runner-deployer`
- **Role**: `github-runner-role` (namespace: `portfolio`)
  - `apps/deployments`: verbs `get`, `list`, `watch`, `patch`
  - `batch/cronjobs`: verbs `get`, `list`, `watch`, `patch`
- **RoleBinding**: `github-runner-rolebinding`
- No cluster-wide privileges, no cluster-admin, and no access to secrets or other namespaces (`database`, `observability`, `traefik`, etc.).

### 8.3 Repository Secrets & Variables

| Secret / Variable | Type | Purpose |
| :--- | :--- | :--- |
| `REGISTRY_USERNAME` | Secret | Basic auth user for `imgregistry.anil-sezer.com` |
| `REGISTRY_PASSWORD` | Secret | Basic auth password for `imgregistry.anil-sezer.com` |
| `GITHUB_PAT` | Secret (Cluster) | Ephemeral token acquisition for in-cluster runner |
| `CD_ENABLED` | Variable | Feature flag (`true`/`false`) toggling live registry push and cluster rollout |

### 8.4 Runner Maintenance Runbook

- **Check Runner Pod Status**:
  ```powershell
  kubectl get pods -n portfolio -l app=github-runner -o wide
  ```
- **Inspect Runner Logs**:
  ```powershell
  kubectl logs -n portfolio -l app=github-runner -c runner -f
  ```
- **Restart Runner Deployment**:
  ```powershell
  kubectl rollout restart deployment/github-runner -n portfolio
  ```
- **Verify GitHub Actions Connectivity**:
  Visit `Settings -> Actions -> Runners` on GitHub. `k8s-pi-portfolio-runner` should show status **Idle** with tags `self-hosted`, `linux`, `ARM64`.

---

## 9. Common Runbooks for AI Agents

### Runbook A: Modifying Frontend UI or Razor Components

1. Razor components are in [src/Portfolio.Ui/Components/Pages/](file:///c:/Repositories/Portfolio/src/Portfolio.Ui/Components/Pages/).
2. Section components (About, ClusterInfo, Contact, Hero, Resume, TechStack) have companion scoped CSS files (`.razor.css`).
3. Localized strings **must** use `IStringLocalizer<AppResources>`:
   - Add new resource keys to both [AppResources.resx](file:///c:/Repositories/Portfolio/src/Portfolio.Ui/Localization/AppResources.resx) (English) and [AppResources.tr-TR.resx](file:///c:/Repositories/Portfolio/src/Portfolio.Ui/Localization/AppResources.tr-TR.resx) (Turkish).
4. Verify changes locally:
   ```powershell
   dotnet build Portfolio.sln
   ```

### Runbook B: Adding / Updating a gRPC Service or Proto Contract

1. Modify or add `.proto` files in [protos/](file:///c:/Repositories/Portfolio/protos/).
2. If modifying Go proto consumers, regenerate Go code using:
   ```bash
   protoc --proto_path=protos --go_out=. --go-grpc_out=. protos/<proto_file>.proto
   ```
3. Update or implement the C# gRPC service in [src/Portfolio.Grpc/Services/](file:///c:/Repositories/Portfolio/src/Portfolio.Grpc/Services/).
4. Register the gRPC client in [src/Portfolio.Ui/Extensions/GrpcClientRegistration.cs](file:///c:/Repositories/Portfolio/src/Portfolio.Ui/Extensions/GrpcClientRegistration.cs).
5. Verify build: `dotnet build Portfolio.sln`.

### Runbook C: Building & Deploying Docker Containers to the Cluster

> [!NOTE]
> Pushing changes to `prod` triggers automated CD (`cd-portfolio.yml` and `cd-crons.yml`), which automatically builds multi-arch containers and restarts deployments via the in-cluster runner when `CD_ENABLED` is active. Manual builds can be run as follows:

1. Ensure Docker daemon is running and logged in to `imgregistry.anil-sezer.com`.
2. Build multi-arch images with `docker buildx`:
   ```bash
   # For Frontend UI:
   docker buildx build -t imgregistry.anil-sezer.com/portfolio-ui:latest -f src/Portfolio.Ui/Dockerfile --platform linux/amd64,linux/arm64 --push .

   # For Backend gRPC:
   docker buildx build -t imgregistry.anil-sezer.com/portfolio-grpc:latest -f src/Portfolio.Grpc/Dockerfile --platform linux/amd64,linux/arm64 --push .

   # For Go IP Lookup Cron:
   cd kubernetes/crons/ip-lookup-cron
   docker buildx build -t imgregistry.anil-sezer.com/iplookup-cron-go:latest --platform linux/amd64,linux/arm64 --push .
   ```
3. Restart Kubernetes deployment to pull the fresh image:
   ```powershell
   kubectl rollout restart deployment/frontend -n portfolio
   kubectl rollout restart deployment/grpc -n portfolio
   ```

### Runbook D: Managing Helm Releases

Remember: **Run all Helm commands via WSL!**
```bash
wsl helm list -A
wsl helm upgrade --install traefik traefik/traefik -n traefik -f kubernetes/traefik-ingress/helm-values.yml
wsl helm upgrade --install grafana grafana-community/grafana -n observability -f kubernetes/observability/grafana/helm-values.yml
```

---

## 10. Known Quirks, Traps & Conventions

1. **Enum Synchronization Marker (`GXJQJZ`)**:
   - The `ImageOfTheDaySource` enum is duplicated across [protos/background_images.proto](file:///c:/Repositories/Portfolio/protos/background_images.proto), [src/Portfolio.Domain/Enums/ImageOfTheDaySource.cs](file:///c:/Repositories/Portfolio/src/Portfolio.Domain/Enums/ImageOfTheDaySource.cs), and Go code. Always grep for `GXJQJZ` when editing image source enums.
2. **No Appsettings Files**:
   - All runtime configuration is driven purely via environment variables (see [src/Portfolio.Infrastructure/Extensions/EnvVars.cs](file:///c:/Repositories/Portfolio/src/Portfolio.Infrastructure/Extensions/EnvVars.cs)) and code-level defaults (e.g. Kestrel HTTP/2 in [src/Portfolio.Grpc/Program.cs](file:///c:/Repositories/Portfolio/src/Portfolio.Grpc/Program.cs)). `appsettings*.json` files have been eliminated across all projects.
3. **Node Count Convention (+1)**:
   - In [src/Portfolio.Grpc/Services/K8sStatsService.cs](file:///c:/Repositories/Portfolio/src/Portfolio.Grpc/Services/K8sStatsService.cs), `NodeCount` is intentionally calculated as `nodes.Items.Count + 1` to account for the unmanaged Debian `cinema` NAS host.
4. **Authentik Forward-Auth URL Trailing Slash**:
   - When configuring or accessing Authentik initial setup, omitting the trailing slash causes a 404 (e.g. use `http://<ip>:9000/if/flow/initial-setup/`).
5. **Authentik PostgreSQL 99-Character Password Limit**:
   - Due to a known PostgreSQL hashing issue within Authentik, database passwords must not exceed 99 characters.
6. **Telegram MarkdownV2 Escaping**:
   - [NotificationProviderTelegram.cs](file:///c:/Repositories/Portfolio/src/Portfolio.Infrastructure/ThirdPartyServices/NotificationProviderTelegram.cs) explicitly escapes characters `_ * [ ] ( ) ~ \` > # + - = | { } . !`. Ensure any newly injected text passes through `EscapeMarkdown()`.
7. **Cache Expirations**:
   - Cluster statistics are cached in memory for **5 minutes** (`ClusterStatsService`).
   - Daily background image is cached in memory for **23 hours** (`BackgroundImageService`).
