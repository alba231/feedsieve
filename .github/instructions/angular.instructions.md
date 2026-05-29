---
applyTo: **/*.ts, **/*.html, **/*.scss
---

## Angular
- Follow existing component structure and naming conventions
- Prefer standalone components
- Use signals over RxJS where Angular version supports it
- Use Angular Material components where possible for consistency
- Keep component logic focused on presentation; delegate business logic to services
- Use `OnPush` change detection strategy for better performance
- Use `async` pipe in templates instead of manual subscription management
- For forms, prefer reactive forms over template-driven forms for better scalability and testability
- Ensure proper accessibility (ARIA attributes, keyboard navigation, etc.)
- Use SCSS variables and mixins for consistent styling across the app