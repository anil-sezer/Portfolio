namespace Portfolio.Ui.Models;

public class TechStackModel
{
    public required string Name { get; init; }
    public required string WebsiteLink { get; init; }
                
    public required string ImageAddress { get; init; }
    public required string ImageAltTextKey { get; init; }
    public required int ImageWidth { get; init; }
    public required int ImageHeight { get; init; }
                
    public required string DescriptionKey { get; init; }
    public string? AriaLabel { get; init; }

    public static IReadOnlyList<TechStackModel> GetTechStack()
    {
        return new List<TechStackModel>
        {
            new()
            {
                Name = "Kubernetes",
                WebsiteLink = "https://kubernetes.io/",

                ImageAddress = "img/techStack/kubernetes.svg",
                ImageAltTextKey = "TechStack_Kubernetes_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Kubernetes_Desc"
            },
            new()
            {
                Name = "Ansible",
                WebsiteLink = "https://www.ansible.com/",

                ImageAddress = "img/techStack/ansible.svg",
                ImageAltTextKey = "TechStack_Ansible_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Ansible_Desc"
            },
            new()
            {
                Name = "Docker",
                WebsiteLink = "https://www.docker.com/",

                ImageAddress = "img/techStack/docker.svg",
                ImageAltTextKey = "TechStack_Docker_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Docker_Desc"
            },
            new()
            {
                Name = ".Net 10 & Blazor",
                WebsiteLink = "https://dotnet.microsoft.com/",

                ImageAddress = "img/techStack/dotnet.svg",
                ImageAltTextKey = "TechStack_DotNet_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_DotNet_Desc"
            },
            new()
            {
                Name = "Go",
                WebsiteLink = "https://go.dev/",

                ImageAddress = "img/techStack/go.svg",
                ImageAltTextKey = "TechStack_Go_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Go_Desc",
                AriaLabel = "Go programming language"
            },
            new()
            {
                Name = "gRPC",
                WebsiteLink = "https://grpc.io/",

                ImageAddress = "img/techStack/grpc.svg",
                ImageAltTextKey = "TechStack_Grpc_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Grpc_Desc"
            },
            new()
            {
                Name = "PostgreSQL",
                WebsiteLink = "https://www.postgresql.org/",

                ImageAddress = "img/techStack/postgres.svg",
                ImageAltTextKey = "TechStack_PostgreSql_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_PostgreSql_Desc"
            },
            new()
            {
                Name = "OpenTelemetry",
                WebsiteLink = "https://opentelemetry.io/",
            
                ImageAddress = "img/techStack/opentelemetry.svg",
                ImageAltTextKey = "TechStack_OpenTelemetry_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_OpenTelemetry_Desc"
            },
            new()
            {
                Name = "Prometheus & Alertmanager",
                WebsiteLink = "https://prometheus.io/",
            
                ImageAddress = "img/techStack/prometheus.svg",
                ImageAltTextKey = "TechStack_Prometheus_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_Prometheus_Desc"
            },
            new()
            {
                Name = "Grafana",
                WebsiteLink = "https://grafana.com/",

                ImageAddress = "img/techStack/grafana.svg",
                ImageAltTextKey = "TechStack_Grafana_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Grafana_Desc"
            },
            new()
            {
                Name = "Jaeger",
                WebsiteLink = "https://jaegertracing.io/",

                ImageAddress = "img/techStack/jaeger.svg",
                ImageAltTextKey = "TechStack_Jaeger_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Jaeger_Desc"
            },
            new()
            {
                Name = "Seq",
                WebsiteLink = "https://datalust.co/",

                ImageAddress = "img/techStack/seq.svg",
                ImageAltTextKey = "TechStack_Seq_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Seq_Desc"
            },
            new()
            {
                Name = "Lens",
                WebsiteLink = "https://lenshq.io/",

                ImageAddress = "img/techStack/lens.svg",
                ImageAltTextKey = "TechStack_Lens_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Lens_Desc"
            },
            new()
            {
                Name = "Kubecolor",
                WebsiteLink = "https://kubecolor.github.io/",

                ImageAddress = "img/techStack/kubecolor.svg",
                ImageAltTextKey = "TechStack_Kubecolor_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Kubecolor_Desc"
            },
            new()
            {
                Name = "Elastic Search",
                WebsiteLink = "https://www.elastic.co/",
            
                ImageAddress = "img/techStack/elastic.svg",
                ImageAltTextKey = "TechStack_Elasticsearch_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_Elasticsearch_Desc"
            },
            new()
            {
                Name = "Authentik",
                WebsiteLink = "https://goauthentik.io/",
            
                ImageAddress = "img/techStack/authentik.svg",
                ImageAltTextKey = "TechStack_Authentik_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_Authentik_Desc"
            },
            new()
            {
                Name = "Traefik",
                WebsiteLink = "https://traefik.io/",

                ImageAddress = "img/techStack/traefik.svg",
                ImageAltTextKey = "TechStack_Traefik_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Traefik_Desc"
            },
            new()
            {
                Name = "Helm",
                WebsiteLink = "https://helm.sh/",

                ImageAddress = "img/techStack/helm.svg",
                ImageAltTextKey = "TechStack_Helm_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Helm_Desc"
            },
            new()
            {
                Name = "MetalLB",
                WebsiteLink = "https://metallb.universe.tf/",

                ImageAddress = "img/techStack/metallb.svg",
                ImageAltTextKey = "TechStack_MetalLB_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_MetalLB_Desc"
            },
            new()
            {
                Name = "Cert-Manager",
                WebsiteLink = "https://cert-manager.io/",

                ImageAddress = "img/techStack/certmanager.svg",
                ImageAltTextKey = "TechStack_CertManager_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_CertManager_Desc"
            },
            new()
            {
                Name = "Flannel",
                WebsiteLink = "https://github.com/flannel-io/flannel",

                ImageAddress = "img/techStack/flannel.svg",
                ImageAltTextKey = "TechStack_Flannel_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Flannel_Desc"
            },
            new()
            {
                Name = "Containerd",
                WebsiteLink = "https://containerd.io/",

                ImageAddress = "img/techStack/containerd.svg",
                ImageAltTextKey = "TechStack_Containerd_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Containerd_Desc"
            },
            new()
            {
                Name = "Uptime Kuma",
                WebsiteLink = "https://uptime.kuma.pet/",

                ImageAddress = "img/techStack/uptimekuma.svg",
                ImageAltTextKey = "TechStack_UptimeKuma_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_UptimeKuma_Desc"
            },
            new()
            {
                Name = "Kibana",
                WebsiteLink = "https://www.elastic.co/kibana",

                ImageAddress = "img/techStack/kibana.svg",
                ImageAltTextKey = "TechStack_Kibana_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Kibana_Desc"
            },
            new()
            {
                Name = "Admin Neo",
                WebsiteLink = "https://adminneo.org/",

                ImageAddress = "img/techStack/adminneo.svg",
                ImageAltTextKey = "TechStack_AdminNeo_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_AdminNeo_Desc"
            },
            new()
            {
                Name = "GitHub Actions",
                WebsiteLink = "https://github.com/features/actions",

                ImageAddress = "img/techStack/githubactions.svg",
                ImageAltTextKey = "TechStack_GitHubActions_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_GitHubActions_Desc"
            }
        };
    }
}