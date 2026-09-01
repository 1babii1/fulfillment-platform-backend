# Senior Portfolio Improvement Roadmap

## Objective

Turn the current runnable demo into a focused senior-level backend case study. The project must demonstrate correctness under concurrency and failure, honest architectural documentation, durable state transitions, observable operations, and a professional pull-request workflow.

The goal is depth in one order-fulfilment flow, not a larger list of unfinished services or technologies.

## Current baseline

Already implemented and verified:

- .NET 10 modular backend with catalog, orders, payments, and shared primitives;
- runnable HTTP flow from catalog lookup to order confirmation and event publication;
- 51 passing unit and integration tests;
- multi-stage non-root Docker image and local Compose environment;
- CI checks for .NET, container build, GitHub Actions, Kustomize, and Ansible;
- generic Kubernetes and Ansible examples without production data;
- architecture overview, ADRs, and reviewer-oriented README.

Main gap: persistence, Outbox, idempotency, brokers, and observability are described as architectural decisions but are not yet implemented. Documentation must distinguish the current system from its planned evolution.

## Working rules

1. Implement complete vertical increments: behavior, persistence, tests, documentation, and CI together.
2. Every new stage gets a GitHub Issue, feature branch, and pull request. Do not rewrite the existing history.
3. Commit messages keep the format `type(scope): description (#issue)`.
4. Never add real hosts, inventories, credentials, provider endpoints, or production topology.
5. Do not claim a capability in README or diagrams until an executable test proves it.
6. Prefer one convincing implementation over several infrastructure placeholders.
7. Every stage must keep `dotnet test FulfillmentPlatform.slnx` and the CI workflow green.

## Stage 1 — Align documentation with reality

**Suggested issue:** `Make architecture documentation implementation-aware`

**Goal:** remove the current credibility gap before adding features.

Deliverables:

- split the architecture overview into `Implemented now` and `Planned evolution`;
- redraw the current component and request-flow diagrams to match the actual modules;
- mark Outbox and idempotency ADRs as `Proposed` until implemented;
- record explicit non-goals: authentication, shipping, notifications, files, multi-service deployment, and HPA;
- add a small capability-status table linking each implemented claim to code or tests.

Acceptance criteria:

- no current-state diagram contains PostgreSQL, a message broker, Identity, Files, Notifications, Argo CD, or tracing;
- all future components are visibly labelled `planned`;
- README, architecture overview, and ADR status do not contradict each other;
- documentation links resolve and CI remains green.

**Team-lead signal:** technical honesty and control of scope.

## Stage 2 — Add durable PostgreSQL persistence

**Suggested issue:** `Persist orders and inventory with PostgreSQL and EF Core`

**Goal:** replace the in-memory state boundary with a realistic persistence adapter while preserving domain isolation.

Deliverables:

- add EF Core and Npgsql infrastructure projects;
- define order, order-line, inventory, and migration mappings outside domain entities;
- introduce repository implementations behind existing interfaces;
- add PostgreSQL to Compose with a health check and persistent volume;
- use `.env.example` only if a public configuration contract becomes necessary;
- add Testcontainers-based persistence integration tests.

Acceptance criteria:

- orders survive an API container restart;
- migrations create a clean database from zero;
- domain projects have no dependency on EF Core;
- tests run against a real PostgreSQL container, not the EF in-memory provider;
- logs and exceptions never expose connection credentials.

**Team-lead signal:** persistence boundaries, migrations, and realistic testing.

## Stage 3 — Guarantee inventory correctness under concurrency

**Suggested issue:** `Make stock reservation concurrency-safe`

**Goal:** prove that simultaneous checkout requests cannot oversell inventory.

Deliverables:

- implement an atomic conditional reservation update or an explicitly documented locking strategy;
- persist reservation identity and state rather than only aggregate counters;
- define release, commit, expiry, and duplicate-operation behavior;
- add concurrent integration tests with more demand than available stock;
- document the selected isolation and locking trade-offs.

Acceptance criteria:

- a concurrency test sends parallel reservations and never produces negative availability or excess reservations;
- retries do not reserve the same request twice;
- failed multi-line checkout releases or rolls back all partial work;
- the chosen database mechanism is explained in an ADR.

**Team-lead signal:** understanding of race conditions and database guarantees.

## Stage 4 — Implement transactional Outbox

**Suggested issue:** `Publish order events through a transactional Outbox`

**Goal:** make the order state change and event creation atomic.

Deliverables:

- store Outbox messages in the same transaction as the aggregate change;
- add a background publisher with bounded batches, retries, and cancellation support;
- use a local demo transport abstraction before introducing a real broker;
- record processing attempts and completion timestamps;
- change ADR 0002 from `Proposed` to `Accepted` only after verification.

