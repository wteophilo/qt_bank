# Testing Strategy

## The Testing Pyramid

```
       /\
      /  \        E2E Tests (Few)
     /----\       - Full system
    /      \      - Slow, brittle
   /--------\
  /          \    Integration Tests (Some)
 /------------\   - Multiple components
/              \  - Medium speed
----------------
      Unit Tests (Many)
      - Single unit
      - Fast, isolated
```

## Test Types

### Unit Tests

Test ONE class or function in isolation.

**Characteristics:**
- Fast (milliseconds)
- No external dependencies (mocked)
- Most of your tests should be unit tests

```csharp
public class OrderTests
{
    [Fact]
    public void CalculatesTotalCorrectly()
    {
        var order = new Order();
        order.AddItem(new Item { Price = 100 });
        order.AddItem(new Item { Price = 50 });

        Assert.Equal(150, order.CalculateTotal());
    }
}
```

### Integration Tests

Test multiple components together.

**Characteristics:**
- Slower (may use real DB)
- Test boundaries between components
- Fewer than unit tests

```csharp
public class OrderServiceIntegrationTests : IAsyncLifetime
{
    private Database db;
    private OrderService service;

    public async Task InitializeAsync()
    {
        db = await Database.Connect();
        service = new OrderService(new PostgresOrderRepo(db));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SavesAndRetrievesAnOrder()
    {
        var order = Order.Create(new OrderProps { CustomerId = "123" });
        await service.Save(order);

        var retrieved = await service.FindById(order.Id);
        Assert.Equal(order, retrieved);
    }
}
```

### E2E / Acceptance Tests

Test the entire system from user perspective.

**Characteristics:**
- Slowest
- Most brittle (many moving parts)
- Test critical paths only

```csharp
public class CheckoutFlowTests
{
    [Fact]
    public async Task UserCanCompletePurchase()
    {
        await page.GotoAsync("/products");
        await page.ClickAsync("[data-testid=\"add-to-cart\"]");
        await page.ClickAsync("[data-testid=\"checkout\"]");
        await page.FillAsync("[name=\"card\"]", "4242424242424242");
        await page.ClickAsync("[data-testid=\"pay\"]");

        Assert.Equal("Order Confirmed", await page.TextContentAsync("h1"));
    }
}
```

---

## Arrange-Act-Assert (AAA)

Structure EVERY test this way:

```csharp
[Fact]
public void AppliesDiscountToPremiumUsers()
{
    // ARRANGE - Set up the test world
    var user = new User(isPremium: true);
    var cart = new Cart(user);
    cart.AddItem(new Item { Price = 100 });

    // ACT - Execute the behavior under test
    var total = cart.CalculateTotal();

    // ASSERT - Verify the expected outcome
    Assert.Equal(80, total); // 20% discount
}
```

### Writing AAA Backwards

Sometimes easier to write in reverse:

1. **Assert first** - What do you want to verify?
2. **Act** - What action produces that result?
3. **Arrange** - What setup is needed?

---

## Test Naming

### Bad: Abstract, Technical

```csharp
[Fact]
public void ShouldWorkCorrectly() { }

[Fact]
public void HandlesTheEdgeCase() { }

[Fact]
public void SetsTheDataProperty() { }
```

### Good: Concrete Examples, Domain Language

```csharp
[Fact]
public void Calculates20PercentDiscountForPremiumUsers() { }

[Fact]
public void ReturnsErrorWhenCartIsEmpty() { }

[Fact]
public void RecognizesRacecarAsAPalindrome() { }
```

### Format

```csharp
// Option 1: Should + behavior
[Fact]
public void ShouldApplyTaxBasedOnShippingState() { }

// Option 2: When + Then
[Fact]
public void WhenAdding2Plus3_ThenReturns5() { }

// Option 3: Given-When-Then (for complex scenarios), via nested classes
public class GivenAPremiumUser
{
    public class WhenTheyCheckout
    {
        [Fact]
        public void ThenTheyReceive20PercentDiscount() { /* ... */ }
    }
}
```

---

## Test Doubles

### Dummy

Object passed but never used.

```csharp
ILogger dummyLogger = default!;
new UserService(realRepo, dummyLogger);
```

### Stub

Returns predefined values.

```csharp
public class StubUserRepo : IUserRepo
{
    public Task<User> FindById(string id) => Task.FromResult(new User { Name = "Test" });
    public Task Save(User user) => Task.CompletedTask;
}

IUserRepo stubRepo = new StubUserRepo();
```

### Spy

Records how it was called.

```csharp
public class EmailSpy
{
    public List<string> SentEmails { get; } = new List<string>();

    public void Send(string to, string message)
    {
        SentEmails.Add(to);
    }
}

// Later
Assert.Contains("user@example.com", emailSpy.SentEmails);
```

### Mock

Verifies expected interactions.

