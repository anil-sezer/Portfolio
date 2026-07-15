# Made a DRY RUN then got the values file from the output, edited to my liking.
# https://kb.leaseweb.com/kb/kubernetes/kubernetes-deploying-a-docker-registry-on-kubernetes/
helm install -f helm-values.yml docker-registry --namespace images twuni/docker-registry --dry-run


Just apply all the files and the secret.

# For testing: curl http://192.168.1.120:30003/v2/_catalog
# For testing: curl https://imgregistry.anil-sezer.com/v2/_catalog


# listing image tags:
curl -u USERNAME https://imgregistry.anil-sezer.com/v2/IMAGE_NAME/tags/list

# listing manifests:
curl -s -u USERNAME \
-H "Accept: application/vnd.docker.distribution.manifest.list.v2+json" \
https://imgregistry.anil-sezer.com/v2/pg_dump/manifests/v18

curl -s -u USERNAME \
-H "Accept: application/vnd.docker.distribution.manifest.list.v2+json" \
https://imgregistry.anil-sezer.com/v2/IMAGE_NAME/manifests/TAG_NAME

