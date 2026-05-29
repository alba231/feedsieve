---
applyTo: **/*.tests/**/*.cs
---


## Testing
- Framework: xUnit + FluentAssertions
- Use `[Theory]` + `[InlineData]` / `[MemberData]` for parameterized tests
- One `Assert` concept per test (multiple `.Should()` chained on the same subject is fine)
- For single-field or two-field assertions use individual `.Should()` chains
- For 3+ fields on the same object use `BeEquivalentTo` with an anonymous object and always include `ExcludingMissingMembers()`:
```csharp
  order.Should().BeEquivalentTo(new { Status = OrderStatus.Pending, ProductId = 123 },
      opts => opts.ExcludingMissingMembers());
```
- Never use `BeEquivalentTo` for time-based or approximate assertions — use `.BeCloseTo()` instead
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Mock with Moq