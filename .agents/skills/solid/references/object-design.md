# Object-Oriented Design

## Responsibility-Driven Design (RDD)

The key insight: **Objects are defined by their responsibilities, not their data.**

### Finding Objects

Start with:
1. **Nouns** in requirements → candidate objects
2. **Verbs** → candidate methods/behaviors
3. **Domain concepts** → value objects

### Finding Responsibilities

Each object should answer:
- What does this object **know**?
- What does this object **do**?
- What does this object **decide**?

### Object Stereotypes

Every class fits one (or maybe two) stereotypes:

| Stereotype | Purpose | Example |
|------------|---------|---------|
| **Information Holder** | Knows things, holds data | `User`, `Product`, `Address` |
| **Structurer** | Maintains relationships | `OrderItems`, `UserGroup` |
| **Service Provider** | Performs work | `PaymentProcessor`, `EmailSender` |
| **Coordinator** | Orchestrates workflow | `OrderFulfillmentService` |
| **Controller** | Makes decisions, delegates | `CheckoutController` |
| **Interfacer** | Transforms between systems | `UserAPIAdapter`, `DatabaseMapper` |

### The Two Questions

For every class, ask:
1. **"What pattern is this?"** - Which stereotype? Which design pattern?
2. **"Is it doing too much?"** - Check object calisthenics rules

If you can't answer clearly, the class needs refactoring.

---

## Tell, Don't Ask

**Command objects to do work. Don't interrogate them and do the work yourself.**

```csharp
// BAD: Asking, then doing
if (account.GetBalance() >= amount)
{
    account.SetBalance(account.GetBalance() - amount);
    // more logic here...
}

// GOOD: Telling
var result = account.Withdraw(amount);
if (result.IsSuccess())
{
    // ...
}
```

The object that has the data should have the behavior.

---

## Design by Contract (DbC)

Every method has:
- **Preconditions** - What must be true BEFORE calling
- **Postconditions** - What will be true AFTER calling
- **Invariants** - What is ALWAYS true about the object

```csharp
public class BankAccount
{
    private Money balance;

    // INVARIANT: balance is never negative

    // PRECONDITION: amount > 0
    // POSTCONDITION: balance decreased by amount OR error returned
    public WithdrawResult Withdraw(Money amount)
    {
        if (amount.IsNegativeOrZero())
        {
            return WithdrawResult.InvalidAmount();
        }

        if (balance.IsLessThan(amount))
        {
            return WithdrawResult.InsufficientFunds();
        }

        balance = balance.Minus(amount);
        return WithdrawResult.Success(balance);
    }
}
```

---

## Composition Over Inheritance

**Prefer composing objects over extending classes.**

### Why Inheritance is Problematic:
- Tight coupling between parent and child
- Fragile base class problem
- Difficult to change parent without breaking children
- Forces "is-a" relationship that may not fit

### When to Use Inheritance:
- True "is-a" relationship (rare)
- Framework requirements
- Template Method pattern (intentional)

### Prefer Composition:
```csharp
// BAD: Inheritance
public class PremiumUser : User
{
    public int GetDiscount() => 20;
}

// GOOD: Composition
public class User
{
    private readonly IDiscountPolicy discountPolicy;

    public User(IDiscountPolicy discountPolicy)
    {
        this.discountPolicy = discountPolicy;
    }

    public decimal GetDiscount()
    {
        return discountPolicy.Calculate();
    }
}

// Now discount behavior is pluggable
new User(new PremiumDiscount());
new User(new StandardDiscount());
new User(new NoDiscount());
```

---

## The Law of Demeter (Principle of Least Knowledge)

**Only talk to your immediate friends.**

A method should only call:
1. Methods on `this`
2. Methods on parameters
3. Methods on objects it creates
4. Methods on its direct components

```csharp
// BAD: Reaching through objects
order.GetCustomer().GetAddress().GetCity();

// GOOD: Ask the immediate friend
order.GetShippingCity();
```

This reduces coupling - changes to `Address` don't ripple through all callers.

