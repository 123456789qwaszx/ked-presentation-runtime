using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(
    menuName = "VN/Dialogue/Surface Layout Preset DB",
    fileName = "DialogueSurfaceLayoutPresetDB")]
public sealed class DialogueSurfaceLayoutPresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = "bottom";

    [Serializable]
    public struct Entry
    {
        [Header("Identity")]
        public string key;

        [Header("Line Rect")]
        public Vector2 lineAnchorMin;
        public Vector2 lineAnchorMax;
        public Vector2 linePivot;
        public Vector2 lineAnchoredPosition;
        public Vector2 lineSizeDelta;

        [Header("Line Typography")]
        public TextAlignmentOptions lineAlignment;
        public float lineFontSize;
        public float lineSpacing;
        public float paragraphSpacing;
        public Vector4 lineMargin;
        public TextOverflowModes lineOverflowMode;
        public TextWrappingModes lineTextWrappingMode;

        [Header("Name Visibility")]
        public bool useName;
        public bool clearNameWhenHidden;

        [Header("Name Rect")]
        public Vector2 nameAnchorMin;
        public Vector2 nameAnchorMax;
        public Vector2 namePivot;
        public Vector2 nameAnchoredPosition;
        public Vector2 nameSizeDelta;

        [Header("Name Typography")]
        public TextAlignmentOptions nameAlignment;
        public float nameFontSize;
        public Vector4 nameMargin;
        public TextOverflowModes nameOverflowMode;
        public TextWrappingModes nameTextWrappingMode;

        public static Entry CreateFallback() => CreateBottom();

        public static Entry CreateBottom()
        {
            return new Entry
            {
                key = DefaultPresetKey,

                lineAnchorMin = new Vector2(0.08f, 0.08f),
                lineAnchorMax = new Vector2(0.92f, 0.28f),
                linePivot = new Vector2(0.5f, 0.5f),
                lineAnchoredPosition = Vector2.zero,
                lineSizeDelta = Vector2.zero,

                lineAlignment = TextAlignmentOptions.TopLeft,
                lineFontSize = 36f,
                lineSpacing = 0f,
                paragraphSpacing = 0f,
                lineMargin = Vector4.zero,
                lineOverflowMode = TextOverflowModes.Overflow,
                lineTextWrappingMode = TextWrappingModes.Normal,

                useName = true,
                clearNameWhenHidden = true,

                nameAnchorMin = new Vector2(0.08f, 0.30f),
                nameAnchorMax = new Vector2(0.35f, 0.36f),
                namePivot = new Vector2(0f, 0.5f),
                nameAnchoredPosition = Vector2.zero,
                nameSizeDelta = Vector2.zero,

                nameAlignment = TextAlignmentOptions.Left,
                nameFontSize = 28f,
                nameMargin = Vector4.zero,
                nameOverflowMode = TextOverflowModes.Overflow,
                nameTextWrappingMode = TextWrappingModes.NoWrap,
            };
        }

        public static Entry CreateLetterboxBottom()
        {
            return new Entry
            {
                key = "letterbox_bottom",

                lineAnchorMin = new Vector2(0.12f, 0.04f),
                lineAnchorMax = new Vector2(0.88f, 0.17f),
                linePivot = new Vector2(0.5f, 0.5f),
                lineAnchoredPosition = Vector2.zero,
                lineSizeDelta = Vector2.zero,

                lineAlignment = TextAlignmentOptions.MidlineLeft,
                lineFontSize = 32f,
                lineSpacing = 0f,
                paragraphSpacing = 0f,
                lineMargin = Vector4.zero,
                lineOverflowMode = TextOverflowModes.Overflow,
                lineTextWrappingMode = TextWrappingModes.Normal,

                useName = false,
                clearNameWhenHidden = true,

                nameAnchorMin = new Vector2(0.12f, 0.18f),
                nameAnchorMax = new Vector2(0.35f, 0.23f),
                namePivot = new Vector2(0f, 0.5f),
                nameAnchoredPosition = Vector2.zero,
                nameSizeDelta = Vector2.zero,

                nameAlignment = TextAlignmentOptions.Left,
                nameFontSize = 26f,
                nameMargin = Vector4.zero,
                nameOverflowMode = TextOverflowModes.Overflow,
                nameTextWrappingMode = TextWrappingModes.NoWrap,
            };
        }

        public static Entry CreateBlackBookPage()
        {
            return new Entry
            {
                key = "blackbook_page",

                lineAnchorMin = new Vector2(0.22f, 0.18f),
                lineAnchorMax = new Vector2(0.78f, 0.82f),
                linePivot = new Vector2(0.5f, 0.5f),
                lineAnchoredPosition = Vector2.zero,
                lineSizeDelta = Vector2.zero,

                lineAlignment = TextAlignmentOptions.TopLeft,
                lineFontSize = 34f,
                lineSpacing = 12f,
                paragraphSpacing = 18f,
                lineMargin = new Vector4(8f, 8f, 8f, 8f),
                lineOverflowMode = TextOverflowModes.Overflow,
                lineTextWrappingMode = TextWrappingModes.Normal,

                useName = false,
                clearNameWhenHidden = true,

                nameAnchorMin = new Vector2(0.22f, 0.83f),
                nameAnchorMax = new Vector2(0.45f, 0.89f),
                namePivot = new Vector2(0f, 0.5f),
                nameAnchoredPosition = Vector2.zero,
                nameSizeDelta = Vector2.zero,

                nameAlignment = TextAlignmentOptions.Left,
                nameFontSize = 26f,
                nameMargin = Vector4.zero,
                nameOverflowMode = TextOverflowModes.Overflow,
                nameTextWrappingMode = TextWrappingModes.NoWrap,
            };
        }

        public static Entry CreateFullTopLeft()
        {
            return new Entry
            {
                key = "full_top_left",

                lineAnchorMin = new Vector2(0.08f, 0.10f),
                lineAnchorMax = new Vector2(0.92f, 0.90f),
                linePivot = new Vector2(0f, 1f),
                lineAnchoredPosition = Vector2.zero,
                lineSizeDelta = Vector2.zero,

                lineAlignment = TextAlignmentOptions.TopLeft,
                lineFontSize = 32f,
                lineSpacing = 8f,
                paragraphSpacing = 16f,
                lineMargin = Vector4.zero,
                lineOverflowMode = TextOverflowModes.Overflow,
                lineTextWrappingMode = TextWrappingModes.Normal,

                useName = false,
                clearNameWhenHidden = true,

                nameAnchorMin = new Vector2(0.08f, 0.91f),
                nameAnchorMax = new Vector2(0.32f, 0.96f),
                namePivot = new Vector2(0f, 0.5f),
                nameAnchoredPosition = Vector2.zero,
                nameSizeDelta = Vector2.zero,

                nameAlignment = TextAlignmentOptions.Left,
                nameFontSize = 26f,
                nameMargin = Vector4.zero,
                nameOverflowMode = TextOverflowModes.Overflow,
                nameTextWrappingMode = TextWrappingModes.NoWrap,
            };
        }
    }

    [SerializeField] private Entry[] entries =
    {
        Entry.CreateBottom(),
        Entry.CreateLetterboxBottom(),
        Entry.CreateBlackBookPage(),
        Entry.CreateFullTopLeft(),
    };

    public Entry FindOrDefault(string key)
    {
        string normalizedKey = NormalizeKey(key);

        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (NormalizeKey(entries[i].key) == normalizedKey)
                    return entries[i];
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (NormalizeKey(entries[i].key) == DefaultPresetKey)
                    return entries[i];
            }

            if (entries.Length > 0)
                return entries[0];
        }

        Debug.LogWarning(
            "[DialogueSurfaceLayoutPresetDBSO] No entries assigned. Returning fallback bottom layout.",
            this);

        return Entry.CreateFallback();
    }

    public bool Contains(string key)
    {
        string normalizedKey = NormalizeKey(key);

        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            if (NormalizeKey(entries[i].key) == normalizedKey)
                return true;
        }

        return false;
    }

    public static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? DefaultPresetKey
            : key.Trim().ToLowerInvariant();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateEntries();
    }

    private void ValidateEntries()
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning(
                "[DialogueSurfaceLayoutPresetDBSO] entries is empty. Add at least the bottom preset.",
                this);
            return;
        }

        bool hasDefault = false;
        var seenKeys = new HashSet<string>();

        for (int i = 0; i < entries.Length; i++)
        {
            string key = entries[i].key;
            string normalizedKey = NormalizeKey(key);

            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning(
                    $"[DialogueSurfaceLayoutPresetDBSO] Empty key at index={i}.",
                    this);
            }

            if (normalizedKey == DefaultPresetKey)
                hasDefault = true;

            if (!seenKeys.Add(normalizedKey))
            {
                Debug.LogWarning(
                    $"[DialogueSurfaceLayoutPresetDBSO] Duplicate key='{normalizedKey}' at index={i}.",
                    this);
            }
        }

        if (!hasDefault)
        {
            Debug.LogWarning(
                $"[DialogueSurfaceLayoutPresetDBSO] Missing default preset key='{DefaultPresetKey}'.",
                this);
        }
    }
#endif
}
