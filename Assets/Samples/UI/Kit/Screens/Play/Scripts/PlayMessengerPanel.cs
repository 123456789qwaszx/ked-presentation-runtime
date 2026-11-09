using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>메신저 말풍선 하나에 그릴 것. 재생 런타임의 기록을 앱이 이 꼴로 옮겨 넘긴다.</summary>
    public struct PlayMessengerBubble
    {
        public string speaker;
        public string text;
        public bool mine;      // 나 = 오른쪽, 아니면 왼쪽
    }

    /// <summary>
    /// 재생 화면의 메신저 패널 — 받은 말풍선 목록을 그대로 그린다. 재생 런타임은 모른다.
    /// 재생 쪽 계약(메신저 뷰)은 앱이 이것 위에 구현한다(ked: KedUIApp/Runtime/Flow/KedMessengerView).
    ///
    /// 목록은 들고 있지 않는다 — Render가 받을 때마다 전부 다시 맞춘다. 같은 목록이면 같은 화면이다.
    /// 아래에서 위로 쌓이고, 넘치면 위가 잘린다. 대사창처럼 입력을 받지 않는다(진행 클릭은 아래의 AdvanceArea가 받는다).
    /// 예제라 프리팹 없이 코드로 세운다 — 그림은 색 판, 아바타는 화자 이름의 첫 글자다.
    /// </summary>
    public sealed class PlayMessengerPanel
    {
        // 기준 해상도 1600×900(빌더의 CanvasScaler) 위의 값.
        private static readonly Vector2 PanelAnchorMin = new(0.28f, 0.24f);
        private static readonly Vector2 PanelAnchorMax = new(0.80f, 0.84f);

        private const float DimAlpha = 0.35f;
        private const float RowSpacing = 10f;
        private const float AvatarSize = 56f;
        private const float AvatarGap = 12f;
        private const float BubbleMaxWidthRatio = 0.62f;
        private const float BubbleFallbackMaxWidth = 480f;
        private const float FontSize = 28f;
        private const float PopDuration = 0.15f;
        private const float PopFromScale = 0.85f;
        private const string WaitingText = "•••";

        private static readonly Vector2 BubblePadding = new(22f, 14f);
        private static readonly Color TheirBubble = new(1f, 1f, 1f, 0.96f);
        private static readonly Color TheirText = new(0.13f, 0.15f, 0.2f, 1f);
        private static readonly Color MyBubble = new(0.2f, 0.52f, 0.95f, 0.96f);
        private static readonly Color MyText = Color.white;
        private static readonly Color AvatarColor = new(0.16f, 0.62f, 0.7f, 1f);

        private sealed class Row
        {
            public GameObject root;
            public HorizontalLayoutGroup layout;
            public Image avatar;
            public TMP_Text avatarLetter;
            public RectTransform bubble;
            public Image bubbleImage;
            public CanvasGroup bubbleGroup;
            public LayoutElement bubbleSize;
            public TMP_Text text;
        }

        private readonly MonoBehaviour _host;
        private readonly TMP_Text _template;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly List<Row> _rows = new();

        private int _bubbleCount;
        private bool _waiting;
        private Coroutine _pop;
        private Row _popRow;

        public RectTransform Root => _root;
        public CanvasGroup Group => _group;

        /// <param name="host">등장 연출(코루틴)을 돌릴 화면.</param>
        /// <param name="before">이 형제의 바로 아래(먼저 그려지는 자리)에 선다 — 대사창 틀.</param>
        /// <param name="template">글꼴을 빌릴 글자 — 대사창의 줄.</param>
        public PlayMessengerPanel(MonoBehaviour host, RectTransform before, TMP_Text template)
        {
            _host = host;
            _template = template;

            Transform parent = before.parent;

            _root = NewRect("MessengerPanel", parent);
            _root.SetSiblingIndex(before.GetSiblingIndex());
            Stretch(_root);

            _group = _root.gameObject.AddComponent<CanvasGroup>();

            // 무대를 살짝 누르는 막 — 화면 전체.
            RectTransform dim = NewRect("Dim", _root);
            Stretch(dim);
            Image dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, DimAlpha);
            dimImage.raycastTarget = false;

            // 말풍선이 보이는 창. 넘친 위쪽을 자른다.
            _viewport = NewRect("Viewport", _root);
            _viewport.anchorMin = PanelAnchorMin;
            _viewport.anchorMax = PanelAnchorMax;
            _viewport.offsetMin = Vector2.zero;
            _viewport.offsetMax = Vector2.zero;
            _viewport.gameObject.AddComponent<RectMask2D>();

            // 말풍선 열. 창의 바닥에 붙어 위로 자란다.
            _content = NewRect("Content", _viewport);
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(1f, 0f);
            _content.pivot = new Vector2(0.5f, 0f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup column = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.LowerCenter;
            column.spacing = RowSpacing;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            ContentSizeFitter fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SetVisibleImmediate(false);
        }

        #region 그리기

        /// <summary>목록 전체를 그린다. animateLast면 마지막 말풍선만 튀어나온다. 대기 표시는 떼어진다.</summary>
        public void Render(IReadOnlyList<PlayMessengerBubble> bubbles, bool animateLast)
        {
            _bubbleCount = bubbles != null ? bubbles.Count : 0;
            _waiting = false;

            StopPop();

            for (int i = 0; i < _bubbleCount; i++)
            {
                PlayMessengerBubble bubble = bubbles[i];

                // 아바타는 이어 보낸 묶음(같은 화자가 연달아)의 마지막 말풍선에만.
                bool endsRun = i == _bubbleCount - 1
                    || bubbles[i + 1].mine != bubble.mine
                    || bubbles[i + 1].speaker != bubble.speaker;

                Fill(RowAt(i), bubble.text, bubble.speaker, bubble.mine, showAvatar: endsRun && !bubble.mine);
            }

            HideRowsFrom(_bubbleCount);
            Rebuild();

            if (animateLast && _bubbleCount > 0 && _root.gameObject.activeInHierarchy && _host != null)
                StartPop(_rows[_bubbleCount - 1]);
        }

        /// <summary>입력 대기 표시(•••). 패널이 보이는 동안에만 열의 끝에 붙는다. 다음 Render가 뗀다.</summary>
        public void SetWaiting(bool waiting)
        {
            bool next = waiting && IsShown;

            if (next == _waiting)
                return;

            _waiting = next;

            if (_waiting)
                Fill(RowAt(_bubbleCount), WaitingText, string.Empty, mine: false, showAvatar: false);

            HideRowsFrom(_waiting ? _bubbleCount + 1 : _bubbleCount);
            Rebuild();
        }

        private bool IsShown => _group.alpha > 0f;

        private Row RowAt(int index)
        {
            while (_rows.Count <= index)
                _rows.Add(NewRow());

            return _rows[index];
        }

        private void HideRowsFrom(int index)
        {
            for (int i = index; i < _rows.Count; i++)
                KedUIUtil.SetActive(_rows[i].root, false);
        }

        private void Fill(Row row, string text, string speaker, bool mine, bool showAvatar)
        {
            KedUIUtil.SetActive(row.root, true);

            row.layout.childAlignment = mine ? TextAnchor.LowerRight : TextAnchor.LowerLeft;

            // 아바타 자리는 왼쪽 줄에 늘 둔다(말풍선의 왼쪽 선이 맞게) — 그림만 묶음 끝에서 보인다.
            KedUIUtil.SetActive(row.avatar, !mine);
            row.avatar.enabled = showAvatar;
            row.avatarLetter.enabled = showAvatar;
            row.avatarLetter.text = string.IsNullOrEmpty(speaker) ? string.Empty : speaker.Substring(0, 1);

            row.bubbleImage.color = mine ? MyBubble : TheirBubble;
            row.text.color = mine ? MyText : TheirText;
            row.text.text = text ?? string.Empty;

            // 말풍선은 글에 맞추되 창 너비의 일부까지만 넓어진다.
            Vector2 preferred = row.text.GetPreferredValues(row.text.text, MaxTextWidth(), 0f);
            row.bubbleSize.preferredWidth = Mathf.Min(preferred.x, MaxTextWidth()) + BubblePadding.x * 2f;
            row.bubbleSize.preferredHeight = preferred.y + BubblePadding.y * 2f;

            row.bubble.localScale = Vector3.one;
            row.bubbleGroup.alpha = 1f;
        }

        private float MaxTextWidth()
        {
            float width = _viewport.rect.width;

            return width > 1f
                ? width * BubbleMaxWidthRatio - BubblePadding.x * 2f
                : BubbleFallbackMaxWidth;
        }

        private void Rebuild()
        {
            if (_root.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        #endregion

        #region 등장 연출

        private void StartPop(Row row)
        {
            _popRow = row;
            _pop = _host.StartCoroutine(Pop(row));
        }

        // 도는 연출을 끝값으로 놓는다.
        private void StopPop()
        {
            if (_pop != null && _host != null)
                _host.StopCoroutine(_pop);

            if (_popRow != null)
            {
                _popRow.bubble.localScale = Vector3.one;
                _popRow.bubbleGroup.alpha = 1f;
            }

            _pop = null;
            _popRow = null;
        }

        private IEnumerator Pop(Row row)
        {
            float elapsed = 0f;

            while (elapsed < PopDuration)
            {
                float t = elapsed / PopDuration;
                float eased = 1f - (1f - t) * (1f - t);

                row.bubble.localScale = Vector3.one * Mathf.LerpUnclamped(PopFromScale, 1f, eased);
                row.bubbleGroup.alpha = eased;

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            row.bubble.localScale = Vector3.one;
            row.bubbleGroup.alpha = 1f;

            _pop = null;
            _popRow = null;
        }

        #endregion

        #region 보이기

        // 대사창처럼 켜 둔 채 알파로만 감춘다 — 감춘 동안에도 글자 크기를 잴 수 있어야 한다.
        public void SetVisibleImmediate(bool visible)
        {
            if (!visible)
                StopPop();

            KedUIUtil.SetActive(_root, true);
            SetShown(visible);
        }

        /// <summary>켜 두되 보이지 않게 — 페이드 인의 출발점.</summary>
        public void PrepareHidden()
        {
            KedUIUtil.SetActive(_root, true);
            SetShown(false);
        }

        /// <summary>보이기만 바꾼다(알파). 입력은 받지 않는다.</summary>
        public void SetShown(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            if (!visible)
                SetWaiting(false);
        }

        #endregion

        #region 세우기

        private Row NewRow()
        {
            Row row = new();

            RectTransform root = NewRect("Bubble" + _rows.Count, _content);
            row.root = root.gameObject;

            row.layout = row.root.AddComponent<HorizontalLayoutGroup>();
            row.layout.spacing = AvatarGap;
            row.layout.childControlWidth = true;
            row.layout.childControlHeight = true;
            row.layout.childForceExpandWidth = false;
            row.layout.childForceExpandHeight = false;

            RectTransform avatar = NewRect("Avatar", root);
            row.avatar = avatar.gameObject.AddComponent<Image>();
            row.avatar.color = AvatarColor;
            row.avatar.raycastTarget = false;

            LayoutElement avatarSize = avatar.gameObject.AddComponent<LayoutElement>();
            avatarSize.preferredWidth = AvatarSize;
            avatarSize.preferredHeight = AvatarSize;

            row.avatarLetter = NewText("Letter", avatar, TextAlignmentOptions.Center);
            row.avatarLetter.color = Color.white;
            Stretch(row.avatarLetter.rectTransform);

            row.bubble = NewRect("Balloon", root);
            row.bubbleImage = row.bubble.gameObject.AddComponent<Image>();
            row.bubbleImage.raycastTarget = false;
            row.bubbleGroup = row.bubble.gameObject.AddComponent<CanvasGroup>();
            row.bubbleGroup.interactable = false;
            row.bubbleGroup.blocksRaycasts = false;
            row.bubbleSize = row.bubble.gameObject.AddComponent<LayoutElement>();

            row.text = NewText("Text", row.bubble, TextAlignmentOptions.TopLeft);
            row.text.textWrappingMode = TextWrappingModes.Normal;

            RectTransform textRect = row.text.rectTransform;
            Stretch(textRect);
            textRect.offsetMin = new Vector2(BubblePadding.x, BubblePadding.y);
            textRect.offsetMax = new Vector2(-BubblePadding.x, -BubblePadding.y);

            return row;
        }

        private TMP_Text NewText(string name, Transform parent, TextAlignmentOptions alignment)
        {
            RectTransform rect = NewRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            if (_template != null)
            {
                text.font = _template.font;
                text.fontSharedMaterial = _template.fontSharedMaterial;
            }

            text.fontSize = FontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.richText = false;

            return text;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
