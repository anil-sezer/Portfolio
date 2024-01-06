## Graceful shutdown
k drain NODENAME --ignore-daemonsets --delete-emptydir-data
k cordon NODENAME

k uncordon NODENAME

## DB
Get superuser:
SELECT usename FROM pg_user WHERE usesuper = TRUE;

Terminate idle connections
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'postgres' AND state = 'idle';

Connections:
SELECT COUNT(*) FROM pg_stat_activity;
SELECT pid, usename, datname, state FROM pg_stat_activity;
SHOW max_connections;

# Adding ConfigMap
k apply -f terminate-idle-db-connections-query-config-map.yaml


# Debug via nikolaka
k apply -f netshoot-debug-pod.yaml
kubectl exec netshoot-debug-pod -- curl -k https://10.152.183.1:443


## Fail2Ban
### Check Fail2Ban's All Jails

for jail in $(sudo fail2ban-client status | grep "Jail list:" | sed -E 's/^[^:]+:[ \t]+//' | tr ',' ' '); do
echo "Jail: $jail"
sudo fail2ban-client status $jail | grep "Banned IP list:"
done

### Check its ban or unban logs:

sudo cat /var/log/fail2ban.log | grep "Ban "

## Migrations
dotnet ef migrations add MIGRATIONNAME --startup-project .\Portfolio.WebUi\ --project .\Portfolio.DataAccess\ --output-dir Migrations
dotnet ef database update --startup-project .\Portfolio.WebUi\

**One liner to build & deploy at the rpi:** <br>
todo: change this command to build from the root folder
website:
git pull && docker build -f ./deployment/Dockerfile -t anilsezer/portfolio . && docker push anilsezer/portfolio:latest && sleep 3 && k rollout restart deployment/portfolio-deployment

api: <br>
git pull && docker build -f ./Portfolio.Web.Api/Dockerfile . -t anilsezer/portfolio-api && docker push anilsezer/portfolio-api:latest && sleep 3 && k rollout restart deployment/portfolio-api-deployment

ip-lookup cron:
git pull && docker build -t anilsezer/iplookup-cron-go -f ./CronJobs/IpLookupCron/Dockerfile ./CronJobs/IpLookupCron/ && docker push anilsezer/iplookup-cron-go:latest && sleep 3 && k apply -f deployment/crons/ip-lookup-go-cronjob

### From Root:
docker build -f ./deployment/Dockerfile -t anilsezer/portfolio Portfolio.WebUi/.
docker run -p 8080:80 anilsezer/portfolio:latest

### Check for ports:
kubectl get svc --all-namespaces -o json | jq -r '.items[] | select(.spec.type == "NodePort") | .spec.ports[] | .nodePort'

#### Get Sqlite file:
kubectl cp portfolio-deployment-6c69c56f8-ct4qn:/app/WebApp.db ~/WebApp.db
getDb