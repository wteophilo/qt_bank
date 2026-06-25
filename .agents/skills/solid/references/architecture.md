# Software Architecture

## The Goal of Architecture

Enable the development team to:
1. **Add** features with minimal friction
2. **Change** existing features safely
3. **Remove** features cleanly
4. **Test** features in isolation
5. **Deploy** independently when possible

## Architectural Principles

### 1. Vertical Boundaries (Features/Slices)

Organize by **feature**, not by technical layer.

```
BAD: Layer-first
src/
  controllers/
    UserController.cs
    OrderController.cs
  services/
    UserService.cs
    OrderService.cs
  repositories/
    UserRepository.cs
    OrderRepository.cs

GOOD: Feature-first
src/
  users/
    UserController.cs
    UserService.cs
    UserRepository.cs
  orders/
    OrderController.cs
    OrderService.cs
    OrderRepository.cs
```

**Why:** Changes to "users" feature stay in `users/`. High cohesion within features.

### 2. Horizontal Boundaries (Layers)

Separate concerns into layers with clear dependencies.

```
┌──────────────────────────────────────┐
│           Presentation               │  UI, Controllers, CLI
├──────────────────────────────────────┤
│           Application                │  Use Cases, Orchestration
├──────────────────────────────────────┤
│             Domain                   │  Business Logic, Entities
├──────────────────────────────────────┤
│          Infrastructure              │  Database, APIs, External
└──────────────────────────────────────┘
```

### 3. The Dependency Rule

**Dependencies point INWARD.**

```
Infrastructure → Application → Domain
      ↓               ↓            ↓
   (outer)        (middle)      (inner)
```

- Inner layers know NOTHING about outer layers
- Domain has zero dependencies on infrastructure
- Use interfaces to invert dependencies

```csharp
// Domain defines the interface (inner)
public interface IUserRepository
{
    Task SaveAsync(User user);
    Task<User?> FindByIdAsync(UserId id);
}

// Infrastructure implements it (outer)
public class PostgresUserRepository : IUserRepository
{
    public Task SaveAsync(User user)
    {
        // SQL here
        return Task.CompletedTask;
    }

    public Task<User?> FindByIdAsync(UserId id)
    {
        return Task.FromResult<User?>(null);
    }
}

// Domain service uses the interface
public class UserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }
}
```

### 4. Contracts

Interfaces define boundaries between components.

```csharp
// The contract
public interface IPaymentGateway
{
    Task<ChargeResult> ChargeAsync(Money amount, CardDetails card);
    Task<RefundResult> RefundAsync(string chargeId);
}

// Multiple implementations possible
public class StripeGateway : IPaymentGateway { }
public class PayPalGateway : IPaymentGateway { }
public class MockGateway : IPaymentGateway { }  // For tests
```

### 5. Cross-Cutting Concerns

Concerns that span multiple features: logging, auth, validation, error handling.

**Options:**
- Middleware/interceptors
- Decorators
- Aspect-oriented approaches
- Base classes (use sparingly)

```csharp
// Middleware approach
public class LoggingMiddleware
{
    public Response Handle(Request request, Func<Request, Response> next)
    {
        Console.WriteLine($"Request: {request.Path}");
        var response = next(request);
        Console.WriteLine($"Response: {response.Status}");
        return response;
    }
}
```

### 6. Conway's Law

> "Organizations design systems that mirror their communication structure."

**Implication:** Team structure affects architecture. Align both intentionally.

---

## Common Architectural Styles

### Layered Architecture

Traditional layers: Presentation → Business → Persistence

**Pros:** Simple, well-understood
**Cons:** Can become a "big ball of mud" without discipline

### Hexagonal Architecture (Ports & Adapters)

Domain at center, adapters around the edges.

```
        ┌─────────────────────┐
        │     HTTP Adapter    │
        └─────────┬───────────┘
                  │
┌─────────────────▼─────────────────┐
│              DOMAIN                │
│   ┌─────────────────────────┐     │
│   │      Business Logic      │     │
│   │      Use Cases           │     │
│   └─────────────────────────┘     │
└─────────────────┬─────────────────┘
                  │
        ┌─────────▼───────────┐
        │   Database Adapter   │
        └─────────────────────┘
```

**Ports:** Interfaces defined by the domain
**Adapters:** Implementations that connect to the outside world

### Clean Architecture

Similar to Hexagonal, with explicit layers:

1. **Entities** - Enterprise business rules
2. **Use Cases** - Application business rules
3. **Interface Adapters** - Controllers, Presenters, Gateways
4. **Frameworks & Drivers** - Web, DB, External interfaces

---

## Feature-Driven Structure (Frontend)

```
src/
  features/
    auth/
      components/
        LoginForm.cs
        SignupForm.cs
      hooks/
        AuthService.cs
      services/
        AuthService.cs
      types/
        AuthTypes.cs
      index.ts  # Public API
    checkout/
      components/
      hooks/
      services/
      types/
      index.ts
  shared/
    components/  # Truly shared UI
    hooks/       # Truly shared hooks
    utils/       # Truly shared utilities
```

---

## Feature-Driven Structure (Backend)

```
src/
  modules/
    users/
      domain/
        User.cs
        UserRepository.cs  # Interface
      application/
        CreateUser.cs      # Use case
        GetUser.cs         # Use case
      infrastructure/
        PostgresUserRepo.cs
      presentation/
        UserController.cs
        UserDto.cs
    orders/
      domain/
      application/
      infrastructure/
      presentation/
  shared/
    domain/        # Shared value objects
    infrastructure/ # Shared infra utilities
```

---

## The Walking Skeleton

Start with a minimal end-to-end slice:

1. **Thinnest possible feature** that touches all layers
2. **Deployable** from day one
3. **Proves the architecture** works

Example walking skeleton for e-commerce:
- User can view ONE product (hardcoded)
- User can add it to cart
- User can "checkout" (just logs)

From there, flesh out each feature fully.

---

## Testing Architecture

```
┌────────────────────────────────────────────┐
│            E2E / Acceptance Tests          │  Few, slow, high confidence
├────────────────────────────────────────────┤
│            Integration Tests               │  Some, medium speed
├────────────────────────────────────────────┤
│              Unit Tests                    │  Many, fast, isolated
└────────────────────────────────────────────┘
```

**Test by layer:**
- **Domain:** Unit tests (most tests here)
- **Application:** Integration tests with mocked infra
- **Infrastructure:** Integration tests with real dependencies
- **E2E:** Critical paths only

---

## Architecture Decision Records (ADRs)

Document significant decisions:

```markdown
# ADR 001: Use PostgreSQL for persistence

## Status
Accepted

## Context
We need a database. Options: PostgreSQL, MongoDB, MySQL

## Decision
PostgreSQL for:
- ACID compliance
- Team familiarity
- JSON support for flexibility

## Consequences
- Need PostgreSQL expertise
- Schema migrations required
- Excellent query capabilities
```

---

## Red Flags in Architecture

- **Circular dependencies** between modules
- **Domain depending on infrastructure**
- **Framework code in business logic**
- **No clear boundaries** between features
- **Shared mutable state** across modules
- **"Util" or "Common" packages** that grow forever
- **Database schema driving domain model**