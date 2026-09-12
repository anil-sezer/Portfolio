# Workspace Rules & AI Agent Directives

## ⚠️ Mandatory Rules of Engagement

1. **Helm Execution Environment**:
   * **Helm commands MUST ALWAYS be run inside WSL** (e.g., `wsl helm <command>`). Never execute `helm` directly from standard Windows PowerShell.
2. **Kubectl Execution Environment**:
   * `kubectl` commands can be executed on standard Windows PowerShell CLI or WSL.
3. **Multi-Architecture Docker Builds**:
   * The physical Kubernetes cluster runs on **ARM64** Raspberry Pi nodes. Any container images built for cluster deployment must be built for multi-arch and pushed to the internal registry:
     ```bash
     docker buildx build -t imgregistry.anil-sezer.com/<image-name>:<tag> --platform linux/amd64,linux/arm64 --push .
     ```
4. **Target Framework & SDK**:
   * All .NET projects target `net10.0` (SDK 10.0.2 pinned in `global.json`). Always test builds with `dotnet build Portfolio.sln`.
5. **Database Migrations Prohibition**:
   * **Agents must NEVER create or add database migrations** (never run `dotnet ef migrations add`). Database schema migrations are strictly managed manually by the human maintainer.
6. **Synchronized Enums Across Languages**:
   * The `ImageOfTheDaySource` enum is shared between Protobuf, C#, and Go. Search for token `GXJQJZ` when modifying to keep all files synchronized.

---

## 🗺️ Master Technical Reference

For comprehensive architecture details, database schemas, gRPC service specifications, cluster hardware topology, and runbooks:
👉 Read [.agents/AGENT_REFERENCE.md](file:///c:/Repositories/Portfolio/.agents/AGENT_REFERENCE.md)

For deep bare-metal cluster maintenance, node upgrade runbooks, Flannel CNI, MetalLB, and NFS storage:
👉 Refer to skill [.agents/skills/k8s-cluster/SKILL.md](file:///c:/Repositories/Portfolio/.agents/skills/k8s-cluster/SKILL.md)

---

## ⚡ Quick Command Cheat Sheet

| Task | Command |
| :--- | :--- |
| **Build .NET Solution** | `dotnet build Portfolio.sln` |
| **Run Go Cron Tests** | `cd kubernetes/crons/ip-lookup-cron; go test -v ./...` |
| **Start Local Stack** | `docker compose up -d` |
| **Run UI Locally** | `dotnet run --project Portfolio.Ui` (accessible at `http://localhost:5002`) |
| **Run gRPC Locally** | `dotnet run --project Portfolio.Grpc` (accessible at `http://localhost:8081`) |
| **Check Cluster Pods** | `kubectl get pods -A -o wide` |
| **List Helm Releases** | `wsl helm list -A` |
| **Restart Deployments** | `kubectl rollout restart deployment/<name> -n portfolio` |