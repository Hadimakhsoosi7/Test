#if UNITY_EDITOR
using Avrin.Chat;
using DA_Assets.CR;
using RTLTMPro;
using TMPro;
using UMI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Avrin.Chat.Editor
{
    public static class ChatUIBuilder
    {
        private const string ScenePath = "Assets/_Scenes/Main.unity";
        private const string PrefabDirectory = "Assets/Chat/Prefabs";
        private const string UserPrefabPath = PrefabDirectory + "/UserMessage.prefab";
        private const string AssistantPrefabPath = PrefabDirectory + "/AssistantMessage.prefab";
        private const string RegularFontPath = "Assets/Persian Fonts/TMPFonts/Sahel SDF.asset";
        private const string SemiboldFontPath = "Assets/Persian Fonts/TMPFonts/Sahel-SemiBold SDF.asset";

        private static TMP_FontAsset _regularFont;
        private static TMP_FontAsset _semiboldFont;

        [InitializeOnLoadMethod]
        private static void BuildOnFirstImport()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UserPrefabPath) != null)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                RebuildMainChatUi();
            };
        }

        [MenuItem("Tools/Chat UI/Rebuild Main Chat UI")]
        public static void RebuildMainChatUi()
        {
            _regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontPath);
            _semiboldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SemiboldFontPath) ?? _regularFont;
            if (_regularFont == null)
            {
                throw new MissingReferenceException("Persian Sahel TMP font was not found.");
            }

            EnsureFolder("Assets/Chat");
            EnsureFolder(PrefabDirectory);
            var userPrefab = BuildMessagePrefab(UserPrefabPath, true);
            var assistantPrefab = BuildMessagePrefab(AssistantPrefabPath, false);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearScene(scene);
            BuildScene(userPrefab, assistantPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Chat UI] Main scene and message prefabs rebuilt successfully.");
        }

        public static void RebuildMainChatUiBatch()
        {
            RebuildMainChatUi();
        }

        private static GameObject BuildMessagePrefab(string path, bool isUser)
        {
            var root = UiObject(isUser ? "UserMessage" : "AssistantMessage", null);
            var row = root.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(28, 28, 7, 7);
            row.spacing = 0f;
            row.childAlignment = isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            row.childControlWidth = false;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            var rowFitter = root.AddComponent<ContentSizeFitter>();
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bubble = UiObject("Bubble", root.transform);
            var bubbleImage = bubble.AddComponent<Image>();
            bubbleImage.color = isUser ? Rgb(86, 99, 246) : Rgb(31, 38, 53);
            var rounder = bubble.AddComponent<CornerRounder>();
            rounder.radiiSerialized = isUser
                ? new Vector4(30f, 30f, 8f, 30f)
                : new Vector4(30f, 30f, 30f, 8f);
            var bubbleLayout = bubble.AddComponent<LayoutElement>();
            bubbleLayout.preferredWidth = 800f;
            bubbleLayout.flexibleWidth = 0f;
            var bubbleGroup = bubble.AddComponent<VerticalLayoutGroup>();
            bubbleGroup.padding = new RectOffset(28, 28, 20, 22);
            bubbleGroup.spacing = 8f;
            bubbleGroup.childAlignment = TextAnchor.UpperRight;
            bubbleGroup.childControlWidth = true;
            bubbleGroup.childControlHeight = true;
            bubbleGroup.childForceExpandWidth = true;
            bubbleGroup.childForceExpandHeight = false;
            var bubbleFitter = bubble.AddComponent<ContentSizeFitter>();
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var role = CreateText("Role", bubble.transform, isUser ? "شما" : "دستیار", 24f, _semiboldFont,
                isUser ? Rgb(225, 229, 255) : Rgb(167, 176, 199));
            role.alignment = isUser ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            role.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var body = CreateText("Message", bubble.transform, "متن پیام", 31f, _regularFont, Rgb(244, 246, 252));
            body.alignment = TextAlignmentOptions.Right;
            body.enableWordWrapping = true;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var view = root.AddComponent<ChatMessageView>();
            view.Setup(bubbleImage, role, body, row, isUser);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildScene(GameObject userPrefab, GameObject assistantPrefab)
        {
            var canvasObject = UiObject("Chat Canvas", null);
            canvasObject.AddComponent<ChatMobileInputBootstrap>();
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                AdditionalCanvasShaderChannels.TexCoord2 |
                                                AdditionalCanvasShaderChannels.TexCoord3 |
                                                AdditionalCanvasShaderChannels.Tangent;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var background = CreatePanel("Background", canvasObject.transform, Rgb(12, 16, 25));
            Stretch(background.rectTransform);

            var safeArea = UiObject("Safe Area", canvasObject.transform);
            Stretch((RectTransform)safeArea.transform);
            safeArea.AddComponent<SafeAreaFitter>();

            BuildHeader(safeArea.transform);
            var scroll = BuildConversation(safeArea.transform, out var conversationRect, out var content, out var typing);
            var composer = BuildComposer(safeArea.transform, out var input, out var mobileInput, out var sendButton);

            var controllerObject = new GameObject("Chat Controller", typeof(RectTransform));
            controllerObject.transform.SetParent(safeArea.transform, false);
            var controller = controllerObject.AddComponent<ChatController>();
            controller.Setup(scroll, conversationRect, content, userPrefab, assistantPrefab, typing,
                input, mobileInput, sendButton, composer, (RectTransform)canvasObject.transform);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildHeader(Transform parent)
        {
            var header = CreatePanel("Header", parent, Rgb(16, 21, 33));
            SetAnchors(header.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -190f), Vector2.zero);

            var avatar = CreatePanel("AI Badge", header.transform, Rgb(86, 99, 246));
            SetAnchors(avatar.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-142f, -52f), new Vector2(-54f, 36f));
            avatar.gameObject.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 24f;
            var spark = CreateText("Spark", avatar.transform, "✦", 40f, _semiboldFont, Color.white);
            spark.alignment = TextAlignmentOptions.Center;
            Stretch(spark.rectTransform);

            var title = CreateText("Title", header.transform, "دستیار هوشمند", 42f, _semiboldFont, Rgb(246, 247, 252));
            title.alignment = TextAlignmentOptions.Right;
            SetAnchors(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                new Vector2(46f, -8f), new Vector2(-172f, -30f));

            var subtitle = CreateText("Subtitle", header.transform, "آماده برای اتصال به مدل اختصاصی شما", 24f, _regularFont, Rgb(145, 155, 181));
            subtitle.alignment = TextAlignmentOptions.Right;
            SetAnchors(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(46f, 28f), new Vector2(-172f, 4f));

            var divider = CreatePanel("Divider", header.transform, Rgba(255, 255, 255, 18));
            SetAnchors(divider.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f));
        }

        private static ScrollRect BuildConversation(Transform parent, out RectTransform conversationRect,
            out RectTransform contentRect, out GameObject typingIndicator)
        {
            var conversation = UiObject("Conversation", parent);
            conversationRect = (RectTransform)conversation.transform;
            SetAnchors(conversationRect, Vector2.zero, Vector2.one, new Vector2(0f, 210f), new Vector2(0f, -190f));
            var scroll = conversation.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 45f;

            var viewport = CreatePanel("Viewport", conversation.transform, Color.clear);
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            var content = UiObject("Messages", viewport.transform);
            contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 28, 28);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;

            typingIndicator = CreateTypingIndicator(content.transform);
            typingIndicator.SetActive(false);
            return scroll;
        }

        private static GameObject CreateTypingIndicator(Transform parent)
        {
            var row = UiObject("Typing Indicator", parent);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(28, 28, 6, 6);
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bubble = CreatePanel("Bubble", row.transform, Rgb(31, 38, 53));
            bubble.gameObject.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 28f;
            var element = bubble.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 260f;
            element.preferredHeight = 72f;
            var text = CreateText("Label", bubble.transform, "در حال نوشتن…", 24f, _regularFont, Rgb(167, 176, 199));
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            return row;
        }

        private static RectTransform BuildComposer(Transform parent, out TMP_InputField input,
            out MobileInputField mobileInput, out Button sendButton)
        {
            var dock = CreatePanel("Composer", parent, Rgb(19, 24, 37));
            SetAnchors(dock.rectTransform, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(20f, 18f), new Vector2(-20f, 182f));
            dock.gameObject.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 34f;

            var send = CreatePanel("Send Button", dock.transform, Rgb(86, 99, 246));
            SetAnchors(send.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(18f, 18f), new Vector2(154f, -18f));
            send.gameObject.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 28f;
            sendButton = send.gameObject.AddComponent<Button>();
            sendButton.targetGraphic = send;
            sendButton.transition = Selectable.Transition.ColorTint;
            var colors = sendButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.9f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.45f);
            sendButton.colors = colors;
            var sendLabel = CreateText("Label", send.transform, "ارسال", 27f, _semiboldFont, Color.white);
            sendLabel.alignment = TextAlignmentOptions.Center;
            Stretch(sendLabel.rectTransform);

            var fieldObject = CreatePanel("Message Input", dock.transform, Rgb(27, 33, 48));
            SetAnchors(fieldObject.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(172f, 18f), new Vector2(-18f, -18f));
            fieldObject.gameObject.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 28f;

            var textArea = UiObject("Text Area", fieldObject.transform);
            var textAreaRect = (RectTransform)textArea.transform;
            SetAnchors(textAreaRect, Vector2.zero, Vector2.one, new Vector2(28f, 16f), new Vector2(-28f, -16f));
            textArea.AddComponent<RectMask2D>();

            var placeholder = CreateText("Placeholder", textArea.transform, "پیام خود را بنویسید…", 30f, _regularFont, Rgb(111, 121, 145));
            placeholder.alignment = TextAlignmentOptions.Right;
            Stretch(placeholder.rectTransform);

            var inputText = CreateText("Text", textArea.transform, string.Empty, 30f, _regularFont, Rgb(242, 244, 250));
            inputText.alignment = TextAlignmentOptions.Right;
            inputText.enableWordWrapping = true;
            Stretch(inputText.rectTransform);

            input = fieldObject.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = textAreaRect;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.characterLimit = 4000;
            input.caretColor = Rgb(124, 137, 255);
            input.selectionColor = Rgba(86, 99, 246, 110);
            input.customCaretColor = true;
            input.richText = false;
            input.shouldHideMobileInput = false;
            input.shouldHideSoftKeyboard = false;

            mobileInput = fieldObject.gameObject.AddComponent<MobileInputField>();
            mobileInput.BackgroundColor = Rgb(27, 33, 48);
            mobileInput.ReturnKey = MobileInputField.ReturnKeyType.Send;
            mobileInput.KeyboardLanguage = "fa";
            mobileInput.IsManualHideControl = true;
            return dock.rectTransform;
        }

        private static void ClearScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject UiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }
            return gameObject;
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var gameObject = UiObject(name, parent);
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RTLTextMeshPro CreateText(string name, Transform parent, string value, float size,
            TMP_FontAsset font, Color color)
        {
            var gameObject = UiObject(name, parent);
            var text = gameObject.AddComponent<RTLTextMeshPro>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.Farsi = true;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Color32 Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        private static Color32 Rgba(byte r, byte g, byte b, byte a) => new Color32(r, g, b, a);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
