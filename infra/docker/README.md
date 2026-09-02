# Local container environment

The Compose environment runs the self-contained demo API only. It uses in-memory data intentionally, so it requires no credentials, cloud services, or production configuration.

## Run

```bash
docker compose up --build
```

The API is available at `http://localhost:8080`. Verify the running container with:

```bash
curl --fail http://localhost:8080/health/ready
curl http://localhost:8080/api/demo/catalog
```

## Stop and reset

```bash
docker compose down
```

There are no persistent volumes in this demo. Restarting the container resets its sample catalog, orders, and events.
