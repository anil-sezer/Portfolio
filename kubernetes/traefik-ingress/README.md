helm install traefik traefik/traefik \
-f helm-values.yml \
--namespace traefik \
--create-namespace \
--wait

helm upgrade traefik traefik/traefik \
-f helm-values.yml \
--namespace traefik \
--wait


helm uninstall traefik --namespace traefik