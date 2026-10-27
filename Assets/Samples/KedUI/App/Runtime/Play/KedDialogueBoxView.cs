using System.Threading;
using TMPro;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 본체의 대사창 계약(IPresentationDialogueBoxView)을 재생 화면의 대사창(PlayDialogueBox) 위에 구현한다.
    /// 화면 묶음은 재생 런타임을 모르므로, 계약에 잇는 이 어댑터는 앱 쪽에 둔다.
    ///
    /// 소유권(PLAN "대사창 소유권"):
    /// - 줄·이름 글자의 자리·크기·정렬은 연출 DB(DialogueSurfaceLayoutPresetDBSO)가 정한다. 새 UI는 새 박스에 맞춘
    ///   프리셋 DB를 쓴다(빌더가 만든 KedSurfaceLayoutPresetDB). 대본의 surface_layout이 그대로 동작한다.
    /// - 그림(박스·이름표)·글꼴·불투명도·글자 배율은 UI가 정한다. 프리셋의 overrideImage는 쓰지 않는다.
    /// - 보이기·감추기(CanvasGroup)는 본체 컨트롤러가 부른다. UI 숨김(퀵메뉴)은 바깥 층(HudRoot)이 맡아 서로 겹치지 않는다.
    /// </summary>
    internal sealed class KedDialogueBoxView : IPresentationDialogueBoxView
    {
        private readonly PlayDialogueBox _box;

        public KedDialogueBoxView(PlayDialogueBox box)
        {
            _box = box;
        }

        public TMP_Text GetLineText() => _box.Line;
        public TMP_Text GetNameText() => _box.Name;

        public void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry) => _box.ApplyLayout(ToLayout(entry));

        public void ResetPresentationTransform() => _box.ResetTransform();

        public void PrimeText(string text, string characterName, bool hasCharacterName) =>
            _box.PrimeText(text, characterName, hasCharacterName);

        public void SetVisibleImmediate(bool visible) => _box.SetVisibleImmediate(visible);

        public void PrepareHidden() => _box.PrepareHidden();

        public async YarnTask FadeInAsync(float duration, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            _box.SetVisibleImmediate(true);

            if (_box.Group == null)
                return;

            _box.Group.alpha = 0f;

            await Effects
                .FadeAlphaAsync(_box.Group, 0f, 1f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            _box.SetShown(true);
        }

        public async YarnTask FadeOutAsync(float duration, CancellationToken token)
        {
            if (token.IsCancellationRequested || _box.Group == null)
                return;

            KedUIUtil.SetActive(_box.Root, true);

            await Effects
                .FadeAlphaAsync(_box.Group, _box.Group.alpha, 0f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            _box.SetShown(false);
        }

        public async YarnTask FadeInAsync(float duration, LinePresentationRun run)
        {
            if (!run.IsValid)
                return;

            _box.SetVisibleImmediate(true);

            if (_box.Group == null)
                return;

            _box.Group.alpha = 0f;

            await Effects
                .FadeAlphaAsync(_box.Group, 0f, 1f, duration, run.VisualToken)
                .SuppressCancellationThrow();

            if (!run.IsValid)
                return;

            _box.SetShown(true);
        }

        public async YarnTask FadeOutAsync(float duration, LinePresentationRun run)
        {
            if (!run.IsValid || _box.Group == null)
                return;

            await Effects
                .FadeAlphaAsync(_box.Group, _box.Group.alpha, 0f, duration, run.VisualToken)
                .SuppressCancellationThrow();

            if (!run.IsValid)
                return;

            _box.SetShown(false);
        }

        private static PlayDialogueLayout ToLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry) => new()
        {
            lineAnchorMin = entry.lineAnchorMin,
            lineAnchorMax = entry.lineAnchorMax,
            linePivot = entry.linePivot,
            lineAnchoredPosition = entry.lineAnchoredPosition,
            lineSizeDelta = entry.lineSizeDelta,
            lineAlignment = entry.lineAlignment,
            lineSpacing = entry.lineSpacing,
            paragraphSpacing = entry.paragraphSpacing,
            lineMargin = entry.lineMargin,
            lineOverflowMode = entry.lineOverflowMode,
            lineTextWrappingMode = entry.lineTextWrappingMode,
            lineFontSize = entry.lineFontSize,

            useName = entry.useName,
            clearNameWhenHidden = entry.clearNameWhenHidden,
            nameAnchorMin = entry.nameAnchorMin,
            nameAnchorMax = entry.nameAnchorMax,
            namePivot = entry.namePivot,
            nameAnchoredPosition = entry.nameAnchoredPosition,
            nameSizeDelta = entry.nameSizeDelta,
            nameAlignment = entry.nameAlignment,
            nameMargin = entry.nameMargin,
            nameOverflowMode = entry.nameOverflowMode,
            nameTextWrappingMode = entry.nameTextWrappingMode,
            nameFontSize = entry.nameFontSize,
        };
    }
}
