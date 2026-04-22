1.  What are your steps to start designing database?
	- Clarify business use-cases and core entities from the domain model.
	- Identify relationships, cardinality, and key constraints.
	- Define non-functional needs: consistency, scale, query patterns, retention, audit.
	- Choose storage type (SQL/NoSQL) based on access patterns and consistency requirements.
	- Draft ER model, normalize (typically up to 3NF for OLTP), then denormalize only for proven read needs.
	- Define indexes, unique constraints, foreign keys, and transaction boundaries.
	- Validate model with sample queries and edge cases (concurrency, deletes, historical data).
	- Implement migration strategy and seed/test data.
2.  When can we say that our database is modeled correctly?
	- It correctly supports required business operations without data anomalies.
	- Integrity is enforced by schema constraints (PK/FK/unique/check) instead of application-only logic.
	- Expected queries are efficient with realistic data volumes (verified by execution plans/benchmarks).
	- Concurrency and transaction behavior preserve consistency.
	- Model is maintainable: clear naming, predictable relationships, and safe migration path.
3.  What is a Data Access Layer (DAL), and how does it simplify database interactions?
	- DAL is an abstraction layer between business logic and persistence storage.
	- It centralizes CRUD operations, query logic, transactions, and mapping concerns.
	- It improves maintainability by isolating DB-specific code from domain/services.
	- It improves testability by allowing mocks/fakes for repositories/interfaces.
	- It enables switching or evolving persistence technology with minimal impact on upper layers.
4.  You need to implement a new service for a customer. How would you select database (SQL or NoSQL)?
	- Start from domain consistency rules and query/access patterns.
	- Prefer SQL when strong consistency, relational joins, and transactional integrity are primary.
	- Prefer NoSQL when horizontal scale, flexible schema, high write throughput, or document/key access dominates.
	- Evaluate latency/SLA, reporting/analytics needs, team skills, and operational cost.
	- Consider polyglot persistence if different subdomains have different storage needs.
	- Validate choice with a small spike/prototype and workload-oriented benchmarking.