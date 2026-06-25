# Code Smells & Anti-Patterns

## What Are Code Smells?

Indicators that something MAY be wrong. Not bugs, but design problems that make code hard to understand, change, or test.

## The Five Categories

### 1. Bloaters
Code that has grown too large.

| Smell | Symptom | Refactoring |
|-------|---------|-------------|
| **Long Method** | > 10 lines | Extract Method |
| **Large Class** | > 50 lines, multiple responsibilities | Extract Class |
| **Long Parameter List** | > 3 parameters | Introduce Parameter Object |
| **Data Clumps** | Same group of variables appear together | Extract Class |
| **Primitive Obsession** | Primitives instead of small objects | Wrap in Value Object |

### 2. Object-Orientation Abusers
Misuse of OO principles.

| Smell | Symptom | Refactoring |
|-------|---------|-------------|
| **Switch Statements** | Type checking, large switch/if-else | Replace with Polymorphism |
| **Parallel Inheritance** | Adding subclass requires adding another | Merge Hierarchies |
| **Refused Bequest** | Subclass doesn't use parent methods | Replace Inheritance with Delegation |
| **Alternative Classes** | Different interfaces, same concept | Rename, Extract Superclass |

### 3. Change Preventers
Code that makes changes difficult.

| Smell | Symptom | Refactoring |
|-------|---------|-------------|
| **Divergent Change** | One class changed for many reasons | Extract Class (SRP) |
| **Shotgun Surgery** | One change touches many classes | Move Method/Field together |
| **Parallel Inheritance** | (see above) | Merge Hierarchies |

### 4. Dispensables
Code that can be removed.

| Smell | Symptom | Refactoring |
|-------|---------|-------------|
| **Comments** | Explaining bad code | Rename, Extract Method |
| **Duplicate Code** | Copy-paste | Extract Method, Pull Up Method |
| **Dead Code** | Unreachable code | Delete |
| **Speculative Generality** | "Just in case" code | Delete (YAGNI) |
| **Lazy Class** | Class that does almost nothing | Inline Class |

### 5. Couplers
Excessive coupling between classes.

| Smell | Symptom | Refactoring |
|-------|---------|-------------|
| **Feature Envy** | Method uses another class's data extensively | Move Method |
| **Inappropriate Intimacy** | Classes know too much about each other | Move Method, Extract Class |
| **Message Chains** | `a.getB().getC().getD()` | Hide Delegate |
| **Middle Man** | Class only delegates | Inline Class |

---

## The Seven Most Common Code Smells

### 1. Long Method

**Symptom:** Method > 10 lines, doing multiple things.

```csharp
// SMELL
public void ProcessOrder(Order order)
{
    ValidateItems(order);
    if (!order.Items.Any()) throw new Exception("Empty");
    if (order.Customer == null) throw new Exception("No customer");

    decimal total = 0;
    foreach (var item in order.Items)
    {
        total += item.Price * item.Quantity;
        if (item.Discount.HasValue)
            total -= item.Discount.Value;
    }

    var taxRate = GetTaxRate(order.Customer.State);
    total *= (1 + taxRate);

    Db.Orders.Insert(order with { Total = total });
    EmailService.Send(order.Customer.Email, "Order confirmed");
}

// REFACTORED
public void ProcessOrder(Order order)
{
    ValidateOrder(order);
    var total = CalculateTotal(order);
    SaveOrder(order, total);
    NotifyCustomer(order);
}
```

### 2. Large Class

**Symptom:** Class with many responsibilities, > 50 lines.

```csharp
// SMELL: God class
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }

    public void Login() {}
    public void Logout() {}
    public void ResetPassword() {}

    public void SetTheme() {}
    public void SetLanguage() {}

    public void SendEmail() {}
    public void SendSms() {}

    public void Charge() {}
    public void Refund() {}
}

// REFACTORED
public class User { public string Name { get; set; } public string Email { get; set; } }
public class AuthService { public void Login(){} public void Logout(){} public void ResetPassword(){} }
public class UserPreferences { public void SetTheme(){} public void SetLanguage(){} }
public class NotificationService { public void SendEmail(){} public void SendSms(){} }
public class BillingService { public void Charge(){} public void Refund(){} }
```

