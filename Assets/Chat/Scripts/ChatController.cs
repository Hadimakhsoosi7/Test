using System;
using System.Collections;
using RTLTMPro;
using TMPro;
using UMI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Avrin.Chat
{
    public sealed class ChatController : MonoBehaviour
    {
        [Header("Conversation")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform conversationRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject assistantMessagePrefab;
        [SerializeField] private GameObject typingIndicator;

        [Header("Composer")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private MobileInputField mobileInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private RectTransform inputDock;
        [SerializeField] private RectTransform canvasRect;

        [Header("Server")]
        [Tooltip("FastAPI chat endpoint.")]
        [SerializeField] private string apiUrl = "http://2.186.114.140:8000/api/chat";
        [SerializeField] private string userId = "unity-trainee";
        [Min(1)]
        [SerializeField] private int requestTimeoutSeconds = 120;

        [Header("Conversation Start")]
        [SerializeField] private bool showWelcomeMessage = true;

        private const float BaseConversationBottom = 210f;
        private const float BaseInputBottom = 18f;
        private const float BaseInputTop = 182f;

        private float _keyboardHeight;
        private float _composerExtraHeight;
        private bool _isWaiting;
        private Coroutine _nativeRectSyncRoutine;

        public event Action<string> MessageSubmitted;

        public void Setup(
            ScrollRect list,
            RectTransform listRect,
            RectTransform listContent,
            GameObject userPrefab,
            GameObject assistantPrefab,
            GameObject typing,
            TMP_InputField field,
            MobileInputField nativeField,
            Button send,
            RectTransform dock,
            RectTransform canvas)
        {
            scrollRect = list;
            conversationRect = listRect;
            content = listContent;
            userMessagePrefab = userPrefab;
            assistantMessagePrefab = assistantPrefab;
            typingIndicator = typing;
            inputField = field;
            mobileInput = nativeField;
            sendButton = send;
            inputDock = dock;
            canvasRect = canvas;
        }

        private void Start()
        {
            sendButton.onClick.AddListener(SendCurrentMessage);
            inputField.onValueChanged.AddListener(OnInputChanged);
            mobileInput.OnReturnPressed += SendCurrentMessage;
            MobileInput.OnKeyboardAction += OnKeyboardAction;
            typingIndicator.SetActive(false);
            RefreshComposer(string.Empty);

            if (showWelcomeMessage)
            {
                AddAssistantMessage("سلام! من دستیار هوشمند شما هستم. سؤال خود را بپرسید.");
            }
        }

        private void OnDestroy()
        {
            MobileInput.OnKeyboardAction -= OnKeyboardAction;
            if (_nativeRectSyncRoutine != null)
            {
                StopCoroutine(_nativeRectSyncRoutine);
            }

            if (mobileInput != null)
            {
                mobileInput.OnReturnPressed -= SendCurrentMessage;
            }
        }

        public void SendCurrentMessage()
        {
            if (_isWaiting)
            {
                return;
            }

            var message = inputField.text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            AddMessage(userMessagePrefab, message);
            mobileInput.Text = string.Empty;
            inputField.SetTextWithoutNotify(string.Empty);
            RefreshComposer(string.Empty);
            MessageSubmitted?.Invoke(message);
            StartCoroutine(SendToServer(message));
        }

        public void AddAssistantMessage(string message)
        {
            _isWaiting = false;
            typingIndicator.SetActive(false);
            AddMessage(assistantMessagePrefab, message);
        }

        public void SetWaiting(bool waiting)
        {
            _isWaiting = waiting;
            typingIndicator.SetActive(waiting);
            if (waiting)
            {
                typingIndicator.transform.SetAsLastSibling();
            }
            sendButton.interactable = !waiting && !string.IsNullOrWhiteSpace(inputField.text);
            ScrollToBottom();
        }

        private void AddMessage(GameObject prefab, string message)
        {
            var instance = Instantiate(prefab, content);
            instance.name = prefab == userMessagePrefab ? "User Message" : "Assistant Message";
            instance.SetActive(true);
            instance.GetComponent<ChatMessageView>().Bind(message);
            typingIndicator.transform.SetAsLastSibling();
            ScrollToBottom();
        }

        private IEnumerator SendToServer(string message)
        {
            SetWaiting(true);

            var payload = new ChatRequest
            {
                userId = string.IsNullOrWhiteSpace(userId) ? SystemInfo.deviceUniqueIdentifier : userId.Trim(),
                message = message
            };
            var json = JsonUtility.ToJson(payload);

            using (var request = new UnityWebRequest(apiUrl.Trim(), UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                request.timeout = Mathf.Max(1, requestTimeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    var detail = TryReadErrorDetail(request.downloadHandler.text);
                    Debug.LogError($"Chat API failed ({request.responseCode}): {request.error}\n{request.downloadHandler.text}");
                    AddAssistantMessage(string.IsNullOrEmpty(detail)
                        ? "ارتباط با سرور برقرار نشد. لطفاً آدرس سرور و اتصال شبکه را بررسی کنید."
                        : $"خطای سرور: {detail}");
                    yield break;
                }

                ChatResponse response;
                try
                {
                    response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Invalid Chat API response: {exception.Message}\n{request.downloadHandler.text}");
                    AddAssistantMessage("پاسخ نامعتبر از سرور دریافت شد.");
                    yield break;
                }

                if (response == null || string.IsNullOrWhiteSpace(response.reply))
                {
                    Debug.LogError($"Chat API response has no reply: {request.downloadHandler.text}");
                    AddAssistantMessage("سرور پاسخ خالی برگرداند.");
                    yield break;
                }

                AddAssistantMessage(response.reply.Trim());
            }
        }

        private static string TryReadErrorDetail(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            try
            {
                var error = JsonUtility.FromJson<ChatErrorResponse>(json);
                return error?.detail ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        [Serializable]
        private sealed class ChatRequest
        {
            public string userId;
            public string message;
        }

        [Serializable]
        private sealed class ChatResponse
        {
            public string reply = string.Empty;
        }

        [Serializable]
        private sealed class ChatErrorResponse
        {
            public string detail = string.Empty;
        }

        private void OnInputChanged(string value)
        {
            RefreshComposer(value);
        }

        private void RefreshComposer(string value)
        {
            sendButton.interactable = !_isWaiting && !string.IsNullOrWhiteSpace(value);
            var preferred = inputField.textComponent.GetPreferredValues(value, inputField.textViewport.rect.width, 0f).y;
            var extraHeight = Mathf.Clamp(preferred - 52f, 0f, 110f);
            var heightChanged = !Mathf.Approximately(_composerExtraHeight, extraHeight);
            _composerExtraHeight = extraHeight;
            ApplyInsets(extraHeight);

            if (heightChanged && _keyboardHeight > 0f)
            {
                RequestNativeInputRectSync();
            }
        }

        private void OnKeyboardAction(bool isVisible, int nativeHeight)
        {
            if (!isVisible)
            {
                _keyboardHeight = 0f;
            }
            else
            {
                var scale = MobileInput.GetScreenScale();
                var ratio = (float)Screen.height / canvasRect.rect.height / Mathf.Max(scale, 0.01f);
                _keyboardHeight = nativeHeight / Mathf.Max(ratio, 0.01f);
#if UNITY_IOS
                if (scale >= 3f)
                {
                    _keyboardHeight *= 2.8f / scale;
                }
#endif
            }

            RefreshComposer(inputField.text);
            RequestNativeInputRectSync();
            ScrollToBottom();
        }

        private void RequestNativeInputRectSync()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!isActiveAndEnabled || mobileInput == null || inputField == null)
            {
                return;
            }

            if (_nativeRectSyncRoutine != null)
            {
                StopCoroutine(_nativeRectSyncRoutine);
            }

            _nativeRectSyncRoutine = StartCoroutine(SyncNativeInputRect());
#endif
        }

        private IEnumerator SyncNativeInputRect()
        {
            // UMI renders the editable text as a native view above Unity's canvas.
            // Resend its rect after both the Unity layout and the OS keyboard animation settle.
            yield return new WaitForEndOfFrame();
            ForceNativeInputRectUpdate();
            yield return new WaitForSecondsRealtime(0.08f);
            ForceNativeInputRectUpdate();
            yield return new WaitForSecondsRealtime(0.12f);
            ForceNativeInputRectUpdate();
            _nativeRectSyncRoutine = null;
        }

        private void ForceNativeInputRectUpdate()
        {
            if (mobileInput == null || inputField == null || inputField.textComponent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (inputDock != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(inputDock);
            }

            // SetRectNative caches its last rect. Sending a different valid rect first
            // guarantees that the final text rect is forwarded to the native control.
            mobileInput.SetRectNative(inputField.transform as RectTransform);
            mobileInput.SetRectNative(inputField.textComponent.rectTransform);
        }

        private void ApplyInsets(float extraHeight)
        {
            inputDock.offsetMin = new Vector2(20f, BaseInputBottom + _keyboardHeight);
            inputDock.offsetMax = new Vector2(-20f, BaseInputTop + _keyboardHeight + extraHeight);
            conversationRect.offsetMin = new Vector2(0f, BaseConversationBottom + _keyboardHeight + extraHeight);
        }

        private void ScrollToBottom()
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
