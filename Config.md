Add Ngnix:

This is not the right way:
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.1.1/deploy/static/provider/cloud/deploy.yaml
Check with that: kubectl get pods -n ingress-nginx
kubectl apply -f portfolio-ingress.yaml

### Check for ports:
kubectl get svc --all-namespaces -o json | jq -r '.items[] | select(.spec.type == "NodePort") | .spec.ports[] | .nodePort'
