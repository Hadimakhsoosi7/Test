using System;
using DA_Assets.CR;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Avrin.Chat
{
    

    [DisallowMultipleComponent]
    public sealed class ModelDropdown : MonoBehaviour
    {
        [Header("Typography")]
        [SerializeField] private TMP_FontAsset dropdownFont;

        private static readonly Color32 SelectedColor = new Color32(86, 99, 246, 255);
        private static readonly Color32 IdleColor = new Color32(35, 42, 59, 255);
        private static readonly Color32 MenuColor = new Color32(15, 19, 30, 255);

        private Button _toggleButton;
        private RTLTextMeshPro _caption;
        private GameObject _menu;
        private Button _fastButton;
        private Button _accurateButton;
        private Image _fastBackground;
        private Image _accurateBackground;
        private int _value;

        public int Value => _value;
        public event Action<int> ValueChanged;

        public void Initialize(TMP_FontAsset font, int initialValue)
        {
            font = dropdownFont != null ? dropdownFont : font;

            if (_toggleButton == null)
            {
                Build(font);
            }

            SetValueWithoutNotify(initialValue);
        }

        public void SetInteractable(bool interactable)
        {
            if (_toggleButton != null) _toggleButton.interactable = interactable;
            if (_fastButton != null) _fastButton.interactable = interactable;
            if (_accurateButton != null) _accurateButton.interactable = interactable;
            if (!interactable && _menu != null) _menu.SetActive(false);
        }

        public void SetValueWithoutNotify(int value)
        {
            _value = Mathf.Clamp(value, 0, 1);
            RefreshVisuals();
        }

        private void Build(TMP_FontAsset font)
        {
            var rootImage = GetComponent<Image>();
            rootImage.color = new Color32(27, 33, 48, 255);
            rootImage.raycastTarget = true;

            _toggleButton = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            _toggleButton.targetGraphic = rootImage;
            _toggleButton.onClick.AddListener(ToggleMenu);

            _caption = CreateLabel(transform, "Caption", 27f, font, TextAlignmentOptions.Center);

            _menu = CreatePanel("Options", transform, MenuColor).gameObject;
            var menuRect = (RectTransform)_menu.transform;
            menuRect.anchorMin = new Vector2(0f, 1f);
            menuRect.anchorMax = Vector2.one;
            menuRect.pivot = new Vector2(0.5f, 0f);
            menuRect.offsetMin = new Vector2(0f, 12f);
            menuRect.offsetMax = new Vector2(0f, 192f);
            _menu.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 24f;

            _accurateButton = CreateOption(_menu.transform, "Accurate Model", "Gemma 4 12B",
                font, new Vector2(0f, 0.5f), Vector2.one, out _accurateBackground);
            _fastButton = CreateOption(_menu.transform, "Fast Model", "Qwen 2.5 7B",
                font, Vector2.zero, new Vector2(1f, 0.5f), out _fastBackground);

            _fastButton.onClick.AddListener(() => Select(0));
            _accurateButton.onClick.AddListener(() => Select(1));
            _menu.SetActive(false);
        }

        private Button CreateOption(Transform parent, string name, string text, TMP_FontAsset font,
            Vector2 anchorMin, Vector2 anchorMax, out Image background)
        {
            background = CreatePanel(name, parent, IdleColor);
            var rect = background.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(7f, 6f);
            rect.offsetMax = new Vector2(-7f, -6f);
            background.gameObject.AddComponent<CornerRounder>().radiiSerialized = Vector4.one * 18f;
            CreateLabel(background.transform, "Label", 25f, font, TextAlignmentOptions.Center).text = text;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            return button;
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.layer = LayerMask.NameToLayer("UI");
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static RTLTextMeshPro CreateLabel(Transform parent, string name, float size,
            TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            item.layer = LayerMask.NameToLayer("UI");
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);
            var label = item.AddComponent<RTLTextMeshPro>();
            label.font = font;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = alignment;
            label.Farsi = true;
            label.raycastTarget = false;
            return label;
        }

        private void ToggleMenu()
        {
            _menu.SetActive(!_menu.activeSelf);
            _menu.transform.SetAsLastSibling();
        }

        private void Select(int value)
        {
            _value = value;
            _menu.SetActive(false);
            RefreshVisuals();
            ValueChanged?.Invoke(_value);
        }

        private void RefreshVisuals()
        {
            if (_caption == null) return;
            _caption.text = _value == 0 ? "Qwen 2.5 7B  ▼" : "Gemma 4 12B  ▼";
            _fastBackground.color = _value == 0 ? SelectedColor : IdleColor;
            _accurateBackground.color = _value == 1 ? SelectedColor : IdleColor;
        }

        private void OnDestroy()
        {
            if (_toggleButton != null) _toggleButton.onClick.RemoveListener(ToggleMenu);
        }
    }
}
