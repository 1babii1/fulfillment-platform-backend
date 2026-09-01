# ADR 0002: Use transactional Outbox and idempotent handlers

**Status:** Proposed — implementation is planned for roadmap Stages 4 and 5.

## Context

An order state transition may need to notify another module. Writing to a database and publishing to a broker are separate operations; a crash between them can leave the system inconsistent. Brokers and HTTP clients can also redeliver messages.

## Proposed decision

Persist outgoing integration events in the same transaction as the state change, then publish them asynchronously from an Outbox. Consumers record or enforce idempotency for messages that can be retried.

## Expected consequences

- The system tolerates transient broker or network failures without silently losing the event.
- At-least-once delivery becomes safe for supported flows.
- Event processing is eventually consistent and needs observable retry behaviour.
- Tests must cover duplicate delivery and failed publication paths.
