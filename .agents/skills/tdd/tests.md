# Good and Bad Tests

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```csharp
// GOOD: Tests observable behavior
[Fact]
public async Task UserCanCheckoutWithValidCart()
{
    var cart = CreateCart();
    cart.Add(product);
    var result = await CheckoutAsync(cart, paymentMethod);
    Assert.Equal("confirmed", result.Status);
}
```

Characteristics:

- Tests behavior users/callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```csharp
// BAD: Tests implementation details
[Fact]
public async Task CheckoutCallsPaymentServiceProcess()
{
    var paymentServiceMock = new Mock<IPaymentService>();
    await CheckoutAsync(cart, paymentServiceMock.Object);
    paymentServiceMock.Verify(x => x.Process(cart.Total), Times.Once);
}
```

Red flags:

- Mocking internal collaborators
- Testing private methods
- Asserting on call counts/order
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT
- Verifying through external means instead of interface

```csharp
// BAD: Bypasses interface to verify
[Fact]
public async Task CreateUserSavesToDatabase()
{
    await CreateUserAsync(new CreateUserDto { Name = "Alice" });
    var row = await db.QueryAsync("SELECT * FROM users WHERE name = @Name", new { Name = "Alice" });
    Assert.NotNull(row);
}

// GOOD: Verifies through interface
[Fact]
public async Task CreateUserMakesUserRetrievable()
{
    var user = await CreateUserAsync(new CreateUserDto { Name = "Alice" });
    var retrieved = await GetUserAsync(user.Id);
    Assert.Equal("Alice", retrieved.Name);
}
```
