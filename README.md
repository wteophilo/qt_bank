# QtBank API

This REST API was built for practical testing by Questrade Financial Group using **.NET 8**, **Clean Architecture**, and **Minimal APIs** to perform account balance lookups and execute idempotent banking transactions (Deposits, Withdrawals, Transfers).

---

## 🌟 Key Features

- **Clean Architecture & CQRS**: Strict isolation of Domain, Application, Infrastructure, and Presentation layers, mediated by MediatR commands and queries.
- **Idempotency**: Protects bank accounts from double-charging using client-generated UUID `IdempotencyKey` headers/payloads.
- **Observability & Metrics**: Configure full observability telemetry using OpenTelemetry, capturing request correlation IDs, trace spans (inbound & outbound), and key application & runtime metrics (CPU, Memory, Garbage Collection) ready for APM integration (Jaeger, Zipkin, Prometheus, Datadog).
- **Validation Pipeline**: FluentValidation integrated directly into the MediatR pipeline using custom behaviors.
- **JWT Authentication**: Secure Bearer tokens protect critical operations (including the `SessionId` claim mapping).
- **Transactional Outbox**: Ensures eventual consistency and reliable downstream event publishing (e.g. to Apache Kafka or RabbitMQ) by saving events within the database transaction boundary, processed asynchronously by a background processor with exponential backoff retries.
- **Comprehensive Testing**: 99.6% line coverage with robust unit and integration tests.

---

## 📚 Documentation Index

To make the codebase easy to navigate, the documentation has been split into smaller, dedicated files:

1. **[Architecture & Component Design](docs/architecture.md)**: Details the Clean Architecture layer breakdown, project file structure, and contains the sequence design system for incoming HTTP requests.
2. **[Technology Stack & Getting Started](docs/getting-started.md)**: Guides the reader through local prerequisites, building and running the API using Docker Compose, and running the test suite with code coverage.
3. **[API Endpoint Reference](docs/api-reference.md)**: Lists all public and protected HTTP endpoints with detailed request and response payload schemas.
4. **[Path to Production & System Design](docs/production-design.md)**: Explains the SRE/APM strategy (SLIs/SLOs, logging, tracing), idempotency handling for mobile apps, and downstream analytics integration via event-driven outbox streaming.
5. **[AI Retrospective](docs/ai-retrospective.md)**: A history log of prompts and AI corrections encountered during development.