# Made a DRY RUN then got the values file from the output, edited to my liking.
# https://kb.leaseweb.com/kb/kubernetes/kubernetes-deploying-a-docker-registry-on-kubernetes/
helm install -f helm-values.yml docker-registry --namespace images twuni/docker-registry --dry-run


Just apply all the files and the secret.

# For testing: curl http://192.168.1.120:30003/v2/_catalog
# For testing: curl https://imgregistry.anil-sezer.com/v2/_catalog

