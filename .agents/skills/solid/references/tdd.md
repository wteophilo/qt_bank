# Test-Driven Development

## The Core Loop

```
RED → GREEN → REFACTOR → RED → ...
```

### RED Phase
Write a failing test that describes the behavior you want. The test should:
- Use domain language, not technical jargon
- Describe WHAT, not HOW
- Be a concrete example, not an abstract statement

```csharp
// BAD: Abstract
[Fact]
public void CanAddNumbers() { /* ... */ }

// GOOD: Concrete example
[Fact]
public void WhenAdding2Plus3_Returns5() { /* ... */ }
```

### GREEN Phase
Write the **simplest possible code** to make the test pass. Two strategies:

1. **Fake It** - Return a hardcoded value
   ```csharp
   public int Add(int a, int b)
   {
       return 5; // Simplest thing!
   }
   ```

2. **Obvious Implementation** - If you know the solution
   ```csharp
   public int Add(int a, int b)
   {
       return a + b;
   }
   ```

**Prefer Fake It** when learning or unsure. Let more tests drive the real implementation.

### REFACTOR Phase
This is where **design happens**. Look for:
- Duplication (but wait for Rule of Three)
- Long methods to extract
- Poor names to improve
- Complex conditions to simplify

## The Three Laws of TDD

1. **No production code** without a failing test
2. **No more test code** than sufficient to fail (compilation failures count)
3. **No more production code** than sufficient to pass the one failing test

## The Rule of Three

**Only extract duplication when you see it THREE times.**

Why? Wrong abstractions are worse than duplication. Wait for the pattern to emerge.

```csharp
// Duplication #1 - Leave it
// Duplication #2 - Note it, leave it
// Duplication #3 - NOW extract it
```

## Triangulation

Each new test "sculpts" the solution toward a general, robust implementation.

Think of **degrees of freedom** - like a car that needs forward/back, left/right, and rotation. Each test carves out one degree of freedom until the implementation handles all cases.

## Transformation Priority Premise

When going from RED to GREEN, prefer simpler transformations:

| Priority | Transformation |
|----------|----------------|
| 1 | {} → nil |
| 2 | nil → constant |
| 3 | constant → variable |
| 4 | unconditional → conditional |
| 5 | scalar → collection |
| 6 | statement → recursion |
| 7 | value → mutated value |

Higher priority = simpler. Avoid jumping to complex transformations too early.

## Arrange-Act-Assert

Structure every test:

```csharp
[Fact]
public void CalculatesTotalWithDiscount()
{
    // ARRANGE - Set up the world
    var order = new Order();
    order.AddItem(new Item { Price = 100 });
    var discount = new PercentDiscount(10);

    // ACT - Execute the behavior
    var total = order.CalculateTotal(discount);

    // ASSERT - Verify the outcome
    Assert.Equal(90, total);
}
```

## Writing Tests Backwards

Sometimes it helps to write AAA in reverse:
1. Write the ASSERT first - what do you want to verify?
2. Write the ACT - what action produces that result?
3. Write the ARRANGE - what setup is needed?

## Test Naming Principles

- Use **behavior-driven names** with domain language
- Provide **concrete examples**, not abstract statements
- **One example per test** for easy debugging
- Avoid leaking implementation details

```csharp
// BAD: Technical, implementation-focused
[Fact]
public void ShouldSetDataPropertyTo1() { /* ... */ }

// GOOD: Behavior-focused, domain language
[Fact]
public void ShouldRecognizeMomAsAPalindrome() { /* ... */ }
```

## Classic vs Mockist TDD

**Classic (Detroit/Chicago) TDD:**
- Test with real dependencies
- Higher confidence, slower tests
- Best for: Pure functions, integration tests

**Mockist (London) TDD:**
- Mock external dependencies
- Faster tests, more isolated
- Best for: Classes with infrastructure dependencies

Start with Classic TDD to learn the technique. Add mocks when testing code with databases, APIs, etc.

## Common Mistakes

1. **Writing code before tests** - Violates the fundamental principle
2. **Writing too much test** - Just enough to fail
3. **Writing too much code** - Just enough to pass
4. **Skipping refactor** - This is where design lives
5. **Testing implementation** - Test behavior, not how it's done
6. **Abstract test names** - Use concrete examples
7. **Extracting too early** - Wait for Rule of Three