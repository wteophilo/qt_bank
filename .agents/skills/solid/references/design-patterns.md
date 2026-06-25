# Design Patterns

## What Are Design Patterns?

Reusable solutions to common design problems. A shared vocabulary for discussing design.

## WARNING: Don't Force Patterns

> "Let patterns emerge from refactoring, don't force them upfront."

Patterns should solve problems you HAVE, not problems you MIGHT have.

## When to Use Patterns

1. **You recognize the problem** - You've seen it before
2. **The pattern fits** - Not forcing it
3. **It simplifies** - Doesn't add unnecessary complexity
4. **Team understands it** - Shared knowledge

---

## Creational Patterns

### Singleton

**Purpose:** Ensure only one instance exists.

**When to use:** Global configuration, connection pools, logging.

**Warning:** Often overused. Consider dependency injection instead.

```csharp
public class Logger
{
    private static Logger? instance;

    private Logger() { }

    public static Logger Instance
    {
        get
        {
            if (instance is null)
            {
                instance = new Logger();
            }
            return instance;
        }
    }

    public void Log(string message) { /* ... */ }
}
```

### Factory

**Purpose:** Create objects without specifying exact class.

**When to use:** Object creation logic is complex, or varies by type.

```csharp
public interface INotification
{
    void Send(string message);
}

public class EmailNotification : INotification
{
    public void Send(string message) { /* ... */ }
}

public class SmsNotification : INotification
{
    public void Send(string message) { /* ... */ }
}

public class PushNotification : INotification
{
    public void Send(string message) { /* ... */ }
}

public enum NotificationType
{
    Email,
    Sms,
    Push
}

public class NotificationFactory
{
    public INotification Create(NotificationType type)
    {
        return type switch
        {
            NotificationType.Email => new EmailNotification(),
            NotificationType.Sms => new SmsNotification(),
            NotificationType.Push => new PushNotification(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
```

### Builder

**Purpose:** Construct complex objects step by step.

**When to use:** Objects with many optional parameters, test data creation.

```csharp
public class UserBuilder
{
    private string? name;
    private string? email;
    private int? age;

    public UserBuilder WithName(string name)
    {
        this.name = name;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        this.email = email;
        return this;
    }

    public UserBuilder WithAge(int age)
    {
        this.age = age;
        return this;
    }

    public User Build()
    {
        return new User(name!, email!, age);
    }
}

// Usage
var user = new UserBuilder()
    .WithName("Alice")
    .WithEmail("alice@example.com")
    .Build();
```

### Prototype

**Purpose:** Create new objects by cloning existing ones.

**When to use:** Object creation is expensive, or you need copies with slight variations.

```csharp
public interface IPrototype
{
    IPrototype Clone();
}

public class Document : IPrototype
{
    public string Title { get; }
    public string Content { get; }
    public Metadata Metadata { get; }

    public Document(string title, string content, Metadata metadata)
    {
        Title = title;
        Content = content;
        Metadata = metadata;
    }

    public IPrototype Clone()
    {
        // Assumes Metadata exposes its own copy logic (e.g. a Clone or copy constructor)
        return new Document(Title, Content, Metadata.Clone());
    }
}
```

---

## Structural Patterns

### Adapter

**Purpose:** Make incompatible interfaces work together.

**When to use:** Integrating third-party libraries, legacy code.

```csharp
// Third-party library with different interface
public class OldPaymentApi
{
    public bool MakePayment(int cents) { /* ... */ return true; }
}

// Our interface
public interface IPaymentGateway
{
    ChargeResult Charge(Money amount);
}

// Adapter
public class OldPaymentAdapter : IPaymentGateway
{
    private readonly OldPaymentApi oldApi;

    public OldPaymentAdapter(OldPaymentApi oldApi)
    {
        this.oldApi = oldApi;
    }

    public ChargeResult Charge(Money amount)
    {
        var cents = amount.ToCents();
        var success = oldApi.MakePayment(cents);
        return success ? ChargeResult.Success() : ChargeResult.Failed();
    }
}
```

