#### Does not work:
docker buildx build --platform linux/arm64 -f ./Dockerfile -t anilsezer/portfolio ..



#### Push the image:
docker build -f ./deployment/Dockerfile -t anilsezer/portfolio Portfolio.WebUi/.
docker login
docker push anilsezer/portfolio:latest

**One liner:**
docker build -f ./deployment/Dockerfile -t anilsezer/portfolio . && docker push anilsezer/portfolio:latest

docker build -f ./Dockerfile -t anilsezer/portfolio ..

No cache: --no-cache
todo: Add dockerignore file, and add daily bing image to it. Also add bin and obj folders? Reduce the image size!


### From Root:
docker build -f ./deployment/Dockerfile -t anilsezer/portfolio Portfolio.WebUi/.
docker run -p 8080:80 anilsezer/portfolio:latest


Search all namespaces: 
k get ingress --all-namespaces
k get letsencrypt-prod --all-namespaces

k get namespaces


### Ngnix:
k get pods -n ingress-nginx
kubectl logs -n ingress-nginx ingress-nginx-controller-f7f5995cc-4vnmz

### Get app logs:
kubectl logs portfolio-deployment-7864bb7676-4lhvq

kubectl describe certificaterequests.cert-manager.io anil-sezer-tls-qwp7c -n default


### Etc:
kubectl get orders.acme.cert-manager.io -n default
kubectl describe orders.acme.cert-manager.io anil-sezer-tls-qwp7c-743662298 -n default

kubectl get challenges.acme.cert-manager.io -n default
kubectl describe challenges.acme.cert-manager.io anil-sezer-tls-qwp7c-743662298-579262875 -n default

kubectl describe ingress [your-ingress-name] -n default

kubectl get configmap -n ingress