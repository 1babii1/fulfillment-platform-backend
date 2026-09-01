# ADR 0001: Use modular service boundaries

**Status:** Accepted

## Context

The implemented portfolio flow contains catalog, orders, payments, and shared primitives. It must remain runnable without coupling business rules to HTTP or temporary in-memory adapters.

## Decision

Organise the current code into explicit domain, application, API, and shared modules. Application modules depend on interfaces for inventory, orders, payment gateways, and event publication. Future persistence adapters remain outside domain projects.

## Consequences

- Business rules are easier to locate and test.
- External dependencies remain at the infrastructure edge.
- Cross-module workflows require explicit interfaces and coordination.
- Future persistence and event delivery can be added without moving domain rules into infrastructure projects.
