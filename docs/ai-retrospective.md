# AI Retrospective

## 1. AI Tools Used
*   **Antigravity (Advanced Agentic Coding AI)**: Operated as the primary pair-programming agent, writing code, executing the test suite, maintaining code coverage, and building comprehensive project documentation. Powered by **Gemini 3.5 Flash**.

---

## 2. Prompts Yielding the Best Architectural Results
* **Skill checks and guidelines**: Reviewing the pre-installed `solid`, `dotnet-architecture`, `csharp-developer` guidelines. Ensuring that namespaces are file-scoped, using primary constructors where appropriate, mapping domain entities strictly to DTOs (`AccountDto`), and enforcing non-negative starting balances.
* **CQRS Separation**: Structuring command inputs (`CreateAccountCommand`, `UpdateAccountCommand`, etc.) separately from the HTTP request payloads (e.g., `UpdateAccountRequest`) to handle path-parameter and body binding correctly in Minimal APIs.
* **Validation and Reorganization**: Reorganizing the project folders into context-based vertical slices (`Accounts`, `Transactions`), versioning endpoints explicitly (`v1/`), configuring pipeline behaviors with FluentValidation, and managing HTTP exceptions via custom validation middleware.
* `"Please write meeting the following requirements: 1. Capture/Generation Middleware: Create a middleware (CorrelationIdMiddleware) that intercepts the HTTP request. It should check for the X-Correlation-Id header. If the header does not exist, it should generate a new GUID. 2. Log Enrichment: The middleware should add this Correlation ID to the log scope (using ILogger.BeginScope or the recommended approach for Serilog, if you are using it), ensuring that all logs for that request are tagged with it. 3. Inclusion in the Response: The middleware must inject the X-Correlation-Id into the response headers (HttpResponse.Headers) so that the client knows which ID was processed. 4. Passing to External Systems: Create a DelegatingHandler (e.g., CorrelationIdDelegatingHandler) to be used alongside the IHttpClientFactory. It should retrieve the Correlation ID from the current request (via IHttpContextAccessor) and automatically inject it into the headers of any outgoing requests that my API makes to other microservices. 5. Configuration: Provide the exact code for registering all these components (Middleware, HttpContextAccessor, HttpClients, and Handlers) in the Program.cs file."` Guided the implementation of middleware and delegating handler for correlation id and logging.
*   `"change all transactions endpoints to request IdempotencyKey"`: Guided the implementation of idempotency checks across the transaction command pipeline.
*   `"refact DTOs with sufix Request or Response"`: Directed the renaming and separation of concerns for API contracts.
*   `"check if is possible use Result into the accounts endpoints"`: Led to the refactoring of account lookup queries to use the `Result<T>` pattern, unifying error signatures across endpoints.
*   `"Apply the proposed changes"`: Initiated the creation of the `OutboxMessage` entity, `InMemoryOutboxRepository`, and `OutboxProcessor` background service.
*   `"Choose the best option and apply"`: Led to the options binding pattern (`OutboxOptions`) in DI configuration to support `.env` and `appsettings.json` override capabilities.
*   `"Apply the proposed changes but do not use vendor names in the files and also create everything in english"`: Guided the implementation of vendor-agnostic OpenTelemetry metrics using `.NET 8` native APIs with a Console exporter.
*   `"Include QtBank.Api/Infrastructure/Telemetry/ApplicationMetrics.cs all transactions endpoints"`: Extended custom transaction metrics collection across Transfer, Deposit, and Withdrawal endpoints.
*   `"change the endpoint that returns transactions by account to return transactions ordered by transaction date from the most recent to the least recent"`: Sorted transaction queries descending by creation date.
*   `"update in readme AI section"`: Directed the capturing of recent tool setups, prompts list, and architectural decisions.
*   `"update readme file explain about metrics endpoint and give some examples how it works"`: Documented local Console output, production `/metrics` route setup guide, and added metrics text format examples.
*   `"update docs/api-reference.md with examples of fail"`: Added request/response validation and business logic failure response examples to the API Reference file.
*   `"Into architecture create a section ### Project Structure"`: Added project directory structure overview to the Architecture file.

---

## 3. AI Corrections & Critical Judgments

### What did the AI get wrong?

During the development process, the AI made several incorrect assumptions and architectural slips that required manual intervention or correction:
- **Dependency Lifetime Violations**: Attempted to inject scoped/transient repositories directly into the singleton background service constructor (`OutboxProcessor`), violating DI scope boundaries.
- **Clean Architecture Boundary Leakage**: Mixed request and response DTOs under the Infrastructure endpoints namespace, leading to Application layer use-cases depending on outer Infrastructure namespaces.
- **JSON Deserialization Overload Conflicts**: Created overloaded constructors for C# `record` classes without annotating the primary constructor with `[JsonConstructor]`, breaking System.Text.Json dynamic deserialization.
- **Static vs Dynamic Method Dispatching**: Attempted to call the generic `IPubSubPublisher.PublishAsync<T>()` using a runtime-loaded type directly as a generic parameter without utilizing .NET reflection (`MakeGenericMethod`).

