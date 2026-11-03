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

        [Header("Box Image")]
        [Tooltip(
            "이 프리셋이 박스 이미지를 정하는가. 끄면 이미지를 건드리지 않는다 — " +
            "아직 값을 안 채운 프리셋에서 씬에 놓인 이미지가 사라지지 않도록 하는 기본값이다.")]
        public bool overrideImage;

        [Tooltip(
            "overrideImage가 켜져 있을 때 박스에 얹을 스프라이트. " +
            "비워 두면 이미지를 끈다 — 텍스트만 있는 종류(OnlyText 계열)가 이 모양이다.")]
        public Sprite image;

        [Tooltip(
            "이 프리셋이 담당하는 박스 종류. 박스가 하나뿐이므로 kind는 '어느 뷰인가'가 아니라 " +
            "'어느 레이아웃인가'를 뜻한다. 같은 kind가 여러 개면 첫 번째를 쓴다.")]
        public DialogueBoxKind kind;

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
                kind = DialogueBoxKind.Surface,

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

        /// <summary>
        /// 이름 있는 라인용. bottom과 같은 자리지만 이름표가 별도 판처럼 두드러진다
        /// (옛 DialogueBox01_Speaker가 이름 박스를 따로 갖고 있던 것에 대응).
        /// 시작값이 bottom과 거의 같다 — 여기서 갈라져 나가는 것이 데이터로 둔 이유다.
        /// </summary>
        public static Entry CreateSpeakerBottom()
        {
            return new Entry
            {
                key = "speaker_bottom",
                kind = DialogueBoxKind.Speaker,

                lineAnchorMin = new Vector2(0.08f, 0.08f),
                lineAnchorMax = new Vector2(0.92f, 0.26f),
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

                nameAnchorMin = new Vector2(0.08f, 0.28f),
                nameAnchorMax = new Vector2(0.38f, 0.35f),
                namePivot = new Vector2(0f, 0.5f),
                nameAnchoredPosition = Vector2.zero,
                nameSizeDelta = Vector2.zero,

                nameAlignment = TextAlignmentOptions.Left,
                nameFontSize = 30f,
                nameMargin = Vector4.zero,
                nameOverflowMode = TextOverflowModes.Overflow,
                nameTextWrappingMode = TextWrappingModes.NoWrap,
            };
        }

        /// <summary>
        /// 초상 컷인이 한쪽에 서는 경우의 좁은 텍스트 폭.
        /// 컷인 자체는 Spine이 그리고, 이 프리셋은 그 옆에 남는 글 자리만 정한다.
        /// 말하는 주체가 초상 당사자라 이름표는 쓰지 않는다(옛 Portrait 박스와 같다).
        /// </summary>
        public static Entry CreatePortraitNarrow()
        {
            return new Entry
            {
                key = "portrait_narrow",
                kind = DialogueBoxKind.Portrait,

                // 왼쪽 30%를 컷인 자리로 비운다.
                lineAnchorMin = new Vector2(0.32f, 0.08f),
                lineAnchorMax = new Vector2(0.92f, 0.30f),
                linePivot = new Vector2(0.5f, 0.5f),
                lineAnchoredPosition = Vector2.zero,
                lineSizeDelta = Vector2.zero,

                lineAlignment = TextAlignmentOptions.TopLeft,
                lineFontSize = 34f,
                lineSpacing = 0f,
                paragraphSpacing = 0f,
                lineMargin = Vector4.zero,
                lineOverflowMode = TextOverflowModes.Overflow,
                lineTextWrappingMode = TextWrappingModes.Normal,

                useName = false,
                clearNameWhenHidden = true,

                nameAnchorMin = new Vector2(0.32f, 0.31f),
                nameAnchorMax = new Vector2(0.60f, 0.37f),
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

        public static Entry CreateLetterboxBottom()
        {
            return new Entry
            {
                key = "letterbox_bottom",
                kind = DialogueBoxKind.LetterBox,

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
                kind = DialogueBoxKind.BlackBook,

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
                kind = DialogueBoxKind.OnlyText,

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

    // kind 6종에 하나씩 대응한다 — 박스가 하나뿐이므로 이 배열이 곧 "박스 종류의 정의"다.
    [SerializeField] private Entry[] entries =
    {
        Entry.CreateBottom(),          // Surface
        Entry.CreateSpeakerBottom(),   // Speaker
        Entry.CreatePortraitNarrow(),  // Portrait
        Entry.CreateLetterboxBottom(), // LetterBox
        Entry.CreateFullTopLeft(),     // OnlyText
        Entry.CreateBlackBookPage(),   // BlackBook
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

    /// <summary>
    /// 박스 종류 → 레이아웃. 박스가 하나뿐이므로 kind는 뷰가 아니라 이 프리셋을 고른다.
    /// 해당 kind가 없으면 기본 프리셋으로 물러선다 — 조용히 아무것도 안 하지 않는다.
    /// </summary>
    public Entry FindByKind(DialogueBoxKind kind)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].kind == kind)
                    return entries[i];
            }
        }

        Debug.LogWarning(
            $"[DialogueSurfaceLayoutPresetDBSO] No preset declares kind={kind}. " +
            $"Falling back to key='{DefaultPresetKey}'.",
            this);

        return FindOrDefault(DefaultPresetKey);
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
        var seenKinds = new HashSet<DialogueBoxKind>();

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

            // FindByKind가 첫 번째를 쓰므로, 중복은 뒤쪽 엔트리가 조용히 무시된다는 뜻이다.
            if (!seenKinds.Add(entries[i].kind))
            {
                Debug.LogWarning(
                    $"[DialogueSurfaceLayoutPresetDBSO] Duplicate kind={entries[i].kind} at index={i}. " +
                    $"FindByKind는 첫 번째만 쓴다.",
                    this);
            }
        }

        if (!hasDefault)
        {
            Debug.LogWarning(
                $"[DialogueSurfaceLayoutPresetDBSO] Missing default preset key='{DefaultPresetKey}'.",
                this);
        }

        // kind 하나라도 빠지면 그 박스 종류가 기본 레이아웃으로 조용히 떨어진다.
        foreach (DialogueBoxKind kind in Enum.GetValues(typeof(DialogueBoxKind)))
        {
            if (!seenKinds.Contains(kind))
            {
                Debug.LogWarning(
                    $"[DialogueSurfaceLayoutPresetDBSO] No preset for kind={kind}. " +
                    $"그 종류는 기본 레이아웃으로 떨어진다.",
                    this);
            }
        }
    }
#endif
}