# Got these from:
# https://www.lorenzobettini.it/2023/07/my-ansible-role-for-oh-my-zsh-and-other-cli-programs/
# and https://shaunandersonaz.github.io/Ansible-To-Setup-OhMyZSH-and-PowerLevel10K/

# For kubernetes on raspberry pi:
# https://devops.datenkollektiv.de/raspberry-pi-5-a-kubernetes-cluster.html
# https://www.danielcolomb.com/2023/07/16/building-a-kubernetes-cluster-with-raspberry-pis-and-containerd/
# https://github.com/geerlingguy/raspberry-pi-dramble/tree/master

# Run this:
ansible-playbook -i inventory.yml verify-node-uniqueness.yml 
ansible-playbook -i inventory.yml prepare-core-components.yml 
ansible-playbook -i inventory.yml setup-kubernetes.yml 

# Dry run:
ansible-playbook -i inventory.yml setup-kubernetes.yml --check

# Ping:
ansible all -m ansible.builtin.ping -i inventory.yml

# Lint:
ansible-lint setup-kubernetes.yml

# Post install:

Remember to add cert manager via helm.

ADD NEW NODES TO THE COREDNS AFTER THEY ARE READY, check core-dns-config.yml for details.

TEST CGROUP

Run this:
kubectl apply --server-side=true -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.5.1/standard-install.yaml
Than apply traefik ingress controller via helm:
helm install traefik traefik/traefik -f ./traefik-ingress/helm-values.yml --wait

kubeadm config print init-defaults