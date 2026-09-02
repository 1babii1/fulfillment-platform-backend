# ADR 0002: Use transactional Outbox and idempotent handlers

**Status:** Accepted — transactional Outbox is implemented in Stage 4; consumer idempotency remains Stage 5 work.

## Context

An order state transition may need to notify another module. Writing to a database and publishing to a broker are separate operations; a crash between them can leave the system inconsistent. Brokers and HTTP clients can also redeliver messages.

## Decision

Persist outgoing integration events in the same EF Core `SaveChanges` transaction as the state change, then publish them asynchronously from an Outbox. The local transport is intentionally in-memory and replaceable; it is not a broker claim.

The publisher locks one pending PostgreSQL row at a time with `FOR UPDATE SKIP LOCKED`, increments its attempt counter, and records either `processed_at` or `last_error`. This prevents two publisher instances from processing the same row concurrently. A failed row remains pending and is retried after a short delay.

Consumers record or enforce idempotency for messages that can be retried. That consumer-side guarantee is deliberately deferred to Stage 5.

HTTP endpoints already accept an operation-scoped `Idempotency-Key`. PostgreSQL stores a SHA-256 request fingerprint and the serialized response in the same transaction as the checkout or payment operation. An identical retry receives the original response; a changed request with the same key is rejected.

## Expected consequences

- The order confirmation and the durable intent to publish cannot be committed separately.
- A transport failure does not roll back the confirmed order or delete its pending Outbox record.
- Delivery is at-least-once and eventually consistent; downstream idempotency is still required before adding a real broker.
- Integration tests cover durable message creation and successful delivery; failure-path and consumer-idempotency coverage expands in Stage 5.