```csharp
var mockRepo = new Mock<IUserRepo>();
mockRepo.Setup(r => r.Save(It.IsAny<User>())).Returns(Task.CompletedTask);

// After test
mockRepo.Verify(r => r.Save(expectedUser), Times.Once);
```

### Fake

Working implementation (simplified).

```csharp
public class InMemoryUserRepo : IUserRepo
{
    private readonly Dictionary<string, User> users = new Dictionary<string, User>();

    public Task Save(User user)
    {
        users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<User?> FindById(string id)
    {
        return Task.FromResult(users.TryGetValue(id, out var user) ? user : null);
    }
}
```

---

## Testing Strategies by Layer

### Domain Layer (Most Tests)

- Unit tests with no mocks
- Test business rules, value objects, entities
- Fast, comprehensive

```csharp
public class MoneyTests
{
    [Fact]
    public void AddsAmountsWithSameCurrency()
    {
        var a = Money.Dollars(10);
        var b = Money.Dollars(20);
        Assert.True(a.Add(b).Equals(Money.Dollars(30)));
    }

    [Fact]
    public void ThrowsWhenAddingDifferentCurrencies()
    {
        var usd = Money.Dollars(10);
        var eur = Money.Euros(10);
        Assert.Throws<CurrencyMismatchException>(() => usd.Add(eur));
    }
}
```

### Application Layer

- Integration tests with mocked infrastructure
- Test use case orchestration

```csharp
public class CreateOrderUseCaseTests
{
    [Fact]
    public async Task CreatesOrderAndSendsConfirmation()
    {
        var orderRepo = new InMemoryOrderRepo();
        var emailService = new Mock<IEmailService>();
        var useCase = new CreateOrderUseCase(orderRepo, emailService.Object);

        await useCase.Execute(new CreateOrderRequest
        {
            CustomerId = "123",
            Items = new List<Item> { /* ... */ }
        });

        Assert.Equal(1, orderRepo.Count());
        emailService.Verify(e => e.Send(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
```

### Infrastructure Layer

- Integration tests with real dependencies
- Test database, API integrations

```csharp
public class PostgresOrderRepoTests : IAsyncLifetime
{
    private PostgresOrderRepo repo;

    public Task InitializeAsync()
    {
        repo = new PostgresOrderRepo(testDb);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PersistsAndRetrievesOrder()
    {
        var order = Order.Create(new OrderProps { /* ... */ });
        await repo.Save(order);

        var found = await repo.FindById(order.Id);
        Assert.Equal(order, found);
    }
}
```

---

## High-Value Integration Tests

Focus integration tests on:

1. **Boundaries** - Where systems meet
2. **Critical paths** - Money, security, core features
3. **Complex queries** - Database operations

### Contract Tests

Verify implementations match interfaces.

```csharp
// Shared contract test
public abstract class UserRepoContractTests
{
    protected abstract IUserRepo CreateRepo();

    [Fact]
    public async Task SavesAndRetrievesUser()
    {
        var repo = CreateRepo();
        var user = User.Create(new UserProps { Name = "Test" });
        await repo.Save(user);
        var found = await repo.FindById(user.Id);
        Assert.Equal(user, found);
    }

    [Fact]
    public async Task ReturnsNullForMissingUser()
    {
        var repo = CreateRepo();
        var found = await repo.FindById("nonexistent");
        Assert.Null(found);
    }
}

// Apply to all implementations
public class InMemoryUserRepoContractTests : UserRepoContractTests
{
    protected override IUserRepo CreateRepo() => new InMemoryUserRepo();
}

public class PostgresUserRepoContractTests : UserRepoContractTests
{
    protected override IUserRepo CreateRepo() => new PostgresUserRepo(testDb);
}
```

---

## Test Builders

Create test objects easily.

```csharp
public class OrderBuilder
{
    private string id = "order-1";
    private string customerId = "cust-1";
    private List<Item> items = new List<Item>();
    private string status = "pending";

    public OrderBuilder WithId(string id)
    {
        this.id = id;
        return this;
    }

    public OrderBuilder WithItems(List<Item> items)
    {
        this.items = items;
        return this;
    }

    public OrderBuilder Paid()
    {
        status = "paid";
        return this;
    }

    public Order Build()
    {
        return Order.Create(new OrderProps
        {
            Id = id,
            CustomerId = customerId,
            Items = items,
            Status = status
        });
    }
}

// Usage
var order = new OrderBuilder()
    .WithItems(new List<Item> { new Item { Sku = "ABC", Price = 100 } })
    .Paid()
    .Build();
```

---

## Common Testing Mistakes

| Mistake | Problem | Solution |
|---------|---------|----------|
| Testing implementation | Brittle tests | Test behavior only |
| Too many mocks | Tests prove nothing | Use real objects when possible |
| Shared state | Flaky tests | Isolate each test |
| No assertions | False confidence | Always assert something meaningful |
| Testing trivial code | Wasted effort | Focus on logic and edge cases |
| Slow tests | Reduced feedback | Optimize, use unit tests |