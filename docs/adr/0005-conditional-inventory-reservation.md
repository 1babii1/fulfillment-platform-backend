# ADR 0005: Reserve inventory with conditional PostgreSQL updates

**Status:** Accepted

## Context

Two API instances can read the same available stock before either writes its reservation. A read-modify-write implementation can therefore accept more orders than the available quantity.

## Decision

Reserve stock with one parameterized PostgreSQL statement:

```sql
UPDATE inventory_items
SET reserved = reserved + @quantity
WHERE variant_id = @variant_id
  AND on_hand - reserved >= @quantity;
```

The affected row count is the reservation result. One changed row means success; zero rows means insufficient inventory or an unknown variant, which the adapter distinguishes with a follow-up lookup. Release uses the symmetric conditional decrement.

## Consequences

- Availability validation and counter mutation happen atomically in the database.
- The primary-key lookup on `variant_id` supports the conditional update; no additional index is needed for this access path.
- Parallel requests cannot reserve more than `on_hand` through this adapter.
- The adapter does not yet persist a reservation identity, expiry, or idempotency key; those are later roadmap concerns.
- Multi-line checkout still compensates previously reserved lines when a later reservation fails.