Acceptance criteria:

- a simulated publication failure leaves a pending Outbox record without losing the order change;
- retry publishes the pending record and marks it processed;
- the same message is not published concurrently by two workers;
- transaction and failure-path integration tests pass with PostgreSQL.

**Team-lead signal:** reliable distributed workflow design grounded in working code.

## Stage 5 — Add request and consumer idempotency

**Suggested issue:** `Make checkout and payment confirmation idempotent`

**Goal:** make client retries and repeated event delivery safe.

Deliverables:

- accept an idempotency key for order creation and payment confirmation;
- store key, operation, request fingerprint, result reference, and status;
- return the original result for an identical retry;
- reject reuse of a key with a different request;
- add a deduplication contract for Outbox consumers;
- define retention and cleanup policy.

Acceptance criteria:

- concurrent requests with the same key create one order and one payment attempt;
- an altered payload with the same key returns a conflict response;
- repeated event delivery changes state once;
- integration tests cover retries before, during, and after completion.

**Team-lead signal:** API reliability and at-least-once delivery semantics.

## Stage 6 — Add operational visibility

**Suggested issue:** `Add OpenTelemetry and dependency health checks`

**Goal:** make failures diagnosable without pretending that monitoring alone creates reliability.

Deliverables:

- add structured logs with correlation, order, and idempotency identifiers;
- instrument HTTP, PostgreSQL, checkout, payment, and Outbox processing;
- expose separate liveness and readiness endpoints;
- make readiness depend on PostgreSQL and required background components;
- provide an optional local OpenTelemetry collector configuration;
- document useful signals and example failure investigation.

Acceptance criteria:

- traces connect HTTP checkout to database and Outbox operations;
- logs contain identifiers but no request bodies, secrets, or personal data;
- liveness stays healthy during a database outage while readiness becomes unhealthy;
- integration tests verify health-state behavior.

**Team-lead signal:** production diagnosis and correct health semantics.

## Stage 7 — Harden delivery evidence

**Suggested issue:** `Strengthen CI and Kubernetes validation`

**Goal:** make infrastructure examples verifiable rather than decorative.

Deliverables:

- validate Kubernetes schemas with a pinned `kubeconform` version;
- scan the built image and repository dependencies with pinned tools;
- test the running container through its health endpoint in CI;
- add a PodDisruptionBudget and a documented rolling-update/rollback procedure;
- keep HPA out until a measured workload and justified metric exist;
- expand Ansible only if it performs a real repeatable bootstrap responsibility.

Acceptance criteria:

- malformed or insecure manifests fail CI;
- the CI smoke test starts the built container and calls a public API endpoint;
- rollback and verification commands are documented;
- no workflow requires deployment credentials or mutates external infrastructure.

**Team-lead signal:** operational discipline and avoidance of infrastructure theatre.

## Stage 8 — Final portfolio release

**Suggested issue:** `Publish the senior backend case study`

**Goal:** make the repository easy to evaluate in ten minutes and defensible in a deep interview.

Deliverables:

- select and add a license;
- add repository description, topics, and CI badge;
- update README with measured test count and a tested checkout example;
- add a `docs/interview-guide.md` covering trade-offs, known limits, and evolution choices;
- create a versioned release after all checks pass;
- pin the repository on the GitHub profile.

Acceptance criteria:

- a new reviewer can clone, start, and complete checkout using documented commands;
- every architectural claim links to implementation, an executable test, or a clearly labelled future section;
- the default branch is green and protected;
- there are no unfinished placeholder sections, real secrets, or private topology.

**Team-lead signal:** clear communication, evidence, and ownership of trade-offs.

## Recommended execution order

```text
Stage 1 documentation truth
    ↓
Stage 2 PostgreSQL persistence
    ↓
Stage 3 concurrency-safe inventory
    ↓
Stage 4 transactional Outbox
    ↓
Stage 5 idempotency
    ↓
Stage 6 observability
    ↓
Stage 7 delivery hardening
    ↓
Stage 8 portfolio release
```

Stages 2–5 form the core senior backend proof. Stages 6–8 strengthen operations and presentation but must not delay correctness work.

## Definition of done for the roadmap

The project is ready to present as a senior-level backend case study when:

- documentation and implementation describe the same system;
- PostgreSQL persists the complete checkout state;
- inventory correctness is proven under concurrency;
- state changes and outgoing events use a transactional Outbox;
- repeated HTTP requests and event deliveries are safe;
- integration tests cover successful, duplicate, concurrent, and failure paths;
- health, logs, and traces explain runtime behavior;
- CI validates code, container runtime, and infrastructure assets;
- the repository can be discussed honestly without relying on private production code.
