# Docker

Local supporting services are defined in the repository-root `docker-compose.yml`.
It currently starts PostgreSQL 17 with a health check and a persistent named volume.

Run it with `docker compose up -d postgres`. Redis and SignalR remain out of scope
until their later phases.
