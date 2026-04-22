1.  What diagram types do you have on your current project?

    The project uses domain/architecture diagrams (`.drawio` files), including component diagrams for the ticketing system architecture and Entity-Relationship (ER) diagrams for the persistence layer.

2.  What are the pros and cons of UML diagrams?

    **Pros:**
    - Standardized notation understood across teams
    - Improves communication between developers and stakeholders
    - Helps visualize complex systems before coding
    - Serves as living documentation

    **Cons:**
    - Time-consuming to create and maintain
    - Can become outdated quickly if not kept in sync with code
    - May be overly complex for small projects
    - Requires tooling and UML knowledge

3.  What is the difference between a structural diagram and a behavioral diagram in UML?

    - **Structural diagrams** describe the static structure of the system — *what exists* (e.g., Class, Component, Deployment diagrams).
    - **Behavioral diagrams** describe dynamic behavior — *how the system works over time* (e.g., Use Case, Sequence, Activity, State Machine diagrams).

4.  Name the relationship types in a use case diagram

    - **Association** — an actor interacts with a use case.
    - **Include** — one use case always includes another (mandatory sub-flow).
    - **Extend** — one use case optionally extends another (conditional behavior).
    - **Generalization** — an actor or use case inherits from another (parent/child relationship).

5.  What is the sequence diagram?

    A behavioral UML diagram that shows how objects interact over time in a specific scenario. It depicts the ordered exchange of messages between participants (actors, objects, services) along a vertical timeline, making it useful for modeling API flows, method call chains, and inter-service communication.