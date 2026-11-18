using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 대사창의 초상 칸 — 자리와 크기를 프레임마다 다시 맞춘다. PlayDialogueBox가 칸을 세울 때 붙인다.
    /// 값은 앱이 넘긴 함수가 낸다(대사창 그림의 왼쪽 아래 귀 기준, 캔버스 px). 값의 집을 재생 중에 고치면 바로 따라가고,
    /// 대사창의 자리가 바뀌어도(화면 크기) 귀에 붙어 있다. 칸의 모양은 찍힌 그림과 같은 비율로 온다 — 찌그러지지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayPortraitCell : MonoBehaviour
    {
        private RectTransform _box;
        private Func<Rect> _readCell;

        public void Bind(RectTransform box, Func<Rect> readCell)
        {
            _box = box;
            _readCell = readCell;

            Fit();
        }

        private void LateUpdate() => Fit();

        private void Fit()
        {
            if (_readCell == null)
                return;

            Rect cell = _readCell();

            // 대사창 그림의 왼쪽 아래 귀(부모 좌표). 그림이 없으면 부모의 피벗에서 잰다.
            Vector2 corner = _box != null
                ? (Vector2)_box.localPosition + _box.rect.min
                : Vector2.zero;

            RectTransform rect = (RectTransform)transform;
            rect.localPosition = corner + cell.position;
            rect.sizeDelta = cell.size;
        }
    }
}
