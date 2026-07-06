1.  What is a message queue? What do message queues store and transfer? 
	A message queue is an intermediary system for asynchronous communication between producers and consumers. It stores messages temporarily and transfers them later, usually in a reliable way, so the sender does not need to wait for the receiver to process the request immediately.

2.  Describe the publisher/subscriber pattern. The difference between Pub/Sub and Observable patterns.
	In pub/sub, publishers send events to a topic or exchange, and multiple subscribers can receive the same event independently. In the Observable pattern, an object notifies its registered observers about state changes, but it is usually tied to a concrete source object and direct observer registration. Pub/sub is more decoupled and often broker-based; Observable is a more local object-oriented pattern.

3.  What is a Message Bus? How does it work? 
	A message bus is a shared communication infrastructure that routes messages between different parts of a system. It allows services or modules to communicate without knowing each other directly. The bus receives a message, applies routing rules, and delivers it to the correct handler, which makes integration more scalable and loosely coupled.

4.  What is the difference between message queue and web services? 
	Message queues support asynchronous, decoupled communication and help with buffering, retries, and load smoothing. Web services are usually synchronous request/response interactions where the caller waits for the result immediately. Use a queue when work can happen later; use a web service when an immediate response is required.

5.  Describe the difference between RabbitMQ and Kafka. Provide some use cases for each of them: in which scenarios you’ll use RabbitMQ, Kafka?
	RabbitMQ is a general-purpose message broker that is good for task queues, command dispatching, routing, and workflows that need flexible delivery semantics. Kafka is a distributed event streaming platform designed for high-throughput event logs, replay, and long-term event processing. Use RabbitMQ for classic messaging, background jobs, and point-to-point or pub/sub routing. Use Kafka for event streaming, analytics pipelines, audit logs, and systems that need to replay events or process large event volumes.