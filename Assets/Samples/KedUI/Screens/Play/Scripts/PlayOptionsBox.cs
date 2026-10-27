using System.Threading;
using UnityEngine;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 본체의 선택지 상자 계약(IPresentationOptionsBoxView)을 PlayScreen의 Refs 위에 구현한다.
    /// 항목은 재생 쪽(VNOptionsPresenter · 진행 예제의 ChapterOptionsView)이 ItemContainer에 만든다 —
    /// 선택 가능·불가 조건도 그쪽이 정한다. 여기는 딤과 상자의 보이기·입력만 맡는다(시안 03).
    /// </summary>
    internal sealed class PlayOptionsBox : IPresentationOptionsBoxView
    {
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly RectTransform _itemContainer;

        public PlayOptionsBox(RectTransform root, CanvasGroup group, RectTransform itemContainer)
        {
            _root = root;
            _group = group;
            _itemContainer = itemContainer;
        }

        public RectTransform ItemContainer => _itemContainer;

        public void ResetPresentationTransform()
        {
            if (_root == null)
                return;

            _root.localPosition = Vector3.zero;
            _root.anchoredPosition = Vector2.zero;
        }

        public void SetVisibleImmediate(bool visible)
        {
            KedUIUtil.SetActive(_root, true);
            SetCanvasVisible(visible);
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_group == null)
                return;

            _group.interactable = enabled;
            _group.blocksRaycasts = enabled;
        }

        public void PrepareHidden()
        {
            KedUIUtil.SetActive(_root, true);
            SetCanvasVisible(false);
        }

        public async YarnTask FadeInAsync(float duration, CancellationToken token)
        {
            SetVisibleImmediate(true);
            SetInputEnabled(false);

            if (_group == null)
                return;

            _group.alpha = 0f;

            await Effects
                .FadeAlphaAsync(_group, 0f, 1f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            SetCanvasVisible(true);
            SetInputEnabled(false);
        }

        public bool IsShowing => _group != null && _group.alpha > 0f;

        private void SetCanvasVisible(bool visible)
        {
            if (_group == null)
                return;

            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
