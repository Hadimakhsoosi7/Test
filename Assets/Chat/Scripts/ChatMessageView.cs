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
        [SerializeField] private bool userStyle;

        private static readonly Color UserBubble = new Color32(86, 99, 246, 255);
        private static readonly Color AssistantBubble = new Color32(31, 38, 53, 255);
        private static readonly Color PrimaryText = new Color32(244, 246, 252, 255);
        private static readonly Color MutedText = new Color32(167, 176, 199, 255);

        public void Setup(Image bubbleImage, RTLTextMeshPro role, RTLTextMeshPro body, HorizontalLayoutGroup layout, bool isUser)
        {
            bubble = bubbleImage;
            roleText = role;
            messageText = body;
            rowLayout = layout;
            userStyle = isUser;
            ApplyStyle();
        }

        public void Bind(string message)
        {
            ApplyStyle();
            messageText.text = message;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        private void OnValidate()
        {
            ApplyStyle();
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
