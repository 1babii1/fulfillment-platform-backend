# ADR 0004: Separate node bootstrap from application delivery

**Status:** Proposed — generic examples exist; no live delivery environment is implemented.

## Context

Kubernetes node configuration and application delivery have different ownership, cadence and failure modes. Combining them in a single imperative deployment script makes review and recovery harder.

## Proposed decision

Use Ansible for generic node bootstrap and Kustomize-compatible manifests for the application layer if this demo gains a live cluster. GitOps reconciliation and VM lifecycle automation are deferred until they become concrete requirements.

## Expected consequences

- Generic bootstrap can remain separate from application configuration.
- Kubernetes and Ansible assets can be reviewed and statically validated without accessing a cluster.
- OpenTofu remains a future option for VM lifecycle rather than duplicated tooling in the first iteration.
