# ADR 0003: Isolate providers behind interfaces and use demo adapters

## Context

Payment, shipping, email and object storage are external concerns. The public portfolio repository must not depend on real provider accounts, endpoints or credentials.

## Decision

Keep provider-neutral interfaces in the application boundary and implement local demo adapters for the runnable scenario. Production-specific adapters are not included in this repository.

## Consequences

- The demo starts without external credentials.
- Domain logic can be tested through deterministic fakes.
- Adapter contracts must model failures and idempotency explicitly.
- A real integration can be added later without changing domain use cases.
