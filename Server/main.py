from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field
import requests


class UTF8JSONResponse(JSONResponse):
    media_type = "application/json; charset=utf-8"


app = FastAPI(
    title="Multi-model Chat API",
    default_response_class=UTF8JSONResponse,
)

OLLAMA_URL = "http://localhost:11434/api/chat"
POCKETBASE_URL = "http://localhost:8090/api/collections/chats/records"

# Only these client-provided model IDs are accepted.
ALLOWED_MODELS = {
    "qwen2.5:7b": "qwen2.5:7b",
    "gemma4:12b-it-q4_K_M": "gemma4:12b-it-q4_K_M",
}
DEFAULT_MODEL = "qwen2.5:7b"

SYSTEM_PROMPT = """
تو یک دستیار هوشمند، دقیق و خوش‌برخورد هستی.
همیشه به زبان کاربر پاسخ بده.
پاسخ‌ها را روشن، کاربردی و تا حد امکان مختصر ارائه کن.
اگر درباره چیزی مطمئن نیستی، صادقانه اعلام کن و اطلاعات ساختگی ارائه نده.
""".strip()


class ChatRequest(BaseModel):
    userId: str = Field(min_length=1, max_length=200)
    message: str = Field(min_length=1, max_length=4000)
    model: str = DEFAULT_MODEL


class ChatResponse(BaseModel):
    reply: str
    model: str


@app.get("/")
def root():
    return {"status": "online", "defaultModel": DEFAULT_MODEL}


@app.get("/health")
def health():
    try:
        response = requests.get("http://localhost:11434/api/tags", timeout=5)
        response.raise_for_status()
        installed = {item.get("name", "") for item in response.json().get("models", [])}
        return {
            "status": "healthy",
            "ollama": "connected",
            "availableModels": [
                model_id
                for model_id, ollama_name in ALLOWED_MODELS.items()
                if ollama_name in installed
            ],
        }
    except requests.RequestException as error:
        raise HTTPException(status_code=503, detail=f"Ollama is unavailable: {error}")


@app.post("/api/chat", response_model=ChatResponse)
def chat_with_ai(request_data: ChatRequest):
    message = request_data.message.strip()
    model_name = ALLOWED_MODELS.get(request_data.model)

    if not message:
        raise HTTPException(status_code=400, detail="Message cannot be empty.")
    if model_name is None:
        raise HTTPException(status_code=400, detail="The selected model is not allowed.")

    ollama_payload = {
        "model": model_name,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": message},
        ],
        "stream": False,
        "think": False,
        "keep_alive": "30m",
        "options": {
            "num_ctx": 4096,
            "num_predict": 500,
            "temperature": 0.25,
            "repeat_penalty": 1.1,
        },
    }

    try:
        ollama_response = requests.post(
            OLLAMA_URL,
            json=ollama_payload,
            timeout=(10, 180),
        )
        ollama_response.raise_for_status()
        ai_reply = ollama_response.json().get("message", {}).get("content", "").strip()
        if not ai_reply:
            raise HTTPException(status_code=502, detail="Ollama returned an empty response.")
    except requests.Timeout:
        raise HTTPException(status_code=504, detail="Ollama response timed out.")
    except requests.RequestException as error:
        error_body = error.response.text if error.response is not None else ""
        print(f"Ollama request failed: {error}\n{error_body}")
        raise HTTPException(status_code=502, detail=f"Ollama connection error: {error}")
    except ValueError as error:
        raise HTTPException(status_code=502, detail=f"Invalid response from Ollama: {error}")

    try:
        pocketbase_response = requests.post(
            POCKETBASE_URL,
            json={
                "user_id": request_data.userId,
                "user_message": message,
                "ai_reply": ai_reply,
            },
            timeout=5,
        )
        if not pocketbase_response.ok:
            print("PocketBase warning:", pocketbase_response.status_code, pocketbase_response.text)
    except requests.RequestException as error:
        print(f"PocketBase warning: {error}")

    return ChatResponse(reply=ai_reply, model=request_data.model)
