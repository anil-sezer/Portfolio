Add Ngnix:

This is not the right way:
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.1.1/deploy/static/provider/cloud/deploy.yaml
Check with that: kubectl get pods -n ingress-nginx
kubectl apply -f portfolio-ingress.yaml

### Check for ports:
kubectl get svc --all-namespaces -o json | jq -r '.items[] | select(.spec.type == "NodePort") | .spec.ports[] | .nodePort'


### Enable Cert Manager:
microk8s enable helm3

microk8s helm3 repo add jetstack https://charts.jetstack.io
microk8s helm3 repo update
microk8s helm3 install \
cert-manager jetstack/cert-manager \
--namespace cert-manager \
--version v1.7.1 \
--create-namespace \
--set installCRDs=true

kubectl describe certificate anil-sezer-tls

