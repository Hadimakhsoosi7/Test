using System;
using System.Collections;
using RTLTMPro;
using TMPro;
using UMI;
using UnityEngine;
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

        [Header("Prototype")]
        [SerializeField] private bool showWelcomeMessage = true;
        [SerializeField] private bool useMockReplies = true;

        private const float BaseConversationBottom = 210f;
        private const float BaseInputBottom = 18f;
        private const float BaseInputTop = 182f;

        private float _keyboardHeight;
        private bool _isWaiting;

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
                AddAssistantMessage("سلام! من دستیار هوشمند شما هستم. فعلاً رابط چت آماده است و در مرحله‌ی بعد می‌توانیم من را به مدل روی سرور شما متصل کنیم.");
            }
        }

        private void OnDestroy()
        {
            MobileInput.OnKeyboardAction -= OnKeyboardAction;
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

            if (useMockReplies)
            {
                StartCoroutine(ShowMockReply());
            }
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

        private IEnumerator ShowMockReply()
        {
            SetWaiting(true);
            yield return new WaitForSeconds(0.65f);
            AddAssistantMessage("پیام شما دریافت شد. این پاسخ فعلاً آزمایشی است؛ API سرور بعداً از همین نقطه جایگزین می‌شود.");
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
            ApplyInsets(extraHeight);
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
            ScrollToBottom();
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
