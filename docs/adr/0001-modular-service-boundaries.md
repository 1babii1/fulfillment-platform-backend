# ADR 0001: Use modular service boundaries

## Context

The platform contains identity, catalogue/orders, payments, notifications and file concerns. Their data ownership and failure modes differ, but the portfolio project must remain understandable and runnable.

## Decision

Organise the code into explicit modules with Domain, Core, Contracts, Infrastructure and Web boundaries. Each module owns its data and exposes contracts rather than allowing other modules to access its persistence layer directly.

## Consequences

- Business rules are easier to locate and test.
- External dependencies remain at the infrastructure edge.
- Cross-module workflows require contracts and asynchronous events, which adds coordination overhead.
- The local demo must provide clear startup and tracing documentation.
