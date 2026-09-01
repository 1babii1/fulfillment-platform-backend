# Fulfillment Platform Backend

A runnable .NET 10 backend sample showing a business-critical flow: reserve inventory, create an order, confirm payment, and publish a domain event.

It is small enough to review in one sitting, while keeping the boundaries and operational concerns expected in a production team.

## Why this is useful in an interview

- The core flow is executable end to end, not just diagrammed.
- Domain rules are tested independently from HTTP and infrastructure concerns.
- Integration tests exercise the real ASP.NET Core request pipeline.
- The project runs locally with .NET or Docker Compose.
- CI verifies tests, container build, workflow syntax, Kubernetes rendering, and Ansible linting.
- Infrastructure examples use secure defaults without exposing a real environment.

## What works today

```text
GET catalog → POST order → reserve stock → confirm payment → publish order-confirmed event
```

Orders and inventory are persisted in local PostgreSQL through EF Core migrations. Payment confirmation events and the payment gateway remain deterministic in-memory demo adapters, so the repository contains no provider credentials or deployment topology.

| Area | Included |
| --- | --- |
| Domain | Catalog, stock reservation, order lifecycle, result/error primitives |
| Application | Checkout compensation, PostgreSQL repositories, payment confirmation, event publishing |
| API | Minimal API endpoints, problem-details errors, `/health` |
| Quality | 51 unit and integration tests, compiler analysis, CI checks |
| Delivery | Multi-stage Docker build, PostgreSQL Compose environment, Kustomize, generic Ansible playbook |

## Quick start

### .NET SDK

```bash
cp .env.example .env
dotnet test FulfillmentPlatform.slnx
docker compose up postgres --detach
ConnectionStrings__FulfillmentDatabase='Host=localhost;Port=5432;Database=fulfillment;Username=fulfillment;Password=<your-local-password>' dotnet run --project src/Api/Fulfillment.Api --urls http://localhost:8080
```

In another terminal:

```bash
curl http://localhost:8080/health
curl http://localhost:8080/api/demo/catalog
```

### Docker Compose

```bash
cp .env.example .env
docker compose up --build
curl http://localhost:8080/health
```

Stop it with `docker compose down`. PostgreSQL data is retained in a local volume; use `docker compose down --volumes` to reset the demo database. Events are intentionally in-memory until the Outbox stage.

## API walkthrough

1. Read an item from `GET /api/demo/catalog`.
2. Create an order with `POST /api/demo/orders`.
3. Confirm it through `POST /api/demo/orders/{orderId}/confirm-payment`.
4. Read the order at `GET /api/demo/orders/{orderId}` and events at `GET /api/demo/events`.

[`DemoCheckoutFlowTests.cs`](tests/Api/Fulfillment.Api.IntegrationTests/DemoCheckoutFlowTests.cs) is the executable request/response example.

## Architecture and trade-offs

```text
Catalog.Domain ──┐
Orders.Domain ───┼──► Orders.Application ───► Fulfillment.Api
SharedKernel ────┘              │
                         Payments.Application
```

- `Catalog.Domain` owns stock quantities and reservation invariants.
- `Orders.Domain` owns valid transitions: `PendingPayment` → `Confirmed` or `Cancelled`.
- `Orders.Application` compensates earlier reservations if a later line fails.
- `Payments.Application` depends on a gateway abstraction; this repository provides a demo adapter.
- The API composes modules and translates expected failures into RFC 7807 problem details.

PostgreSQL persistence is implemented. Concurrency-safe reservations, transactional Outbox, idempotency, and observability remain planned evolution steps; see the [senior portfolio roadmap](docs/SENIOR_PORTFOLIO_ROADMAP.md) for their order and acceptance criteria.

See the [architecture overview](docs/architecture/overview.md) and [ADRs](docs/adr/README.md).

## Operational examples

[`infra`](infra/README.md) contains generic examples, not instructions for a real environment:

- [`infra/docker`](infra/docker/README.md) — multi-stage non-root container and Compose.
- [`infra/kubernetes`](infra/kubernetes) — probes, resource limits, restrictive security context, `NetworkPolicy`, Kustomize.
- [`infra/ansible`](infra/ansible/README.md) — inventory-free bootstrap example.

No credentials, host addresses, inventories, kubeconfig, or provider configuration are included.

## Repository map

```text
src/        Business modules and HTTP API
tests/      Unit and integration tests
docs/       Architecture overview and ADRs
infra/      Docker, Kubernetes and Ansible examples
.github/    Continuous integration
```

## Interview discussion

- Replacing in-memory reservations with concurrency-safe database updates.
- When transactional outbox and idempotency become necessary.
- Evolving the demo gateway into a real provider adapter.
- Selecting Kubernetes probes, resource limits, and scaling metrics for actual traffic.

## License

The license will be selected before the repository is made public.
