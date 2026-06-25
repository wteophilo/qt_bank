# Interface Design for Testability

Good interfaces make testing natural:

1. **Accept dependencies, don't create them**

   ```csharp
   // Testable
   public void ProcessOrder(Order order, IPaymentGateway paymentGateway) {}

   // Hard to test
   public void ProcessOrder(Order order)
   {
       var gateway = new StripePaymentGateway();
   }
   ```

2. **Return results, don't produce side effects**

   ```csharp
   // Testable
   public Discount CalculateDiscount(Cart cart) {}

   // Hard to test
   public void ApplyDiscount(Cart cart)
   {
       cart.Total -= _discount;
   }
   ```

3. **Small surface area**
   - Fewer methods = fewer tests needed
   - Fewer params = simpler test setup
