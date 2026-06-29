# Architecture & Component Design

The project strictly follows **Clean Architecture** principles, decoupling business rules from external concerns.

```mermaid
graph TD
    classDef domain fill:#f9f,stroke:#333,stroke-width:2px;
    classDef app fill:#bbf,stroke:#333,stroke-width:2px;
    classDef infra fill:#fbb,stroke:#333,stroke-width:2px;
    classDef pres fill:#bfb,stroke:#333,stroke-width:2px;

    subgraph Presentation ["Presentation Layer (API)"]
        Endpoints[Endpoints: Auth, Account, Transaction]:::pres
        Program[Program.cs / Middleware Config]:::pres
    end

    subgraph Infrastructure ["Infrastructure Layer"]
        Repositories[InMemory Repositories]:::infra
        OutboxRepo[InMemoryOutboxRepository]:::infra
        OutboxProc[OutboxProcessor Background Worker]:::infra
        Messaging[InMemoryPubSubPublisher]:::infra
        Security[TokenGenerator]:::infra
        Telemetry[OpenTelemetry Tracing & Metrics]:::infra
    end

    subgraph Application ["Application Layer"]
        Commands[CQRS Commands]:::app
        Queries[CQRS Queries]:::app
        Behaviors[ValidationBehavior]:::app
        DTOs[Request / Response DTOs]:::app
    end

    subgraph Domain ["Domain Layer"]
        Models[Account, Transaction]:::domain
        OutboxMsg[OutboxMessage]:::domain
        Events[Completed Events]:::domain
        Exceptions[DomainException]:::domain
    end

    Program --> Endpoints
    Endpoints -. Sends Command/Query .-> Application
    Commands --> Behaviors
    Queries --> Behaviors
    Commands --> Repositories
    Queries --> Repositories
    Repositories --> Models
    
    %% Outbox execution flow
    Commands --> OutboxRepo
    OutboxRepo --> OutboxMsg
    OutboxProc -. Polls .-> OutboxRepo
    OutboxProc -. Publishes .-> Messaging
    Messaging -. Dispatches .-> Events
```

### Layer Breakdown

