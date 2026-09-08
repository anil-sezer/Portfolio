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

    public static IReadOnlyList<TechStackModel> GetTechStack()
    {
        return new List<TechStackModel>
        {
            new()
            {
                Name = "Kubernetes",
                WebsiteLink = "https://kubernetes.io/",

                ImageAddress = "img/techStack/kubernetes.png",
                ImageAltTextKey = "TechStack_Kubernetes_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Kubernetes_Desc"
            },
            new()
            {
                Name = "Ansible",
                WebsiteLink = "https://www.ansible.com/",

                ImageAddress = "img/techStack/ansible.png",
                ImageAltTextKey = "TechStack_Ansible_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Ansible_Desc"
            },
            new()
            {
                Name = "Docker",
                WebsiteLink = "https://www.docker.com/",

                ImageAddress = "img/techStack/docker.png",
                ImageAltTextKey = "TechStack_Docker_Alt",
                ImageWidth = 45,
                ImageHeight = 27,

                DescriptionKey = "TechStack_Docker_Desc"
            },
            new()
            {
                Name = ".Net 10 & Blazor",
                WebsiteLink = "https://dotnet.microsoft.com/",

                ImageAddress = "img/techStack/dotnet.png",
                ImageAltTextKey = "TechStack_DotNet_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_DotNet_Desc"
            },
            new()
            {
                Name = "Go",
                WebsiteLink = "https://go.dev/",

                ImageAddress = "img/techStack/go.png",
                ImageAltTextKey = "TechStack_Go_Alt",
                ImageWidth = 42,
                ImageHeight = 21,

                DescriptionKey = "TechStack_Go_Desc"
            },
            new()
            {
                Name = "gRPC",
                WebsiteLink = "https://grpc.io/",

                ImageAddress = "img/techStack/grpc.png",
                ImageAltTextKey = "TechStack_Grpc_Alt",
                ImageWidth = 32,
                ImageHeight = 14,

                DescriptionKey = "TechStack_Grpc_Desc"
            },
            new()
            {
                Name = "PostgreSQL",
                WebsiteLink = "https://www.postgresql.org/",

                ImageAddress = "img/techStack/postgres.png",
                ImageAltTextKey = "TechStack_PostgreSql_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_PostgreSql_Desc"
            },
            new()
            {
                Name = "OpenTelemetry",
                WebsiteLink = "https://opentelemetry.io/",
            
                ImageAddress = "img/techStack/opentelemetry.png",
                ImageAltTextKey = "TechStack_OpenTelemetry_Alt",
                ImageWidth = 32,
                ImageHeight = 33,
            
                DescriptionKey = "TechStack_OpenTelemetry_Desc"
            },
            new()
            {
                Name = "Prometheus",
                WebsiteLink = "https://prometheus.io/",
            
                ImageAddress = "img/techStack/prometheus.png",
                ImageAltTextKey = "TechStack_Prometheus_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_Prometheus_Desc"
            },
            new()
            {
                Name = "Grafana",
                WebsiteLink = "https://grafana.com/",

                ImageAddress = "img/techStack/grafana.png",
                ImageAltTextKey = "TechStack_Grafana_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Grafana_Desc"
            },
            new()
            {
                Name = "Jaeger",
                WebsiteLink = "https://jaegertracing.io/",

                ImageAddress = "img/techStack/jaeger.png",
                ImageAltTextKey = "TechStack_Jaeger_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Jaeger_Desc"
            },
            new()
            {
                Name = "Seq",
                WebsiteLink = "https://datalust.co/",

                ImageAddress = "img/techStack/seq.png",
                ImageAltTextKey = "TechStack_Seq_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Seq_Desc"
            },
            new()
            {
                Name = "Lens",
                WebsiteLink = "https://lenshq.io/",

                ImageAddress = "img/techStack/lens.png",
                ImageAltTextKey = "TechStack_Lens_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Lens_Desc"
            },
            new()
            {
                Name = "Kubecolor",
                WebsiteLink = "https://kubecolor.github.io/",

                ImageAddress = "img/techStack/kubecolor.png",
                ImageAltTextKey = "TechStack_Kubecolor_Alt",
                ImageWidth = 32,
                ImageHeight = 32,

                DescriptionKey = "TechStack_Kubecolor_Desc"
            },
            new()
            {
                Name = "Elastic Search",
                WebsiteLink = "https://www.elastic.co/",
            
                ImageAddress = "img/techStack/elastic.png",
                ImageAltTextKey = "TechStack_Elasticsearch_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_Elasticsearch_Desc"
            },
            new()
            {
                Name = "Authentik",
                WebsiteLink = "https://goauthentik.io/",
            
                ImageAddress = "img/techStack/authentik.png",
                ImageAltTextKey = "TechStack_Authentik_Alt",
                ImageWidth = 32,
                ImageHeight = 32,
            
                DescriptionKey = "TechStack_Authentik_Desc"
            }
        };
    }
}