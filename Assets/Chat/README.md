# Chat UI base

The `Main` scene contains a mobile-first Persian chat UI with separate user and assistant message prefabs.

## Server integration

- `ChatController` sends `POST /api/chat` with JSON shaped as
  `{ "userId": "unity-trainee", "message": "..." }` and displays the returned
  `{ "reply": "..." }` value.
- The configured server endpoint is `http://2.186.114.140:8000/api/chat`.
- Authentication is intentionally not required yet; requests use the temporary
  `unity-trainee` user id.
- Tap the compact model dropdown beside the message field, then choose fast Qwen
  or accurate Gemma. The choice is saved in `PlayerPrefs` and is included in every
  chat request as the `model` field.
- The matching allowlisted FastAPI implementation is stored at `Server/main.py`.
- The current Android build explicitly includes Internet permission and allows the
  server's plain HTTP endpoint. Replace it with HTTPS before production release.

## Regenerating the UI

Use `Tools > Chat UI > Rebuild Main Chat UI`. This intentionally replaces the contents of the `Main` scene.

UMI is initialized before input fields, uses the Persian keyboard hint, and reports keyboard height so the composer and conversation move above the native keyboard.