### Decorator

**Purpose:** Add behavior to objects dynamically.

**When to use:** Adding features without modifying existing code.

```csharp
public interface INotifier
{
    void Send(string message);
}

public class EmailNotifier : INotifier
{
    public void Send(string message)
    {
        Console.WriteLine($"Email: {message}");
    }
}

// Decorators
public class SmsDecorator : INotifier
{
    private readonly INotifier wrapped;

    public SmsDecorator(INotifier wrapped)
    {
        this.wrapped = wrapped;
    }

    public void Send(string message)
    {
        wrapped.Send(message);
        Console.WriteLine($"SMS: {message}");
    }
}

public class SlackDecorator : INotifier
{
    private readonly INotifier wrapped;

    public SlackDecorator(INotifier wrapped)
    {
        this.wrapped = wrapped;
    }

    public void Send(string message)
    {
        wrapped.Send(message);
        Console.WriteLine($"Slack: {message}");
    }
}

// Usage - compose behaviors
INotifier notifier = new SlackDecorator(
    new SmsDecorator(
        new EmailNotifier()));

notifier.Send("Alert!"); // Sends to all three
```

### Proxy

**Purpose:** Control access to an object.

**When to use:** Lazy loading, access control, logging, caching.

```csharp
public interface IImage
{
    void Display();
}

public class RealImage : IImage
{
    private readonly string filename;

    public RealImage(string filename)
    {
        this.filename = filename;
        LoadFromDisk(); // Expensive
    }

    private void LoadFromDisk() { /* ... */ }

    public void Display() { /* ... */ }
}

// Lazy loading proxy
public class ImageProxy : IImage
{
    private readonly string filename;
    private RealImage? realImage;

    public ImageProxy(string filename)
    {
        this.filename = filename;
    }

    public void Display()
    {
        if (realImage is null)
        {
            realImage = new RealImage(filename);
        }
        realImage.Display();
    }
}
```

### Composite

**Purpose:** Treat individual objects and compositions uniformly.

**When to use:** Tree structures, hierarchies (files/folders, UI components).

```csharp
public interface IComponent
{
    decimal GetPrice();
}

public class Product : IComponent
{
    private readonly decimal price;

    public Product(decimal price)
    {
        this.price = price;
    }

    public decimal GetPrice() => price;
}

public class Box : IComponent
{
    private readonly List<IComponent> children = new List<IComponent>();

    public void Add(IComponent component)
    {
        children.Add(component);
    }

    public decimal GetPrice()
    {
        return children.Sum(child => child.GetPrice());
    }
}

// Usage
var smallBox = new Box();
smallBox.Add(new Product(10));
smallBox.Add(new Product(20));

var bigBox = new Box();
bigBox.Add(smallBox);
bigBox.Add(new Product(50));

Console.WriteLine(bigBox.GetPrice()); // 80
```

---

## Behavioral Patterns

### Strategy

**Purpose:** Define a family of algorithms, make them interchangeable.

**When to use:** Multiple ways to do something, switchable at runtime.

```csharp
public interface IPricingStrategy
{
    decimal Calculate(decimal basePrice);
}

public class RegularPricing : IPricingStrategy
{
    public decimal Calculate(decimal basePrice) => basePrice;
}

public class PremiumDiscount : IPricingStrategy
{
    public decimal Calculate(decimal basePrice) => basePrice * 0.8m; // 20% off
}

public class BlackFriday : IPricingStrategy
{
    public decimal Calculate(decimal basePrice) => basePrice * 0.5m; // 50% off
}

public class ShoppingCart
{
    private readonly IPricingStrategy pricing;

    public ShoppingCart(IPricingStrategy pricing)
    {
        this.pricing = pricing;
    }

    public decimal CalculateTotal(IEnumerable<Item> items)
    {
        var basePrice = items.Sum(i => i.Price);
        return pricing.Calculate(basePrice);
    }
}
```

