# Kubernetes review example

These manifests are static review material, not a deployable production environment. They deliberately omit a registry image, PostgreSQL topology, secrets, namespaces, and ingress configuration.

Validate the rendered manifests locally:

```bash
docker run --rm --volume "$PWD:/workspace:ro" --workdir /workspace \
  registry.k8s.io/kustomize/kustomize:v5.7.1 build infra/kubernetes
```

For a separately configured environment, verify a rollout and roll it back only through its approved change process:

```bash
kubectl rollout status deployment/fulfillment-api --namespace <namespace>
kubectl rollout undo deployment/fulfillment-api --namespace <namespace>
kubectl rollout status deployment/fulfillment-api --namespace <namespace>
```

After a rollback, check `/health/ready` through the environment's approved internal route and confirm that the previous image revision is running.
