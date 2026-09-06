using DA_Assets.CR;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Avrin.Chat
{
    public sealed class ChatMessageView : MonoBehaviour
    {
        [SerializeField] private Image bubble;
        [SerializeField] private RTLTextMeshPro roleText;
        [SerializeField] private RTLTextMeshPro messageText;
        [SerializeField] private HorizontalLayoutGroup rowLayout;
        [SerializeField] private LayoutElement bubbleLayout;
        [SerializeField] private bool userStyle;

        private float _lastRowWidth = -1f;

        private static readonly Color UserBubble = new Color32(86, 99, 246, 255);
        private static readonly Color AssistantBubble = new Color32(31, 38, 53, 255);
        private static readonly Color PrimaryText = new Color32(244, 246, 252, 255);
        private static readonly Color MutedText = new Color32(167, 176, 199, 255);

        public void Setup(Image bubbleImage, RTLTextMeshPro role, RTLTextMeshPro body,
            HorizontalLayoutGroup layout, LayoutElement layoutElement, bool isUser)
        {
            bubble = bubbleImage;
            roleText = role;
            messageText = body;
            rowLayout = layout;
            bubbleLayout = layoutElement;
            userStyle = isUser;
            ApplyStyle();
            RefreshBubbleWidth();
        }

        public void Bind(string message)
        {
            ApplyStyle();
            messageText.text = message;
            RefreshBubbleWidth();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        private void OnEnable()
        {
            RefreshBubbleWidth();
        }

        private void LateUpdate()
        {
            var rowWidth = ((RectTransform)transform).rect.width;
            if (!Mathf.Approximately(rowWidth, _lastRowWidth))
            {
                RefreshBubbleWidth();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshBubbleWidth();
        }

        private void OnValidate()
        {
            ApplyStyle();
            RefreshBubbleWidth();
        }

        private void RefreshBubbleWidth()
        {
            if (bubbleLayout == null || rowLayout == null)
            {
                return;
            }

            var rowRect = (RectTransform)transform;
            var rowWidth = rowRect.rect.width;
            if (rowWidth <= 1f && transform.parent is RectTransform parentRect)
            {
                rowWidth = parentRect.rect.width;
            }

            if (rowWidth <= 1f)
            {
                return;
            }

            _lastRowWidth = rowWidth;
            var availableWidth = Mathf.Max(1f, rowWidth - rowLayout.padding.horizontal);
            var responsiveWidth = Mathf.Min(820f, availableWidth * 0.86f);
            if (!Mathf.Approximately(bubbleLayout.preferredWidth, responsiveWidth))
            {
                bubbleLayout.preferredWidth = responsiveWidth;
                LayoutRebuilder.MarkLayoutForRebuild(rowRect);
            }
        }

        private void ApplyStyle()
        {
            if (bubble != null)
            {
                bubble.color = userStyle ? UserBubble : AssistantBubble;
                var rounder = bubble.GetComponent<CornerRounder>();
                if (rounder != null)
                {
                    rounder.radiiSerialized = userStyle
                        ? new Vector4(30f, 30f, 8f, 30f)
                        : new Vector4(30f, 30f, 30f, 8f);
                }
            }

            if (rowLayout != null)
            {
                rowLayout.childAlignment = userStyle ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            }

            if (roleText != null)
            {
                roleText.text = userStyle ? "شما" : "دستیار";
                roleText.color = userStyle ? new Color32(225, 229, 255, 255) : MutedText;
                roleText.alignment = userStyle ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
                roleText.Farsi = true;
            }

            if (messageText != null)
            {
                messageText.color = PrimaryText;
                messageText.alignment = TextAlignmentOptions.Right;
                messageText.Farsi = true;
            }
        }
    }
}
