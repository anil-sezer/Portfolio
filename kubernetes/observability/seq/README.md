Setup pv and pvc first. Then:

helm install seq datalust/seq -f helm-values.yml -n observability


helm uninstall seq