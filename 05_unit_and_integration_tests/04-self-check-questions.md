# Self-check

1. What are benefits and drawbacks of unit tests?

  Benefits:

    - Very fast feedback.
    - Easy to run frequently in local development and CI.
    - Good isolation of business logic and edge cases.
    - Makes refactoring safer.

  Drawbacks:

    - Can miss integration/configuration problems.
    - Excessive mocking can make tests unrealistic.
    - High unit coverage alone does not guarantee system correctness.

2. What are benefits and drawbacks of integration tests?

  Benefits:

    - Validate cooperation of real components (API, DB, repositories).
    - Catch wiring issues (DI, mappings, serialization, transactions).
    - Provide stronger confidence for real workflows.

  Drawbacks:

    - Slower and more expensive than unit tests.
    - More setup and maintenance effort.
    - Failures are sometimes harder to diagnose.

3. What are benefits and drawbacks of end-to-end tests?

  Benefits:

    - Closest to real user behavior.
    - Verifies full system behavior from entry point to data layer.
    - Useful as release confidence checks for critical paths.

  Drawbacks:

    - Slowest and most costly test type.
    - More prone to flaky failures.
    - Broad failures can hide exact root cause.

4. When/why you would do database integration tests?

  Use database integration tests when persistence behavior is important for correctness:

    - Query behavior (filters, joins, ordering, projections).
    - Migrations and schema compatibility.
    - Transaction semantics (commit/rollback).
    - Concurrency-sensitive flows (for example seat booking state changes).
    - Provider-specific behavior that mocks cannot reproduce.

5. How testing trophy differs from testing pyramid model?

  Testing pyramid emphasizes mostly unit tests, fewer integration tests, and very few E2E tests.

  Testing trophy gives relatively more weight to integration tests and treats them as a primary confidence layer, with unit tests and a smaller E2E set around them (plus static checks).

6. What code coverage metrics do you know? What metric would you use?

  Common metrics:

    - Line coverage.
    - Branch coverage.
    - Method/function coverage.
    - Statement coverage.

  Practical choice:

    - Use line + branch coverage together.
    - Track trends over time and critical-path coverage quality, not just a single percentage.

7. What is practically reasonable percent of code coverage?

  There is no universal target for all projects.

  In practice, around 70-85% is often reasonable for business systems, with higher expectations for critical modules (payments, ordering, security, concurrency).

  Focus should stay on meaningful tests for high-risk logic, not only on increasing the raw number.
