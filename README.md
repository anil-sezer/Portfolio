# anilsezer.net — Portfolio & Bare-Metal Kubernetes Lab

Welcome to my portfolio repository! You can explore the entire codebase and cluster configuration here, or visit the live system at **[anilsezer.net](https://anilsezer.net)**.

[![ci-dotnet](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-dotnet.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-dotnet.yml)
[![ci-go](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-go.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-go.yml)
[![ci-infra](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-infra.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-infra.yml)
[![ci-security](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-security.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-security.yml)
[![ci-yaml-lint](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-yaml-lint.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/ci-yaml-lint.yml)
[![cd-portfolio](https://github.com/anil-sezer/Portfolio/actions/workflows/cd-portfolio.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/cd-portfolio.yml)
[![cd-crons](https://github.com/anil-sezer/Portfolio/actions/workflows/cd-crons.yml/badge.svg?branch=prod)](https://github.com/anil-sezer/Portfolio/actions/workflows/cd-crons.yml)

---

Made with love, applying **Domain-Driven Design** principles from [Eric Evans](https://ddd.academy/eric-evans/).

## What Is This?

Rather than hosting a static page on a managed cloud provider, **[anilsezer.net](https://anilsezer.net)** runs on a **6-node bare-metal Kubernetes cluster** (5 Raspberry Pi 4/5 nodes + a dedicated NAS storage server) that I built and operate at home.

The cluster runs **64+ pods**, **42+ services**, and **30+ deployments**—and the live cluster statistics shown on the website are queried directly from the Kubernetes API via the official C# [`KubernetesClient`](https://github.com/kubernetes-client/csharp).

- **Single-Command Node & Cluster Setup:** Nodes are provisioned, hardened, and joined using Ansible ([`ansible-prepare-k8s-cluster/`](./ansible-prepare-k8s-cluster)).
- **Bare-Metal Networking & TLS:** [MetalLB](https://metallb.io/) for Layer 2 load balancing, [Traefik](https://traefik.io/traefik) as the Ingress controller, and [Let's Encrypt](https://letsencrypt.org/) (`cert-manager`) for automated TLS certificates.
- **Polyglot Microservices over gRPC:** A `.NET 10` Blazor frontend communicates with a `.NET 10` gRPC backend and `Go` CronJobs using shared Protocol Buffer contracts ([`protos/`](./protos)).
- **Full Self-Hosted Observability:** Instrumented end-to-end with OpenTelemetry, Prometheus, Grafana, Jaeger (backed by Elasticsearch), and Seq, with internal dashboards protected by Authentik SSO.

---

## Tech Stack

| <img src="src/Portfolio.Ui/wwwroot/img/techStack/kubernetes.svg" height="32" alt="Kubernetes" /> <br/> **Kubernetes** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/ansible.svg" height="32" alt="Ansible" /> <br/> **Ansible** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/docker.svg" height="32" alt="Docker" /> <br/> **Docker** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/dotnet.svg" height="32" alt=".NET 10 & Blazor" /> <br/> **.NET 10 & Blazor** |
|:---:|:---:|:---:|:---:|
| <img src="src/Portfolio.Ui/wwwroot/img/techStack/go.svg" height="32" alt="Go" /> <br/> **Go** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/grpc.svg" height="32" alt="gRPC" /> <br/> **gRPC** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/postgres.svg" height="32" alt="PostgreSQL" /> <br/> **PostgreSQL** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/opentelemetry.svg" height="32" alt="OpenTelemetry" /> <br/> **OpenTelemetry** |
| <img src="src/Portfolio.Ui/wwwroot/img/techStack/prometheus.svg" height="32" alt="Prometheus" /> <br/> **Prometheus** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/grafana.svg" height="32" alt="Grafana" /> <br/> **Grafana** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/jaeger.svg" height="32" alt="Jaeger" /> <br/> **Jaeger** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/elastic.svg" height="32" alt="Elasticsearch" /> <br/> **Elasticsearch** |
| <img src="src/Portfolio.Ui/wwwroot/img/techStack/seq.svg" height="32" alt="Seq" /> <br/> **Seq** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/authentik.svg" height="32" alt="Authentik" /> <br/> **Authentik** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/lens.svg" height="32" alt="Lens" /> <br/> **Lens** | <img src="src/Portfolio.Ui/wwwroot/img/techStack/kubecolor.svg" height="32" alt="Kubecolor" /> <br/> **Kubecolor** |

1. **`Kubernetes:`** After setting up this bare-metal cluster on my own, I learned that doing it from scratch is famously known as [Kubernetes The Hard Way](https://github.com/kelseyhightower/kubernetes-the-hard-way). All 5 Raspberry Pi nodes and the NAS storage server are prepared and deployed with a **single Ansible command**.
2. **`Ansible:`** I felt this Kubernetes setup would be incomplete without full automation. It also encourages experimentation—if I break the lab beyond repair while testing something new, I can reset and reprovision the entire cluster rapidly.
3. **`Docker:`** Every service is containerized and compiled for multi-arch (`linux/arm64` & `linux/amd64`). Public images are on [my DockerHub](https://hub.docker.com/repositories/anilsezer), while private images run from a self-hosted Docker private image registry inside the cluster.
4. **`.NET 10 & Blazor:`** Built with Domain-Driven Design (DDD) and Clean Architecture using the latest `.NET 10` and Blazor features, complete with bilingual (EN/TR) localization and unit/integration test suites.
5. **`Go:`** Whenever a scheduled job needs more than plain YAML, I write it in Go—both to keep cron workloads lightweight and to broaden my perspective across languages.
6. **`gRPC:`** Wanted to move beyond standard REST for internal service-to-service communication. Both the Blazor UI and the Go CronJobs talk to the backend over gRPC.
7. **`PostgreSQL:`** Primary database for the cluster (running on persistent NFS storage backed by automated `pg_dump` cron backups). I enjoy its feature set and love experimenting with it.
8. **`OpenTelemetry:`** Distributed traces across services are a must.
9. **`Prometheus:`** I've been using it for 4+ years and continue diving deeper into custom metrics and alerting rules.
10. **`Grafana:`** This cluster wouldn't be complete without the mesmerizing dashboards of Grafana.
11. **`Jaeger & Elasticsearch:`** Used for collecting, persisting, and inspecting distributed traces across the UI, gRPC backend, and cron jobs.
12. **`Seq:`** Structured log ingestion correlated with trace IDs—has a clean, fast UI.
13. **`Authentik:`** Secures access to my internal cluster dashboards and admin panels via Forward-Auth SSO.
14. **`Lens & Kubecolor:`** Lens gives a great graphical overview of the cluster, and `kubecolor` is a must-have for readable `kubectl` output and pod logs in the terminal.

---

## Repository Structure

```text
Portfolio/
├── ansible-prepare-k8s-cluster/   # Ansible playbooks for bare-metal node setup, containerd, kubeadm & MetalLB
├── kubernetes/                    # Kubernetes manifests & Helm values for the entire cluster
│   ├── website/                   # Frontend (Blazor) & Backend (gRPC) deployments, services, RBAC & Ingress
│   ├── crons/                     # Go & Bash CronJobs (Bing daily wallpaper, IP geolocation, DB backups)
│   ├── observability/             # OpenTelemetry, Prometheus, Grafana, Jaeger & Seq configurations
│   ├── traefik-ingress/           # Traefik v3 ingress controller, TLS certificates & middlewares
│   ├── authentik/                 # Authentik SSO & forward-auth middleware
│   ├── db/                        # PostgreSQL, Redis, Elasticsearch & Adminer manifests
│   └── image-registry/            # Self-hosted Docker private image registry
├── protos/                        # Shared gRPC Protocol Buffer contracts (used by both C# and Go)
├── src/
│   ├── Portfolio.Domain/          # Pure DDD domain entities, enums & interfaces
│   ├── Portfolio.Infrastructure/  # EF Core (PostgreSQL), OpenTelemetry, KubernetesClient & Telegram notifications
│   ├── Portfolio.Grpc/            # .NET 10 gRPC backend service & async Channel background workers
│   └── Portfolio.Ui/              # .NET 10 Blazor web application (EN/TR)
└── tests/
    └── Portfolio.Test.*/          # Unit & integration test suites (Domain, Grpc, Infrastructure, UI)
```

---

## Credits

- **Daily background images:** [Bing Daily Wallpapers](https://www.microsoft.com/en-us/bing/bing-wallpaper) (fetched daily by a Go CronJob)
- **Contact map tiles:** [Leaflet Provider Previews](https://leaflet-extras.github.io/leaflet-providers/preview/)
- **Tech stack section gradient:** [Post Wook](https://x.com/POSTWOOK/status/2097037189049925818)

---

Setting up and properly managing a Kubernetes cluster on bare metal was hard when I set this up years ago. Now, I'm quite comfy with it.

![It's a joke about struggles I had with the cluster](we-thought-it-would-be-easy.webp)
