# ADR 0003: Isolate providers behind interfaces and use demo adapters

**Status:** Accepted for the payment flow; future providers are out of scope.

## Context

The implemented payment flow must not depend on a real provider account, endpoint, or credential. Shipping, email, and object storage are not implemented in this portfolio version.

## Decision

Keep `IPaymentGateway` in the application boundary and implement a local demo adapter for the runnable scenario. Production-specific adapters are not included in this repository.

## Consequences

- The demo starts without external credentials.
- Domain logic can be tested through deterministic fakes.
- A future production adapter must model failures and idempotency explicitly.
- A real integration can be added later without changing domain use cases.
