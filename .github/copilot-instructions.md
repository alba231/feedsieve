## Stack
- .NET 9, C# 13
- Angular (frontend)
- Azure / GCP
- Terraform (infrastructure)

## Architecture
- Repository pattern for data access
- No business logic in controllers — delegate to services/handlers
- Prefer MediatR handlers (CQRS) for application logic if already used in the project
- Keep controllers thin: validate input, call handler, return result

## Error Handling
- Use result types or custom exceptions — no swallowing exceptions silently
- Validate at boundaries (controllers, public service methods); trust internals