1. **[Domain](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain)**: Holds core enterprise models ([Account](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain/Models/Account.cs), [Transaction](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain/Models/Transaction.cs), [OutboxMessage](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain/Models/OutboxMessage.cs)), repository interfaces ([IAccountRepository](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain/Repositories/IAccountRepository.cs), [ITransactionRepository](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain/Repositories/ITransactionRepository.cs), [IOutboxRepository](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Domain/Repositories/IOutboxRepository.cs)), Domain Events, and Exceptions. Zero dependencies on outer layers.
2. **[Application](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application)**: Orchestrates business use cases using MediatR. Contains Queries ([GetAccountBalanceQuery](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application/Accounts/Queries/GetAccountBalanceQuery.cs), [GetAccountTransactionsQuery](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application/Transactions/Queries/GetAccountTransactionsQuery.cs)), Commands ([DepositCommand](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application/Transactions/Commands/DepositCommand.cs), [WithdrawalCommand](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application/Transactions/Commands/WithdrawalCommand.cs), [TransferCommand](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application/Transactions/Commands/TransferCommand.cs)), DTOs, and Validators, with cross-cutting validation handled by [ValidationBehavior](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Application/Behaviors/ValidationBehavior.cs).
3. **[Infrastructure](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure)**: Handles cross-cutting concerns, repository implementations ([InMemoryAccountRepository](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Repositories/InMemoryAccountRepository.cs), [InMemoryTransactionRepository](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Repositories/InMemoryTransactionRepository.cs), [InMemoryOutboxRepository](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Repositories/InMemoryOutboxRepository.cs)), the hosted [OutboxProcessor](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Outbox/OutboxProcessor.cs) background worker, token generator ([TokenGenerator](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Security/TokenGenerator.cs)), telemetry registration ([TelemetryServiceCollectionExtensions](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Telemetry/TelemetryServiceCollectionExtensions.cs)), application metrics collection ([ApplicationMetrics](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Telemetry/ApplicationMetrics.cs)), and messaging publishers ([InMemoryPubSubPublisher](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Messaging/InMemoryPubSubPublisher.cs)).
4. **Presentation (API)**: Exposes routes via Minimal API endpoints ([AccountEndpoints](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Endpoints/v1/AccountEndpoints.cs), [AuthEndpoints](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Endpoints/v1/AuthEndpoints.cs), [TransactionEndpoints](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Infrastructure/Endpoints/v1/TransactionEndpoints.cs)), configures the ASP.NET Core DI container and request/response middlewares in [Program.cs](file:///home/wt/Development/entrevistas/qt_bank/QtBank.Api/Program.cs).

---

## 🎨 API Design System

The API is architected around a robust design system for processing financial transactions. The following sequence diagram visualizes how incoming HTTP requests flow through the pipeline, enforcing correlation, validation, security, database transactional consistency (outbox), and observability recording.

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Middleware as Middleware Pipeline
    participant MediatR as MediatR Pipeline
    participant DB as Transactional DB (OLTP)
    participant Outbox as Outbox Store
    participant Broker as Message Broker (Pub/Sub)
    participant Metrics as Telemetry / Metrics

    Client->>Middleware: HTTP POST /api/v1/transactions/transfer {IdempotencyKey, JWT}
    activate Middleware
    Note over Middleware: 1. Generate/Capture X-Correlation-Id<br/>2. Validate & parse JWT SessionId
    Middleware->>MediatR: Send Command
    deactivate Middleware
    activate MediatR
    Note over MediatR: 3. FluentValidation check (Fail -> HTTP 400)<br/>4. Check Idempotency Key in Repository
    MediatR->>DB: Execute Transfer (Debit source / Credit dest)
    activate DB
    DB-->>MediatR: Account & Transaction states persisted
    deactivate DB
    MediatR->>Outbox: Serialize & save event as OutboxMessage
    activate Outbox
    Outbox-->>MediatR: OutboxMessage persisted
    deactivate Outbox
    Note over DB, Outbox: (Steps 3 & 4 occur within the same atomic transaction boundary)
    MediatR->>Metrics: RecordTransaction("Transfer", "Success", Amount)
    MediatR-->>Client: HTTP 202 Accepted (TransactionId)
    deactivate MediatR

    Note over Outbox, Broker: ASYNCHRONOUS BACKGROUND PROCESSOR
    loop Every 1 Second
        Outbox->>Outbox: Poll unprocessed messages
        Outbox->>Broker: Publish event (automatic retry + exponential backoff)
        Broker-->>Outbox: Event published successfully
        Outbox->>Outbox: Mark message as processed (set ProcessedOnUtc)
    end
```

### Core Design Patterns & Principles

1. **Protocol Semantics**: Clean usage of REST HTTP verbs and status codes:
   - `GET` for idempotent queries (`200 OK` or `404 Not Found`).
   - `POST` for command execution (`202 Accepted` for queued transactions).
   - `400 Bad Request` or `422 Unprocessable` for input schema and business validation errors.
2. **Atomic Transaction Boundary**: Ensures write consistency. Changes to `Account` and `Transaction` records are committed to the data store in the exact same transaction block as the `OutboxMessage` event queue entry.
3. **Vendor-Agnostic Metrics Contract**: Emits metrics using standard .NET `System.Diagnostics.Metrics` APIs, completely decoupling application codebase from vendor-specific libraries (e.g. Datadog, Prometheus, Azure Monitor).
4. **Resiliency Loop**: Worker executes retries with exponential backoff on transient network issues when delivering events to the message broker.


### Project Structure

Below is the directory tree layout of the solution:

```text
qt_bank/
├── QtBank.sln                 # .NET Solution File
├── Dockerfile                 # Containerization Config
├── docker-compose.yml         # Compose configuration
├── docs/                      # Sub-topic Documentation files
│   ├── architecture.md
│   ├── getting-started.md
│   ├── api-reference.md
│   ├── production-design.md
│   └── ai-retrospective.md
├── QtBank.Api/                # Main Web API Project
│   ├── Domain/                # Enterprise models & Repository Interfaces
│   ├── Application/           # CQRS MediatR Handlers & Business Logic
│   ├── Infrastructure/        # Telemetry, Security, Outbox, & Data stores
│   └── Program.cs             # API Startup & Pipelines Configuration
└── QtBank.Api.Tests/          # Unit & Integration Tests Suite
    ├── Domain/                # Enterprise models & Repository Interfaces Tests
    ├── Application/           # Command & Query Handler Tests
    └── Infrastructure/        # Observability, Outbox, & Middleware Tests
```