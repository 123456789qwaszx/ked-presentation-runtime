using System.Threading;
using UnityEngine;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 본체의 선택지 상자 계약(IPresentationOptionsBoxView)을 재생 화면의 선택지 상자(PlayOptionsBox) 위에 구현한다.
    /// 항목은 재생 쪽(VNOptionsPresenter · 진행 예제의 ChapterOptionsView)이 ItemContainer에 만든다 —
    /// 선택 가능·불가 조건도 그쪽이 정한다.
    /// </summary>
    internal sealed class KedOptionsBoxView : IPresentationOptionsBoxView
    {
        private readonly PlayOptionsBox _box;

        public KedOptionsBoxView(PlayOptionsBox box)
        {
            _box = box;
        }

        public RectTransform ItemContainer => _box.ItemContainer;

        public void ResetPresentationTransform() => _box.ResetTransform();

        public void SetVisibleImmediate(bool visible) => _box.SetVisibleImmediate(visible);

        public void SetInputEnabled(bool enabled) => _box.SetInputEnabled(enabled);

        public void PrepareHidden() => _box.PrepareHidden();

        public async YarnTask FadeInAsync(float duration, CancellationToken token)
        {
            _box.SetVisibleImmediate(true);
            _box.SetInputEnabled(false);

            if (_box.Group == null)
                return;

            _box.Group.alpha = 0f;

            await Effects
                .FadeAlphaAsync(_box.Group, 0f, 1f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            _box.SetShown(true);
            _box.SetInputEnabled(false);
        }
    }
}
