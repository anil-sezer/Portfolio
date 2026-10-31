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
                Name = "Prometheus",
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
            }
        };
    }
}