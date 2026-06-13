1.  What are the differences between performance, load, and stress testing?

- Performance testing checks how fast and efficient the system is under expected conditions. It focuses on latency, throughput, and resource usage for normal workloads.
- Load testing checks how the system behaves under a target or expected workload. It helps verify that the system can handle the anticipated number of users or requests.
- Stress testing pushes the system beyond normal or expected limits to find the breaking point and observe how it fails and recovers.

2.  When would you prefer vertical scaling over horizontal?

- When the application is small or not yet designed for multiple instances.
- When the bottleneck is CPU, memory, or disk on a single machine and upgrading hardware is the fastest fix.
- When the system depends on local state, local files, or stateful components that are hard to distribute.
- When operational simplicity matters more than fault tolerance or elastic scaling.

3.  Does ASP.NET Core API support horizontal scaling? Explain your answer.

Yes. ASP.NET Core API can be horizontally scaled because the framework is stateless by default and runs well behind a load balancer on multiple instances. The real requirement is that application state must not live only in one process instance. If sessions, caches, or concurrency-sensitive data are stored in memory, the app will not scale correctly until that state is externalized to a database, distributed cache, or another shared store.