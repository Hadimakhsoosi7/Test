# Chat UI base

The `Main` scene contains a mobile-first Persian chat UI with separate user and assistant message prefabs.

## Server integration

- Subscribe to `ChatController.MessageSubmitted` and send the received text to your server.
- Disable `Use Mock Replies` on `Chat Controller` when the real service is connected.
- Call `SetWaiting(true)` while the request is running.
- Call `AddAssistantMessage(responseText)` when the response arrives.

## Regenerating the UI

Use `Tools > Chat UI > Rebuild Main Chat UI`. This intentionally replaces the contents of the `Main` scene.

UMI is initialized before input fields, uses the Persian keyboard hint, and reports keyboard height so the composer and conversation move above the native keyboard.
