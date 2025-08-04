namespace Portfolio.Ui.Models;

public class TechStackModel
{
    public required string Name { get; init; }
    public required string WebsiteLink { get; init; }
                
    public required string ImageAddress { get; init; }
    public required string ImageAltText { get; init; }
    public required int ImageWidth { get; init; }
    public required int ImageHeight { get; init; }
                
    public required string Description { get; init; }

    public static IReadOnlyList<TechStackModel> GetTechStack()
    {
        return new List<TechStackModel>
        {
            new()
            {
                Name = "Kubernetes",
                WebsiteLink = "https://kubernetes.io/",

                ImageAddress = "img/techStack/kubernetes.png",
                ImageAltText = "Kubernetes logo. Blue hexagonal that has a white ship helm inside. A minimalist brand design.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "Proudly self-hosted at my Raspberry Pi 5-4 cluster! Nodes are prepared and cluster deployed with a <strong>single Ansible command</strong>."
            },
            new()
            {
                Name = "Ansible",
                WebsiteLink = "https://www.ansible.com/",

                ImageAddress = "img/techStack/ansible.png",
                ImageAltText = "Ansible logo. Black filled circle with an odd A letter in it.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "I felt this Kubernetes setup would be incomplete without fully automating nodes. Also encourages experimentation since I can reset the lab rapidly if things break beyond repair."
            },
            new()
            {
                Name = "Docker",
                WebsiteLink = "https://www.docker.com/",

                ImageAddress = "img/techStack/docker.png",
                ImageAltText = "Docker logo. A cute blue smiling whale with blue containers on it.",
                ImageWidth = 45,
                ImageHeight = 27,

                Description = "Deployed with Docker images. All my images are at <a href=\"https://hub.docker.com/repositories/anilsezer\" target=\"_blank\">my DockerHub account</a>."
            },
            new()
            {
                Name = ".Net 9 & Blazor",
                WebsiteLink = "https://dotnet.microsoft.com/",

                ImageAddress = "img/techStack/dotnet.png",
                ImageAltText = ".NET logo. A purple circle that has .NET Core written in it.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "This project is created with Domain Driven Design in mind and uses latest .Net + Blazor features."
            },
            new()
            {
                Name = "Go",
                WebsiteLink = "https://go.dev/",

                ImageAddress = "img/techStack/go.png",
                ImageAltText = "Blue letters of 'GO'.",
                ImageWidth = 42,
                ImageHeight = 21,

                Description = "If writing a cron with just yaml is not practical, I write it with Go. For trying out other languages to widen my view."
            },
            new()
            {
                Name = "PostgreSQL",
                WebsiteLink = "https://www.postgresql.org/",

                ImageAddress = "img/techStack/postgres.png",
                ImageAltText = "PostgreSql logo. A blue elephant head with blue tusks. It's a minimalist logo.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "Current database. I like Its features and like to experiment with it."
            },
            new()
            {
                Name = "gRPC",
                WebsiteLink = "https://grpc.io/",

                ImageAddress = "img/techStack/grpc.png",
                ImageAltText = "gRPC logo. Just a undercase g and uppercase RPC letters. g has something arrow-ish on its top.",
                ImageWidth = 32,
                ImageHeight = 14,

                Description = "Wanted to try something other than REST, used gRPC and liked it."
            },
            // new()
            // {
            //     Name = "OpenTelemetry",
            //     WebsiteLink = "https://opentelemetry.io/",
            //
            //     ImageAddress = "img/techStack/opentelemetry.png",
            //     ImageAltText = "OpenTelemetry logo. A minimal, blue and orange colored telescope.",
            //     ImageWidth = 32,
            //     ImageHeight = 33,
            //
            //     Description = "Coming soon!"
            // },
            // new()
            // {
            //     Name = "Prometheus",
            //     WebsiteLink = "https://prometheus.io/",
            //
            //     ImageAddress = "img/techStack/prometheus.png",
            //     ImageAltText = "Prometheus logo. A nice torch.",
            //     ImageWidth = 32,
            //     ImageHeight = 32,
            //
            //     Description = "Coming soon!"
            // },
            new()
            {
                Name = "Grafana",
                WebsiteLink = "https://grafana.com/",

                ImageAddress = "img/techStack/grafana.png",
                ImageAltText = "Grafana logo. A spiral with wide spikes, has gradient color from orange to yellow.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "Using it to visualize my data. I like to play with it!"
            },
            new()
            {
                Name = "Bootstrap",
                WebsiteLink = "https://getbootstrap.com/",

                ImageAddress = "img/techStack/bootstrap.png",
                ImageAltText = "Bootstrap logo. A purple box with a B in it.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "Used it to create numerous projects including this and I like it, but after delivering multiple projects I'm gravitating towards other CSS frameworks like Tailwind CSS."
            },
            new()
            {
                Name = "Lens",
                WebsiteLink = "https://k8slens.dev/",

                ImageAddress = "img/techStack/lens.png",
                ImageAltText = "Lens logo. A minimalist design. It has white aperture blades in a light blue box.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "Using it to observe the cluster in a more graphical way. Thinking about trying K9s later. Especially for colored logs."
            },
            new()
            {
                Name = "Kubecolor",
                WebsiteLink = "https://kubecolor.github.io/",

                ImageAddress = "img/techStack/kubecolor.png",
                ImageAltText = "Kubernetes logo. A blue hexagonal that has a white ship helm inside. Helm is filled with various colors.",
                ImageWidth = 32,
                ImageHeight = 32,

                Description = "It's a must have. It colors all the kubectl output, including logs of the pods!"
            }
            // new()
            // {
            //     Name = "Elastic Search",
            //     WebsiteLink = "https://www.elastic.co/",
            //
            //     ImageAddress = "img/techStack/elastic.png",
            //     ImageAltText = "Elastic Search logo. Looks like colorful bubbles fused together.",
            //     ImageWidth = 32,
            //     ImageHeight = 32,
            //
            //     Description = "Coming soon!"
            // }
        };
    }
}