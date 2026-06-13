# Message Queues Module

This module contains a self-contained console demo for Task 1 of the message queues home task.

The sample shows the full flow:
- a ticketing app produces a notification message,
- the message is written to an in-memory queue,
- the notification handler marks the notification as `InProgress` in a persisted store,
- the handler builds an email request and sends it to an email provider,
- the provider returns success or failure and the result is shown in the console.

## Run

```bash
dotnet run --project 08_message_queues/sources/NotificationDemo/NotificationDemo.csproj
```

The demo stores notification states in a local JSON file next to the executable so you can inspect the status transitions after processing.
