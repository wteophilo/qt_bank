# Clean Code Practices

## What is Clean Code?

Code that is:
- **Easy to understand** - reveals intent clearly
- **Easy to change** - modifications are localized
- **Easy to test** - dependencies are injectable
- **Simple** - no unnecessary complexity

## The Human-Centered Approach

Code has THREE consumers:
1. **Users** - get their needs met
2. **Customers** - make or save money
3. **Developers** - must maintain it

Design for all three, but remember: **developers read code 10x more than they write it.**

## Naming Principles

### 1. Consistency & Uniqueness (HIGHEST PRIORITY)
Same concept = same name everywhere. One name per concept.

```csharp
// BAD: Inconsistent names for same concept
GetUserById(id);
GetCustomerById(id);
GetClientById(id);

// GOOD: Consistent
GetUser(id);
GetOrder(id);
GetProduct(id);
```

### 2. Understandability
Use domain language, not technical jargon.

```csharp
// BAD: Technical
var arr = users.filter(u => u.isActive);

// GOOD: Domain language
var activeCustomers = users.filter(user => user.isActive);
```

### 3. Specificity
Avoid vague names: `data`, `info`, `manager`, `handler`, `processor`, `utils`

```csharp
// BAD: Vague
class DataManager { }
public static processInfo(data) { }

// GOOD: Specific
class OrderRepository { }
public static validatePayment(payment) { }
```

### 4. Brevity (but not at cost of clarity)
Short names are good only if meaning is preserved.

```csharp
// BAD: Too cryptic
var usrLst = getUsrs();

// BAD: Unnecessarily long
var listOfAllActiveUsersInTheSystem = getActiveUsers();

// GOOD: Brief but clear
var activeUsers = getActiveUsers();
```

### 5. Searchability
Names should be unique enough to grep/search.

```csharp
// BAD: Common word, hard to search
var data = fetch();

// GOOD: Unique, searchable
var orderSummary = fetchOrderSummary();
```

### 6. Pronounceability
You should be able to say it in conversation.

```csharp
// BAD
var genymdhms = generateYearMonthDayHourMinuteSecond();

// GOOD
var timestamp = generateTimestamp();
```

### 7. Austerity
Avoid unnecessary filler words.

```csharp
// BAD: Redundant
var userData = user; // 'Data' adds nothing
class UserClass { }    // 'Class' adds nothing

// GOOD
var user = user;
class User { }
```

---

## Object Calisthenics (9 Rules)

Exercises to improve OO design. Follow strictly during practice, relax slightly in production.

### 1. One Level of Indentation per Method

```csharp
// BAD: Multiple levels
public static process(orders: Order[]) {
  for (var order of orders) {
    if (order.isValid()) {
      for (var item of order.items) {
        if (item.inStock) {
          // process...
        }
      }
    }
  }
}

// GOOD: Extract methods
public static process(orders: Order[]) {
  orders.filter(o => o.isValid()).forEach(processOrder);
}

public static processOrder(order: Order) {
  order.items.filter(i => i.inStock).forEach(processItem);
}
```

### 2. Don't Use the ELSE Keyword

Use early returns, guard clauses, or polymorphism.

```csharp
// BAD: else
public static getDiscount(user: User) {
  if (user.isPremium) {
    return 20;
  } else {
    return 0;
  }
}

// GOOD: Early return
public static getDiscount(user: User) {
  if (user.isPremium) return 20;
  return 0;
}
```

### 3. Wrap All Primitives and Strings

Primitives should be wrapped in domain objects when they have meaning.

```csharp
// BAD: Primitive obsession
public static createUser(email, age) { }

// GOOD: Value objects
public class Email
{
    private readonly string _value;

    public Email(string value)
    {
        if (!IsValid(value))
            throw new InvalidEmailException();

        _value = value;
    }

    private bool IsValid(string email) => true;
}

class Age {
  constructor(private value) {
    if (value < 0 || value > 150) throw new InvalidAge();
  }
}

public static createUser(email: Email, age: Age) { }
```

### 4. First-Class Collections

Any class with a collection should have no other instance variables.

```csharp
// BAD: Collection mixed with other state
class Order {
  items: OrderItem[] = [];
  customerId;
  total;
}

// GOOD: Collection is its own class
class OrderItems {
  constructor(private items: OrderItem[] = []) {}

  add(item: OrderItem): void { ... }
  total(): Money { ... }
  isEmpty() { ... }
}

class Order {
  constructor(
    private items: OrderItems,
    private customerId: CustomerId
  ) {}
}
```

### 5. One Dot per Line (Law of Demeter)

Don't chain through object graphs.

```csharp
// BAD: Train wreck
var city = order.customer.address.city;

// GOOD: Tell, don't ask
var city = order.getShippingCity();
```

### 6. Don't Abbreviate

If a name is too long to type, the class is doing too much.

```csharp
// BAD
var custRepo = new CustRepo();
var ord = new Ord();

// GOOD
var customerRepository = new CustomerRepository();
var order = new Order();
```

### 7. Keep All Entities Small

- Classes: < 50 lines
- Methods: < 10 lines
- Files: < 100 lines

If larger, it's probably doing too much. Split it.

### 8. No Classes with More Than Two Instance Variables

Forces small, focused classes.

```csharp
// BAD: Too many variables
class Order {
  id;
  customerId;
  items: Item[];
  total;
  status;
}

// GOOD: Composed of smaller objects
class Order {
  constructor(
    private id: OrderId,
    private details: OrderDetails
  ) {}
}

class OrderDetails {
  constructor(
    private customer: Customer,
    private lineItems: LineItems
  ) {}
}
```

### 9. No Getters/Setters/Properties

Objects should have behavior, not just data. Tell objects what to do.

```csharp
// BAD: Data bag with getters
class Account {
  getBalance() { return this.balance; }
  setBalance(value) { this.balance = value; }
}

// Caller does the work
if (account.getBalance() >= amount) {
  account.setBalance(account.getBalance() - amount);
}

// GOOD: Behavior-rich object
class Account {
  withdraw(amount: Money): WithdrawResult {
    if (!this.canWithdraw(amount)) {
      return WithdrawResult.insufficientFunds();
    }
    this.balance = this.balance.subtract(amount);
    return WithdrawResult.success();
  }
}

// Caller tells, object decides
var result = account.withdraw(amount);
```

---

## Comments

### When to Write Comments

**Only write comments to explain WHY, not WHAT or HOW.**

Code explains what and how. Comments explain business reasons, non-obvious decisions, or warnings.

```csharp
// BAD: Explains what (redundant)
// Add 1 to counter
counter++;

// GOOD: Explains why
// Compensate for 0-based indexing in legacy API
counter++;
```

### Prefer Self-Documenting Code

Instead of commenting, rename to make intent clear.

```csharp
// BAD: Comment needed
// Check if user can access premium features
if (user.subscriptionLevel >= 2 && !user.isBanned) { }

// GOOD: Self-documenting
if (user.canAccessPremiumFeatures()) { }
```

---

## Formatting

### Vertical Spacing
- Related code together
- Blank lines between concepts
- Most important/public at top

### Horizontal Spacing
- Consistent indentation
- Space around operators
- Max line length ~80-120 characters

### Storytelling
Code should read top-to-bottom like a story. High-level at top, details below.

```csharp
class OrderProcessor {
  // Public API first
  process(order: Order): ProcessResult {
    this.validate(order);
    this.calculateTotals(order);
    return this.save(order);
  }

  // Supporting methods below, in order of appearance
  private validate(order: Order): void { ... }
  private calculateTotals(order: Order): void { ... }
  private save(order: Order): ProcessResult { ... }
}
```