### Observer

**Purpose:** Notify multiple objects about state changes.

**When to use:** Event systems, pub/sub, reactive updates.

```csharp
public interface IObserver
{
    void Update(Event @event);
}

public class EventEmitter
{
    private readonly List<IObserver> observers = new List<IObserver>();

    public void Subscribe(IObserver observer)
    {
        observers.Add(observer);
    }

    public void Unsubscribe(IObserver observer)
    {
        observers.Remove(observer);
    }

    public void Notify(Event @event)
    {
        foreach (var observer in observers)
        {
            observer.Update(@event);
        }
    }
}

// Usage
public class OrderService : EventEmitter
{
    public void PlaceOrder(Order order)
    {
        // Process order...
        Notify(new Event { Type = "ORDER_PLACED", Order = order });
    }
}

public class EmailService : IObserver
{
    public void Update(Event @event)
    {
        if (@event.Type == "ORDER_PLACED")
        {
            SendConfirmation(@event.Order);
        }
    }

    private void SendConfirmation(Order order) { /* ... */ }
}
```

### Template Method

**Purpose:** Define algorithm skeleton, let subclasses override steps.

**When to use:** Common algorithm with varying steps.

```csharp
public abstract class DataExporter
{
    // Template method - defines the algorithm
    public void Export(List<Data> data)
    {
        Validate(data);
        var formatted = Format(data);
        Write(formatted);
        Notify();
    }

    // Common steps
    private void Validate(List<Data> data) { /* ... */ }
    private void Notify() { /* ... */ }

    // Steps to override
    protected abstract string Format(List<Data> data);
    protected abstract void Write(string content);
}

public class CsvExporter : DataExporter
{
    protected override string Format(List<Data> data)
    {
        return string.Join("\n", data.Select(d => d.ToCsv()));
    }

    protected override void Write(string content)
    {
        File.WriteAllText("export.csv", content);
    }
}

public class JsonExporter : DataExporter
{
    protected override string Format(List<Data> data)
    {
        return JsonSerializer.Serialize(data);
    }

    protected override void Write(string content)
    {
        File.WriteAllText("export.json", content);
    }
}
```

### Command

**Purpose:** Encapsulate a request as an object.

**When to use:** Undo/redo, queuing, logging actions.

```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}

public class AddItemCommand : ICommand
{
    private readonly Cart cart;
    private readonly Item item;

    public AddItemCommand(Cart cart, Item item)
    {
        this.cart = cart;
        this.item = item;
    }

    public void Execute()
    {
        cart.Add(item);
    }

    public void Undo()
    {
        cart.Remove(item);
    }
}

public class CommandHistory
{
    private readonly Stack<ICommand> history = new Stack<ICommand>();

    public void Execute(ICommand command)
    {
        command.Execute();
        history.Push(command);
    }

    public void Undo()
    {
        if (history.Count > 0)
        {
            var command = history.Pop();
            command.Undo();
        }
    }
}
```

---

## Pattern Awareness

### The Four-Dimensional Lens

When analyzing new code/libraries, ask:

1. **What problem does it solve?** (Creational, Structural, Behavioral)
2. **What scope?** (Object-level, Class-level, System-level)
3. **When is it applied?** (Compile-time, Runtime)
4. **How coupled?** (Tight, Loose)

This helps recognize patterns even in unfamiliar code.

---

## Anti-Patterns to Avoid

| Anti-Pattern | Problem | Solution |
|--------------|---------|----------|
| **God Object** | Class does everything | Split by responsibility |
| **Spaghetti Code** | Tangled, no structure | Refactor to layers |
| **Golden Hammer** | Using one pattern for everything | Match pattern to problem |
| **Premature Optimization** | Optimizing before needed | YAGNI, profile first |
| **Copy-Paste Programming** | Duplication | Extract, Rule of Three |