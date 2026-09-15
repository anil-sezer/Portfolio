Welcome to my portfolios repository! You can check the entire code here, or you can visit the website at: https://www.anil-sezer.com/
---

Made with love, by using **Domain Driven Design** from [Eric Evans!](https://ddd.academy/eric-evans/)

## Tech Stack:

|      ![kubernetes.png](Portfolio.Ui/wwwroot/img/techStack/kubernetes.png) <br/> **Kubernetes**       |       ![ansible.png](Portfolio.Ui/wwwroot/img/techStack/ansible.png) <br/> **Ansible**       |      ![docker.png](Portfolio.Ui/wwwroot/img/techStack/docker.png) <br/> **Docker**       |
|:----------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------:|:----------------------------------------------------------------------------------------:|
|        ![dotnet.png](Portfolio.Ui/wwwroot/img/techStack/dotnet.png) <br/> **.NET 9 & Blazor**        |              ![go.png](Portfolio.Ui/wwwroot/img/techStack/go.png) <br/> **Go**               |  ![postgres.png](Portfolio.Ui/wwwroot/img/techStack/postgres.png) <br/> **PostgreSQL**   |
|  ![opentelemetry.png](Portfolio.Ui/wwwroot/img/techStack/opentelemetry.png) <br/> **OpenTelemetry**  |  ![prometheus.png](Portfolio.Ui/wwwroot/img/techStack/prometheus.png) <br/> **Prometheus**   |     ![grafana.png](Portfolio.Ui/wwwroot/img/techStack/grafana.png) <br/> **Grafana**     |

1. `Kubernetes:` After setting this bare metal cluster by myself, I learnt that it is called: [Kubernetes The Hard Way](https://github.com/kelseyhightower/kubernetes-the-hard-way). Nodes are prepared and cluster deployed with a single Ansible command.
2. `Ansible:` I felt this Kubernetes setup would be incomplete without fully automating nodes. Also encourages experimentation since I can reset the lab rapidly if things break beyond repair
3. `Docker:` Deployed with Docker images. All images are stored in my self hosted private image registry
4. `.Net 10 & Blazor:` This website is written with .Net 10 & Blazor
5. `Go:` If writing a cron with just yaml is not practical, I write it with Go. For trying out other languages to widen my view
6. `PostgreSQL:` Current database. I like It's features and like to experiment with it
7. `OpenTelemetry:` Traces are a must
8. `Prometheus:` Using it for 4+ years but I wanna sink more time specifically into it
9. `Grafana:` This cluster wouldn't be complete without mesmerizing dashboards of Grafana

Map is chosen from this website: https://leaflet-extras.github.io/leaflet-providers/preview/



Setting up and correctly managing a k8s cluster on bare metal was hard. But the experience was refreshing:

![It's a joke about struggles I had with the cluster](we-thought-it-would-be-easy.webp)
