# Ansible example

`playbooks/bootstrap-demo-host.yml` is a generic bootstrap example. It does not contain an inventory, host addresses, credentials, clusters, or application secrets.

Supply your own inventory outside this repository and review the playbook before running it:

```bash
ansible-playbook -i /path/to/inventory.yml infra/ansible/playbooks/bootstrap-demo-host.yml --check
```