---

### Detailed Corrections Log

During the development process, the following AI issues were encountered and manually corrected:

1.  **System.Text.Json Record Deserialization (CS7036 / NotSupportedException)**:
    *   *AI Action*: The AI introduced overloaded constructors for `TransferRequest`, `DepositRequest`, and `WithdrawalRequest` DTOs to default the `IdempotencyKey` for test suite backward compatibility.
    *   *Issue*: System.Text.Json failed to deserialize incoming JSON payloads because the record had multiple constructors, and the primary constructor was not decorated.
    *   *Correction*: Manually forced the addition of the `[method: JsonConstructor]` attribute target on the record declarations to guide the deserializer.
2.  **Clean Architecture Namespace Boundary Violation**:
    *   *AI Action*: The AI placed all Request and Response DTOs in the `Infrastructure/Endpoints/v1/DTOs` folder.
    *   *Issue*: This forced the core Application query handlers (`GetAccountBalanceQueryHandler`, etc.) to import and depend on the outer Infrastructure namespace, violating the dependency rule of Clean Architecture.
    *   *Correction*: Manually split the DTO types: kept `Request` records in the Infrastructure layer (as they represent HTTP presentation contracts) and kept `Response` records in the Application layer (as they represent core use-case outputs).
3.  **Integration Test State Isolation**:
    *   *AI Action*: Integration tests executed against a shared in-memory database where balances mutated.
    *   *Issue*: Running tests sequentially caused assertions to fail depending on execution order (e.g., balance was not matching the hardcoded expectations).
    *   *Correction*: Manually introduced a setup/cleanup block in `AccountEndpointsTests.cs` to reset test account balances before each run, ensuring state isolation and test reproducibility.
4.  **Legacy REST Playground Endpoints**:
    *   *AI Action*: The REST client playground file (`QtBank.Api.http`) contained legacy endpoints (`/api/auth/login`, `/api/weatherforecast`) from a previous project template.
    *   *Issue*: Running these requests caused `404 Not Found` errors because they did not exist in the Clean Architecture implementation.
    *   *Correction*: Aligned the HTTP playground with actual v1 API endpoint schemas, creating mock payloads using realistic UUIDs and active accounts.
5.  **Local dotnet CLI Dependency Exception**:
    *   *AI Action*: Attempted to execute unit/integration tests directly using standard host command shells.
    *   *Issue*: Host terminal returned command not found errors because the dotnet CLI was missing.
    *   *Correction*: Added a containerized testing alternative to the documentation (`docker run --rm -v "$(pwd):/app" -w /app mcr.microsoft.com/dotnet/sdk:8.0 dotnet test`) to enable seamless local execution.
6.  **Dynamic Generic Type Resolution for Outbox Deserialization**:
    *   *AI Action*: The AI designed the `OutboxProcessor` to automatically poll and process messages of any type dynamically.
    *   *Issue*: C# `IPubSubPublisher` specifies a generic `PublishAsync<T>` method which could not be called directly with a variable of type `object` at compile time.
    *   *Correction*: Utilized .NET Reflection (`GetMethod` and `MakeGenericMethod`) at runtime to dynamically bind the correct generic type argument and successfully invoke the method asynchronously.
7.  **Scoped Services in Hosted Background Service**:
    *   *AI Action*: The AI initially attempted to inject the scoped/transient repositories directly into the singleton `OutboxProcessor` background service constructor.
    *   *Issue*: Injecting scoped services into a singleton hosted service throws a dependency injection validation error at startup.
    *   *Correction*: Injected `IServiceScopeFactory` into the hosted service constructor instead, creating a custom scope dynamically within each polling loop execution to resolve transient/scoped repository instances.
8.  **Generic Dynamic Method Dispatching for Pub/Sub Integration Events**:
    *   *AI Action*: Solved a C# generic method constraint challenge where `IPubSubPublisher.PublishAsync<T>` had to be dynamically invoked at runtime based on the event's serialized string Type.
    *   *Correction*: Resolved this by retrieving `MethodInfo` via reflection and generating a concrete generic method at runtime using `MakeGenericMethod(type)`.
9.  **Loose Coupling of Metrics Instrumentation (Vendor Independence)**:
    *   *AI Action*: Kept telemetry and metrics decoupled from specific vendor SDKs by utilizing the standard .NET `System.Diagnostics.Metrics` API in `ApplicationMetrics.cs`, leaving the exporter logic solely inside the infrastructure configuration block.