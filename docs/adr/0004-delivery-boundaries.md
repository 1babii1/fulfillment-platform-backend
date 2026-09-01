# ADR 0004: Separate node bootstrap from application delivery

## Context

Kubernetes node configuration and application delivery have different ownership, cadence and failure modes. Combining them in a single imperative deployment script makes review and recovery harder.

## Decision

Use Ansible for generic k3s node bootstrap and use Kustomize-compatible manifests for the application layer. GitOps tooling can reconcile the application manifests after the cluster is available. VM lifecycle provisioning is deferred until it becomes a concrete operational requirement.

## Consequences

- Node bootstrap can be repeated without embedding application configuration.
- Application changes are reviewable independently of host configuration.
- The repository needs validation for both Ansible and Kubernetes assets.
- OpenTofu remains a future option for VM lifecycle, rather than duplicated tooling in the first iteration.
