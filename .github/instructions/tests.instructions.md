---
applyTo: **/*.tests/**/*.cs
---


## Testing
- Framework: xUnit + FluentAssertions
- Use `[Theory]` + `[InlineData]` / `[MemberData]` for parameterized tests
- One `Assert` concept per test
- For single-field or two-field assertions use individual `.Should()` chains
- For 3+ fields on the same object use `BeEquivalentTo` with an anonymous object`:
```csharp
  order.Should().BeEquivalentTo(new { Status = OrderStatus.Pending, ProductId = 123 });
```
- Never use `BeEquivalentTo` for time-based or approximate assertions — use `.BeCloseTo()` instead
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Mock with Moq
- Always use AAA (Arrange, Act, Assert) pattern for test structure. Add comment lines to separate the three sections for clarity. Separate the three sections with a blank line between them.