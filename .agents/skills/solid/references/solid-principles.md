# SOLID Principles

## Overview

SOLID helps structure software to be flexible, maintainable, and testable. These principles reduce coupling and increase cohesion.

## S - Single Responsibility Principle (SRP)

> "A class should have one, and only one, reason to change."

### Problem It Solves
God objects that do everything - hard to test, hard to change, hard to understand.

### How to Apply
Each class handles ONE responsibility. If you find yourself saying "and" when describing what a class does, split it.

```csharp
// BAD: Multiple responsibilities
public class Order
{
    public decimal CalculateTotal() { /* ... */ return 0; }
    public void SaveToDatabase() { /* ... */ }       // Persistence
    public string GenerateInvoice() { /* ... */ return ""; } // Presentation
}

// GOOD: Single responsibility each
public class Order
{
    private List<OrderItem> items = new List<OrderItem>();

    public void AddItem(OrderItem item) { /* ... */ }
    public decimal CalculateTotal() { /* ... */ return 0; }
}

public class OrderRepository
{
    public Task Save(Order order) { /* ... */ return Task.CompletedTask; }
}

public class InvoiceGenerator
{
    public Invoice Generate(Order order) { /* ... */ return new Invoice(); }
}
```

### Detection Questions
- Does this class have multiple reasons to change?
- Can I describe it without using "and"?
- Would different stakeholders request changes to different parts?

---

## O - Open/Closed Principle (OCP)

> "Software entities should be open for extension but closed for modification."

### Problem It Solves
Having to modify existing, tested code every time requirements change. Risk of breaking working features.

### How to Apply
Design abstractions that allow new behavior through new classes, not edits to existing ones.

```csharp
// BAD: Must modify to add new shipping
public class ShippingCalculator
{
    public decimal Calculate(string type, decimal value)
    {
        if (type == "standard") return value < 50 ? 5 : 0;
        if (type == "express") return 15;
        // Must add more ifs for new types!
        throw new ArgumentException("Unknown type");
    }
}

// GOOD: Open for extension
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

// Add new shipping by creating new class, not modifying existing
public class SameDayShipping : IShippingMethod
{
    public decimal CalculateCost(decimal orderValue)
    {
        return 25;
    }
}
```

### Architectural Insight
OCP at architecture level means: **design your codebase so new features are added by adding code, not changing existing code.**

---

## L - Liskov Substitution Principle (LSP)

> "Subtypes must be substitutable for their base types without altering program correctness."

### Problem It Solves
Subclasses that break expectations, requiring type-checking and special cases.

### How to Apply
Subclasses must honor the contract of the parent. If the parent returns positive numbers, subclasses cannot return negatives.

```csharp
// BAD: Violates parent's contract
public class DiscountPolicy
{
    public virtual decimal GetDiscount(decimal value)
    {
        return 0; // Non-negative expected
    }
}

public class WeirdDiscount : DiscountPolicy
{
    public override decimal GetDiscount(decimal value)
    {
        return -5; // Increases cost! Breaks expectations
    }
}

// GOOD: Enforces contract
public class DiscountPolicy
{
    private readonly decimal discount;

    public DiscountPolicy(decimal discount)
    {
        if (discount < 0) throw new ArgumentException("Discount must be non-negative");
        this.discount = discount;
    }

    public decimal GetDiscount()
    {
        return discount;
    }
}
```

### Key Insight
This is why you can swap `InMemoryUserRepo` for `PostgresUserRepo` - they both honor the `IUserRepo` interface contract.

---

## I - Interface Segregation Principle (ISP)

> "Clients should not be forced to depend on methods they do not use."

### Problem It Solves
Fat interfaces that force partial implementations, empty methods, or throws.

### How to Apply
Split large interfaces into smaller, cohesive ones. Clients depend only on what they need.

```csharp
// BAD: Fat interface
public interface IWarehouseDevice
{
    void PrintLabel(string orderId);
    string ScanBarcode();
    void PackageItem(string orderId);
}

public class BasicPrinter : IWarehouseDevice
{
    public void PrintLabel(string orderId) { /* works */ }
    public string ScanBarcode() { throw new NotSupportedException(); } // Forced!
    public void PackageItem(string orderId) { throw new NotSupportedException(); }
}

// GOOD: Segregated interfaces
public interface ILabelPrinter
{
    void PrintLabel(string orderId);
}

public interface IBarcodeScanner
{
    string ScanBarcode();
}

public interface IItemPackager
{
    void PackageItem(string orderId);
}

public class BasicPrinter : ILabelPrinter
{
    public void PrintLabel(string orderId) { /* only what it does */ }
}
```

### Detection
If you see `throw new NotImplementedException()` or empty method bodies, the interface is too fat.

---

## D - Dependency Inversion Principle (DIP)

> "High-level modules should not depend on low-level modules. Both should depend on abstractions."

### Problem It Solves
Tight coupling to specific implementations (databases, APIs, frameworks). Hard to test, hard to swap.

### How to Apply
Depend on interfaces, inject implementations.

```csharp
// BAD: Direct dependency on concrete class
public class OrderService
{
    private readonly SendGridEmailService emailService = new SendGridEmailService(); // Locked in!

    public void ConfirmOrder(string email)
    {
        emailService.Send(email, "Order confirmed");
    }
}

// GOOD: Depend on abstraction
public interface IEmailService
{
    void Send(string to, string message);
}

public class OrderService
{
    private readonly IEmailService emailService;

    public OrderService(IEmailService emailService)
    {
        this.emailService = emailService;
    }

    public void ConfirmOrder(string email)
    {
        emailService.Send(email, "Order confirmed");
    }
}

// Now can inject any implementation
new OrderService(new SendGridEmailService());
new OrderService(new SesEmailService());
new OrderService(new MockEmailService()); // For tests!
```

### The Dependency Rule
Source code dependencies should point **inward** toward high-level policies (domain logic), never toward low-level details (infrastructure).

```
Infrastructure → Application → Domain
      ↑              ↑            ↑
    (outer)       (middle)     (inner)

Dependencies flow: outer → inner
Never: inner → outer
```

---

## Applying SOLID at Architecture Level

These principles scale beyond classes:

| Principle | Architecture Application |
|-----------|--------------------------|
| SRP | Each bounded context has one responsibility |
| OCP | New features = new modules, not edits to existing |
| LSP | Microservices with same contract are substitutable |
| ISP | Thin interfaces between services |
| DIP | High-level business logic doesn't know about databases/frameworks |

---

## Quick Reference

| Principle | One-Liner | Red Flag |
|-----------|-----------|----------|
| SRP | One reason to change | "This class handles X and Y and Z" |
| OCP | Add, don't modify | `if/else` chains for types |
| LSP | Subtypes are substitutable | Type-checking in calling code |
| ISP | Small, focused interfaces | Empty method implementations |
| DIP | Depend on abstractions | `new ConcreteClass()` in business logic |