using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 본체의 대사창 계약(IPresentationDialogueBoxView)을 PlayScreen의 Refs 위에 구현한다.
    ///
    /// 소유권(PLAN "대사창 소유권"):
    /// - 줄·이름 글자의 자리·크기·정렬은 연출 DB(DialogueSurfaceLayoutPresetDBSO)가 정한다. 새 UI는 새 박스에 맞춘
    ///   프리셋 DB를 쓴다(빌더가 만든 KedSurfaceLayoutPresetDB). 대본의 surface_layout이 그대로 동작한다.
    /// - 그림(박스·이름표)·글꼴·불투명도·글자 배율은 UI가 정한다. 프리셋의 overrideImage는 쓰지 않는다.
    /// - 보이기·감추기(CanvasGroup)는 본체 컨트롤러가 부른다. UI 숨김(퀵메뉴)은 바깥 층(HudRoot)이 맡아 서로 겹치지 않는다.
    ///
    /// 이름표 그림은 이름 글자의 자리를 따라간다 — 프리셋이 이름 자리를 옮겨도 판이 글자를 감싼다.
    /// </summary>
    internal sealed class PlayDialogueBox : IPresentationDialogueBoxView
    {
        // 시안: 이름표 332×58, 이름 글자 250×40 — 글자 둘레의 판 여백(왼쪽·아래·오른쪽·위).
        private static readonly Vector2 PlatePadMin = new(26f, 13f);
        private static readonly Vector2 PlatePadMax = new(56f, 5f);

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _box;
        private readonly RectTransform _plate;
        private readonly Image _plateImage;
        private readonly TMP_Text _line;
        private readonly TMP_Text _name;
        private readonly GameObject _nextMarker;

        private bool _usesName = true;
        private bool _clearNameWhenHidden = true;
        private bool _hasEntry;
        private DialogueSurfaceLayoutPresetDBSO.Entry _entry;
        private float _textScale = 1f;
        private float _opacity = 1f;

        public PlayDialogueBox(
            RectTransform root,
            CanvasGroup group,
            Image box,
            RectTransform plate,
            TMP_Text line,
            TMP_Text name,
            GameObject nextMarker)
        {
            _root = root;
            _group = group;
            _box = box;
            _plate = plate;
            _plateImage = plate != null ? plate.GetComponent<Image>() : null;
            _line = line;
            _name = name;
            _nextMarker = nextMarker;
        }

        #region UI 쪽 — 설정·테마

        /// <summary>사용자 글자 배율(UIContext.TextScale). 프리셋 크기에 곱한다.</summary>
        public void SetTextScale(float scale)
        {
            float next = scale > 0f ? scale : 1f;
            if (Mathf.Approximately(next, _textScale))
                return;

            _textScale = next;

            if (_hasEntry)
                ApplyFontSizes(_entry);
        }

        /// <summary>대사창 그림의 불투명도(설정 · 대사창 불투명도). 글자는 그대로.</summary>
        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            ApplyOpacity(_box);
            ApplyOpacity(_plateImage);
        }

        public void SetNextMarkerVisible(bool visible)
        {
            KedUIUtil.SetActive(_nextMarker, visible);
        }

        private void ApplyOpacity(Image image)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = _opacity;
            image.color = color;
        }

        #endregion

        #region IPresentationDialogueBoxView

        public TMP_Text GetLineText() => _line;
        public TMP_Text GetNameText() => _name;

        public void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry)
        {
            _entry = entry;
            _hasEntry = true;

            ApplyLineLayout(entry);
            ApplyNameLayout(entry);
            ApplyFontSizes(entry);
        }

        public void ResetPresentationTransform()
        {
            if (_root == null)
                return;

            _root.localPosition = Vector3.zero;
            _root.anchoredPosition = Vector2.zero;
        }

        public void PrimeText(string text, string characterName, bool hasCharacterName)
        {
            SetNextMarkerVisible(false);

            _line.text = text ?? string.Empty;
            _line.maxVisibleCharacters = 0;
            _line.ForceMeshUpdate();

            if (_name == null)
                return;

            bool showName = _usesName && hasCharacterName;

            if (showName)
            {
                _name.text = characterName ?? string.Empty;
                SetNameVisible(true);
                return;
            }

            if (_clearNameWhenHidden)
                _name.text = string.Empty;

            SetNameVisible(false);
        }

        public void SetVisibleImmediate(bool visible)
        {
            KedUIUtil.SetActive(_root, true);
            SetCanvas(visible);
        }

        public void PrepareHidden()
        {
            KedUIUtil.SetActive(_root, true);
            SetCanvas(false);
        }

        public async YarnTask FadeInAsync(float duration, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            SetVisibleImmediate(true);

            if (_group == null)
                return;

            _group.alpha = 0f;

            await Effects
                .FadeAlphaAsync(_group, 0f, 1f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            SetCanvas(true);
        }

        public async YarnTask FadeOutAsync(float duration, CancellationToken token)
        {
            if (token.IsCancellationRequested || _group == null)
                return;

            KedUIUtil.SetActive(_root, true);

            await Effects
                .FadeAlphaAsync(_group, _group.alpha, 0f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            SetCanvas(false);
        }

        public async YarnTask FadeInAsync(float duration, LinePresentationRun run)
        {
            if (!run.IsValid)
                return;

            SetVisibleImmediate(true);

            if (_group == null)
                return;

            _group.alpha = 0f;

            await Effects
                .FadeAlphaAsync(_group, 0f, 1f, duration, run.VisualToken)
                .SuppressCancellationThrow();

            if (!run.IsValid)
                return;

            SetCanvas(true);
        }

        public async YarnTask FadeOutAsync(float duration, LinePresentationRun run)
        {
            if (!run.IsValid || _group == null)
                return;

            await Effects
                .FadeAlphaAsync(_group, _group.alpha, 0f, duration, run.VisualToken)
                .SuppressCancellationThrow();

            if (!run.IsValid)
                return;

            SetCanvas(false);
        }

        #endregion

        private void ApplyLineLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry)
        {
            RectTransform rect = _line.rectTransform;

            rect.anchorMin = entry.lineAnchorMin;
            rect.anchorMax = entry.lineAnchorMax;
            rect.pivot = entry.linePivot;
            rect.anchoredPosition = entry.lineAnchoredPosition;
            rect.sizeDelta = entry.lineSizeDelta;

            _line.alignment = entry.lineAlignment;
            _line.lineSpacing = entry.lineSpacing;
            _line.paragraphSpacing = entry.paragraphSpacing;
            _line.margin = entry.lineMargin;
            _line.overflowMode = entry.lineOverflowMode;
            _line.textWrappingMode = entry.lineTextWrappingMode;
        }

        private void ApplyNameLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry)
        {
            _usesName = entry.useName && _name != null;
            _clearNameWhenHidden = entry.clearNameWhenHidden;

            if (_name == null)
                return;

            SetNameVisible(_usesName);

            if (!_usesName)
                return;

            RectTransform rect = _name.rectTransform;

            rect.anchorMin = entry.nameAnchorMin;
            rect.anchorMax = entry.nameAnchorMax;
            rect.pivot = entry.namePivot;
            rect.anchoredPosition = entry.nameAnchoredPosition;
            rect.sizeDelta = entry.nameSizeDelta;

            _name.alignment = entry.nameAlignment;
            _name.margin = entry.nameMargin;
            _name.overflowMode = entry.nameOverflowMode;
            _name.textWrappingMode = entry.nameTextWrappingMode;

            FitPlateToName(rect);
        }

        private void ApplyFontSizes(DialogueSurfaceLayoutPresetDBSO.Entry entry)
        {
            _line.fontSize = entry.lineFontSize * _textScale;

            if (_name != null)
                _name.fontSize = entry.nameFontSize * _textScale;
        }

        // 이름표 판은 이름 글자와 같은 앵커·자리에 두고 시안의 여백만큼 넓힌다.
        private void FitPlateToName(RectTransform nameRect)
        {
            if (_plate == null)
                return;

            _plate.anchorMin = nameRect.anchorMin;
            _plate.anchorMax = nameRect.anchorMax;
            _plate.pivot = nameRect.pivot;
            _plate.anchoredPosition = nameRect.anchoredPosition;
            _plate.sizeDelta = nameRect.sizeDelta;

            _plate.offsetMin -= PlatePadMin;
            _plate.offsetMax += PlatePadMax;
        }

        private void SetNameVisible(bool visible)
        {
            KedUIUtil.SetActive(_name, visible);
            KedUIUtil.SetActive(_plate, visible);
        }

        private void SetCanvas(bool visible)
        {
            if (_group == null)
                return;

            _group.alpha = visible ? 1f : 0f;
            // 대사창은 입력을 받지 않는다 — 진행 클릭은 아래의 AdvanceArea가 받는다.
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }
    }
}