---

## Encapsulation

**Hide internal details, expose behavior.**

### Levels of Encapsulation:
1. **Data** - private fields, no direct access
2. **Implementation** - how things work internally
3. **Type** - concrete class hidden behind interface
4. **Design** - architectural decisions hidden from clients

```csharp
// BAD: Exposed internals
public class Order
{
    public List<Item> Items = new List<Item>();
    public decimal Total = 0;
}

// Client can corrupt state
order.Items.Add(item);
order.Total = -999; // Oops!

// GOOD: Encapsulated
public class Order
{
    private OrderItems items;
    private Money total;

    public void AddItem(Item item)
    {
        items.Add(item);
        RecalculateTotal();
    }

    public Money GetTotal()
    {
        return total; // Returns copy or immutable
    }

    private void RecalculateTotal() { /* ... */ }
}
```

---

## Polymorphism

**Replace conditionals with types.**

```csharp
// BAD: Type checking
public decimal CalculateShipping(string method, decimal value)
{
    if (method == "standard") return value < 50 ? 5 : 0;
    if (method == "express") return 15;
    if (method == "overnight") return 25;
    throw new ArgumentException("Unknown method");
}

// GOOD: Polymorphism
public interface IShippingMethod
{
    decimal CalculateCost(decimal orderValue);
}

public class StandardShipping : IShippingMethod
{
    public decimal CalculateCost(decimal orderValue)
    {
        return orderValue < 50 ? 5 : 0;
    }
}

public class ExpressShipping : IShippingMethod
{
    public decimal CalculateCost(decimal orderValue)
    {
        return 15;
    }
}

// Usage - no conditionals
public decimal CalculateShipping(IShippingMethod method, decimal value)
{
    return method.CalculateCost(value);
}
```

---

## Value Objects vs Entities

### Value Objects
- Defined by their attributes (no identity)
- Immutable
- Comparable by value
- Examples: `Money`, `Email`, `Address`, `DateRange`

```csharp
public class Money
{
    private readonly decimal amount;
    private readonly string currency;

    public Money(decimal amount, string currency)
    {
        this.amount = amount;
        this.currency = currency;
    }

    public bool Equals(Money other)
    {
        return amount == other.amount &&
               currency == other.currency;
    }

    public Money Add(Money other)
    {
        if (currency != other.currency)
        {
            throw new CurrencyMismatchException();
        }
        return new Money(amount + other.amount, currency);
    }
}
```

### Entities
- Have identity (survives attribute changes)
- Usually mutable (via methods)
- Comparable by identity
- Examples: `User`, `Order`, `Product`

```csharp
public class User
{
    private readonly UserId id;
    private Email email;
    private Name name;

    public User(UserId id, Email email, Name name)
    {
        this.id = id;
        this.email = email;
        this.name = name;
    }

    public bool Equals(User other)
    {
        return id.Equals(other.id); // Identity comparison
    }

    public void ChangeEmail(Email newEmail)
    {
        email = newEmail; // Still same user
    }
}
```

---

## Aggregates

A cluster of objects treated as a single unit for data changes.

- One object is the **aggregate root** (entry point)
- External code only references the root
- Root enforces invariants for the entire cluster

```csharp
// Order is the aggregate root
public class Order
{
    private List<OrderItem> items = new List<OrderItem>();

    // All access through the root
    public void AddItem(Product product, int quantity)
    {
        var item = new OrderItem(product, quantity);
        items.Add(item);
        ValidateTotal();
    }

    public void RemoveItem(ItemId itemId)
    {
        items = items.Where(i => !i.Id.Equals(itemId)).ToList();
    }

    // Root enforces invariants
    private void ValidateTotal()
    {
        if (CalculateTotal().Exceeds(MaxOrderValue))
        {
            throw new OrderTotalExceededException();
        }
    }
}

// BAD: Accessing items directly
order.Items.Add(new OrderItem(/* ... */)); // Bypasses validation!

// GOOD: Through the root
order.AddItem(product, 2); // Validation happens
```