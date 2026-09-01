# Fulfillment Platform Backend

Production-style backend platform for catalog, order fulfilment, payments, notifications and file assets.

> **Status:** architecture and safe extraction in progress. The repository is intentionally built as an independent, runnable portfolio version rather than a copy of an existing deployment.

## What this project demonstrates

- Modular .NET services with clear domain and infrastructure boundaries.
- Reliable order processing with transactional Outbox and idempotent workflows.
- PostgreSQL, EF Core and integration tests for critical business flows.
- Containerised local development with Docker Compose.
- Kubernetes delivery with Kustomize and GitOps-oriented manifests.
- Cluster bootstrap practices using Ansible and k3s.
- Logs, metrics and traces with OpenTelemetry-oriented observability.

## Architecture

```text
Identity ──► Catalog + Orders ──► Payments / Shipping
    │                   │
    │                   └──► Domain events + Outbox ──► Notifications
    │
    └── Roles and permissions
```

The first end-to-end demo flow will cover user registration, inventory reservation, order creation, demo payment confirmation and notification delivery.

See the [architecture overview](docs/architecture/overview.md) for component, sequence and delivery diagrams.

## Planned components

| Component | Responsibility |
| --- | --- |
| Identity | Users, roles and permissions |
| Catalog + Orders | Products, stock reservations, carts, orders and returns |
| Payments | Provider-agnostic payment workflow with a demo gateway |
| Notifications | Event-driven delivery through a demo sender |
| Files | Provider-agnostic file storage with a local adapter |
| Shared | Contracts, domain events, messaging, observability and persistence primitives |

## Repository layout

```text
src/        Application services and shared libraries
tests/      Unit and integration tests
docs/       Architecture records and diagrams
infra/      Docker, Kubernetes and Ansible examples
```

## Engineering decisions

Architecture decisions will be documented in [`docs/adr`](docs/adr/README.md). The initial topics are modular boundaries, transactional Outbox, idempotency, provider abstractions, Kubernetes delivery and Ansible-based k3s bootstrap.

## Roadmap

1. Extract and sanitise domain and application layers.
2. Deliver the first local end-to-end demo flow.
3. Add automated quality gates, Kubernetes manifests and Ansible examples.
4. Publish architecture diagrams and decision records.

## License

The license will be selected before the repository becomes public.