### 3. Feature Envy

**Symptom:** Method uses another class's data more than its own.

```csharp
// SMELL
public class Order
{
    public double CalculateShipping(Customer customer)
    {
        if (customer.Country == "US")
            return customer.State == "CA" ? 10 : 15;
        return 25;
    }
}

// REFACTORED
public class Customer
{
    public string Country { get; set; }
    public string State { get; set; }

    public double GetShippingCost()
        => Country == "US" ? (State == "CA" ? 10 : 15) : 25;
}

public class Order
{
    public Customer Customer { get; set; }
    public double CalculateShipping() => Customer.GetShippingCost();
}
```

### 4. Primitive Obsession

**Symptom:** Using primitives for domain concepts.

```csharp
// SMELL
public void CreateUser(string email, int age, string zipCode)
{
    if (!email.Contains("@")) throw new Exception();
    if (age < 0) throw new Exception();
}

// REFACTORED
public class Email
{
    public Email(string value)
    {
        if (!value.Contains("@")) throw new InvalidEmailException();
    }
}

public class Age
{
    public Age(int value)
    {
        if (value < 0 || value > 150)
            throw new InvalidAgeException();
    }
}

public void CreateUser(Email email, Age age, Address address) {}
```

### 5. Switch Statements

**Symptom:** Switching on type, repeated across codebase.

```csharp
// SMELL
public double GetArea(Shape shape)
{
    switch (shape.Type)
    {
        case "circle": return Math.PI * Math.Pow(shape.Radius, 2);
        case "rectangle": return shape.Width * shape.Height;
        case "triangle": return 0.5 * shape.Base * shape.Height;
        default: return 0;
    }
}

// REFACTORED
public interface IShape
{
    double GetArea();
    double GetPerimeter();
}

public class Circle : IShape
{
    private readonly double _radius;

    public Circle(double radius) => _radius = radius;

    public double GetArea() => Math.PI * _radius * _radius;
    public double GetPerimeter() => 2 * Math.PI * _radius;
}
```

### 6. Inappropriate Intimacy

**Symptom:** Classes know too much about each other's internals.

```csharp
// SMELL
public class Order
{
    public void Process()
    {
        var inventory = new Inventory();

        foreach (var item in Items)
        {
            var stock = inventory.StockLevels[item.Sku];
            if (stock.Quantity < item.Quantity)
                throw new Exception("Out of stock");

            inventory.StockLevels[item.Sku].Quantity -= item.Quantity;
        }
    }
}

// REFACTORED
public class Inventory
{
    public ReserveResult Reserve(IEnumerable<OrderItem> items)
    {
        return ReserveResult.Success();
    }
}

public class Order
{
    public void Process(Inventory inventory)
    {
        var result = inventory.Reserve(Items);

        if (!result.IsSuccess)
            throw new OutOfStockException();
    }
}
```

### 7. Speculative Generality

**Symptom:** "Just in case" abstractions that aren't used.

```csharp
// SMELL
public interface IPaymentProcessor
{
    void Process();
    void Rollback();
    void Audit();
    void GenerateReport();
    void ScheduleRecurring();
}

public class StripeProcessor : IPaymentProcessor
{
    public void Process() {}
    public void Rollback() => throw new NotImplementedException();
    public void Audit() => throw new NotImplementedException();
    public void GenerateReport() => throw new NotImplementedException();
    public void ScheduleRecurring() => throw new NotImplementedException();
}

// REFACTORED
public interface IPaymentProcessor
{
    void Process();
}

public class StripeProcessor : IPaymentProcessor
{
    public void Process() {}
}
```

---

## Prevention Strategies

1. **Follow Object Calisthenics** - Rules prevent most smells
2. **Practice TDD** - Tests reveal design problems early
3. **Review in pairs** - Fresh eyes catch smells
4. **Refactor continuously** - Don't let smells accumulate
5. **Apply SOLID** - Prevents structural smells
6. **Use static analysis** - Tools catch common issues

---

## When You Find a Smell

1. **Confirm it's a problem** - Not all smells need fixing
2. **Ensure test coverage** - Before refactoring
3. **Refactor in small steps** - Keep tests passing
4. **Commit frequently** - Easy to revert if needed