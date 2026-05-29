---
applyTo: "**/*.cs"
---

# C# Code Review Instructions
When reviewing C# code, please follow these guidelines to ensure a thorough and constructive review process:

## C# Conventions
- Use latest C# features where appropriate (primary constructors, collection expressions, etc.)
- Prefer `record` types for DTOs and value objects
- Use `init`-only properties on immutable types
- Explicit types over `var` unless the type is obvious from the right-hand side
- No `.Result`, `.Wait()`, or blocking async calls — always `await`
- Constructor injection only — no service locator, no `IServiceProvider` in business logic
- Prefer `IReadOnlyList<T>` / `IReadOnlyCollection<T>` over mutable collections in return types

## 1. Code Quality
- Ensure the code follows C# coding conventions (naming, formatting, etc.).
- Check for proper use of access modifiers (public, private, protected).
- Look for any redundant code or unnecessary complexity.
- Verify that the code is modular and adheres to the Single Responsibility Principle.
- Ensure that the code is well-structured and organized logically.
- Check for proper use of async/await and avoid blocking calls.
