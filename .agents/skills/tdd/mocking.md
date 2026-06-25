# When to Mock

Mock at **system boundaries** only:

- External APIs (payment, email, etc.)
- Databases (sometimes - prefer test DB)
- Time/randomness
- File system (sometimes)

Don't mock:

- Your own classes/modules
- Internal collaborators
- Anything you control

## Designing for Mockability

At system boundaries, design interfaces that are easy to mock:

**1. Use dependency injection**

Pass external dependencies in rather than creating them internally:

```csharp
// Easy to mock
public Task<ChargeResult> ProcessPayment(Order order, IPaymentClient paymentClient)
{
    return paymentClient.ChargeAsync(order.Total);
}

// Hard to mock
public Task<ChargeResult> ProcessPayment(Order order)
{
    var client = new StripeClient(Environment.GetEnvironmentVariable("STRIPE_KEY"));
    return client.ChargeAsync(order.Total);
}
```

**2. Prefer SDK-style interfaces over generic fetchers**

Create specific functions for each external operation instead of one generic function with conditional logic:

```csharp
// GOOD: Each function is independently mockable via interface methods
public interface IApiClient
{
    Task<User> GetUserAsync(string id);
    Task<List<Order>> GetOrdersAsync(string userId);
    Task<Order> CreateOrderAsync(CreateOrderDto data);
}

// BAD: Mocking requires conditional logic inside the mock, e.g. checking paths/methods
public interface IGenericHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
}
```

The SDK approach means:
- Each mock returns one specific shape
- No conditional logic in test setup
- Easier to see which endpoints a test exercises
- Type safety per endpoint
